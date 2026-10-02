using System;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Labels;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// Tool window of the extension with search, hit list, details and references
    /// (FA01 to FA04). Its content is Remote UI of the new model. Opening it starts loading
    /// the labels in the background.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class LabelToolWindow : Microsoft.VisualStudio.Extensibility.ToolWindows.ToolWindow
    {
        // Solution Explorer: the window opens as a tab next to it, narrow and docked at the side.
        private static readonly Guid SolutionExplorer = new("3AE79031-E1BC-11D0-8F78-00A0C9110057");

        private readonly LabelToolWindowData data = new();
        private readonly LabelLoader loader;
        private readonly LabelStore store;

        /// <summary>Creates the tool window.</summary>
        /// <param name="extensibility">Entry point to the Visual Studio extensibility API.</param>
        /// <param name="loader">Starts loading the labels.</param>
        /// <param name="store">The loaded labels.</param>
        public LabelToolWindow(VisualStudioExtensibility extensibility, LabelLoader loader, LabelStore store)
            : base(extensibility)
        {
            this.Title = "BE-LabelExtension";
            this.loader = loader;
            this.store = store;
            this.store.Changed += this.OnStoreChanged;
        }

        /// <inheritdoc />
        public override ToolWindowConfiguration ToolWindowConfiguration => new()
        {
            Placement = ToolWindowPlacement.DockedTo(SolutionExplorer),
        };

        /// <inheritdoc />
        public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken)
        {
            this.loader.EnsureLoaded();
            this.UpdateStatus();
            return Task.FromResult<IRemoteUserControl>(new LabelToolWindowContent(this.data));
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.store.Changed -= this.OnStoreChanged;
            }

            base.Dispose(disposing);
        }

        private void OnStoreChanged(object? sender, EventArgs e) => this.UpdateStatus();

        private void UpdateStatus()
        {
            this.data.StatusText = this.store.IsLoaded
                ? $"{this.store.Count} labels loaded from {this.store.LabelFiles.Count} label files."
                : "Labels are being loaded.";
        }
    }
}
