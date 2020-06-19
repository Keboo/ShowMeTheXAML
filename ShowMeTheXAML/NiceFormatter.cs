using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ShowMeTheXAML.Helpers;

namespace ShowMeTheXAML
{
	public class NiceFormatter : IXamlFormatter
	{
		public List<string> NamespacesToRemove { get; } = new List<string>
		{
			"http://schemas.microsoft.com/winfx/2006/xaml/presentation",
			"http://schemas.microsoft.com/winfx/2006/xaml"
		};

		public string FormatXaml(string xaml)
		{
			if (string.IsNullOrWhiteSpace(xaml)) return string.Empty;
			try
			{
				var element = XElement.Parse(xaml);

				// fix intendation & remove XamlDisplay root
				var buffer = new StringBuilder();
				using (var custom = new NiceXmlWriter(buffer))
				{
					element.WriteTo(custom);
				}

				var result = buffer.ToString();
				result = RemoveXmlns(result, NamespacesToRemove);

				return result;
			}
			catch (Exception)
			{
				return xaml;
			}
		}

		private static string RemoveXmlns(string xaml, IEnumerable<string> namespaces)
		{
			return Regex.Replace(xaml, @"\s+xmlns(:(?<prefix>\w+))?=""(?<ns>[^""]+)""", m =>
			{
				return namespaces?.Contains(m.Groups["ns"].Value) == true
					? null
					: m.Value;
			});
		}

		internal class XmlWriterProxy : XmlWriter
		{
			protected readonly StringBuilder _buffer;
			protected readonly TextWriter _textWriter;
			private readonly XmlWriter _writer;
			private bool? _noopInnerCallOverride;

			public XmlWriterProxy(StringBuilder buffer, XmlWriterSettings settings)
			{
				_buffer = buffer;
				_textWriter = new StringWriter(buffer);
				_writer = XmlWriter.Create(_textWriter, settings);
			}
			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);
				GetProxy()?.Flush();
			}

			protected virtual bool NoopInnerCall => false;
			protected IDisposable OverrideNoopInnerCall(bool value)
			{
				return Disposable.Create(() => _noopInnerCallOverride = value, () => _noopInnerCallOverride = null);
			}
			private XmlWriter GetProxy() => _noopInnerCallOverride ?? NoopInnerCall ? default : _writer;

			#region XmlWriter implementation
			public override XmlWriterSettings Settings => _writer.Settings;
			public override WriteState WriteState => _writer.WriteState;

			public override string LookupPrefix(string ns) => _writer.LookupPrefix(ns);

			public override void Flush() => GetProxy()?.Flush();
			public override void WriteBase64(byte[] buffer, int index, int count) => GetProxy()?.WriteBase64(buffer, index, count);
			public override void WriteCData(string text) => GetProxy()?.WriteCData(text);
			public override void WriteCharEntity(char ch) => GetProxy()?.WriteCharEntity(ch);
			public override void WriteChars(char[] buffer, int index, int count) => GetProxy()?.WriteChars(buffer, index, count);
			public override void WriteComment(string text) => GetProxy()?.WriteComment(text);
			public override void WriteDocType(string name, string pubid, string sysid, string subset) => GetProxy()?.WriteDocType(name, pubid, sysid, subset);
			public override void WriteEndAttribute() => GetProxy()?.WriteEndAttribute();
			public override void WriteEndDocument() => GetProxy()?.WriteEndDocument();
			public override void WriteEndElement() => GetProxy()?.WriteEndElement();
			public override void WriteEntityRef(string name) => GetProxy()?.WriteEntityRef(name);
			public override void WriteFullEndElement() => GetProxy()?.WriteFullEndElement();
			public override void WriteProcessingInstruction(string name, string text) => GetProxy()?.WriteProcessingInstruction(name, text);
			public override void WriteRaw(char[] buffer, int index, int count) => GetProxy()?.WriteRaw(buffer, index, count);
			public override void WriteRaw(string data) => GetProxy()?.WriteRaw(data);
			public override void WriteStartAttribute(string prefix, string localName, string ns) => GetProxy()?.WriteStartAttribute(prefix, localName, ns);
			public override void WriteStartDocument() => GetProxy()?.WriteStartDocument();
			public override void WriteStartDocument(bool standalone) => GetProxy()?.WriteStartDocument(standalone);
			public override void WriteStartElement(string prefix, string localName, string ns) => GetProxy()?.WriteStartElement(prefix, localName, ns);
			public override void WriteString(string text) => GetProxy()?.WriteString(text);
			public override void WriteSurrogateCharEntity(char lowChar, char highChar) => GetProxy()?.WriteSurrogateCharEntity(lowChar, highChar);
			public override void WriteWhitespace(string ws) => GetProxy()?.WriteWhitespace(ws);

