using BE.LabelExtension.Core.Labels;
using Xunit;

namespace BE.LabelExtension.Tests.Labels
{
    /// <summary>KA03: the arguments of strFmt fill the placeholders of the inline display.</summary>
    public class FormatCallTests
    {
        [Theory]
        [InlineData("    info(strFmt(\"@BDM1:L1\", custTable.Name, qty));", "custTable.Name|qty")]
        [InlineData("    str s = strFmt(literalStr(\"@BDM1:L1\"), this.name(), \"x, y\");", "this.name()|\"x, y\"")]
        [InlineData("    str s = STRFMT( \"@BDM1:L1\", a[1, 2],", "a[1, 2]")]
        [InlineData("    str s = strFmt(\"@BDM1:L1\");", "")]
        public void ArgumentsFor_LabelAsFirstArgument_GivesTheOthers(string line, string expected)
        {
            int position = line.IndexOf('"');

            Assert.Equal(expected, string.Join("|", FormatCall.ArgumentsFor(line, position)!));
        }

        [Theory]
        [InlineData("    info(strFmt(\"%1 %2\", \"@BDM1:L1\"));")]
        [InlineData("    info(\"@BDM1:L1\");")]
        [InlineData("    myStrFmt(\"@BDM1:L1\", x);")]
        public void ArgumentsFor_LabelNotTheFirstArgumentOfStrFmt_IsNull(string line)
        {
            Assert.Null(FormatCall.ArgumentsFor(line, line.IndexOf("\"@", System.StringComparison.Ordinal)));
        }

        [Theory]
        [InlineData("Customer %1 ordered %2", "Customer {custTable.Name} ordered {qty}")]
        [InlineData("%2 before %1", "{qty} before {custTable.Name}")]
        [InlineData("Missing %3 stays", "Missing %3 stays")]
        [InlineData("No placeholder", "No placeholder")]
        public void ReplacePlaceholders_PutsTheArgumentsInBraces(string text, string expected)
        {
            Assert.Equal(expected, FormatCall.ReplacePlaceholders(text, new[] { "custTable.Name", "qty" }));
        }
    }
}