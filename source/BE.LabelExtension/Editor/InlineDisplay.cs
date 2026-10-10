using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.TextFormatting;
using BE.LabelExtension.Core.Display;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace BE.LabelExtension.Editor
{
    /// <summary>
    /// Inline display (FA06): above every line of code with a label ID, the translations in
    /// every loaded language, missing ones marked. It can be switched off in the settings.
    /// </summary>
    /// <remarks>
    /// The space above the line comes from a <see cref="SpaceNegotiatingAdornmentTag"/>, the
    /// technique the Developer Tools use for their reference display (findings log, CodeLens on
    /// X++). The tag has no width and stands at the start of the line, so the text keeps its
    /// place. An adornment layer draws the translations into the space.
    /// </remarks>
    internal sealed class InlineDisplay : ITagger<SpaceNegotiatingAdornmentTag>
    {
        /// <summary>Name of the adornment layer.</summary>
        public const string LayerName = "BE-LabelExtension Inline Display";

        // Size of the display relative to the text of the editor.
        private const double Scale = 0.85;

        private readonly IWpfTextView view;
        private readonly LabelRecognizer recognizer;
        private readonly LabelLookup lookup;
        private readonly IClassificationFormatMap formats;
        private readonly IAdornmentLayer layer;
        private readonly IDisposable watch;

        public InlineDisplay(IWpfTextView view, LabelRecognizer recognizer, LabelLookup lookup, IClassificationFormatMap formats)
        {
            this.view = view;
            this.recognizer = recognizer;
            this.lookup = lookup;
            this.formats = formats;
            this.layer = view.GetAdornmentLayer(LayerName);
            this.view.LayoutChanged += this.OnLayoutChanged;
            this.view.Closed += this.OnClosed;
            this.lookup.Changed += this.OnLabelsChanged;
            this.formats.ClassificationFormatMappingChanged += this.OnLabelsChanged;
            this.view.TextBuffer.Changed += this.OnTextChanged;
            this.watch = recognizer.Watch(this.RaiseForLines);
            this.lookup.EnsureLoaded();
        }

        /// <inheritdoc />
        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        // The font of the editor text, known before the first layout, unlike the line height of the view.
        private TextRunProperties Text => this.formats.DefaultTextProperties;

        private double FontSize => this.Text.FontRenderingEmSize * Scale;

        private double TopSpace => Math.Ceiling(this.FontSize * this.Text.Typeface.FontFamily.LineSpacing);

        /// <inheritdoc />
        public IEnumerable<ITagSpan<SpaceNegotiatingAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || !this.lookup.InlineDisplay)
            {
                yield break;
            }

            ITextSnapshot snapshot = spans[0].Snapshot;
            int done = -1;
            foreach (SnapshotSpan span in spans)
            {
                int first = Math.Max(snapshot.GetLineNumberFromPosition(span.Start), done + 1);
                int last = snapshot.GetLineNumberFromPosition(span.End);
                for (int number = first; number <= last; number++)
                {
                    ITextSnapshotLine line = snapshot.GetLineFromLineNumber(number);
                    if (this.Parts(line) != null)
                    {
                        var tag = new SpaceNegotiatingAdornmentTag(0, this.TopSpace, 0, 0, 0, PositionAffinity.Successor, null, this);
                        yield return new TagSpan<SpaceNegotiatingAdornmentTag>(new SnapshotSpan(line.Start, 0), tag);
                    }
                }

                done = Math.Max(done, last);
            }
        }

        // What to show above a line, or null for nothing.
        private IReadOnlyList<InlinePart>? Parts(ITextSnapshotLine line)
        {
            if (!this.lookup.InlineDisplay)
            {
                return null;
            }

            IReadOnlyList<RecognizedLabel> labels = this.recognizer.FindInLine(line);
            if (labels.Count == 0)
            {
                return null;
            }

            IReadOnlyList<LabelSummary>? summaries = this.lookup.Summarize(labels.Select(l => l.Id).ToList());
            return summaries == null ? null : LabelSummary.Inline(summaries);
        }

        private void OnLayoutChanged(object? sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (ITextViewLine line in e.NewOrReformattedLines)
            {
                this.Draw(line);
            }
        }

        // Only the first view line of a code line gets the display, also with word wrap.
        private void Draw(ITextViewLine line)
        {
            this.layer.RemoveMatchingAdornments(line.Extent, element => element.Tag == this);
            if (!line.IsFirstTextViewLineForSnapshotLine)
            {
                return;
            }

            ITextSnapshotLine code = line.Start.GetContainingLine();
            IReadOnlyList<InlinePart>? parts = this.Parts(code);
            if (parts == null)
            {
                return;
            }

            TextRunProperties text = this.Text;
            var block = new TextBlock
            {
                FontFamily = text.Typeface.FontFamily,
                FontSize = this.FontSize,
                Foreground = text.ForegroundBrush,
                Opacity = 0.6,
                IsHitTestVisible = false,
            };
            foreach (InlinePart part in parts)
            {
                block.Inlines.Add(new Run(part.Text) { FontStyle = part.IsMarked ? FontStyles.Italic : FontStyles.Normal });
            }

            // Above the first character of the code, like the reference display.
            string content = code.GetText();
            int indent = content.Length - content.TrimStart().Length;
            double left = indent < content.Length
                ? line.GetCharacterBounds(new SnapshotPoint(code.Snapshot, code.Start + indent)).Left
                : line.TextLeft;
            Canvas.SetLeft(block, left);
            Canvas.SetTop(block, line.Top);
            this.layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, line.Extent, this, block, null);
        }

        // The store and the settings report from any thread; tags change on the main thread.
        private void OnLabelsChanged(object? sender, EventArgs e)
        {
            this.lookup.RunOnMainThread(() =>
            {
                if (!this.view.IsClosed)
                {
                    ITextSnapshot snapshot = this.view.TextSnapshot;
                    this.RaiseTagsChanged(new SnapshotSpan(snapshot, 0, snapshot.Length));
                }
            });
        }

        private void RaiseTagsChanged(SnapshotSpan span) => this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));

        // The tag stands at the start of the line, but the editor asks only for the changed part
        // of a line after an edit or a new classification. Whole lines make it ask for the start.
        private void RaiseForLines(SnapshotSpan span)
        {
            ITextSnapshotLine first = span.Start.GetContainingLine();
            ITextSnapshotLine last = span.End.GetContainingLine();
            this.RaiseTagsChanged(new SnapshotSpan(first.Start, last.EndIncludingLineBreak));
        }

        private void OnTextChanged(object? sender, TextContentChangedEventArgs e)
        {
            foreach (ITextChange change in e.Changes)
            {
                this.RaiseForLines(new SnapshotSpan(e.After, change.NewSpan));
            }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            this.view.LayoutChanged -= this.OnLayoutChanged;
            this.view.Closed -= this.OnClosed;
            this.lookup.Changed -= this.OnLabelsChanged;
            this.formats.ClassificationFormatMappingChanged -= this.OnLabelsChanged;
            this.view.TextBuffer.Changed -= this.OnTextChanged;
            this.watch.Dispose();
        }
    }

    /// <summary>Creates one inline display per view, for the tagger and the view listener.</summary>
    [Export(typeof(InlineDisplayFactory))]
    internal sealed class InlineDisplayFactory
    {
        private readonly IViewClassifierAggregatorService classifiers;
        private readonly IClassificationTypeRegistryService registry;
        private readonly IClassificationFormatMapService formats;
        private readonly LabelLookup lookup;

        [ImportingConstructor]
        public InlineDisplayFactory(IViewClassifierAggregatorService classifiers, IClassificationTypeRegistryService registry, IClassificationFormatMapService formats, LabelLookup lookup)
        {
            this.classifiers = classifiers;
            this.registry = registry;
            this.formats = formats;
            this.lookup = lookup;
        }

        /// <summary>The inline display of a view, created on first use.</summary>
        /// <param name="view">The view.</param>
        /// <returns>The display, or <c>null</c> if the view shows no labels.</returns>
        public InlineDisplay? GetOrCreate(IWpfTextView view)
        {
            if (view.Properties.TryGetProperty(typeof(InlineDisplay), out InlineDisplay existing))
            {
                return existing;
            }

            LabelRecognizer? recognizer = LabelRecognizer.TryCreate(view, this.classifiers, this.registry);
            if (recognizer == null)
            {
                return null;
            }

            var display = new InlineDisplay(view, recognizer, this.lookup, this.formats.GetClassificationFormatMap(view));
            view.Properties.AddProperty(typeof(InlineDisplay), display);
            return display;
        }
    }

    /// <summary>Gives the editor the space above lines with labels.</summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(LabelRecognizer.XppContentType)]
