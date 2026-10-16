using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// Reads a call of <c>strFmt</c> in a line of X++ code, for the placeholders of the inline
    /// display (KA03): in <c>strFmt("@BDM1:L1", custTable.Name, qty)</c> the label text
    /// "Customer %1 ordered %2" shows as "Customer {custTable.Name} ordered {qty}".
    /// </summary>
    /// <remarks>
    /// Only the line itself is read, as the concept allows: arguments on the next lines are
    /// missing, and their placeholders stay. The label must be the first argument, also inside
    /// a call such as <c>literalStr</c>.
    /// </remarks>
    public static class FormatCall
    {
        private const string FunctionName = "strFmt";

        private static readonly Regex Placeholder = new("%([1-9])", RegexOptions.CultureInvariant);

        /// <summary>The further arguments of the <c>strFmt</c> call whose first argument holds the position.</summary>
        /// <param name="line">The line of code.</param>
        /// <param name="position">Where the label ID stands in the line, its quote or its <c>@</c>.</param>
        /// <returns>The arguments after the first, as they stand in the code, or <c>null</c> if the position is in no first argument of <c>strFmt</c>.</returns>
        public static IReadOnlyList<string>? ArgumentsFor(string line, int position)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            int from = 0;
            while (true)
            {
                int at = IndexOfName(line, from);
                if (at < 0)
                {
                    return null;
                }

                from = at + FunctionName.Length;
                int open = from;
                while (open < line.Length && char.IsWhiteSpace(line[open]))
                {
                    open++;
                }

                if (open >= line.Length || line[open] != '(')
                {
                    continue;
                }

                List<(int Start, int End)> arguments = Split(line, open + 1);
                if (arguments.Count > 0 && position >= arguments[0].Start && position < arguments[0].End)
                {
                    return arguments.Skip(1)
                        .Select(a => line.Substring(a.Start, a.End - a.Start).Trim())
                        .TakeWhile(a => a.Length > 0)
                        .ToList();
                }
            }
        }

        /// <summary>Replaces <c>%1</c> to <c>%9</c> by the arguments in braces; a placeholder without argument stays.</summary>
        /// <param name="text">The label text.</param>
        /// <param name="arguments">The arguments, the first for <c>%1</c>.</param>
        /// <returns>The text with the arguments.</returns>
        public static string ReplacePlaceholders(string text, IReadOnlyList<string> arguments)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return arguments.Count == 0
                ? text
                : Placeholder.Replace(text, m =>
                {
                    int index = m.Groups[1].Value[0] - '1';
                    return index < arguments.Count ? "{" + arguments[index] + "}" : m.Value;
                });
        }

        // The name as a word of its own, in any case, as X++ does not mind the case.
        private static int IndexOfName(string line, int from)
        {
            while (from < line.Length)
            {
                int at = line.IndexOf(FunctionName, from, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                {
                    return -1;
                }

                int end = at + FunctionName.Length;
                bool before = at == 0 || !IsNameCharacter(line[at - 1]);
                bool after = end >= line.Length || !IsNameCharacter(line[end]);
                if (before && after)
                {
                    return at;
                }

                from = end;
            }

            return -1;
        }

        // The arguments from the given position up to the closing parenthesis or the end of the
        // line, split at the commas outside parentheses, brackets and strings.
        private static List<(int Start, int End)> Split(string line, int start)
        {
            var arguments = new List<(int Start, int End)>();
            int depth = 0;
            int argument = start;
            for (int i = start; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"' || c == '\'')
                {
                    i = EndOfString(line, i);
                }
                else if (c == '(' || c == '[' || c == '{')
                {
                    depth++;
                }
                else if (c == ')' || c == ']' || c == '}')
                {
                    if (depth == 0)
                    {
                        arguments.Add((argument, i));
                        return arguments;
                    }

                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    arguments.Add((argument, i));
                    argument = i + 1;
                }
            }

            if (argument < line.Length)
            {
                arguments.Add((argument, line.Length));
            }

            return arguments;
        }

        // Index of the closing quote, or the end of the line; in double quotes a backslash escapes.
        private static int EndOfString(string line, int open)
        {
            char quote = line[open];
            int i = open + 1;
            while (i < line.Length && line[i] != quote)
            {
                i += quote == '"' && line[i] == '\\' ? 2 : 1;
            }

            return Math.Min(i, line.Length - 1);
        }

        private static bool IsNameCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';
    }
}
