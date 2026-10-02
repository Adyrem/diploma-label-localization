#if DEBUG
// The probes run on the main thread from start to end. The analyzer does not follow that
// into the lambdas passed to ProbeReport.Safe, which are called synchronously.
#pragma warning disable VSTHRD010

using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BE.LabelExtension.Probes
{
    /// <summary>
    /// Probe for D1: lists the open documents and whether they have unsaved changes, once
    /// from the running document table and once from DTE. Shows whether an element opened
    /// in the designer or the X++ editor can be recognized as unsaved before the extension
    /// writes to its XML file.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class ProbeUnsavedDocumentsCommand : Command
    {
        private const string Title = "Probe: List unsaved documents";

        private readonly ErrorBoundary errorBoundary;
        private readonly IMessageSink sink;

        public ProbeUnsavedDocumentsCommand(ErrorBoundary errorBoundary, IMessageSink sink)
        {
            this.errorBoundary = errorBoundary;
            this.sink = sink;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.ProbeUnsavedDocumentsCommand.DisplayName%")
        {
            Icon = new(ImageMoniker.KnownValues.SaveAll, IconSettings.IconAndText),
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(Title, this.RunAsync, cancellationToken);

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var report = new ProbeReport(Title);
            report.Line("Running document table (state, moniker, document data, flags):");

            int count = 0;
            int unsaved = 0;
            foreach (RunningDocumentInfo info in new RunningDocumentTable(ServiceProvider.GlobalProvider))
            {
                count++;
                string state = DirtyState(info.DocData);
                if (state == "unsaved")
                {
                    unsaved++;
                }

                report.Line($"  {state,-8} {info.Moniker} | {ProbeReport.TypeName(info.DocData)} | 0x{info.Flags:X}");
            }

            report.Line($"  {count} documents, {unsaved} unsaved");

            report.Line("DTE documents (Saved, kind, full name):");
            DTE2 dte = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SDTE, DTE2>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            foreach (EnvDTE.Document document in dte.Documents)
            {
                report.Line($"  {ProbeReport.Safe(() => document.Saved.ToString()),-6} {ProbeReport.Safe(() => document.Kind)} {ProbeReport.Safe(() => document.FullName)}");
            }

            report.Send(this.sink);
        }

        private static string DirtyState(object? docData)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int dirty;
            if (docData is IVsPersistDocData persist && persist.IsDocDataDirty(out dirty) == VSConstants.S_OK)
            {
                return dirty != 0 ? "unsaved" : "saved";
            }

            if (docData is IPersistFileFormat file && file.IsDirty(out dirty) == VSConstants.S_OK)
            {
                return dirty != 0 ? "unsaved" : "saved";
            }

            return "unknown";
        }
    }
}
#endif
