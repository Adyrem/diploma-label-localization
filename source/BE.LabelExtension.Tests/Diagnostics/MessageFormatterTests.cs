using System;
using BE.LabelExtension.Core.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Diagnostics
{
    public class MessageFormatterTests
    {
        private static readonly DateTime Timestamp = new(2026, 10, 9, 14, 3, 12);

        [Theory]
        [InlineData(MessageSeverity.Error, "14:03:12  Error    Loading failed\r\n")]
        [InlineData(MessageSeverity.Warning, "14:03:12  Warning  Loading failed\r\n")]
        [InlineData(MessageSeverity.Message, "14:03:12  Message  Loading failed\r\n")]
        public void Format_SingleLine_HasTimeKindAndText(MessageSeverity severity, string expected)
        {
            Assert.Equal(expected, MessageFormatter.Format(severity, Timestamp, "Loading failed"));
        }

        [Fact]
        public void Format_SeveralLines_IndentsFollowingLinesToTheText()
        {
            string text = MessageFormatter.Format(MessageSeverity.Error, Timestamp, "first\nsecond\r\nthird");

            Assert.Equal(
                "14:03:12  Error    first\r\n" +
                "                   second\r\n" +
                "                   third\r\n",
                text);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Format_NoText_KeepsTimeAndKind(string? message)
        {
            Assert.Equal("14:03:12  Message  \r\n", MessageFormatter.Format(MessageSeverity.Message, Timestamp, message));
        }
    }
}
