using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Display;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace BE.LabelExtension.Editor
{
    /// <summary>
    /// Tooltip of a label ID in the X++ editor with the text in every loaded language (FA05).
    /// Its entry stands in the same tooltip as that of the Developer Tools, after it.
    /// </summary>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("BE-LabelExtension QuickInfo Source")]
    [Order(After = "X++ QuickInfo Source")]
    [ContentType(LabelRecognizer.XppContentType)]
#if DEBUG
    [ContentType("text")]
#endif
    internal sealed class LabelQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        private readonly IViewClassifierAggregatorService classifiers;
        private readonly IClassificationTypeRegistryService registry;
        private readonly LabelLookup lookup;

        [ImportingConstructor]
        public LabelQuickInfoSourceProvider(IViewClassifierAggregatorService classifiers, IClassificationTypeRegistryService registry, LabelLookup lookup)
        {
            this.classifiers = classifiers;
            this.registry = registry;
            this.lookup = lookup;
        }

        /// <inheritdoc />
        public IAsyncQuickInfoSource? TryCreateQuickInfoSource(ITextBuffer textBuffer)
            => LabelRecognizer.AppliesTo(textBuffer)
                ? textBuffer.Properties.GetOrCreateSingletonProperty(() => new LabelQuickInfoSource(textBuffer, this.classifiers, this.registry, this.lookup))
                : null;
    }

    /// <summary>The tooltip entry for the label ID under the mouse.</summary>
    internal sealed class LabelQuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer buffer;
        private readonly IViewClassifierAggregatorService classifiers;
        private readonly IClassificationTypeRegistryService registry;
        private readonly LabelLookup lookup;

        public LabelQuickInfoSource(ITextBuffer buffer, IViewClassifierAggregatorService classifiers, IClassificationTypeRegistryService registry, LabelLookup lookup)
        {
            this.buffer = buffer;
            this.classifiers = classifiers;
            this.registry = registry;
            this.lookup = lookup;
        }

        /// <inheritdoc />
        public async Task<QuickInfoItem?> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            SnapshotPoint? trigger = session.GetTriggerPoint(this.buffer.CurrentSnapshot);
            if (trigger == null || session.TextView.TextBuffer != this.buffer)
            {
                return null;
            }

            // Classifiers expect the main thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            this.lookup.EnsureLoaded();
            LabelRecognizer? recognizer = LabelRecognizer.TryCreate(session.TextView, this.classifiers, this.registry);
            if (recognizer == null)
            {
                return null;
            }

            SnapshotPoint point = trigger.Value;
            RecognizedLabel? hovered = recognizer.FindInLine(point.GetContainingLine())
                .Where(l => l.Span.Contains(point) || l.Span.End == point)
                .Select(l => (RecognizedLabel?)l)
                .FirstOrDefault();
            if (hovered == null)
            {
                return null;
            }

            LabelSummary? summary = this.lookup.Summarize(hovered.Value.Id);
            if (summary == null)
            {
                return null;
            }

            ITrackingSpan span = point.Snapshot.CreateTrackingSpan(hovered.Value.Span, SpanTrackingMode.EdgeInclusive);
            return new QuickInfoItem(span, Content(summary));
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        // ID and state in the first row, then one row per language as in the demonstration.
        private static ContainerElement Content(LabelSummary summary)
        {
            var rows = new List<object>
            {
                new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, summary.Id.FullId),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.NaturalLanguage, "  " + summary.Status)),
            };

            foreach (LabelSummaryLine line in summary.Lines)
            {
                rows.Add(new ClassifiedTextElement(
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.NaturalLanguage, line.Language + "  "),
                    line.IsMissing
                        ? new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, "translation missing", ClassifiedTextRunStyle.Italic)
                        : new ClassifiedTextRun(PredefinedClassificationTypeNames.String, line.Text!)));
            }

            return new ContainerElement(ContainerElementStyle.Stacked, rows);
        }
    }
}
