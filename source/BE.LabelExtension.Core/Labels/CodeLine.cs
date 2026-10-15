using System.Collections.Generic;
using System.Text;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// Reads what stands at the cursor in a line of X++ code, for the commands in the editor:
    /// the string literal for the search (FA07) and the extraction (FA09), and the label ID for
    /// opening it in the tool window (FA08).
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
        public static string? StringLiteralAt(string line, int column) => StringLiteralSpanAt(line, column)?.RawContent;

        /// <summary>
        /// The string literal the cursor stands in, with its place in the line, for the
        /// extraction (FA09). Like <see cref="StringLiteralAt"/>, and a verbatim string
        /// <c>@"…"</c> counts with its <c>@</c>; in it, two quotes stand for one.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="column">The position of the cursor in the line, starting at 0.</param>
        /// <returns>The literal, or <c>null</c> if the cursor stands in none.</returns>
        public static StringLiteral? StringLiteralSpanAt(string line, int column)
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

                bool verbatim = quote == '"' && i > 0 && line[i - 1] == '@';
                int end = i + 1;
                while (end < line.Length)
                {
                    if (line[end] == quote)
                    {
                        if (verbatim && end + 1 < line.Length && line[end + 1] == quote)
                        {
                            end += 2;
                            continue;
                        }

                        break;
                    }

                    end += !verbatim && quote == '"' && line[end] == '\\' ? 2 : 1;
                }

                if (end >= line.Length)
                {
                    return null;
                }

                if (column > i && column <= end)
                {
                    int start = verbatim ? i - 1 : i;
                    return new StringLiteral(start, end + 1 - start, quote, verbatim, line.Substring(i + 1, end - i - 1));
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

    /// <summary>A string literal in a line of X++ code, see <see cref="CodeLine.StringLiteralSpanAt"/>.</summary>
    public sealed class StringLiteral
    {
        /// <summary>Creates the literal.</summary>
        /// <param name="start">Where the literal starts in the line, at the <c>@</c> of a verbatim string.</param>
        /// <param name="length">Its length up to and with the closing quote.</param>
        /// <param name="quote">The quote character.</param>
        /// <param name="isVerbatim">Whether it is a verbatim string <c>@"…"</c>.</param>
        /// <param name="rawContent">The content between the quotes as it stands in the code.</param>
        public StringLiteral(int start, int length, char quote, bool isVerbatim, string rawContent)
        {
            this.Start = start;
            this.Length = length;
            this.Quote = quote;
            this.IsVerbatim = isVerbatim;
            this.RawContent = rawContent;
            this.Text = Unescape(rawContent, quote, isVerbatim);
        }

        /// <summary>Where the literal starts in the line, at the <c>@</c> of a verbatim string.</summary>
        public int Start { get; }

        /// <summary>The length of the literal up to and with the closing quote.</summary>
        public int Length { get; }

        /// <summary>The quote character, <c>"</c> or <c>'</c>.</summary>
        public char Quote { get; }

        /// <summary>Whether it is a verbatim string <c>@"…"</c>.</summary>
        public bool IsVerbatim { get; }

        /// <summary>The content between the quotes as it stands in the code.</summary>
        public string RawContent { get; }

        /// <summary>
        /// The text the literal stands for, with its escape sequences resolved; <c>null</c> if it
        /// holds a line break, which a label cannot hold.
        /// </summary>
        public string? Text { get; }

        /// <summary>
        /// The literal that replaces this one with a label ID, in the same quotes. A verbatim
        /// string becomes an ordinary one, like every other use of a label ID in code.
        /// </summary>
        /// <param name="labelId">The complete label ID.</param>
        /// <returns>The new literal, such as <c>"@BDM1:BDM110000003"</c>.</returns>
        public string Replacement(string labelId)
        {
            char quote = this.IsVerbatim ? '"' : this.Quote;
            return quote + labelId + quote;
        }

        // In double quotes, a backslash escapes the next character, as in StringLiteralAt; in a
        // verbatim string two quotes stand for one.
        private static string? Unescape(string raw, char quote, bool verbatim)
        {
            if (verbatim)
            {
                string text = raw.Replace("\"\"", "\"");
                return text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0 ? null : text;
            }

            if (quote != '"')
            {
                return raw;
            }

            var builder = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c != '\\' || i + 1 >= raw.Length)
                {
                    builder.Append(c);
                    continue;
                }

                char next = raw[++i];
                switch (next)
                {
                    case 'n':
                    case 'r':
                        return null;
                    case 't':
                        builder.Append('\t');
                        break;
                    case '\\':
                    case '"':
                    case '\'':
                        builder.Append(next);
                        break;
                    default:
                        builder.Append(c).Append(next);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
