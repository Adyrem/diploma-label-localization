using System;
using System.Globalization;
using System.Text;

namespace BE.LabelExtension.Core.Diagnostics
{
    /// <summary>
    /// Formats a message as text for the Output Window, with its time and kind (FA17).
    /// </summary>
    /// <example>
    /// <code>
    /// 14:03:12  Error    Open label window failed: Access denied
    ///                    System.UnauthorizedAccessException: Access denied
    /// </code>
    /// </example>
    public static class MessageFormatter
    {
        private const string LineBreak = "\r\n";

        // Wide enough for the longest kind, "Warning" and "Message".
        private const int SeverityWidth = 7;

        /// <summary>
        /// Formats a message. Every line ends with a line break, and lines after the first
        /// are indented to the column of the message text.
        /// </summary>
        /// <param name="severity">Kind of the message.</param>
        /// <param name="timestamp">Time the message was reported.</param>
        /// <param name="message">Text of the message, may span several lines or be empty.</param>
        /// <returns>The formatted text, ending with a line break.</returns>
        public static string Format(MessageSeverity severity, DateTime timestamp, string? message)
        {
            string prefix = timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                + "  " + severity.ToString().PadRight(SeverityWidth) + "  ";
            string indent = new string(' ', prefix.Length);

            string[] lines = (message ?? string.Empty).Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            var text = new StringBuilder();
            text.Append(prefix).Append(lines[0]).Append(LineBreak);
            for (int i = 1; i < lines.Length; i++)
            {
                text.Append(indent).Append(lines[i]).Append(LineBreak);
            }

            return text.ToString();
        }
    }
}
