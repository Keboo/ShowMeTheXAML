using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Xml;
using System.Xml.Linq;

namespace MDIX.Test
{
    public class FormatXmlValueConverter : IValueConverter
    {
        public string Indent { get; set; } = "    ";
        public bool NewLineOnAttributes { get; set; } 

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string xml = value?.ToString();
            if (string.IsNullOrWhiteSpace(xml)) return "";
            try
            {
                var document = XDocument.Parse(xml);

                foreach (var textElement in document.DescendantNodes().OfType<XText>())
                {
                    textElement.Value = Environment.NewLine + string.Join("", Enumerable.Repeat(Indent, CountParents(textElement))) +
                                        textElement.Value.Trim() + Environment.NewLine;
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
                return Binding.DoNothing;
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}