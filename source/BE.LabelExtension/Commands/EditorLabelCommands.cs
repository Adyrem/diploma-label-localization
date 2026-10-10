using System;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.ToolWindow;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.Shell;

namespace BE.LabelExtension.Commands
{
    // The commands in the context menu of the X++ editor (concept, GUI design): Search label
    // (FA07) and Open in label window (FA08); Extract to label follows in AP3.8. They stand in
    // the group of Go To Definition and show only for .xpp files, which the X++ editor shows.
    // Their shortcuts, Ctrl+Shift+Alt with L and E, are free in Visual Studio 2026 like the
    // others of the extension. The configurations are evaluated when building and may call no
    // own methods, so the placement stands in each: guidSHLMainMenu and
    // IDG_VS_CODEWIN_NAVIGATETOLOCATION (0x02B1) of vsshlids.h.

    /// <summary>Reads the text at the cursor for the editor commands.</summary>
    internal static class EditorText
    {
        /// <summary>The line at the cursor and the position of the cursor in it.</summary>
        /// <param name="view">The text view.</param>
        /// <returns>Line and column, starting at 0.</returns>
        public static (string Line, int Column) LineAtCursor(ITextViewSnapshot view)
        {
            int caret = view.Selection.ActivePosition;
            ITextDocumentSnapshotLine line = view.Document.GetLineFromPosition(caret);
            return (Text(line.Text), caret - line.Text.Start.Offset);
        }

        /// <summary>The selected text, empty if nothing is selected.</summary>
        /// <param name="view">The text view.</param>
        /// <returns>The text.</returns>
        public static string Selected(ITextViewSnapshot view) => view.Selection.IsEmpty ? string.Empty : Text(view.Selection.Extent);

        private static string Text(TextRange range)
        {
            var characters = new char[range.Length];
            for (int i = 0; i < characters.Length; i++)
            {
                characters[i] = range[i];
            }

            return new string(characters);
        }
    }

    /// <summary>
    /// Opens the tool window and searches the selected text, without a selection the content of
    /// the string literal at the cursor (FA07).
    /// </summary>
    [VisualStudioContribution]
    internal sealed class SearchLabelCommand : Command
    {
        // A search term longer than this is no text of a label.
        private const int MaxLength = 250;

        private readonly LabelWindowController controller;
        private readonly ErrorBoundary errorBoundary;

        /// <summary>Creates the command.</summary>
        /// <param name="controller">Searches in the tool window.</param>
        /// <param name="errorBoundary">Reports errors of the command.</param>
        public SearchLabelCommand(LabelWindowController controller, ErrorBoundary errorBoundary)
        {
            this.controller = controller;
            this.errorBoundary = errorBoundary;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.SearchLabelCommand.DisplayName%")
        {
            Placements = [CommandPlacement.VsctParent(new Guid("d309f791-903f-11d0-9efc-00a0c911004f"), 0x02B1, 0x0100)],
            VisibleWhen = ActivationConstraint.ClientContext(ClientContextKey.Shell.ActiveEditorFileName, @"\.[xX][pP][pP]$"),
            Icon = new(ImageMoniker.KnownValues.Search, IconSettings.IconAndText),
            Shortcuts = [new CommandShortcutConfiguration(ModifierKey.ControlShiftLeftAlt, Key.L)],
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(
                "Search label",
                async token =>
                {
                    ITextViewSnapshot? view = await this.Extensibility.Editor().GetActiveTextViewAsync(context, token);
                    string? text = null;
                    if (view != null)
                    {
                        text = EditorText.Selected(view).Trim();
                        if (text.Length == 0)
                        {
                            (string line, int column) = EditorText.LineAtCursor(view);
                            text = CodeLine.StringLiteralAt(line, column)?.Trim();
                        }
                    }

                    if (string.IsNullOrEmpty(text) || text!.Length > MaxLength || text.IndexOf('\n') >= 0)
                    {
                        await this.Extensibility.Shell().ShowPromptAsync("Select a text in the code or place the cursor in a string, then search again.", PromptOptions.OK, token);
                        return;
                    }

                    await this.Extensibility.Shell().ShowToolWindowAsync<LabelToolWindow>(activate: true, token);
                    this.controller.SearchFor(text);
                },
                cancellationToken);
    }

    /// <summary>Opens the label ID at the cursor in the detail view of the tool window (FA08).</summary>
    [VisualStudioContribution]
    internal sealed class OpenInLabelWindowCommand : Command
    {
        private readonly LabelWindowController controller;
        private readonly ErrorBoundary errorBoundary;

        /// <summary>Creates the command.</summary>
        /// <param name="controller">Shows the label.</param>
        /// <param name="errorBoundary">Reports errors of the command.</param>
        public OpenInLabelWindowCommand(LabelWindowController controller, ErrorBoundary errorBoundary)
        {
            this.controller = controller;
            this.errorBoundary = errorBoundary;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.OpenInLabelWindowCommand.DisplayName%")
        {
            Placements = [CommandPlacement.VsctParent(new Guid("d309f791-903f-11d0-9efc-00a0c911004f"), 0x02B1, 0x0101)],
            VisibleWhen = ActivationConstraint.ClientContext(ClientContextKey.Shell.ActiveEditorFileName, @"\.[xX][pP][pP]$"),
            Icon = new(ImageMoniker.KnownValues.Localize, IconSettings.IconAndText),
            Shortcuts = [new CommandShortcutConfiguration(ModifierKey.ControlShiftLeftAlt, Key.E)],
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(
                "Open in label window",
                async token =>
                {
                    ITextViewSnapshot? view = await this.Extensibility.Editor().GetActiveTextViewAsync(context, token);
                    LabelId? id = null;
                    if (view != null)
                    {
                        string selected = EditorText.Selected(view).Trim().Trim('"', '\'');
                        if (LabelId.TryParse(selected, out LabelId parsed))
                        {
                            id = parsed;
                        }
                        else
                        {
                            (string line, int column) = EditorText.LineAtCursor(view);
                            id = CodeLine.LabelIdAt(line, column);
                        }
                    }

                    if (id == null)
                    {
                        await this.Extensibility.Shell().ShowPromptAsync("Place the cursor on a label ID in the code, then open it again.", PromptOptions.OK, token);
                        return;
                    }

                    await this.Extensibility.Shell().ShowToolWindowAsync<LabelToolWindow>(activate: true, token);
                    string? problem = this.controller.OpenLabel(id.Value);
                    if (problem != null)
                    {
                        await this.Extensibility.Shell().ShowPromptAsync(problem, PromptOptions.OK, token);
                    }
                },
                cancellationToken);
    }
}
