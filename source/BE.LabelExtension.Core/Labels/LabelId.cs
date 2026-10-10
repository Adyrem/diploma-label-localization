using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// Label ID in one of the two forms. The new form <c>@BDM1:L3F2A9C15B8047DE1</c> consists of
    /// the label file and the label. The old form <c>@SYS12345</c> is never split: the whole
    /// string is the ID, and the label file follows only from the file the label stands in.
    /// </summary>
    /// <remarks>
    /// The ID keeps only the complete string and where its label part starts. Label file and
    /// label are cut out when asked for, because the store holds about half a million IDs and
    /// most of them are never asked (F14).
    /// </remarks>
    public readonly struct LabelId : IEquatable<LabelId>
    {
        private const string Part = "[A-Za-z0-9_]+";
        private const string Legacy = "[A-Za-z]+[0-9]+";

        // An ID in running text: no ID character directly before or after it.
        private static readonly Regex TextPattern = new($"(?<![A-Za-z0-9_])@(?:{Part}:{Part}|{Legacy})(?![A-Za-z0-9_:])", RegexOptions.CultureInvariant);

        // Index of the label part in FullId: after the colon for the new form, 0 for the old one.
        private readonly int keyStart;

        private LabelId(string fullId, int keyStart)
        {
            this.FullId = fullId;
            this.keyStart = keyStart;
        }

        /// <summary>The complete ID including the leading <c>@</c>, as it is used in code.</summary>
        public string FullId { get; }

        /// <summary>Label file part of the new form, <c>null</c> for the old form.</summary>
        public string? File => this.keyStart == 0 ? null : this.FullId.Substring(1, this.keyStart - 2);

        /// <summary>
        /// The label as it stands left of the equals sign in the label file: the part after the
        /// colon for the new form, the complete ID for the old form.
        /// </summary>
        public string Key => this.keyStart == 0 ? this.FullId : this.FullId.Substring(this.keyStart);

        /// <summary>Whether this is the old form without colon, such as <c>@SYS12345</c>.</summary>
        public bool IsLegacy => this.keyStart == 0;

        /// <summary>Compares <see cref="Key"/> with a text without cutting it out.</summary>
        /// <param name="text">The text, for example a search term.</param>
        /// <param name="comparison">How to compare.</param>
        /// <returns>Whether the label part equals the text.</returns>
        public bool KeyEquals(string text, StringComparison comparison)
            => this.FullId != null
                && this.FullId.Length - this.keyStart == text.Length
                && string.Compare(this.FullId, this.keyStart, text, 0, text.Length, comparison) == 0;

        /// <summary>
        /// Parses a complete label ID. A failure is the normal case for search terms and property
        /// values, so it is reported with <c>false</c> instead of an exception.
        /// </summary>
        /// <param name="text">Text that may be a label ID, without surrounding quotes or spaces.</param>
        /// <param name="id">The parsed ID if the method returns <c>true</c>.</param>
        /// <returns>Whether <paramref name="text"/> is a label ID in one of the two forms.</returns>
        public static bool TryParse(string? text, out LabelId id)
        {
            id = default;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            // @<Part>:<Part> or @<letters><digits>, without a regular expression because every
            // label of every loaded file passes here.
            string value = text!;
            if (value.Length < 2 || value[0] != '@')
            {
                return false;
            }

            int colon = value.IndexOf(':');
            if (colon >= 0)
            {
                if (!IsPart(value, 1, colon) || !IsPart(value, colon + 1, value.Length))
                {
                    return false;
                }

                id = new LabelId(value, colon + 1);
                return true;
            }

            if (!IsLegacyForm(value))
            {
                return false;
            }

            id = new LabelId(value, 0);
            return true;
        }

        /// <summary>Parses a complete label ID.</summary>
        /// <param name="text">The label ID.</param>
        /// <returns>The parsed ID.</returns>
        /// <exception cref="FormatException"><paramref name="text"/> is not a label ID.</exception>
        public static LabelId Parse(string text)
            => TryParse(text, out LabelId id) ? id : throw new FormatException($"'{text}' is not a label ID.");

        /// <summary>
        /// Builds the ID of a label read from a label file. A key that starts with <c>@</c> is an
        /// old ID and stands for itself; any other key belongs to the label file.
        /// </summary>
        /// <param name="labelFileIdPrefix">
        /// The label file part of the IDs, see <see cref="LabelFile.IdPrefix"/>.
        /// </param>
        /// <param name="key">The label left of the equals sign.</param>
        /// <param name="id">The ID if the method returns <c>true</c>.</param>
        /// <returns>Whether the key forms a valid label ID.</returns>
        public static bool TryCreate(string labelFileIdPrefix, string key, out LabelId id)
        {
            if (key.StartsWith("@", StringComparison.Ordinal))
            {
                return TryParse(key, out id);
            }

            if (!IsPart(labelFileIdPrefix, 0, labelFileIdPrefix.Length) || !IsPart(key, 0, key.Length))
            {
                id = default;
                return false;
            }

            id = new LabelId("@" + labelFileIdPrefix + ":" + key, labelFileIdPrefix.Length + 2);
            return true;
        }

        /// <summary>
        /// Finds all label IDs in a line of text, for example in code. Used where no
        /// classification of the X++ editor is available (risk R07).
        /// </summary>
        /// <param name="text">The text to search.</param>
        /// <returns>The IDs with their position, in the order they occur.</returns>
        public static IReadOnlyList<LabelIdMatch> FindAll(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var found = new List<LabelIdMatch>();
            foreach (Match match in TextPattern.Matches(text))
            {
                found.Add(new LabelIdMatch(Parse(match.Value), match.Index));
            }

            return found;
        }

        /// <inheritdoc />
        public bool Equals(LabelId other) => string.Equals(this.FullId, other.FullId, StringComparison.Ordinal);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is LabelId other && this.Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => this.FullId == null ? 0 : StringComparer.Ordinal.GetHashCode(this.FullId);

        /// <inheritdoc />
        public override string ToString() => this.FullId ?? string.Empty;

        // [A-Za-z0-9_]+ in text[start, end).
        private static bool IsPart(string text, int start, int end)
        {
            if (end <= start)
            {
                return false;
            }

            for (int i = start; i < end; i++)
            {
                char c = text[i];
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        // @[A-Za-z]+[0-9]+ as the whole text.
        private static bool IsLegacyForm(string text)
        {
            int i = 1;
            while (i < text.Length && ((text[i] >= 'A' && text[i] <= 'Z') || (text[i] >= 'a' && text[i] <= 'z')))
            {
                i++;
            }

            int digitsStart = i;
            while (i < text.Length && text[i] >= '0' && text[i] <= '9')
            {
                i++;
            }

            return digitsStart > 1 && i > digitsStart && i == text.Length;
        }

        /// <summary>Compares two IDs ordinally.</summary>
        /// <param name="left">The first ID.</param>
        /// <param name="right">The second ID.</param>
        /// <returns>Whether both IDs are equal.</returns>
        public static bool operator ==(LabelId left, LabelId right) => left.Equals(right);

        /// <summary>Compares two IDs ordinally.</summary>
        /// <param name="left">The first ID.</param>
        /// <param name="right">The second ID.</param>
        /// <returns>Whether the IDs differ.</returns>
        public static bool operator !=(LabelId left, LabelId right) => !left.Equals(right);
    }
}
