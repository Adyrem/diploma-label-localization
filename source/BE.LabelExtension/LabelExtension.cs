using BE.LabelExtension.Commands;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Diagnostics;
using BE.LabelExtension.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace BE.LabelExtension
{
    /// <summary>
    /// Entry point of the extension. It runs in the Visual Studio process, because the
    /// tooltip and the inline display need MEF (variant decision V4). In this mode the
    /// identity comes from source.extension.vsixmanifest, so Metadata stays empty.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class LabelExtension : Extension
    {
        /// <inheritdoc />
        public override ExtensionConfiguration ExtensionConfiguration => new()
        {
            RequiresInProcessHosting = true,
        };

#if DEBUG
        /// <summary>
        /// Menu BE-LabelExtension under Extensions. The Debug build adds the probe commands
        /// for the first pass on the test environment (D1).
        /// </summary>
        [VisualStudioContribution]
        public static MenuConfiguration ExtensionMenu => new("%BE.LabelExtension.Menu.DisplayName%")
        {
            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
            Children =
            [
                MenuChild.Command<OpenLabelWindowCommand>(),
                MenuChild.Separator,
                MenuChild.Command<Probes.ProbeOpenElementCommand>(),
                MenuChild.Command<Probes.ProbeUnsavedDocumentsCommand>(),
                MenuChild.Command<Probes.ProbeDesignerSelectionCommand>(),
                MenuChild.Separator,
                MenuChild.Command<Probes.ProbeTestErrorCommand>(),
            ],
        };
#else
        /// <summary>Menu BE-LabelExtension under Extensions.</summary>
        [VisualStudioContribution]
        public static MenuConfiguration ExtensionMenu => new("%BE.LabelExtension.Menu.DisplayName%")
        {
            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
            Children =
            [
                MenuChild.Command<OpenLabelWindowCommand>(),
            ],
        };
#endif

        /// <inheritdoc />
        protected override void InitializeServices(IServiceCollection serviceCollection)
        {
            base.InitializeServices(serviceCollection);

            serviceCollection.AddSingleton<ExtensionTasks>();
            serviceCollection.AddSingleton<IMessageSink, OutputWindowSink>();
            serviceCollection.AddSingleton<ErrorBoundary>();
        }
    }
}
