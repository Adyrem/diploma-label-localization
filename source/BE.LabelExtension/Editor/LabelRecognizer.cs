using System;
using System.Collections.Generic;
using System.Linq;
using BE.LabelExtension.Core.Labels;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace BE.LabelExtension.Editor
{
    /// <summary>
    /// Finds the label IDs in a line of the X++ editor (component label recognition, FA05,
    /// FA06). It uses the classifications of the Developer Tools; their names stand only here
    /// (risk R07).
    /// </summary>
    /// <remarks>
    /// <para>A label token starts with the opening quote of its string literal, see
    /// <see cref="LabelId.TryParseToken"/>.</para>
    /// <para>Without these classifications, because a later version of the Developer Tools
    /// names them otherwise, the recognition falls back to the regular expression of
    /// <see cref="LabelId.FindAll"/> on the text of the line. The Debug build uses the same for
    /// <c>.xpp</c> files opened as plain text, in place of the X++ editor that does not exist on
    /// the development machine.</para>
    /// </remarks>
    internal sealed class LabelRecognizer
    {
        /// <summary>Content type of the X++ editor of the Developer Tools.</summary>
        public const string XppContentType = "X++";

        private static readonly string[] LabelClassifications = { "X++ Modern Label", "X++ Legacy Label" };

        private readonly IClassifier? classifier;

        private LabelRecognizer(IClassifier? classifier)
        {
            this.classifier = classifier;
        }


        /// <summary>Whether labels are recognized by the regular expression instead of the classification.</summary>
        public bool UsesRegex => this.classifier == null;

        /// <summary>
        /// Whether the extension shows labels in a buffer: X++ code, and in the Debug build also
        /// plain text in a <c>.xpp</c> file.
        /// </summary>
        /// <param name="buffer">The text buffer.</param>
        /// <returns>Whether tooltip and inline display apply.</returns>
        public static bool AppliesTo(ITextBuffer buffer)
        {
            if (buffer.ContentType.IsOfType(XppContentType))
            {
                return true;
            }

#if DEBUG
            return buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document)
                && document.FilePath != null
                && document.FilePath.EndsWith(".xpp", StringComparison.OrdinalIgnoreCase);
#else
            return false;
#endif
        }

        /// <summary>Creates the recognition for a text view.</summary>
        /// <param name="view">The view.</param>
        /// <param name="classifiers">Gives the classification of the view.</param>
        /// <param name="registry">Tells whether the classifications of the Developer Tools exist.</param>
        /// <returns>The recognition, or <c>null</c> if the view shows no labels.</returns>
        public static LabelRecognizer? TryCreate(ITextView view, IViewClassifierAggregatorService classifiers, IClassificationTypeRegistryService registry)
        {
            ITextBuffer buffer = view.TextBuffer;
            if (!AppliesTo(buffer))
            {
                return null;
            }

            bool classified = buffer.ContentType.IsOfType(XppContentType)
                && LabelClassifications.Any(name => registry.GetClassificationType(name) != null);
            if (!classified)
            {
                return new LabelRecognizer(null);
            }

            // The aggregator of a view belongs to the view and ends with it.
            return new LabelRecognizer(classifiers.GetClassifier(view));
        }

        /// <summary>
        /// Calls back when the classification of a part of the text changed, so its labels may
        /// differ, for example once the language service has classified the file.
        /// </summary>
        /// <param name="changed">Receives the changed part.</param>
        /// <returns>Ends the calls when disposed.</returns>
        public IDisposable Watch(Action<SnapshotSpan> changed)
        {
            IClassifier? watched = this.classifier;
            if (watched == null)
            {
                return new Subscription(() => { });
            }

            EventHandler<ClassificationChangedEventArgs> handler = (_, e) => changed(e.ChangeSpan);
            watched.ClassificationChanged += handler;
            return new Subscription(() => watched.ClassificationChanged -= handler);
        }

        /// <summary>Finds the label IDs in a line, in their order.</summary>
        /// <param name="line">The line.</param>
        /// <returns>The IDs with their place.</returns>
        public IReadOnlyList<RecognizedLabel> FindInLine(ITextSnapshotLine line)
        {
            var found = new List<RecognizedLabel>();
            if (this.classifier == null)
            {
                foreach (LabelIdMatch match in LabelId.FindAll(line.GetText()))
                {
                    found.Add(new RecognizedLabel(new SnapshotSpan(line.Snapshot, line.Start + match.Index, match.Length), match.Id));
                }

                return found;
            }

            foreach (ClassificationSpan span in this.classifier.GetClassificationSpans(line.Extent))
            {
                if (LabelClassifications.Any(name => span.ClassificationType.IsOfType(name)) && LabelId.TryParseToken(span.Span.GetText(), out LabelId id))
                {
                    found.Add(new RecognizedLabel(span.Span, id));
                }
            }

            return found;
        }
    }

    /// <summary>Ends a subscription once.</summary>
    internal sealed class Subscription : IDisposable
    {
        private Action? end;

        /// <summary>Creates the subscription.</summary>
        /// <param name="end">Ends it.</param>
        public Subscription(Action end)
        {
            this.end = end;
        }

        /// <inheritdoc />
        public void Dispose() => System.Threading.Interlocked.Exchange(ref this.end, null)?.Invoke();
    }

    /// <summary>A label ID found in the code.</summary>
    internal readonly struct RecognizedLabel
    {
        /// <summary>Creates the entry.</summary>
        /// <param name="span">Where the token stands.</param>
        /// <param name="id">The ID.</param>
        public RecognizedLabel(SnapshotSpan span, LabelId id)
        {
            this.Span = span;
            this.Id = id;
        }

        /// <summary>Where the token stands; for a classified token with its quote.</summary>
        public SnapshotSpan Span { get; }

        /// <summary>The ID.</summary>
        public LabelId Id { get; }
    }
}
