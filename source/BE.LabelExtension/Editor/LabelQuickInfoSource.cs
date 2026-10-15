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
    /// <remarks>
    /// One provider per content type. A provider registered for X++ and text alike was asked
    /// twice for an X++ buffer, because X++ derives from text, and the entry stood twice in
    /// the tooltip (D2).
    /// </remarks>
    internal abstract class LabelQuickInfoSourceProviderBase : IAsyncQuickInfoSourceProvider
    {
        private readonly IViewClassifierAggregatorService classifiers;
        private readonly IClassificationTypeRegistryService registry;
        private readonly LabelLookup lookup;

        protected LabelQuickInfoSourceProviderBase(IViewClassifierAggregatorService classifiers, IClassificationTypeRegistryService registry, LabelLookup lookup)
        {
            this.classifiers = classifiers;
            this.registry = registry;
            this.lookup = lookup;
        }

        /// <inheritdoc />
        public IAsyncQuickInfoSource? TryCreateQuickInfoSource(ITextBuffer textBuffer)
            => this.Serves(textBuffer)
                ? textBuffer.Properties.GetOrCreateSingletonProperty(() => new LabelQuickInfoSource(textBuffer, this.classifiers, this.registry, this.lookup))
                : null;

        /// <summary>Whether this provider answers for the buffer.</summary>
        protected abstract bool Serves(ITextBuffer buffer);
    }

    /// <summary>The tooltip in the X++ editor.</summary>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("BE-LabelExtension QuickInfo Source")]
    [Order(After = "X++ QuickInfo Source")]
    [ContentType(LabelRecognizer.XppContentType)]
    internal sealed class LabelQuickInfoSourceProvider : LabelQuickInfoSourceProviderBase
    {
        [ImportingConstructor]
        public LabelQuickInfoSourceProvider(IViewClassifierAggregatorService classifiers, IClassificationTypeRegistryService registry, LabelLookup lookup)
            : base(classifiers, registry, lookup)
        {
        }

        /// <inheritdoc />
        protected override bool Serves(ITextBuffer buffer) => buffer.ContentType.IsOfType(LabelRecognizer.XppContentType);
    }

#if DEBUG
    /// <summary>The tooltip in .xpp files opened as plain text, the stand-in for the X++ editor.</summary>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("BE-LabelExtension QuickInfo Source for .xpp text")]
    [ContentType("text")]
    internal sealed class StandInQuickInfoSourceProvider : LabelQuickInfoSourceProviderBase
    {
        [ImportingConstructor]
        public StandInQuickInfoSourceProvider(IViewClassifierAggregatorService classifiers, IClassificationTypeRegistryService registry, LabelLookup lookup)
            : base(classifiers, registry, lookup)
        {
        }

        /// <inheritdoc />
        protected override bool Serves(ITextBuffer buffer) => LabelRecognizer.IsStandIn(buffer);
    }
#endif

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

        // The ID in the first row, then one row per language. The count of translations of the
        // demonstration was left out after D2; an unknown ID still says so.
        private static ContainerElement Content(LabelSummary summary)
        {
            var rows = new List<object>
            {
                summary.IsKnown
                    ? new ClassifiedTextElement(new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, summary.Id.FullId))
                    : new ClassifiedTextElement(
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
