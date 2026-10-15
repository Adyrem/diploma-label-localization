using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace BE.LabelExtension.Core.Elements
{
    /// <summary>
    /// Replaces the text of a label property in the XML file of an element, for the extraction
    /// in the designer (FA09, concept: technical decisions, extraction in the Properties window).
    /// </summary>
    /// <remarks>
    /// The node is found by the name the designer shows for it, and the property is replaced
    /// only if it holds exactly the text the designer shows. If the place is not unique, nothing
    /// is changed. Only the text of that one property changes; the rest of the file stays as it
    /// is, character for character, so the file is not reformatted.
    /// </remarks>
    public static class ElementPropertyEditor
    {
        /// <summary>The properties of elements that take a label, in the order they are offered.</summary>
        public static IReadOnlyList<string> LabelProperties { get; } = new[] { "Label", "HelpText", "Caption", "Text", "Description", "DeveloperDocumentation" };

        /// <summary>
        /// Replaces the text of a property of the node with the given name.
        /// </summary>
        /// <param name="xml">Content of the XML file.</param>
        /// <param name="nodeName">The name of the node, as the property Name in the designer.</param>
        /// <param name="property">The property, such as <c>Label</c>.</param>
        /// <param name="expectedValue">The text the property must hold, as the designer shows it.</param>
        /// <param name="newValue">The new text, such as a label ID.</param>
        /// <param name="result">The changed content if the method returns <c>true</c>.</param>
        /// <param name="problem">Why nothing was changed if the method returns <c>false</c>.</param>
        /// <returns>Whether the property was found once and replaced.</returns>
        public static bool TryReplaceValue(string xml, string nodeName, string property, string expectedValue, string newValue, out string result, out string problem)
        {
            result = xml;
            XDocument document;
            try
            {
                using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
                document = XDocument.Load(reader, LoadOptions.SetLineInfo);
            }
            catch (XmlException exception)
            {
                problem = $"the XML file cannot be read ({exception.Message})";
                return false;
            }

            List<XElement> nodes = document.Descendants()
                .Where(e => e.Elements().Any(c => c.Name.LocalName == "Name" && !c.HasElements && c.Value == nodeName))
                .ToList();
            if (nodes.Count == 0)
            {
                problem = $"no node named {nodeName} is in the XML file";
                return false;
            }

            List<XElement> matches = nodes
                .SelectMany(n => n.Elements().Where(c => c.Name.LocalName == property && !c.HasElements && c.Value == expectedValue))
                .ToList();
            if (matches.Count == 0)
            {
                problem = $"{property} of {nodeName} has another text in the XML file than the designer shows; save the element and try again";
                return false;
            }

            if (matches.Count > 1)
            {
                problem = $"{property} of {nodeName} stands {matches.Count} times in the XML file with this text, so the place is not unique";
                return false;
            }

            if (!TryFindContent(xml, matches[0], out int start, out int length))
            {
                problem = $"the place of {property} of {nodeName} in the XML file cannot be determined";
                return false;
            }

            result = xml.Substring(0, start) + Escape(newValue) + xml.Substring(start + length);
            problem = string.Empty;
            return true;
        }

        // The text between start and end tag of the element in the raw content. The line
        // information of the reader counts \r\n, \r and \n as one line break each and points
        // at the name after "<".
        private static bool TryFindContent(string xml, XElement element, out int start, out int length)
        {
            start = length = 0;
            var info = (IXmlLineInfo)element;
            int offset = OffsetOf(xml, info.LineNumber, info.LinePosition);
            if (offset <= 0 || offset >= xml.Length || xml[offset - 1] != '<')
            {
                return false;
            }

            int close = xml.IndexOf('>', offset);
            if (close < 0 || xml[close - 1] == '/')
            {
                return false;
            }

            int end = xml.IndexOf("</", close + 1, StringComparison.Ordinal);
            if (end < 0)
            {
                return false;
            }

            // The raw text must stand for the value read, otherwise something lies in between.
            string raw = xml.Substring(close + 1, end - close - 1);
            try
            {
                if (XElement.Parse("<x>" + raw + "</x>").Value != element.Value)
                {
                    return false;
                }
            }
            catch (XmlException)
            {
                return false;
            }

            start = close + 1;
            length = end - start;
            return true;
        }

        private static int OffsetOf(string text, int line, int position)
        {
            int offset = 0;
            for (int current = 1; current < line; current++)
            {
                int next = text.IndexOfAny(new[] { '\r', '\n' }, offset);
                if (next < 0)
                {
                    return -1;
                }

                offset = text[next] == '\r' && next + 1 < text.Length && text[next + 1] == '\n' ? next + 2 : next + 1;
            }

            return offset + position - 1;
        }

        private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
