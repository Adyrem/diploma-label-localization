using System;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// Text and comment of a label in one language, together with the label file it comes
    /// from. That file is usually the label file of the ID, but may also be its
    /// <c>_Extension</c> or its compiled form.
    /// </summary>
    public sealed class Translation
    {
        /// <summary>Creates a translation.</summary>
        /// <param name="language">The language.</param>
        /// <param name="text">The text, empty if there is none.</param>
        /// <param name="comment">The comment, <c>null</c> if there is none.</param>
        /// <param name="labelFile">The label file the translation stands in.</param>
        public Translation(string language, string text, string? comment, LabelFile labelFile)
        {
            this.Language = language ?? throw new ArgumentNullException(nameof(language));
            this.Text = text ?? string.Empty;
            this.Comment = comment;
            this.LabelFile = labelFile ?? throw new ArgumentNullException(nameof(labelFile));
        }

        /// <summary>The language.</summary>
        public string Language { get; }

        /// <summary>The text, empty if there is none.</summary>
        public string Text { get; }

        /// <summary>The comment, <c>null</c> if there is none.</summary>
        public string? Comment { get; }

        /// <summary>The label file the translation stands in.</summary>
        public LabelFile LabelFile { get; }

        /// <summary>Whether the translation must not be changed, see <see cref="Labels.LabelFile.IsReadOnly"/>.</summary>
        public bool IsReadOnly => this.LabelFile.IsReadOnly;
    }
}
