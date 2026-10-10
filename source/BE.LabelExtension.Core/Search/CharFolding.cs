using System;
using System.Collections.Generic;

namespace BE.LabelExtension.Core.Search
{
    /// <summary>
    /// Compares text without case through a table of all 65 536 characters, without
    /// allocating. On .NET Framework, <see cref="string.IndexOf(string, StringComparison)"/>
    /// with <see cref="StringComparison.OrdinalIgnoreCase"/> was the slowest part of the search
    /// (F14).
    /// </summary>
    internal sealed class CharFolding
    {
        private readonly char[] map = new char[char.MaxValue + 1];

        private CharFolding(Func<char, char> fold)
        {
            for (int c = 0; c <= char.MaxValue; c++)
            {
                this.map[c] = fold((char)c);
            }
        }

        /// <summary>The rule of <see cref="StringComparison.Ordinal"/>: each character as it is.</summary>
        public static CharFolding Ordinal { get; } = new(c => c);

        /// <summary>
        /// The rule of <see cref="StringComparison.OrdinalIgnoreCase"/>: each character in its
        /// invariant upper case.
        /// </summary>
        public static CharFolding OrdinalIgnoreCase { get; } = new(char.ToUpperInvariant);

        /// <summary>
        /// The rule of a regular expression with <c>IgnoreCase</c> and <c>CultureInvariant</c>:
        /// each character in its invariant lower case.
        /// </summary>
        public static CharFolding RegexIgnoreCase { get; } = new(char.ToLowerInvariant);

        /// <summary>Prepares a term once per search.</summary>
        /// <param name="term">The term, for example a word of Anything like that.</param>
        /// <returns>The term, ready to be found in fields that are not folded.</returns>
        public FoldedTerm Prepare(string term)
        {
            char[] chars = term.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = this.map[chars[i]];
            }

            // All characters that fold to the first one, usually its two cases.
            var firsts = new List<char>();
            if (chars.Length > 0)
            {
                for (int c = 0; c <= char.MaxValue; c++)
                {
                    if (this.map[c] == chars[0])
                    {
                        firsts.Add((char)c);
                    }
                }
            }

            return new FoldedTerm(this.map, new string(chars), firsts.ToArray());
        }

        /// <summary>A term folded once, to be found in many fields.</summary>
        internal sealed class FoldedTerm
        {
            private readonly char[] map;
            private readonly char[] firsts;

            public FoldedTerm(char[] map, string folded, char[] firsts)
            {
                this.map = map;
                this.Folded = folded;
                this.firsts = firsts;
            }

            /// <summary>The folded term.</summary>
            public string Folded { get; }

            /// <summary>The length of the term.</summary>
            public int Length => this.Folded.Length;

            /// <summary>Finds the term in a field that is not folded.</summary>
            /// <param name="field">The field, for example the text of a label.</param>
            /// <param name="start">Where to start in the field.</param>
            /// <returns>The index of the first occurrence from <paramref name="start"/> on, or -1.</returns>
            public int IndexOf(string field, int start)
            {
                string folded = this.Folded;
                int length = folded.Length;
                if (length == 0)
                {
                    return start <= field.Length ? start : -1;
                }

                // The native search of the framework jumps to the candidates; the table only
                // checks them.
                char[] table = this.map;
                int last = field.Length - length;
                while (start <= last)
                {
                    int i = this.firsts.Length == 1 ? field.IndexOf(this.firsts[0], start) : field.IndexOfAny(this.firsts, start);
                    if (i < 0 || i > last)
                    {
                        return -1;
                    }

                    int j = 1;
                    while (j < length && table[field[i + j]] == folded[j])
                    {
                        j++;
                    }

                    if (j == length)
                    {
                        return i;
                    }

                    start = i + 1;
                }

                return -1;
            }
        }
    }
}
