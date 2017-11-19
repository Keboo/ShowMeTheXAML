using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ShowMeTheXAML
{
    public class IndentedXamlFormatter : IXamlFormatter
    {
        public string Indent { get; set; } = "    ";
        public bool NewLineOnAttributes { get; set; }
        public bool FormatTextElements { get; set; } = true;

        public string FormatXaml(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml)) return "";
            try
            {
                var document = XDocument.Parse(xaml);

                if (FormatTextElements)
                {
                    foreach (var textElement in document.DescendantNodes().OfType<XText>())
                    {
                        textElement.Value = Environment.NewLine +
                                            string.Join("", Enumerable.Repeat(Indent, CountParents(textElement))) +
                                            textElement.Value.Trim() + Environment.NewLine;
                    }
                }
                var sb = new StringBuilder();

                using (var writer = XmlWriter.Create(sb, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = Indent,
                    NewLineOnAttributes = NewLineOnAttributes,
                    OmitXmlDeclaration = true
                }))
                {
                    document.WriteTo(writer);
                }
                string rv = sb.ToString();
                return rv;

            }
            catch (Exception)
            {
                return "";
            }

            int CountParents(XObject @object)
            {
                int count = 0;
                for (XElement parent = @object.Parent; parent != null; parent = parent.Parent)
                {
                    count++;
                }
                return count;
            }
        }
    }
}