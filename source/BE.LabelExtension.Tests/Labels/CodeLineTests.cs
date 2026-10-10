using BE.LabelExtension.Core.Labels;
using Xunit;

namespace BE.LabelExtension.Tests.Labels
{
    /// <summary>FA07 and FA08 in the core logic: what stands at the cursor in a line of code.</summary>
    public class CodeLineTests
    {
        private const string Line = "    info(strFmt(\"%1 %2\", \"@BDM1:BDM110000003\", 'Lieferadresse'));";

        [Theory]
        [InlineData(17, "%1 %2")]
        [InlineData(22, "%1 %2")]
        [InlineData(30, "@BDM1:BDM110000003")]
        [InlineData(26, "@BDM1:BDM110000003")]
        [InlineData(52, "Lieferadresse")]
        public void StringLiteralAt_CursorInLiteral_GivesItsContent(int column, string expected)
        {
            Assert.Equal(expected, CodeLine.StringLiteralAt(Line, column));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(8)]
        [InlineData(23)]
        [InlineData(66)]
        public void StringLiteralAt_CursorOutside_IsNull(int column)
        {
            Assert.Null(CodeLine.StringLiteralAt(Line, column));
        }

        [Fact]
        public void StringLiteralAt_EscapedQuoteAndComment()
        {
            Assert.Equal("say \\\"hi\\\"", CodeLine.StringLiteralAt("x = \"say \\\"hi\\\"\";", 7));
            Assert.Null(CodeLine.StringLiteralAt("x = 1; // \"not code\"", 13));
            Assert.Null(CodeLine.StringLiteralAt("x = \"open", 6));
        }

        [Theory]
        [InlineData(26, "@BDM1:BDM110000003")]
        [InlineData(35, "@BDM1:BDM110000003")]
        [InlineData(44, "@BDM1:BDM110000003")]
        public void LabelIdAt_CursorOnId_GivesIt(int column, string expected)
        {
            Assert.Equal(expected, CodeLine.LabelIdAt(Line, column)?.FullId);
        }

        [Fact]
        public void LabelIdAt_OldFormAndNothing()
        {
            Assert.Equal("@SYS12345", CodeLine.LabelIdAt("x = literalStr(@SYS12345);", 18)?.FullId);
            Assert.Null(CodeLine.LabelIdAt(Line, 10));
        }
    }
}
