using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// Tool window of the extension with search, hit list and details (FA01, FA03). Its
    /// content is Remote UI of the new model, its behaviour lies in
    /// <see cref="LabelWindowController"/>, which outlives the window, so search and detail
    /// stay when it is closed and opened again. Opening it starts loading the labels in the
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
        /// <param name="controller">The behaviour, shared with the shortcuts.</param>
        public LabelToolWindow(VisualStudioExtensibility extensibility, LabelWindowController controller)
            : base(extensibility)
        {
            this.Title = "BE-LabelExtension";
            this.controller = controller;
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
    }
}
