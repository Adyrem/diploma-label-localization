using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Commands;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Elements;
using BE.LabelExtension.Core.Files;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Core.TranslationServices;
using BE.LabelExtension.Elements;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.RpcContracts.Documents;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// The extraction of hardcoded texts (FA09, UC08) and the suggested translations of a new
    /// label (FA10, UC02).
    /// </summary>
    /// <remarks>
    /// <para>The extraction opens a new label with the text, in a label file of the model the
    /// code or element belongs to. Only creating the label replaces the text: the string
    /// literal in the X++ editor, as one edit the editor can undo, or the property in the XML
    /// file of the element, which the designer then offers to reload.</para>
    /// <para>Suggestions replace a text of the new label only while the developer has not
    /// changed it, and stand in italics until the developer changes them or creates the label.
    /// The Output Window names the languages; there is no hint per row and no prompt (D3).</para>
    /// </remarks>
    internal sealed partial class LabelWindowController
    {
        // The text replaced by the ID once the new label is created.
        private PendingExtraction? extraction;

        // Counts the new labels shown, so a late answer of the service finds its own.
        private int draftGeneration;

        // While suggestions are entered, the change of a text is no edit of the developer.
        private bool applyingSuggestion;

        /// <summary>
        /// Extracts a hardcoded text into a new label (FA09): in the X++ editor the string
        /// literal at the cursor, in the designer a label property of the selected node.
        /// </summary>
        /// <param name="context">The state of Visual Studio when the command ran.</param>
        /// <param name="cancellationToken">Cancels the action.</param>
        /// <returns>A task that completes when the new label is shown or a hint explains why not.</returns>
        public Task ExtractAsync(IClientContext context, CancellationToken cancellationToken)
            => this.RunAsync(
                "Extract to label",
                async token =>
                {
                    if (!this.store.IsLoaded)
                    {
                        await this.ShowMessageAsync("Labels are being loaded. Extract the text when they are loaded.", token);
                        return;
                    }

                    // The active document decides: the designer shows the XML file of an element.
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                    DesignerSelection? selection = DesignerSelection.Current;
                    string? document = selection?.ActiveDocument();
                    DesignerNode? node = document != null && document.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ? selection!.Read() : null;
                    await TaskScheduler.Default;

                    if (node != null)
                    {
                        await this.ExtractFromDesignerAsync(node, null, token);
                    }
                    else if (document != null && document.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        await this.ShowMessageAsync("Select a node in the designer, such as a field, then extract again.", token);
                    }
                    else
                    {
                        await this.ExtractFromEditorAsync(context, token);
                    }
                },
                cancellationToken);

        private Task TranslateAsync()
        {
            this.SuggestTranslations();
            return Task.CompletedTask;
        }

        // UC08 with the X++ editor: the string literal at the cursor or the selection.
        private async Task ExtractFromEditorAsync(IClientContext context, CancellationToken cancellationToken)
        {
            ITextViewSnapshot? view = await this.extensibility.Editor().GetActiveTextViewAsync(context, cancellationToken);
            if (view == null)
            {
                await this.ShowMessageAsync("Place the cursor in a string in the X++ editor, or select a node in the designer, then extract again.", cancellationToken);
                return;
            }

            // The cursor, otherwise the inside of a selection that includes the quotes.
            var positions = new List<int> { view.Selection.ActivePosition };
            if (!view.Selection.IsEmpty)
            {
                positions.Add(view.Selection.Extent.Start.Offset + 1);
                positions.Add(view.Selection.Extent.End.Offset - 1);
            }

            StringLiteral? literal = null;
            int lineStart = 0;
            foreach (int position in positions.Where(p => p >= 0 && p <= view.Document.Length))
            {
                ITextDocumentSnapshotLine line = view.Document.GetLineFromPosition(position);
                lineStart = line.Text.Start.Offset;
                literal = CodeLine.StringLiteralSpanAt(EditorText.Text(line.Text), position - lineStart);
                if (literal != null)
                {
                    break;
                }
            }

            string? problem = literal == null ? "There is no string at the cursor. Place the cursor in a string, or select it, then extract again."
                : literal.Text == null ? "The string holds a line break, which a label cannot hold."
                : literal.Text.Trim().Length == 0 ? "The string is empty."
                : LabelId.TryParse(literal.Text.Trim(), out _) ? $"The string is already the label ID {literal.Text.Trim()}."
                : null;
            if (problem != null)
            {
                await this.ShowMessageAsync(problem, cancellationToken);
                return;
            }

            var range = new TrackingTextRange(view.Document, lineStart + literal!.Start, literal.Length, TextRangeTrackingMode.ExtendNone);
            string token = EditorText.Text(new TextRange(view.Document, lineStart + literal.Start, literal.Length));
            ModelInfo? model = ModelDiscovery.FindModelFor(view.FilePath ?? string.Empty, this.store.Models);
            await this.ShowExtractionAsync(new EditorExtraction(view.Document.Uri, Path.GetFileName(view.FilePath), range, token, literal), model, literal.Text!, cancellationToken);
        }

        // UC08 1a with the designer: a label property of the selected node, chosen by the
        // developer unless the area of the designer node already named it (KA05).
        private async Task ExtractFromDesignerAsync(DesignerNode node, DesignerProperty? preselected, CancellationToken cancellationToken)
        {
            ModelInfo? model = ModelDiscovery.FindModelFor(node.DocumentPath, this.store.Models);
            if (model == null || model.IsReadOnly || ElementDocuments.ElementFile(node.DocumentPath, new[] { model }) == null)
            {
                await this.ShowMessageAsync($"{Path.GetFileNameWithoutExtension(node.DocumentPath)} is no element of a writable model, so its properties stay as they are.", cancellationToken);
                return;
            }

            // Texts that are label IDs already are no hardcoded texts.
            List<DesignerProperty> texts = node.Properties
                .Where(p => !LabelId.TryParse(p.Value.Trim(), out _) && this.store.Find(p.Value.Trim()) == null && p.Value.IndexOf('\n') < 0)
                .ToList();
            if (texts.Count == 0)
            {
                await this.ShowMessageAsync($"No label property with a hardcoded text, such as Label or Help Text, was found on {node.Name}.", cancellationToken);
                return;
            }

            DesignerProperty? chosen = preselected == null ? null : texts.FirstOrDefault(p => p.Name == preselected.Name && p.Value == preselected.Value);
            if (chosen == null)
            {
                // The prompt returns values, so it returns the position in the list, -1 for Cancel.
                var choices = new ChoiceResultCollection<int>(texts
                    .Select((p, i) => new KeyValuePair<ChoiceDescription, int>(Shorten($"{p.Name}: {p.Value}", 70), i))
                    .Append(new KeyValuePair<ChoiceDescription, int>("Cancel", -1)));
                int index = await this.extensibility.Shell().ShowPromptAsync(
                    $"Which property of {node.Name} becomes a new label?",
                    new PromptOptions<int>(choices, 0, -1),
                    cancellationToken);
                if (index < 0 || index >= texts.Count)
                {
                    return;
                }

                chosen = texts[index];
            }

            // The designer may show a text not yet saved; the XML file must hold it (B25).
            if (!await this.EnsureElementSavedAsync(node.DocumentPath, model, cancellationToken))
            {
                return;
            }

            await this.ShowExtractionAsync(new DesignerExtraction(node.DocumentPath, node.Name, chosen.Name, chosen.Value), model, chosen.Value, cancellationToken);
        }

        // Step 3 of UC08: the new label with the text, in a label file of the model if it has one.
        private async Task ShowExtractionAsync(PendingExtraction pending, ModelInfo? model, string text, CancellationToken cancellationToken)
        {
            // The command can come before the tool window has opened.
            if (this.languages.Count == 0)
            {
                this.UpdateLanguages();
                this.UpdateLabelFiles();
            }

            IReadOnlyList<LabelFile> files = this.changes.CreatableLabelFiles();
            LabelFile? selected = this.SelectedLabelFile();
            LabelFile? file = model == null ? null
                : selected != null && ReferenceEquals(selected.Model, model) ? selected
                : files.FirstOrDefault(f => ReferenceEquals(f.Model, model));
            if (file != null)
            {
                this.SelectLabelFile(file);
            }
            else
            {
                file = selected;
            }

            if (file == null)
            {
                await this.ShowMessageAsync("There is no label file to create labels in. New labels go into label files of writable models.", cancellationToken);
                return;
            }

            await this.extensibility.Shell().ShowToolWindowAsync<LabelToolWindow>(activate: true, cancellationToken);
            this.extraction = pending;
            this.ShowDraft(file, text.Trim(), null);
            this.Data.StatusText = model != null && ReferenceEquals(file.Model, model)
                ? $"New label for {pending.Source}. Check the texts, then create it."
                : $"Model {model?.Name ?? "of the file"} has no label file to create labels in; choose one, then create the label.";
            this.SuggestTranslations();
        }

        // Step 5 of UC08, after the label is created, or with a label of the same text (KA08).
        private async Task CompleteExtractionAsync(PendingExtraction pending, Label label, bool created, CancellationToken cancellationToken)
        {
            string done = $"{label.Id.FullId} {(created ? "is created" : "has the same text and is used")}";
            string result = pending switch
            {
                EditorExtraction editor => await this.ReplaceInEditorAsync(editor, label, done, cancellationToken),
                DesignerExtraction designer => await this.ReplaceInElementAsync(designer, label, done, cancellationToken),
                _ => string.Empty,
            };

            this.Data.StatusText = result;
            this.messages.Report(MessageSeverity.Message, result);
        }

        private async Task<string> ReplaceInEditorAsync(EditorExtraction pending, Label label, string done, CancellationToken cancellationToken)
        {
            string id = label.Id.FullId;
            DocumentSnapshot? document = await this.extensibility.Documents().GetOpenDocumentAsync(pending.Document, cancellationToken);
            if (document == null)
            {
                return $"{done}. {pending.FileName} is no longer open, so the string stays; insert the ID with Insert.";
            }

            ITextDocumentSnapshot? text = await document.AsTextDocumentAsync(this.extensibility, cancellationToken);
            if (text == null)
            {
                return $"{done}. {pending.FileName} is no longer open as text, so the string stays; insert the ID with Insert.";
            }

            TextRange range = pending.Range.TranslateTo(text).Range;
            if (EditorText.Text(range) != pending.Token)
            {
                return $"{done}. The string in {pending.FileName} changed meanwhile, so it stays; insert the ID with Insert.";
            }

            await this.extensibility.Editor().EditAsync(batch => text.AsEditable(batch).Replace(range, pending.Literal.Replacement(id)), cancellationToken);
            return $"{done}. It stands in {pending.FileName} in place of the string. Undo in the editor brings the string back.";
        }

        private async Task<string> ReplaceInElementAsync(DesignerExtraction pending, Label label, string done, CancellationToken cancellationToken)
        {
            string id = label.Id.FullId;
            string element = Path.GetFileNameWithoutExtension(pending.XmlPath);
            ModelInfo? model = ModelDiscovery.FindModelFor(pending.XmlPath, this.store.Models);
            if (model == null || !await this.EnsureElementSavedAsync(pending.XmlPath, model, cancellationToken))
            {
                return $"{done}. {element} has unsaved changes, so {pending.Property} of {pending.Node} stays; set it to {id} in the Properties window.";
            }

            (string xml, Encoding encoding) = await Task.Run(() => TextFile.Read(pending.XmlPath), cancellationToken);
            if (!ElementPropertyEditor.TryReplaceValue(xml, pending.Node, pending.Property, pending.Value, id, out string changed, out string problem))
            {
                return $"{done}, but {pending.Property} of {pending.Node} in {element} stays: {problem}.";
            }

            await Task.Run(() => TextFile.Write(pending.XmlPath, changed, encoding), cancellationToken);
            this.UpdateDesignerProperty(pending.XmlPath, pending.Node, pending.Property, id);
            return $"{done}. It is now {pending.Property} of {pending.Node} in {element}. The designer offers to reload the element.";
        }

        // Saving only this element, if the developer agrees (UC08 1a, B25).
        private async Task<bool> EnsureElementSavedAsync(string elementFile, ModelInfo model, CancellationToken cancellationToken)
        {
            List<UnsavedElement> unsaved = (await UnsavedElements.FindAsync(new[] { model }, cancellationToken))
                .Where(u => string.Equals(u.ElementFile, elementFile, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (unsaved.Count == 0)
            {
                return true;
            }

            var choices = new ChoiceResultCollection<bool>(new[]
            {
                new KeyValuePair<ChoiceDescription, bool>("Save and continue", true),
                new KeyValuePair<ChoiceDescription, bool>("Cancel", false),
            });
            string question = $"The element {Path.GetFileNameWithoutExtension(elementFile)} has unsaved changes. The extension changes element files only when they are saved, so that saving them later does not undo the change.";
            if (!await this.extensibility.Shell().ShowPromptAsync(question, new PromptOptions<bool>(choices, 0, false), cancellationToken))
            {
                return false;
            }

            await UnsavedElements.SaveAsync(unsaved, cancellationToken);
            return (await UnsavedElements.FindAsync(new[] { model }, cancellationToken)).All(u => !string.Equals(u.ElementFile, elementFile, StringComparison.OrdinalIgnoreCase));
        }

        // UC02 step 3: the text in the source language goes to the service, the suggestions
        // replace the other languages. Without a key or with an error, every language keeps the
        // text and a message names the reason (3a).
        private void SuggestTranslations()
        {
            if (!this.isDraft)
            {
                return;
            }

            LabelSettings settings = this.loader.Settings;
            IReadOnlyList<TranslationRow> rows = this.Data.Detail.Translations;
            // The text stands in the source language, also where the label file lacks it.
            string source = settings.SourceLanguage ?? rows.FirstOrDefault()?.Language ?? string.Empty;
            TranslationRow? sourceRow = rows.FirstOrDefault(r => string.Equals(r.Language, source, StringComparison.OrdinalIgnoreCase)) ?? rows.FirstOrDefault();
            string text = sourceRow?.Text.Trim() ?? string.Empty;
            List<TranslationRow> targets = rows.Where(r => r.HasFile && !string.Equals(r.Language, source, StringComparison.OrdinalIgnoreCase)).ToList();
            if (text.Length == 0 || targets.Count == 0)
            {
                return;
            }

            ITranslationService? service = this.translators.Find(settings.TranslationService);
            string? missing = service == null ? $"the translation service {settings.TranslationService} of the settings is not known"
                : !service.IsConfigured ? $"no API key for {service.Name} is set in the options (Tools > Options > BE-LabelExtension)"
                : null;
            if (missing != null)
            {
                this.ReportNoSuggestions(missing, MessageSeverity.Message);
                return;
            }

            int generation = this.draftGeneration;
            var requested = targets.ToDictionary(r => r, r => r.Text);
            this.Data.StatusText = $"{service!.Name} translates the text into {targets.Count} languages.";
            _ = this.tasks.Factory.RunAsync(() => this.errorBoundary.RunAsync(
                "Suggest translations",
                async token =>
                {
                    TranslationResult result = await service.TranslateAsync(text, source, targets.Select(r => r.Language).ToList(), token);
                    if (generation != this.draftGeneration || !this.isDraft)
                    {
                        return;
                    }

                    if (!result.IsSuccess)
                    {
                        this.ReportNoSuggestions(result.Error!, MessageSeverity.Warning);
                        return;
                    }

                    var applied = new List<string>();
                    this.applyingSuggestion = true;
                    try
                    {
                        foreach (KeyValuePair<TranslationRow, string> target in requested)
                        {
                            // A text the developer changed meanwhile stays.
                            if (target.Key.Text == target.Value && result.Translations.TryGetValue(target.Key.Language, out string? suggestion))
                            {
                                target.Key.Text = suggestion;
                                target.Key.IsSuggested = true;
                                applied.Add(target.Key.Language);
                            }
                        }
                    }
                    finally
                    {
                        this.applyingSuggestion = false;
                    }

                    string suggested = applied.Count == 1 ? "1 translation" : $"{applied.Count} translations";
                    this.Data.StatusText = $"{service.Name} suggested {suggested}, shown in italics. Check the texts, then create the label.";
                    if (applied.Count > 0)
                    {
                        this.messages.Report(MessageSeverity.Message, $"{service.Name} suggested {suggested} for the new label: {string.Join(", ", applied)}.");
                    }
                },
                this.tasks.ShutdownToken));
        }

        private void ReportNoSuggestions(string reason, MessageSeverity severity)
        {
            string message = $"No translations suggested: {reason}. The new label keeps the text in every language.";
            this.Data.StatusText = message;
            this.messages.Report(severity, message);
        }

        // A suggestion the developer changes is the developer's text.
        private void OnDraftEdited(TranslationRow row, string? propertyName)
        {
            if (!this.applyingSuggestion && row.IsSuggested && propertyName == nameof(TranslationRow.Text))
            {
                row.IsSuggested = false;
            }
        }

        private void SelectLabelFile(LabelFile file)
        {
            int index;
            lock (this.gate)
            {
                index = IndexOfFile(this.labelFiles, file);
            }

            if (index >= 0)
            {
                this.Data.SelectedLabelFileIndex = index;
            }
        }

        private static string Shorten(string text, int length) => text.Length <= length ? text : text.Substring(0, length - 1) + "…";

        /// <summary>A text that becomes a label ID once the new label is created.</summary>
        private abstract class PendingExtraction
        {
            /// <summary>Where the text stands, for the status line.</summary>
            public abstract string Source { get; }

            /// <summary>What creating the label changes, for the detail view.</summary>
            public abstract string Description { get; }
        }

        /// <summary>A string literal in the X++ editor.</summary>
        private sealed class EditorExtraction : PendingExtraction
        {
            public EditorExtraction(Uri document, string fileName, TrackingTextRange range, string token, StringLiteral literal)
            {
                this.Document = document;
                this.FileName = fileName;
                this.Range = range;
                this.Token = token;
                this.Literal = literal;
            }

            public Uri Document { get; }

            public string FileName { get; }

            // Follows the literal while the developer edits the code around it.
            public TrackingTextRange Range { get; }

            // The literal as it stood, so a changed literal is left alone.
            public string Token { get; }

            public StringLiteral Literal { get; }

            public override string Source => $"the string in {this.FileName}";

            public override string Description => $"Creating it replaces the string in {this.FileName} by its ID.";
        }

        /// <summary>A label property of a node in the designer.</summary>
        private sealed class DesignerExtraction : PendingExtraction
        {
            public DesignerExtraction(string xmlPath, string node, string property, string value)
            {
                this.XmlPath = xmlPath;
                this.Node = node;
                this.Property = property;
                this.Value = value;
            }

            public string XmlPath { get; }

            public string Node { get; }

            public string Property { get; }

            public string Value { get; }

            public override string Source => $"{this.Property} of {this.Node}";

            public override string Description => $"Creating it sets {this.Property} of {this.Node} in {Path.GetFileNameWithoutExtension(this.XmlPath)} to its ID.";
        }
    }
}
