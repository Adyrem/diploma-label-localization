using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BE.LabelExtension.Settings
{
    /// <summary>Opens the options page of the extension, for the button Settings of the tool window.</summary>
    internal static class OptionsPage
    {
        /// <summary>
        /// Opens Tools, Options at BE-LabelExtension, General. The page belongs to the classic
        /// package, which loads first if it has not yet.
        /// </summary>
        /// <param name="cancellationToken">Cancels before the page opens.</param>
        /// <returns>A task that completes when the dialog is closed.</returns>
        public static async Task ShowAsync(CancellationToken cancellationToken)
        {
            IVsShell shell = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsShell, IVsShell>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var guid = new Guid(LabelExtensionPackage.PackageGuidString);
            if (shell.IsPackageLoaded(ref guid, out IVsPackage? package) != VSConstants.S_OK || package == null)
            {
                ErrorHandler.ThrowOnFailure(shell.LoadPackage(ref guid, out package));
            }

            ((Package)package).ShowOptionPage(typeof(LabelOptionsPage));
        }
    }
}
