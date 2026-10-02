using System;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// One label as it stands in the file of one language: key, text and optional comment.
    /// </summary>
    public sealed class LabelEntry
    {
        /// <summary>Creates an entry.</summary>
        /// <param name="key">The label left of the equals sign.</param>
        /// <param name="text">The text, empty if there is none.</param>
        /// <param name="comment">The comment, <c>null</c> if the label has no comment line.</param>
        public LabelEntry(string key, string text, string? comment)
        {
            this.Key = key ?? throw new ArgumentNullException(nameof(key));
            this.Text = text ?? string.Empty;
            this.Comment = comment;
        }

        /// <summary>The label left of the equals sign.</summary>
        public string Key { get; }

        /// <summary>The text, empty if there is none.</summary>
        public string Text { get; }

        /// <summary>The comment, <c>null</c> if the label has no comment line.</summary>
        public string? Comment { get; }
    }
}
