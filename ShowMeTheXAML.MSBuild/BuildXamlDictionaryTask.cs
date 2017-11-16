using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ShowMeTheXAML.MSBuild
{
    public class BuildXamlDictionaryTask : Task, ICancelableTask
    {
        /// <summary>Gets or sets the name of the application definition XAML file.</summary>
        /// <returns>The name of the application definition XAML file.</returns>
        public ITaskItem[] ApplicationMarkup { get; set; }

        /// <summary>Gets or sets a list of XAML files to process.</summary>
        /// <returns>A list of XAML files to process.</returns>
        public ITaskItem[] PageMarkup { get; set; }

        private ITaskItem[] _generatedCodeFiles;
        /// <summary>Gets or sets the list of generated managed code files.</summary>
        /// <returns>The list of generated managed code files.</returns>
        [Output]
        public ITaskItem[] GeneratedCodeFiles
        {
            get => _generatedCodeFiles ?? new ITaskItem[0];
            set => _generatedCodeFiles = value;
        }

        /// <summary>Gets or sets the location of generated code files.</summary>
        /// <returns>The location of generated code files.</returns>
        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            bool success = true;

            ITaskItem generated = BuildGeneratedFile(ParseXamlFiles());

            GeneratedCodeFiles = new[] { generated };

            return success;
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<(string key, string xaml)> ParseXamlFiles()
        {
            if (PageMarkup?.Any() == true)
            {
                foreach (ITaskItem item in PageMarkup)
                {
                    string fullPath = item.GetMetadata("FullPath");
                    if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                    {
                        string text = File.ReadAllText(item.ItemSpec);
                        var document = XDocument.Parse(text);
                        foreach (var displayer in document.Descendants(XName.Get("XamlDisplay",
                            "clr-namespace:ShowMeTheXAML;assembly=ShowMeTheXAML")))
                        {
                            string key = displayer.Attribute("Key")?.Value;

                            yield return (key, GetXamlString(displayer));
                        }
                    }
                }
            }

            string GetXamlString(XElement displayer)
            {
                foreach (var element in displayer.DescendantsAndSelf())
                {
                    element.Name = XName.Get(element.Name.LocalName, "");
                }
                var sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb, new XmlWriterSettings
                {
                    ConformanceLevel = ConformanceLevel.Fragment,
                    NamespaceHandling = NamespaceHandling.OmitDuplicates,
                    OmitXmlDeclaration = true,
                    Encoding = Encoding.UTF8
                }))
                {

                    displayer.FirstNode.WriteTo(writer);
                }

                string xaml = sb.ToString().Replace("\"", "\"\"");
                return xaml;
            }
        }

        private ITaskItem BuildGeneratedFile(IEnumerable<(string key, string xaml)> pairs)
        {
            string generatedFilePath = Path.Combine(OutputPath, "Generated.g.cs");
            File.WriteAllText(generatedFilePath, $@"
using System.Collections.Generic;

namespace ShowMeTheXAML
{{
    public static class XamlDictionary
    {{
        static XamlDictionary()
        {{
            {string.Join(Environment.NewLine, pairs.Select(p => $"XamlResolver.Set(\"{p.key}\", @\"{p.xaml}\");"))}
        }}
    }}
}}");
            return new TaskItem(generatedFilePath);
        }
    }
}
