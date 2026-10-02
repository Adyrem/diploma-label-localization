#if DEBUG
using System;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace BE.LabelExtension.Probes
{
    /// <summary>
    /// Throws on purpose. Shows that the error boundary reports the error in the Output
    /// Window and Visual Studio keeps running (AP3.1, NFA04).
    /// </summary>
    [VisualStudioContribution]
    internal sealed class ProbeTestErrorCommand : Command
    {
        private readonly ErrorBoundary errorBoundary;

        public ProbeTestErrorCommand(ErrorBoundary errorBoundary)
        {
            this.errorBoundary = errorBoundary;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.ProbeTestErrorCommand.DisplayName%")
        {
            Icon = new(ImageMoniker.KnownValues.Bug, IconSettings.IconAndText),
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(
                "Probe: Trigger test error",
                _ => throw new InvalidOperationException("Test error triggered on purpose. Visual Studio keeps running."),
                cancellationToken);
    }
}
#endif
