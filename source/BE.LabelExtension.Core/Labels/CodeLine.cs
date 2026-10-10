using System.Collections.Generic;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// Reads what stands at the cursor in a line of X++ code, for the commands in the editor:
    /// the string literal for the search (FA07) and the label ID for opening it in the tool
    /// window (FA08).
    /// </summary>
    public static class CodeLine
    {
        /// <summary>
        /// The content of the string literal the cursor stands in, in double or single quotes.
        /// A cursor directly after the opening or before the closing quote counts as inside. In
        /// double quotes a backslash escapes the next character.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="column">The position of the cursor in the line, starting at 0.</param>
        /// <returns>The content without quotes, or <c>null</c> if the cursor stands in no literal.</returns>
        public static string? StringLiteralAt(string line, int column)
        {
            int i = 0;
            while (i < line.Length)
            {
                char quote = line[i];
                if (quote == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    return null;
                }

                if (quote != '"' && quote != '\'')
                {
                    i++;
                    continue;
                }

                int end = i + 1;
                while (end < line.Length && line[end] != quote)
                {
                    end += quote == '"' && line[end] == '\\' ? 2 : 1;
                }

                if (end >= line.Length)
                {
                    return null;
                }

                if (column > i && column <= end)
                {
                    return line.Substring(i + 1, end - i - 1);
                }

                i = end + 1;
            }

            return null;
        }

        /// <summary>The label ID the cursor stands on, in either form.</summary>
        /// <param name="line">The line.</param>
        /// <param name="column">The position of the cursor in the line, starting at 0; directly after the ID still counts.</param>
        /// <returns>The ID, or <c>null</c> if the cursor stands on none.</returns>
        public static LabelId? LabelIdAt(string line, int column)
        {
            IReadOnlyList<LabelIdMatch> found = LabelId.FindAll(line);
            foreach (LabelIdMatch match in found)
            {
                if (column >= match.Index && column <= match.Index + match.Length)
                {
                    return match.Id;
                }
            }

            return null;
        }
    }
}
