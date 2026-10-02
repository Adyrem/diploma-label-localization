using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// Tool window of the extension with search, hit list, details and references
    /// (FA01 to FA04). Its content is Remote UI of the new model.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class LabelToolWindow : Microsoft.VisualStudio.Extensibility.ToolWindows.ToolWindow
    {
        // Solution Explorer: the window opens as a tab next to it, narrow and docked at the side.
        private static readonly Guid SolutionExplorer = new("3AE79031-E1BC-11D0-8F78-00A0C9110057");

        private readonly LabelToolWindowData data = new();

        /// <summary>Creates the tool window.</summary>
        /// <param name="extensibility">Entry point to the Visual Studio extensibility API.</param>
        public LabelToolWindow(VisualStudioExtensibility extensibility)
            : base(extensibility)
        {
            this.Title = "BE-LabelExtension";
        }

        /// <inheritdoc />
        public override ToolWindowConfiguration ToolWindowConfiguration => new()
        {
            Placement = ToolWindowPlacement.DockedTo(SolutionExplorer),
        };

        /// <inheritdoc />
        public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken)
            => Task.FromResult<IRemoteUserControl>(new LabelToolWindowContent(this.data));
    }
}
