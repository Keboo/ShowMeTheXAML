using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

[assembly:InternalsVisibleTo("ShowMeTheXAML.MSBuild.Tests")]

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

        public string GeneratedFileName { get; set; }

        private bool _success;
        public override bool Execute()
        {
            _success = true;

            ITaskItem generated = BuildGeneratedFile(FromPageMarkup());

            GeneratedCodeFiles = new[] { generated };

            return _success;
        }

        private bool _cancelRequested;

        public void Cancel()
        {
            _cancelRequested = true;
        }

        private IEnumerable<Xaml> FromPageMarkup()
        {
            Dictionary<string, DisplayerLocation> seenDisplayers = new Dictionary<string, DisplayerLocation>();

            foreach (ITaskItem item in PageMarkup ?? Array.Empty<ITaskItem>())
            {
                if (_cancelRequested) yield break;

                string fullPath = item?.GetMetadata("FullPath");
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                {
                    XDocument document;
                    try
                    {
                        document = XDocument.Load(fullPath, LoadOptions.SetLineInfo);
                    }
                    catch (Exception)
                    {
                        //Likely a XAML parse exception, will skip showing an error and just let the XAML compiler deal with any issues.
                        continue;
                    }

                    if (_cancelRequested) yield break;

                    foreach (DisplayerLocation location in ParseXamlFile(document, fullPath))
                    {
                        if (seenDisplayers.TryGetValue(location.UniqueKey, out DisplayerLocation duplicateLocation))
                        {
                            //NB: This will only show the first match, not all matches. Eh, something to improve later.
                            LogDuplicateKeyError(location, duplicateLocation);
                        }
                        else
                        {
                            seenDisplayers[location.UniqueKey] = location;
                        }
                        yield return new Xaml(location.UniqueKey, location.XamlData);
                    }
                }

            }

            void LogDuplicateKeyError(DisplayerLocation location1, DisplayerLocation location2)
            {
                LogError("Duplicate key specified",
                    $"Duplicate key specified on more than one XamlDisplay element. '{location1.File}' line {location1.Line}, position {location1.Column} and '{location2.File}' line {location2.Line}, position {location2.Column}",
                    location1.File, location1.Line, location1.Column);
            }
        }

        internal IEnumerable<DisplayerLocation> ParseXamlFile(XDocument xamlFile, string fileLocationReference)
        {
            var xamlDisplayXName =
#if __UNO__
                XName.Get("XamlDisplay", "using:ShowMeTheXAML");
#else
                XName.Get("XamlDisplay", "clr-namespace:ShowMeTheXAML;assembly=ShowMeTheXAML");
#endif

            foreach (XElement displayer in xamlFile.Descendants(xamlDisplayXName))
            {
                if (_cancelRequested) yield break;
                DisplayerLocation location = new DisplayerLocation(fileLocationReference, displayer);
                if (string.IsNullOrWhiteSpace(location.UniqueKey))
                {
                    LogNoKeyError(location);
                    continue;
                }

                location.XamlData = GetXamlString(displayer);
                yield return location;
            }

            string GetXamlString(XElement displayer)
            {
                var sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb, new XmlWriterSettings
                {
                    ConformanceLevel = ConformanceLevel.Fragment,
                    NamespaceHandling = NamespaceHandling.OmitDuplicates,
                    OmitXmlDeclaration = true,
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    IndentChars = "  ",
                    NewLineHandling = NewLineHandling.None
                }))
                {
                    displayer.WriteTo(writer);
                }

                //Escape quotes for storage as a C# literal string
                string xaml = sb.ToString().Replace("\"", "\"\"");
                return xaml;
            }

            void LogNoKeyError(DisplayerLocation location)
            {
                LogError("No key specified",
                    $"No key was specified on XamlDisplay element. A unique key is required. Line {location.Line}, position {location.Column}.",
                    location.File, location.Line, location.Column);
            }
        }
        
        private void LogError(string subcategory, string message, string file, int line, int column)
        {
            _success = false;
            //TODO: Error code
            //TODO: help keyword
            BuildEngine.LogErrorEvent(new BuildErrorEventArgs(subcategory, "", file, line, column, 0, 0,
                message, "", nameof(BuildXamlDictionaryTask)));
        }

        internal class DisplayerLocation
        {
            public string File { get; }
            public int Line { get; }
            public int Column { get; }
            public string UniqueKey { get; set; }
            public string XamlData { get; set; }
            public DisplayerLocation(string file, XElement displayer)
            {
                File = file ?? throw new ArgumentNullException(nameof(file));
                if (displayer == null) throw new ArgumentNullException(nameof(displayer));

                UniqueKey = displayer.Attribute("UniqueKey")?.Value;
                IXmlLineInfo lineInfo = displayer;
                if (lineInfo.HasLineInfo())
                {
                    Line = lineInfo.LineNumber;
                    Column = lineInfo.LinePosition;
                }
            }
        }

        private ITaskItem BuildGeneratedFile(IEnumerable<Xaml> pairs)
        {
            string generatedFileName = !string.IsNullOrWhiteSpace(GeneratedFileName)
                ? GeneratedFileName
                : "ShowMeTheXaml_XamlDictionary.g.cs";
            string generatedFilePath = Path.Combine(OutputPath, generatedFileName);
            File.WriteAllText(generatedFilePath, $@"
using System.Collections.Generic;

namespace ShowMeTheXAML
{{
    public static class XamlDictionary
    {{
        static XamlDictionary()
        {{
            {string.Join(Environment.NewLine, pairs.Select(p => $"XamlResolver.Set(\"{p.Key}\", @\"{p.Data}\");"))}
        }}
    }}
}}");
            return new TaskItem(generatedFilePath);
        }

        private class Xaml
        {
            public Xaml(string key, string xaml)
            {
                Key = key;
                Data = xaml;
            }

            public string Key { get; }
            public string Data { get; }
        }
    }
}
