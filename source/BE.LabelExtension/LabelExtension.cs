using BE.LabelExtension.Commands;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Sources;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Diagnostics;
using BE.LabelExtension.Labels;
using BE.LabelExtension.Threading;
using BE.LabelExtension.ToolWindow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace BE.LabelExtension
{
    /// <summary>
    /// Entry point of the extension. It runs in the Visual Studio process, because the
    /// tooltip and the inline display need MEF (decision E3, extension model). In this mode the
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
        /// Submenu BE-LabelExtension under Extensions with the probe commands of the Debug build,
        /// for the passes on the test environment. The commands of the extension stand in the
        /// menu Extensions itself, see the shortcut commands.
        /// </summary>
        [VisualStudioContribution]
        public static MenuConfiguration ProbeMenu => new("%BE.LabelExtension.Menu.DisplayName%")
        {
            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu.WithPriority(0x0600)],
            Children =
            [
                MenuChild.Command<Probes.ProbeOpenElementCommand>(),
                MenuChild.Command<Probes.ProbeUnsavedDocumentsCommand>(),
                MenuChild.Command<Probes.ProbeDesignerSelectionCommand>(),
                MenuChild.Command<Probes.ProbeSearchTimesCommand>(),
                MenuChild.Command<Probes.ProbeShortcutsCommand>(),
                MenuChild.Separator,
                MenuChild.Command<Probes.ProbeTestErrorCommand>(),
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

            serviceCollection.AddSingleton(_ => new ModelDiscovery(MetadataConfiguration.DefaultFolder));
            serviceCollection.AddSingleton<LabelFileWatcher>();
            serviceCollection.AddSingleton(provider => new LabelStore(
                provider.GetRequiredService<ModelDiscovery>(),
                new ILabelSource[] { new LabelFileSource(), new CompiledLabelSource() },
                provider.GetRequiredService<LabelFileWatcher>(),
                provider.GetRequiredService<IMessageSink>()));
            serviceCollection.AddSingleton<LabelLoader>();
            serviceCollection.AddSingleton(provider => SharedServices.Changes = new LabelChanges(
                provider.GetRequiredService<LabelStore>(),
                provider.GetRequiredService<IMessageSink>()));
            serviceCollection.AddSingleton<LabelOperations>();

            // One controller for the tool window and the shortcuts.
            serviceCollection.AddSingleton<LabelWindowController>();
        }
    }
}
