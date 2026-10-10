using System;
using BE.LabelExtension.Core.Labels;
using Xunit;

namespace BE.LabelExtension.Tests.Labels
{
    /// <summary>TC02: label IDs of both forms are recognized; the new form is split, the old one stays whole.</summary>
    public class LabelIdTests
    {
        [Fact]
        public void TryParse_NewForm_IsSplitIntoLabelFileAndLabel()
        {
            Assert.True(LabelId.TryParse("@BDM1:L3F2A9C15B8047DE1", out LabelId id));

            Assert.Equal("@BDM1:L3F2A9C15B8047DE1", id.FullId);
            Assert.Equal("BDM1", id.File);
            Assert.Equal("L3F2A9C15B8047DE1", id.Key);
            Assert.False(id.IsLegacy);
        }

        [Fact]
        public void TryParse_OldForm_StaysWhole()
        {
            Assert.True(LabelId.TryParse("@SYS12345", out LabelId id));

            Assert.Equal("@SYS12345", id.FullId);
            Assert.Null(id.File);
            Assert.Equal("@SYS12345", id.Key);
            Assert.True(id.IsLegacy);
        }

        [Fact]
        public void TryParse_LabelFileWithUnderscore_IsAccepted()
        {
            Assert.True(LabelId.TryParse("@FieldDescriptions_Demo:BDMCustomer_Name", out LabelId id));
            Assert.Equal("FieldDescriptions_Demo", id.File);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("@")]
        [InlineData("SYS12345")]
        [InlineData("@SYS")]
        [InlineData("@12345")]
        [InlineData("@BDM1:")]
        [InlineData("@:L3F2")]
        [InlineData("@BDM1:L3F2 X")]
        [InlineData(" @SYS12345")]
        [InlineData("@SYS12345 ")]
        [InlineData("\"@SYS12345\"")]
        [InlineData("Lieferadresse")]
        [InlineData("@SYS12345\n")]
        [InlineData("@SYS12A")]
        [InlineData("@BDM1:L3:F2")]
        [InlineData("@BDÄ:L3F2")]
        [InlineData("@@SYS12345")]
        public void TryParse_Invalid_ReturnsFalseWithoutException(string? text)
        {
            Assert.False(LabelId.TryParse(text, out _));
        }

        [Fact]
        public void Parse_Invalid_Throws()
        {
            Assert.Throws<FormatException>(() => LabelId.Parse("Lieferadresse"));
        }

        [Fact]
        public void FindAll_FindsBothFormsWithPosition()
        {
            string line = "info(strFmt(\"@BDM1:L3F2A9C15B8047DE1\", \"@SYS12345\"));";

            var found = LabelId.FindAll(line);

            Assert.Equal(2, found.Count);
            Assert.Equal("@BDM1:L3F2A9C15B8047DE1", found[0].Id.FullId);
            Assert.Equal(13, found[0].Index);
            Assert.Equal("@SYS12345", found[1].Id.FullId);
            Assert.Equal(line.IndexOf("@SYS12345", StringComparison.Ordinal), found[1].Index);
            Assert.Equal(9, found[1].Length);
        }

        [Theory]
        [InlineData("mail@SYS12345")]
        [InlineData("@SYS12345abc")]
        [InlineData("@SYS12345_x")]
        [InlineData("@BDM1:")]
        public void FindAll_NoCompleteId_FindsNothing(string text)
        {
            Assert.Empty(LabelId.FindAll(text));
        }

        [Fact]
        public void TryCreate_KeyOfLabelFile_GetsTheLabelFilePart()
        {
            Assert.True(LabelId.TryCreate("BDM1", "BDM110000003", out LabelId id));
            Assert.Equal("@BDM1:BDM110000003", id.FullId);
        }

        [Fact]
        public void TryCreate_OldKey_StandsForItself()
        {
            Assert.True(LabelId.TryCreate("DMO", "@DMO1001", out LabelId id));
            Assert.Equal("@DMO1001", id.FullId);
            Assert.True(id.IsLegacy);
        }

        [Theory]
        [InlineData("BDM 1", "BDM110000003")]
        [InlineData("BDM1", "BDM1 10000003")]
        [InlineData("", "BDM110000003")]
        [InlineData("BDM1", "")]
        public void TryCreate_InvalidPart_ReturnsFalse(string prefix, string key)
        {
            Assert.False(LabelId.TryCreate(prefix, key, out _));
        }

        [Fact]
        public void KeyEquals_ComparesTheLabelPartWithoutCuttingItOut()
        {
            LabelId id = LabelId.Parse("@BDM1:L3F2A9C15B8047DE1");

            Assert.True(id.KeyEquals("L3F2A9C15B8047DE1", StringComparison.Ordinal));
            Assert.True(id.KeyEquals("l3f2a9c15b8047de1", StringComparison.OrdinalIgnoreCase));
            Assert.False(id.KeyEquals("l3f2a9c15b8047de1", StringComparison.Ordinal));
            Assert.False(id.KeyEquals("L3F2A9C15B8047DE", StringComparison.Ordinal));
            Assert.True(LabelId.Parse("@SYS12345").KeyEquals("@SYS12345", StringComparison.Ordinal));
            Assert.False(default(LabelId).KeyEquals("x", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("ABC_Extension", "ABC")]
        [InlineData("ABC_extension", "ABC")]
        [InlineData("ABC", "ABC")]
        [InlineData("_Extension", "_Extension")]
        [InlineData("FieldDescriptions_Demo", "FieldDescriptions_Demo")]
        public void GetIdPrefix_ExtensionFile_UsesThePartBefore(string labelFile, string expected)
        {
            Assert.Equal(expected, LabelFile.GetIdPrefix(labelFile));
        }

        /// <summary>
        /// TC02 for the label recognition: the X++ editor classifies the token with the opening
        /// quote of the string literal, as the feasibility study observed.
        /// </summary>
        [Theory]
        [InlineData("\"@BDM1:L3F2A9C15B8047DE1", "@BDM1:L3F2A9C15B8047DE1")]
        [InlineData("\"@BDM1:L3F2A9C15B8047DE1\"", "@BDM1:L3F2A9C15B8047DE1")]
        [InlineData("'@SYS12345'", "@SYS12345")]
        [InlineData("  @SYS12345 ", "@SYS12345")]
        [InlineData("@SYS12345", "@SYS12345")]
        [InlineData("(@SYS12345)", "@SYS12345")]
        public void TryParseToken_ClassifiedToken_GivesTheId(string token, string expected)
        {
            Assert.True(LabelId.TryParseToken(token, out LabelId id));
            Assert.Equal(expected, id.FullId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("\"")]
        [InlineData("\"Lieferadresse\"")]
        [InlineData("\"@SYS12345 @SYS67890\"")]
        public void TryParseToken_NoSingleId_IsFalse(string? token)
        {
            Assert.False(LabelId.TryParseToken(token, out _));
        }

        [Fact]
        public void Equality_IsOrdinalOnTheFullId()
        {
            Assert.Equal(LabelId.Parse("@SYS12345"), LabelId.Parse("@SYS12345"));
            Assert.NotEqual(LabelId.Parse("@SYS12345"), LabelId.Parse("@sys12345"));
        }
    }
}
