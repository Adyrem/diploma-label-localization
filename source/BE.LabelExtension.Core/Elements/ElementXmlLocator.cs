using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace BE.LabelExtension.Core.Elements
{
    /// <summary>
    /// Finds a place in the XML file of an element, for example the node selected in the
    /// designer by its name, or a property by its value (FA09, jump to a reference in FA04).
    /// </summary>
    public static class ElementXmlLocator
    {
        /// <summary>
        /// Finds all elements with the given name whose text equals the given value.
        /// Elements with child elements never match.
        /// </summary>
        /// <param name="xml">Content of the XML file.</param>
        /// <param name="elementName">Local name of the XML element, for example <c>Name</c> or <c>Label</c>.</param>
        /// <param name="value">Exact text the element must contain.</param>
        /// <returns>The matching places in document order.</returns>
        /// <exception cref="XmlException">The XML is not well-formed.</exception>
        public static IReadOnlyList<XmlNodeLocation> FindElements(TextReader xml, string elementName, string value)
        {
            if (xml == null)
            {
                throw new ArgumentNullException(nameof(xml));
            }

            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };
            using var reader = XmlReader.Create(xml, settings);
            XDocument document = XDocument.Load(reader, LoadOptions.SetLineInfo);

            return document
                .Descendants()
                .Where(e => e.Name.LocalName == elementName && !e.HasElements && e.Value == value)
                .Select(e => new XmlNodeLocation(PathOf(e), ((IXmlLineInfo)e).LineNumber, ((IXmlLineInfo)e).LinePosition))
                .ToList();
        }

        private static string PathOf(XElement element)
            => string.Join("/", element.AncestorsAndSelf().Reverse().Select(e => e.Name.LocalName));
    }
}
