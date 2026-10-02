using System;
using System.Collections.Generic;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// A label with its translations, one per language.
    /// </summary>
    public sealed class Label
    {
        private readonly Dictionary<string, Translation> translations = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Creates a label without translations.</summary>
        /// <param name="id">The ID.</param>
        /// <param name="labelFile">The label file the label was found in first.</param>
        public Label(LabelId id, LabelFile labelFile)
        {
            this.Id = id;
            this.LabelFile = labelFile ?? throw new ArgumentNullException(nameof(labelFile));
        }

        /// <summary>The ID.</summary>
        public LabelId Id { get; }

        /// <summary>
        /// The label file the label was found in first. For an ID of the old form it is the
        /// only source of the label file, see <see cref="LabelId"/>.
        /// </summary>
        public LabelFile LabelFile { get; }

        /// <summary>The translations by language.</summary>
        public IReadOnlyDictionary<string, Translation> Translations => this.translations;

        /// <summary>Whether a change is not yet in the label files.</summary>
        public bool IsModified { get; set; }

        /// <summary>Returns the text in one language.</summary>
        /// <param name="language">The language.</param>
        /// <returns>The text, or <c>null</c> if the translation is missing.</returns>
        public string? GetText(string language)
            => this.translations.TryGetValue(language, out Translation? translation) ? translation.Text : null;

        /// <summary>
        /// Adds a translation unless the label already has one in this language. The first
        /// translation found wins.
        /// </summary>
        /// <param name="translation">The translation.</param>
        /// <returns>Whether it was added.</returns>
        public bool TryAddTranslation(Translation translation)
        {
            if (this.translations.ContainsKey(translation.Language))
            {
                return false;
            }

            this.translations.Add(translation.Language, translation);
            return true;
        }

        /// <inheritdoc />
        public override string ToString() => this.Id.FullId;
    }
}