#if DEBUG
    [ContentType("text")]
#endif
    [TagType(typeof(SpaceNegotiatingAdornmentTag))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class InlineDisplayTaggerProvider : IViewTaggerProvider
    {
        private readonly InlineDisplayFactory factory;

        [ImportingConstructor]
        public InlineDisplayTaggerProvider(InlineDisplayFactory factory)
        {
            this.factory = factory;
        }

        /// <inheritdoc />
        public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer)
            where T : ITag
            => textView is IWpfTextView view && buffer == view.TextBuffer ? this.factory.GetOrCreate(view) as ITagger<T> : null;
    }

    /// <summary>Attaches the inline display to every view of X++ code, and defines its layer.</summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(LabelRecognizer.XppContentType)]
#if DEBUG
    [ContentType("text")]
#endif
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class InlineDisplayViewListener : IWpfTextViewCreationListener
    {
#pragma warning disable CS0169, CS0649, IDE0044, IDE0051 // Read by MEF only.
        [Export(typeof(AdornmentLayerDefinition))]
        [Name(InlineDisplay.LayerName)]
        [Order(After = PredefinedAdornmentLayers.Text)]
        private AdornmentLayerDefinition? layer;
#pragma warning restore CS0169, CS0649, IDE0044, IDE0051

        private readonly InlineDisplayFactory factory;

        [ImportingConstructor]
        public InlineDisplayViewListener(InlineDisplayFactory factory)
        {
            this.factory = factory;
        }

        /// <inheritdoc />
        public void TextViewCreated(IWpfTextView textView) => this.factory.GetOrCreate(textView);
    }
}
