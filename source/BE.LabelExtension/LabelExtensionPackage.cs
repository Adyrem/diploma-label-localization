using System;
using System.Runtime.InteropServices;
using System.Threading;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Elements;
using BE.LabelExtension.Settings;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace BE.LabelExtension
{
    /// <summary>
    /// The classic part of the extension (F5): the options page (FA11), the question about
    /// unsaved labels when Visual Studio closes (fachliche Regel Speichern) and following the
    /// selection of the designer (FA09). Everything else runs in the new model, see
    /// <see cref="LabelExtension"/>.
    /// </summary>
    /// <remarks>
    /// The package loads in the background once the shell is ready, so that it can answer
    /// QueryClose. While loading it only registers for the selection events.
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideOptionPage(typeof(LabelOptionsPage), "BE-LabelExtension", "General", 0, 0, true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class LabelExtensionPackage : AsyncPackage
    {
        /// <summary>The GUID of the package.</summary>
        public const string PackageGuidString = "8c626a28-d716-4b1d-878a-c55b3f05c235";

        // Results of a message box.
        private const int IdYes = 6;
        private const int IdNo = 7;

        /// <inheritdoc />
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // The designer selection is followed from the start, so the extraction knows the
            // node selected before the tool window opened (FA09).
            await DesignerSelection.StartAsync(this, cancellationToken);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            // Visual Studio disposes the package on the main thread when it closes; elsewhere
            // the registration simply ends with the process.
            if (disposing && ThreadHelper.CheckAccess())
            {
#pragma warning disable VSTHRD010 // Checked with CheckAccess just above.
                DesignerSelection.Stop();
#pragma warning restore VSTHRD010
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Asks whether to save labels with changes not yet saved before Visual Studio closes.
        /// Yes saves, No closes without saving, Cancel keeps Visual Studio open. If saving
        /// fails, Visual Studio stays open and the Output Window names the files.
        /// </summary>
        /// <param name="canClose">Whether Visual Studio may close.</param>
        /// <returns><see cref="VSConstants.S_OK"/>.</returns>
        protected override int QueryClose(out bool canClose)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            canClose = true;
            LabelChanges? changes = SharedServices.Changes;
            int count = changes?.Count ?? 0;
            if (changes == null || count == 0)
            {
                return VSConstants.S_OK;
            }

            int answer = VsShellUtilities.ShowMessageBox(
                this,
                (count == 1 ? "1 label has" : $"{count} labels have") + " changes that are not saved. Save them before closing?",
                "BE-LabelExtension",
                OLEMSGICON.OLEMSGICON_QUERY,
                OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            if (answer == IdYes)
            {
                changes.Save();
                if (changes.Count > 0)
                {
                    VsShellUtilities.ShowMessageBox(
                        this,
                        "Some labels could not be saved. The Output Window, pane BE-LabelExtension, names the files.",
                        "BE-LabelExtension",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    canClose = false;
                }
            }
            else if (answer != IdNo)
            {
                canClose = false;
            }

            return VSConstants.S_OK;
        }
    }
}
