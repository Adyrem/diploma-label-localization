using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.ToolWindow;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace BE.LabelExtension.Commands
{
    /// <summary>
    /// Opens the tool window, from View, Other Windows and from the menu BE-LabelExtension.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class OpenLabelWindowCommand : Command
    {
        private readonly ErrorBoundary errorBoundary;

        /// <summary>Creates the command.</summary>
        /// <param name="errorBoundary">Reports errors of the command.</param>
        public OpenLabelWindowCommand(ErrorBoundary errorBoundary)
        {
            this.errorBoundary = errorBoundary;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.OpenLabelWindowCommand.DisplayName%")
        {
            Placements = [CommandPlacement.KnownPlacements.ViewOtherWindowsMenu],
            Icon = new(ImageMoniker.KnownValues.Localize, IconSettings.IconAndText),
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(
                "Open label window",
                token => this.Extensibility.Shell().ShowToolWindowAsync<LabelToolWindow>(activate: true, token),
                cancellationToken);
    }
}
