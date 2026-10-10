using System;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Labels;
using BE.LabelExtension.Threading;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// Tool window of the extension with search, hit list and details (FA01, FA03). Its
    /// content is Remote UI of the new model, its behaviour lies in
    /// <see cref="LabelWindowController"/>. Opening it starts loading the labels in the
    /// background.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class LabelToolWindow : Microsoft.VisualStudio.Extensibility.ToolWindows.ToolWindow
    {
        // Solution Explorer: the window opens as a tab next to it, narrow and docked at the side.
        private static readonly Guid SolutionExplorer = new("3AE79031-E1BC-11D0-8F78-00A0C9110057");

        private readonly LabelWindowController controller;

        /// <summary>Creates the tool window.</summary>
        /// <param name="extensibility">Entry point to the Visual Studio extensibility API.</param>
        /// <param name="loader">Starts loading the labels.</param>
        /// <param name="store">The loaded labels.</param>
        /// <param name="changes">The changes not yet saved.</param>
        /// <param name="errorBoundary">Reports errors of searching and saving.</param>
        /// <param name="messages">Receives problems with single inputs.</param>
        /// <param name="tasks">Runs the work nobody waits for.</param>
        public LabelToolWindow(VisualStudioExtensibility extensibility, LabelLoader loader, LabelStore store, LabelChanges changes, ErrorBoundary errorBoundary, IMessageSink messages, ExtensionTasks tasks)
            : base(extensibility)
        {
            this.Title = "BE-LabelExtension";
            this.controller = new LabelWindowController(store, loader, changes, errorBoundary, messages, tasks);
        }

        /// <inheritdoc />
        public override ToolWindowConfiguration ToolWindowConfiguration => new()
        {
            Placement = ToolWindowPlacement.DockedTo(SolutionExplorer),
        };

        /// <inheritdoc />
        public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken)
        {
            this.controller.Open();
            return Task.FromResult<IRemoteUserControl>(new LabelToolWindowContent(this.controller.Data));
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.controller.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
