using System;
using System.Collections.Generic;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// A label with its translations, one per language.
    /// </summary>
    /// <remarks>
    /// The translations lie in a plain array in the order they were added. A label has only a
    /// few, and the store holds about half a million labels; a dictionary per label cost a
    /// quarter of the memory (F14).
    /// </remarks>
    public sealed class Label
    {
        private Translation[] translations = Array.Empty<Translation>();

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

        /// <summary>The translations, one per language, in the order they were added.</summary>
        public IReadOnlyList<Translation> Translations => this.translations;

        /// <summary>The same as an array, for the search, which goes through it without allocating.</summary>
        internal Translation[] TranslationArray => this.translations;

        /// <summary>Whether a change is not yet in the label files.</summary>
        public bool IsModified { get; set; }

        /// <summary>Returns the translation in one language.</summary>
        /// <param name="language">The language, compared without case.</param>
        /// <returns>The translation, or <c>null</c> if it is missing.</returns>
        public Translation? GetTranslation(string language)
        {
            foreach (Translation translation in this.translations)
            {
                if (string.Equals(translation.Language, language, StringComparison.OrdinalIgnoreCase))
                {
                    return translation;
                }
            }

            return null;
        }

        /// <summary>Returns the text in one language.</summary>
        /// <param name="language">The language.</param>
        /// <returns>The text, or <c>null</c> if the translation is missing.</returns>
        public string? GetText(string language) => this.GetTranslation(language)?.Text;

        /// <summary>
        /// Adds a translation unless the label already has one in this language. The first
        /// translation found wins.
        /// </summary>
        /// <param name="translation">The translation.</param>
        /// <returns>Whether it was added.</returns>
        public bool TryAddTranslation(Translation translation)
        {
            if (this.GetTranslation(translation.Language) != null)
            {
                return false;
            }

            Translation[] grown = new Translation[this.translations.Length + 1];
            Array.Copy(this.translations, grown, this.translations.Length);
            grown[this.translations.Length] = translation;
            this.translations = grown;
            return true;
        }

        /// <summary>
        /// Replaces the translation in its language or adds it. The array is replaced as a whole,
        /// so a search running at the same time sees either the old or the new state.
        /// </summary>
        /// <param name="translation">The new translation.</param>
        internal void SetTranslation(Translation translation)
        {
            lock (this)
            {
                Translation[] current = this.translations;
                int index = Array.FindIndex(current, t => string.Equals(t.Language, translation.Language, StringComparison.OrdinalIgnoreCase));
                Translation[] next = new Translation[index >= 0 ? current.Length : current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[index >= 0 ? index : current.Length] = translation;
                this.translations = next;
            }
        }

        /// <summary>
        /// Returns the instance of an equal text or comment the label already holds, otherwise
        /// the value itself. In the label files of the platform the comment is the same in every
        /// language, and de-CH often has the text of de; sharing saves that memory.
        /// </summary>
        /// <param name="value">A text or comment read for this label.</param>
        /// <returns>An equal string, shared if possible.</returns>
        internal string Share(string value)
        {
            foreach (Translation translation in this.translations)
            {
                if (string.Equals(translation.Text, value, StringComparison.Ordinal))
                {
                    return translation.Text;
                }

                if (translation.Comment != null && string.Equals(translation.Comment, value, StringComparison.Ordinal))
                {
                    return translation.Comment;
                }
            }

            return value;
        }

        /// <inheritdoc />
        public override string ToString() => this.Id.FullId;
    }
}
