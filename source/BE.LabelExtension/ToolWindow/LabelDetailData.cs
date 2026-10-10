using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>The detail view: the selected label with one row per loaded language.</summary>
    [DataContract]
    internal sealed class LabelDetailData : NotifyPropertyChangedObject
    {
        /// <summary>The detail view without a label.</summary>
        public static readonly LabelDetailData Empty = new(string.Empty, "Select a label to see and change its translations.", new List<TranslationRow>());

        /// <summary>Creates the detail view.</summary>
        /// <param name="id">The complete label ID, empty for none.</param>
        /// <param name="info">Label file, model and whether the label can be changed.</param>
        /// <param name="translations">One row per loaded language.</param>
        public LabelDetailData(string id, string info, IReadOnlyList<TranslationRow> translations)
        {
            this.Id = id;
            this.Info = info;
            this.Translations = translations;
        }

        /// <summary>The complete label ID.</summary>
        [DataMember]
        public string Id { get; }

        /// <summary>Label file, model and whether the label can be changed.</summary>
        [DataMember]
        public string Info { get; }

        /// <summary>One row per loaded language.</summary>
        [DataMember]
        public IReadOnlyList<TranslationRow> Translations { get; }
    }

    /// <summary>
    /// Text and comment of the selected label in one language. The user's input arrives as
    /// changed properties, which the tool window applies to the label store.
    /// </summary>
    [DataContract]
    internal sealed class TranslationRow : NotifyPropertyChangedObject
    {
        private string text;
        private string comment;

        /// <summary>Creates a row.</summary>
        /// <param name="language">The language.</param>
        /// <param name="text">The text, empty if the translation is missing.</param>
        /// <param name="comment">The comment, empty if there is none.</param>
        /// <param name="isReadOnly">Whether the translation cannot be changed.</param>
        /// <param name="hasFile">Whether the label file exists in this language; without, there is nothing to show or enter.</param>
        /// <param name="hint">Why it cannot be changed, or that it is missing.</param>
        public TranslationRow(string language, string text, string comment, bool isReadOnly, bool hasFile, string hint)
        {
            this.Language = language;
            this.text = text;
            this.comment = comment;
            this.IsReadOnly = isReadOnly;
            this.HasFile = hasFile;
            this.Hint = hint;
        }

        /// <summary>The language.</summary>
        [DataMember]
        public string Language { get; }

        /// <summary>The text, changed by the user.</summary>
        [DataMember]
        public string Text
        {
            get => this.text;
            set => this.SetProperty(ref this.text, value ?? string.Empty);
        }

        /// <summary>The comment, changed by the user.</summary>
        [DataMember]
        public string Comment
        {
            get => this.comment;
            set => this.SetProperty(ref this.comment, value ?? string.Empty);
        }

        /// <summary>
        /// Whether the translation cannot be changed. Its text can still be selected and copied,
        /// for example to reuse a label of the platform.
        /// </summary>
        [DataMember]
        public bool IsReadOnly { get; }

        /// <summary>
        /// Whether the label file exists in this language. Without, the fields are disabled, so
        /// nobody types what cannot be saved (RE27).
        /// </summary>
        [DataMember]
        public bool HasFile { get; }

        /// <summary>Why the translation cannot be changed, or that it is missing; empty otherwise.</summary>
        [DataMember]
        public string Hint { get; }
    }
}
