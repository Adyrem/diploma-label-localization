#if DEBUG
// The probes run on the main thread from start to end. The analyzer does not follow that
// into the lambdas passed to ProbeReport.Safe, which are called synchronously.
#pragma warning disable VSTHRD010

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Usages;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace BE.LabelExtension.Probes
{
    /// <summary>
    /// Probe for D1: tries to open an element and to put the cursor on a label ID in it.
    /// It reports which editor opened and what the designer offers for selecting a node,
    /// as the basis for the jump to a reference (FA04).
    /// </summary>
    [VisualStudioContribution]
    internal sealed class ProbeOpenElementCommand : Command
    {
        private const string Title = "Probe: Open element at a location";

        private static readonly Regex DeveloperToolsCommand = new("Dynamics|D365|Xpp|X\\+\\+|AOT|Label", RegexOptions.IgnoreCase);

        private readonly ErrorBoundary errorBoundary;
        private readonly IMessageSink sink;

        public ProbeOpenElementCommand(ErrorBoundary errorBoundary, IMessageSink sink)
        {
            this.errorBoundary = errorBoundary;
            this.sink = sink;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.ProbeOpenElementCommand.DisplayName%")
        {
            Icon = new(ImageMoniker.KnownValues.OpenFile, IconSettings.IconAndText),
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(Title, this.RunAsync, cancellationToken);

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            ShellExtensibility shell = this.Extensibility.Shell();

            string? strategy = await shell.ShowPromptAsync(
                "Probe: open an element. Choose a way:\n" +
                "1 = open the file in the code view\n" +
                "2 = open the file in the designer view\n" +
                "3 = open the file with DTE ItemOperations.OpenFile\n" +
                "4 = only list the Visual Studio commands of the Developer Tools",
                new InputPromptOptions { DefaultText = "1" },
                cancellationToken);
            strategy = strategy?.Trim();
            if (string.IsNullOrEmpty(strategy))
            {
                return;
            }

            if (strategy == "4")
            {
                await this.ListDeveloperToolsCommandsAsync(cancellationToken);
                return;
            }

            if (strategy != "1" && strategy != "2" && strategy != "3")
            {
                this.sink.Report(MessageSeverity.Warning, $"{Title}: unknown choice '{strategy}', expected 1, 2, 3 or 4.");
                return;
            }

            string? path = await shell.ShowPromptAsync(
                "Full path of the element's XML file, or of its .xpp file in XppSource:",
                new InputPromptOptions(),
                cancellationToken);
            path = path?.Trim().Trim('"');
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string? target = await shell.ShowPromptAsync(
                "Optional: label ID and occurrence to put the cursor on, for example \"@SYS12345 2\" for the second one. Leave empty to only open the element.",
                new InputPromptOptions(),
                cancellationToken);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var report = new ProbeReport($"{Title}, way {strategy}");
            report.Line($"file: {path} (exists: {File.Exists(path)})");

            IVsWindowFrame? frame = strategy == "3"
                ? await OpenWithDteAsync(path!, report)
                : OpenWithShell(path!, strategy == "2" ? VSConstants.LOGVIEWID.Designer_guid : VSConstants.LOGVIEWID.Code_guid, report);

            if (frame != null)
            {
                DescribeFrame(frame, report);
                if (!string.IsNullOrWhiteSpace(target))
                {
                    PlaceCaret(frame, target!, report);
                }
            }

            report.Send(this.sink);
        }

        private static IVsWindowFrame? OpenWithShell(string path, Guid logicalView, ProbeReport report)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, path, logicalView, out _, out _, out IVsWindowFrame frame);
                ErrorHandler.ThrowOnFailure(frame.Show());
                report.Line($"opened with VsShellUtilities.OpenDocument, logical view {logicalView}");
                return frame;
            }
            catch (Exception exception)
            {
                report.Line($"OpenDocument failed: {exception.GetType().Name}: {ProbeReport.Shorten(exception.Message, 200)}");
                return null;
            }
        }

        private static async Task<IVsWindowFrame?> OpenWithDteAsync(string path, ProbeReport report)
        {
            DTE2 dte = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SDTE, DTE2>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                EnvDTE.Window window = dte.ItemOperations.OpenFile(path, EnvDTE.Constants.vsViewKindPrimary);
                report.Line($"opened with DTE: caption '{ProbeReport.Safe(() => window.Caption)}', kind {ProbeReport.Safe(() => window.Kind)}, object kind {ProbeReport.Safe(() => window.ObjectKind)}, document {ProbeReport.Safe(() => window.Document?.FullName ?? "none")}");
            }
            catch (Exception exception)
            {
                report.Line($"DTE OpenFile failed: {exception.GetType().Name}: {ProbeReport.Shorten(exception.Message, 200)}");
                return null;
            }

            if (VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, path, Guid.Empty, out _, out _, out IVsWindowFrame frame))
            {
                return frame;
            }

            report.Line("no window frame registered for this file after DTE OpenFile");
            return null;
        }

        private static void DescribeFrame(IVsWindowFrame frame, ProbeReport report)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            frame.GetProperty((int)__VSFPROPID.VSFPROPID_Caption, out object caption);
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object moniker);
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszPhysicalView, out object physicalView);
            frame.GetGuidProperty((int)__VSFPROPID.VSFPROPID_guidEditorType, out Guid editorType);
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object docView);
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out object docData);

            report.Line($"window: caption '{caption}', moniker {moniker}, physical view '{physicalView}', editor type {editorType}");
            report.DescribeObject("document view", docView);
            report.DescribeObject("document data", docData);
            report.Line($"text view available: {VsShellUtilities.GetTextView(frame) != null}");
        }

        private static void PlaceCaret(IVsWindowFrame frame, string target, ProbeReport report)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string[] parts = target.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string labelId = parts[0];
            int occurrence = parts.Length > 1 && int.TryParse(parts[1], out int n) && n > 0 ? n : 1;

            IVsTextView? view = VsShellUtilities.GetTextView(frame);
            if (view == null)
            {
                report.Line("cursor: no text view in this window, so no cursor to place");
                return;
            }

            ErrorHandler.ThrowOnFailure(view.GetBuffer(out IVsTextLines lines));
            ErrorHandler.ThrowOnFailure(lines.GetLastLineIndex(out int lastLine, out int lastIndex));
            ErrorHandler.ThrowOnFailure(lines.GetLineText(0, 0, lastLine, lastIndex, out string text));

            IReadOnlyList<int> found = LabelIdOccurrences.Find(text, labelId);
            report.Line($"cursor: {found.Count} occurrences of {labelId} in the text view ({text.Length} characters)");
            if (found.Count < occurrence)
            {
                report.Line($"cursor: occurrence {occurrence} does not exist");
                return;
            }

            ErrorHandler.ThrowOnFailure(lines.GetLineIndexOfPosition(found[occurrence - 1], out int line, out int column));
            ErrorHandler.ThrowOnFailure(view.SetCaretPos(line, column));
            view.CenterLines(line, 1);
            report.Line($"cursor: placed on occurrence {occurrence} at line {line + 1}, column {column + 1}");
        }

        private async Task ListDeveloperToolsCommandsAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var report = new ProbeReport($"{Title}, commands of the Developer Tools");
            DTE2 dte = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SDTE, DTE2>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            List<string> names = dte.Commands
                .Cast<EnvDTE.Command>()
                .Select(c => ProbeReport.Safe(() => c.Name))
                .Where(n => !string.IsNullOrEmpty(n) && DeveloperToolsCommand.IsMatch(n))
                .Distinct()
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            report.Line($"{names.Count} commands match {DeveloperToolsCommand}:");
            foreach (string name in names)
            {
                report.Line($"  {name}");
            }

            report.Send(this.sink);
        }
    }
}
#endif
