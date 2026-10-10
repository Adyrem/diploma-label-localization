using System;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>Text and comment of a label in one language, as entered for a new label.</summary>
    public sealed class LabelText
    {
        /// <summary>Creates the text of one language.</summary>
        /// <param name="language">The language.</param>
        /// <param name="text">The text.</param>
        /// <param name="comment">The comment, <c>null</c> or empty for none.</param>
        public LabelText(string language, string text, string? comment)
        {
            this.Language = language ?? throw new ArgumentNullException(nameof(language));
            this.Text = text ?? string.Empty;
            this.Comment = string.IsNullOrEmpty(comment) ? null : comment;
        }

        /// <summary>The language.</summary>
        public string Language { get; }

        /// <summary>The text.</summary>
        public string Text { get; }

        /// <summary>The comment, <c>null</c> if there is none.</summary>
        public string? Comment { get; }
    }
}
