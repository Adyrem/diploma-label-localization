using System;
using BE.LabelExtension.Core.Usages;
using Xunit;

namespace BE.LabelExtension.Tests.Usages
{
    public class LabelIdOccurrencesTests
    {
        [Fact]
        public void Find_LongerIdWithSamePrefix_IsNotReported()
        {
            Assert.Empty(LabelIdOccurrences.Find("info(\"@SYS12345\");", "@SYS1234"));
        }

        [Fact]
        public void Find_SeveralOccurrences_ReturnsAllInOrder()
        {
            string text = "a = \"@BDM1:L3F2A9C15B8047DE1\"; b = \"@BDM1:L3F2A9C15B8047DE1\";";

            Assert.Equal(new[] { 5, 36 }, LabelIdOccurrences.Find(text, "@BDM1:L3F2A9C15B8047DE1"));
        }

        [Theory]
        [InlineData("@SYS12345")]
        [InlineData("<Label>@SYS12345</Label>")]
        [InlineData("\"@SYS12345\"")]
        [InlineData("x @SYS12345, y")]
        public void Find_IdFollowedByNonIdCharacterOrEnd_IsReported(string text)
        {
            Assert.Single(LabelIdOccurrences.Find(text, "@SYS12345"));
        }

        [Theory]
        [InlineData("@SYS12345a")]
        [InlineData("@SYS12345_")]
        public void Find_IdFollowedByIdCharacter_IsNotReported(string text)
        {
            Assert.Empty(LabelIdOccurrences.Find(text, "@SYS12345"));
        }

        [Fact]
        public void Find_EmptyId_Throws()
        {
            Assert.Throws<ArgumentException>(() => LabelIdOccurrences.Find("text", ""));
        }
    }
}
