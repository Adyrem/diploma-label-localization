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
    public readonly struct LabelId : IEquatable<LabelId>
    {
        private const string Part = "[A-Za-z0-9_]+";
        private const string Legacy = "[A-Za-z]+[0-9]+";

        private static readonly Regex FullPattern = new($"^@(?:(?<file>{Part}):(?<key>{Part})|{Legacy})$", RegexOptions.CultureInvariant);

        // An ID in running text: no ID character directly before or after it.
        private static readonly Regex TextPattern = new($"(?<![A-Za-z0-9_])@(?:{Part}:{Part}|{Legacy})(?![A-Za-z0-9_:])", RegexOptions.CultureInvariant);

        private LabelId(string fullId, string? file, string key)
        {
            this.FullId = fullId;
            this.File = file;
            this.Key = key;
        }

        /// <summary>The complete ID including the leading <c>@</c>, as it is used in code.</summary>
        public string FullId { get; }

        /// <summary>Label file part of the new form, <c>null</c> for the old form.</summary>
        public string? File { get; }

        /// <summary>
        /// The label as it stands left of the equals sign in the label file: the part after the
        /// colon for the new form, the complete ID for the old form.
        /// </summary>
        public string Key { get; }

        /// <summary>Whether this is the old form without colon, such as <c>@SYS12345</c>.</summary>
        public bool IsLegacy => this.File == null;

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

            Match match = FullPattern.Match(text);
            if (!match.Success)
            {
                return false;
            }

            id = match.Groups["file"].Success
                ? new LabelId(text!, match.Groups["file"].Value, match.Groups["key"].Value)
                : new LabelId(text!, null, text!);
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
            => key.StartsWith("@", StringComparison.Ordinal)
                ? TryParse(key, out id)
                : TryParse("@" + labelFileIdPrefix + ":" + key, out id);

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
