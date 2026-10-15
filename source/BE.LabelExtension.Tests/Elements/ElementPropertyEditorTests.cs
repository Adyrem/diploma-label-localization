using BE.LabelExtension.Core.Elements;
using Xunit;

namespace BE.LabelExtension.Tests.Elements
{
    /// <summary>FA09 in the core logic: the extraction in the designer replaces one property in the XML file of the element.</summary>
    public class ElementPropertyEditorTests
    {
        private const string Table =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
            "<AxTable xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n" +
            "\t<Name>BDMDelivery</Name>\r\n" +
            "\t<Label>Lieferungen</Label>\r\n" +
            "\t<Fields>\r\n" +
            "\t\t<AxTableField xmlns=\"\" i:type=\"AxTableFieldString\">\r\n" +
            "\t\t\t<Name>DeliveryAddress</Name>\r\n" +
            "\t\t\t<HelpText>Address &amp; contact</HelpText>\r\n" +
            "\t\t\t<Label>Lieferadresse</Label>\r\n" +
            "\t\t</AxTableField>\r\n" +
            "\t\t<AxTableField xmlns=\"\" i:type=\"AxTableFieldString\">\r\n" +
            "\t\t\t<Name>InvoiceAddress</Name>\r\n" +
            "\t\t\t<Label>Lieferadresse</Label>\r\n" +
            "\t\t</AxTableField>\r\n" +
            "\t</Fields>\r\n" +
            "</AxTable>\r\n";

        [Fact]
        public void TryReplaceValue_PropertyOfAField_ChangesOnlyItsText()
        {
            Assert.True(ElementPropertyEditor.TryReplaceValue(Table, "DeliveryAddress", "Label", "Lieferadresse", "@BDM1:L1", out string result, out string problem));

            Assert.Equal(string.Empty, problem);
            Assert.Equal(Table.Replace("\t\t\t<Label>Lieferadresse</Label>\r\n\t\t</AxTableField>\r\n\t\t<AxTableField", "\t\t\t<Label>@BDM1:L1</Label>\r\n\t\t</AxTableField>\r\n\t\t<AxTableField"), result);
            Assert.Contains("<Name>InvoiceAddress</Name>\r\n\t\t\t<Label>Lieferadresse</Label>", result);
        }

        [Fact]
        public void TryReplaceValue_EscapedText_IsComparedAsTheDesignerShowsIt()
        {
            Assert.True(ElementPropertyEditor.TryReplaceValue(Table, "DeliveryAddress", "HelpText", "Address & contact", "@BDM1:L2", out string result, out _));

            Assert.Contains("\t\t\t<HelpText>@BDM1:L2</HelpText>\r\n", result);
        }

        [Fact]
        public void TryReplaceValue_TheElementItself_IsFoundByItsName()
        {
            Assert.True(ElementPropertyEditor.TryReplaceValue(Table, "BDMDelivery", "Label", "Lieferungen", "@BDM1:L3", out string result, out _));

            Assert.Contains("\t<Name>BDMDelivery</Name>\r\n\t<Label>@BDM1:L3</Label>\r\n", result);
        }

        [Theory]
        [InlineData("DeliveryAddress", "Label", "Rechnungsadresse", "Label of DeliveryAddress has another text in the XML file than the designer shows; save the element and try again")]
        [InlineData("Unknown", "Label", "Lieferadresse", "no node named Unknown is in the XML file")]
        public void TryReplaceValue_NoSuchPlace_ChangesNothing(string node, string property, string value, string expected)
        {
            Assert.False(ElementPropertyEditor.TryReplaceValue(Table, node, property, value, "@BDM1:L1", out string result, out string problem));

            Assert.Equal(expected, problem);
            Assert.Equal(Table, result);
        }

        [Fact]
        public void TryReplaceValue_PlaceNotUnique_ChangesNothing()
        {
            string twice = Table.Replace("<Name>InvoiceAddress</Name>", "<Name>DeliveryAddress</Name>");

            Assert.False(ElementPropertyEditor.TryReplaceValue(twice, "DeliveryAddress", "Label", "Lieferadresse", "@BDM1:L1", out string result, out string problem));

            Assert.Equal("Label of DeliveryAddress stands 2 times in the XML file with this text, so the place is not unique", problem);
            Assert.Equal(twice, result);
        }
    }
}