			#endregion
		}
		internal class NiceXmlWriter : XmlWriterProxy
		{
			private Stack<(string Prefix, string LocalName, string NS)> _elements = new Stack<(string Prefix, string LocalName, string NS)>();
			private bool _wroteFirstElementAttribute = false;
			private int _currentIndentLevel = -1;
			private int _currentElementAttributeIndentLength = 0;

			// align attributes to the first one, remove XamlDisplay element, but leave nested elements intact
			public NiceXmlWriter(StringBuilder buffer) : base(buffer, GetSettings())
			{
			}
			private static XmlWriterSettings GetSettings()
			{
				return new XmlWriterSettings
				{
					Indent = true,
					IndentChars = "   ",
					NamespaceHandling = NamespaceHandling.OmitDuplicates,
					NewLineOnAttributes = false,
					OmitXmlDeclaration = true,
					NewLineHandling = NewLineHandling.Replace,
					NewLineChars = Environment.NewLine,
					ConformanceLevel = ConformanceLevel.Fragment,
				};
			}

			protected override bool NoopInnerCall => _elements.Any() && _elements.Peek().LocalName == nameof(XamlDisplay);

			public override void WriteStartAttribute(string prefix, string localName, string ns)
			{
				if (NoopInnerCall) return;
				if (_wroteFirstElementAttribute)
				{
					// flush xml buffer, so we can write with _textWrite at appropriate location
					Flush();

					// align to first attribute
					_textWriter.Write(Environment.NewLine);
					if (Settings.Indent && !string.IsNullOrEmpty(Settings.IndentChars))
					{
						for (int i = 0; Settings.Indent && i < _currentIndentLevel; i++)
						{
							_textWriter.Write(Settings.IndentChars);
						}
					}
					_textWriter.Write(new string(' ', _currentElementAttributeIndentLength));
				}

				base.WriteStartAttribute(prefix, localName, ns);
			}
			public override void WriteEndAttribute()
			{
				if (NoopInnerCall) return;

				base.WriteEndAttribute();

				_wroteFirstElementAttribute = true;
			}
			public override void WriteStartElement(string prefix, string localName, string ns)
			{
				_elements.Push((prefix, localName, ns));
				if (NoopInnerCall) return;

				base.WriteStartElement(prefix, localName, ns);

				_currentIndentLevel++;
				_wroteFirstElementAttribute = false;
				_currentElementAttributeIndentLength =
					// length: (${prefix}:)?localName\s
					(string.IsNullOrEmpty(prefix) ? 0 : prefix.Length + 1) + localName.Length + 1;
			}
			public override void WriteEndElement()
			{
				base.WriteEndElement();
				_elements.Pop();

				_currentIndentLevel--;
			}
			public override void WriteFullEndElement()
			{
				base.WriteFullEndElement();
				_elements.Pop();

				_currentIndentLevel--;
			}
			public override void WriteComment(string text)
			{
				using (OverrideNoopInnerCall(false))
				{
					base.WriteComment(text);
				}
			}
		}
	}
}
