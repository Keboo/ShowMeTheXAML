using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ShowMeTheXAML
{
    public class XamlFormatter : IXamlFormatter
    {
        public string Indent { get; set; } = "    ";
        public bool NewLineOnAttributes { get; set; }
        public bool FormatTextElements { get; set; } = true;
        public bool IncludeIgnoredElements { get; set; }
        public bool FixupIndentation { get; set; } = true;

        public bool RemoveEmptyLines { get; set; } = true;

        public string FormatXaml(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml)) return "";
            const string RemoveName = "XAML_FORMATTER_REMOVE_FROM_DISPLAY";

            try
            {
                var document = XDocument.Parse(xaml);

                if (!IncludeIgnoredElements)
                {
                    foreach (var node in document.DescendantNodes().OfType<XElement>().ToList())
                    {
                        if (node.Name.LocalName.Contains(".") && node.Parent?.Name == RemoveName)
                        {
                            node.Remove();
                            continue;
                        }
                        //TODO Check namespace
                        var ignoreAttribute = node.Attributes().FirstOrDefault(a => a.Name.LocalName == "XamlDisplay.Ignore");
                        switch (ignoreAttribute?.Value)
                        {
                            case nameof(Scope.This):
                                node.Name = RemoveName;
                                node.RemoveAttributes();
                                break;
                            case nameof(Scope.ThisAndChildren):
                                node.Remove();
                                break;
                        }
                        //node.Attribute(XName.Get(XamlDisplay.IgnoreProperty.Name));
                    }
                }

                if (FormatTextElements)
                {
                    foreach (var textElement in document.DescendantNodes().OfType<XText>())
                    {
                        int indentLevel = CountParents(textElement);
                        textElement.Value = Environment.NewLine +
                                            string.Join("", Enumerable.Repeat(Indent, indentLevel)) +
                                            textElement.Value.Trim() + Environment.NewLine
                            + string.Join("", Enumerable.Repeat(Indent, Math.Max(0, indentLevel - 1)));
                    }
                }
                var sb = new StringBuilder();

                using (var writer = XmlWriter.Create(sb, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = Indent,
                    NewLineOnAttributes = NewLineOnAttributes,
                    OmitXmlDeclaration = true,
                    NewLineHandling = NewLineHandling.Replace,
                    NewLineChars = Environment.NewLine,
                }))
                {
                    document.WriteTo(writer);
                }
                string rv = sb.ToString();

                if (!IncludeIgnoredElements)
                {
                    //TODO: save regex
                    rv = Regex.Replace(rv, $@"</?\s*{RemoveName}\s*>", "");
                }

                if (FixupIndentation)
                {
                    //TODO: Save regex
                    int smallestIndent = Regex.Matches(rv, @"(?<=^|\r?\n)[\s-[\r\n]]*(?=[^\s])", RegexOptions.Multiline).Cast<Match>().Select(m => m.Length).Min();
                    if (smallestIndent > 0)
                    {
                        rv = Regex.Replace(rv, $@"(?<=^|\r?\n)[\s-[\r\n]]{{{smallestIndent}}}", "", RegexOptions.Multiline);
                    }
                }

                if (RemoveEmptyLines)
                {
                    rv = Regex.Replace(rv, @"^\s*\n+", "", RegexOptions.Multiline);
                    rv = rv.Trim();
                }

                return rv;

            }
            catch (Exception ex)
            {
                return xaml;
            }

            int CountParents(XObject @object)
            {
                int count = 0;
                for (XElement parent = @object.Parent; parent != null; parent = parent.Parent)
                {
                    if (parent.Name.LocalName != RemoveName)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
    }
}