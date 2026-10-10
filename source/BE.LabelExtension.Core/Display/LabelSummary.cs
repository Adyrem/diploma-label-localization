using System;
using System.Collections.Generic;
using System.Linq;
using BE.LabelExtension.Core.Labels;

namespace BE.LabelExtension.Core.Display
{
    /// <summary>
    /// What the tooltip (FA05) and the inline display (FA06) show for a label ID in the code:
    /// the text in every loaded language, missing translations marked, an unknown ID named as
    /// such. The wording follows the demonstrations of the concept, in English like the rest of
    /// the interface.
    /// </summary>
    public sealed class LabelSummary
    {
        private LabelSummary(LabelId id, bool isKnown, IReadOnlyList<LabelSummaryLine> lines)
        {
            this.Id = id;
            this.IsKnown = isKnown;
            this.Lines = lines;
        }

        /// <summary>The label ID as it stands in the code.</summary>
        public LabelId Id { get; }

        /// <summary>Whether the label store knows the ID.</summary>
        public bool IsKnown { get; }

        /// <summary>One line per language, in the order of the settings; empty for an unknown ID.</summary>
        public IReadOnlyList<LabelSummaryLine> Lines { get; }

        /// <summary>How many of the languages have no translation.</summary>
        public int MissingCount => this.Lines.Count(l => l.IsMissing);

        /// <summary>The state for the head of the tooltip, for example "2 of 3 languages translated, 1 missing".</summary>
        public string Status
        {
            get
            {
                int count = this.Lines.Count;
                int missing = this.MissingCount;
                return !this.IsKnown ? "not a known label"
                    : count == 0 ? "no languages to show"
                    : missing == 0 ? (count == 1 ? "translated in 1 language" : $"all {count} languages translated")
                    : $"{count - missing} of {count} languages translated, {missing} missing";
            }
        }

        /// <summary>Builds the summary of a label ID.</summary>
        /// <param name="id">The ID in the code.</param>
        /// <param name="label">The label of the store, <c>null</c> if the ID is unknown.</param>
        /// <param name="languages">The loaded languages, in the order to show them.</param>
        /// <returns>The summary.</returns>
        public static LabelSummary Create(LabelId id, Label? label, IReadOnlyList<string> languages)
        {
            if (label == null || label.IsDeleted)
            {
                return new LabelSummary(id, false, Array.Empty<LabelSummaryLine>());
            }

            return new LabelSummary(id, true, languages.Select(language => new LabelSummaryLine(language, label.GetText(language))).ToList());
        }

        /// <summary>
        /// The inline display of one line of code, as parts so that missing translations can be
        /// set apart. A line with one label shows only its translations, as in the demonstration;
        /// with several labels each part starts with its ID.
        /// </summary>
        /// <param name="summaries">The labels of the line, in their order.</param>
        /// <returns>The parts, empty if the line has no label.</returns>
        public static IReadOnlyList<InlinePart> Inline(IReadOnlyList<LabelSummary> summaries)
        {
            var parts = new List<InlinePart>();
            for (int i = 0; i < summaries.Count; i++)
            {
                LabelSummary summary = summaries[i];
                if (i > 0)
                {
                    parts.Add(new InlinePart("    ", false));
                }

                if (summaries.Count > 1)
                {
                    parts.Add(new InlinePart(summary.Id.FullId + ": ", false));
                }

                summary.AddInline(parts);
            }

            return parts;
        }

        /// <summary>The inline display as plain text, see <see cref="Inline"/>.</summary>
        /// <param name="summaries">The labels of the line, in their order.</param>
        /// <returns>The text.</returns>
        public static string InlineText(IReadOnlyList<LabelSummary> summaries) => string.Concat(Inline(summaries).Select(p => p.Text));

        private void AddInline(List<InlinePart> parts)
        {
            if (!this.IsKnown)
            {
                parts.Add(new InlinePart("unknown label", true));
                return;
            }

            int count = this.Lines.Count;
            int missing = this.MissingCount;
            parts.Add(new InlinePart(missing == 0 ? (count == 1 ? "1 translation" : $"{count} translations") : $"{count - missing} of {count} translations", false));
            foreach (LabelSummaryLine line in this.Lines)
            {
                parts.Add(new InlinePart(" | ", false));
                parts.Add(line.IsMissing ? new InlinePart($"{line.Language} missing", true) : new InlinePart($"{line.Language}: {line.Text}", false));
            }
        }
    }

    /// <summary>The text of a label in one language, or that it is missing.</summary>
    public sealed class LabelSummaryLine
    {
        /// <summary>Creates the line.</summary>
        /// <param name="language">The language.</param>
        /// <param name="text">The text, <c>null</c> if the translation is missing.</param>
        public LabelSummaryLine(string language, string? text)
        {
            this.Language = language;
            this.Text = text;
        }

        /// <summary>The language.</summary>
        public string Language { get; }

        /// <summary>The text, <c>null</c> if the translation is missing.</summary>
        public string? Text { get; }

        /// <summary>Whether the translation is missing.</summary>
        public bool IsMissing => this.Text == null;
    }

    /// <summary>A part of the inline display.</summary>
    public sealed class InlinePart
    {
        /// <summary>Creates the part.</summary>
        /// <param name="text">The text.</param>
        /// <param name="isMarked">Whether it names a missing translation or an unknown label.</param>
        public InlinePart(string text, bool isMarked)
        {
            this.Text = text;
            this.IsMarked = isMarked;
        }

        /// <summary>The text.</summary>
        public string Text { get; }

        /// <summary>Whether it names a missing translation or an unknown label, shown in italics.</summary>
        public bool IsMarked { get; }
    }
}
