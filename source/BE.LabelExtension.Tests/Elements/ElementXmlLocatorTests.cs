using System.IO;
using System.Xml;
using BE.LabelExtension.Core.Elements;
using Xunit;

namespace BE.LabelExtension.Tests.Elements
{
    public class ElementXmlLocatorTests
    {
        // Synthetic element in the shape of a table XML, invented names only.
        private const string TableXml =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<AxTable xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">\n" +
            "  <Name>BDMDelivery</Name>\n" +
            "  <Label>@BDM1:L3F2A9C15B8047DE1</Label>\n" +
            "  <Fields>\n" +
            "    <AxTableField xmlns=\"\" i:type=\"AxTableFieldString\">\n" +
            "      <Name>DeliveryName</Name>\n" +
            "      <HelpText>Name of the recipient</HelpText>\n" +
            "      <Label>Delivery name</Label>\n" +
            "    </AxTableField>\n" +
            "    <AxTableField xmlns=\"\" i:type=\"AxTableFieldString\">\n" +
            "      <Name>Street</Name>\n" +
            "      <Label>Delivery name</Label>\n" +
            "    </AxTableField>\n" +
            "  </Fields>\n" +
            "</AxTable>\n";

        [Fact]
        public void FindElements_NodeName_ReturnsPathAndLine()
        {
            var location = Assert.Single(ElementXmlLocator.FindElements(new StringReader(TableXml), "Name", "DeliveryName"));

            Assert.Equal("AxTable/Fields/AxTableField/Name", location.Path);
            Assert.Equal(7, location.Line);
            Assert.Equal(8, location.Column);
        }

        [Fact]
        public void FindElements_ValueInSeveralNodes_ReturnsAll()
        {
            var locations = ElementXmlLocator.FindElements(new StringReader(TableXml), "Label", "Delivery name");

            Assert.Equal(2, locations.Count);
            Assert.Equal(9, locations[0].Line);
            Assert.Equal(13, locations[1].Line);
        }

        [Fact]
        public void FindElements_ElementWithChildren_DoesNotMatch()
        {
            Assert.Empty(ElementXmlLocator.FindElements(new StringReader(TableXml), "Fields", ""));
        }

        [Fact]
        public void FindElements_NoMatch_ReturnsEmpty()
        {
            Assert.Empty(ElementXmlLocator.FindElements(new StringReader(TableXml), "Name", "Unknown"));
        }

        [Fact]
        public void FindElements_MalformedXml_Throws()
        {
            Assert.Throws<XmlException>(() => ElementXmlLocator.FindElements(new StringReader("<AxTable><Name>"), "Name", "x"));
        }
    }
}
