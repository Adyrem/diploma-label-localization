using System;
using System.Collections.Generic;

namespace BE.LabelExtension.Core.Usages
{
    /// <summary>
    /// Finds a complete label ID in a text. An occurrence counts only if no further
    /// character of an ID follows, so that a search for <c>@SYS1234</c> does not report
    /// <c>@SYS12345</c>.
    /// </summary>
    public static class LabelIdOccurrences
    {
        /// <summary>
        /// Returns the start index of every occurrence of <paramref name="labelId"/> in
        /// <paramref name="text"/>, in ascending order.
        /// </summary>
        /// <param name="text">Text to search, for example a line of code or an XML file.</param>
        /// <param name="labelId">Complete label ID including the leading <c>@</c>.</param>
        /// <returns>Start indexes of the occurrences, empty if there are none.</returns>
        public static IReadOnlyList<int> Find(string text, string labelId)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (string.IsNullOrEmpty(labelId))
            {
                throw new ArgumentException("The label ID must not be empty.", nameof(labelId));
            }

            var found = new List<int>();
            int index = text.IndexOf(labelId, StringComparison.Ordinal);
            while (index >= 0)
            {
                int end = index + labelId.Length;
                if (end == text.Length || !IsIdCharacter(text[end]))
                {
                    found.Add(index);
                }

                index = text.IndexOf(labelId, index + 1, StringComparison.Ordinal);
            }

            return found;
        }

        private static bool IsIdCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';
    }
}
