using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.ToolWindow;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace BE.LabelExtension.Commands
{
    // The shortcuts of FA16. The existing tool used Ctrl+S, Ctrl+Shift+A, Ctrl+Shift+S and
    // Ctrl+N, which Visual Studio already uses. The extension takes Ctrl+Shift+Alt with S for
    // Save, I for Insert, A for Save and insert, which also inserts like Apply did on
    // Ctrl+Shift+A, and N for New. Visual Studio 2026 binds none of them; the probe List
    // shortcut conflicts checks them where the Developer Tools are installed. All act on the
    // label in the detail view of the tool window.
    //
    // Each command is placed directly in the menu Extensions. Only such a placement gives it
    // a name in Tools, Options, Keyboard, where it can be rebound; a command that is only the
    // child of a menu of the extension does not appear there.

    /// <summary>Saves all label changes (FA03, FA16).</summary>
    [VisualStudioContribution]
    internal sealed class SaveLabelsCommand : Command
    {
        private readonly LabelWindowController controller;

        /// <summary>Creates the command.</summary>
        /// <param name="controller">Saves the changes.</param>
        public SaveLabelsCommand(LabelWindowController controller)
        {
            this.controller = controller;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.SaveLabelsCommand.DisplayName%")
        {
            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu.WithPriority(0x0200)],
            Icon = new(ImageMoniker.KnownValues.Save, IconSettings.IconAndText),
            Shortcuts = [new CommandShortcutConfiguration(ModifierKey.ControlShiftLeftAlt, Key.S)],
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.controller.SaveAsync(cancellationToken);
    }

    /// <summary>Inserts the ID of the label in the detail view at the cursor (FA14, FA16).</summary>
    [VisualStudioContribution]
    internal sealed class InsertLabelIdCommand : Command
    {
        private readonly LabelWindowController controller;

        /// <summary>Creates the command.</summary>
        /// <param name="controller">Inserts the ID.</param>
        public InsertLabelIdCommand(LabelWindowController controller)
        {
            this.controller = controller;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.InsertLabelIdCommand.DisplayName%")
        {
            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu.WithPriority(0x0300)],
            Icon = new(ImageMoniker.KnownValues.Paste, IconSettings.IconAndText),
            Shortcuts = [new CommandShortcutConfiguration(ModifierKey.ControlShiftLeftAlt, Key.I)],
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.controller.InsertAsync(context, save: false, cancellationToken);
    }

    /// <summary>Saves all label changes and inserts the ID of the label at the cursor (FA14, FA16).</summary>
    [VisualStudioContribution]
    internal sealed class SaveAndInsertLabelIdCommand : Command
    {
        private readonly LabelWindowController controller;

        /// <summary>Creates the command.</summary>
        /// <param name="controller">Saves and inserts.</param>
        public SaveAndInsertLabelIdCommand(LabelWindowController controller)
        {
            this.controller = controller;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.SaveAndInsertLabelIdCommand.DisplayName%")
        {
            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu.WithPriority(0x0400)],
            Icon = new(ImageMoniker.KnownValues.SaveAll, IconSettings.IconAndText),
            Shortcuts = [new CommandShortcutConfiguration(ModifierKey.ControlShiftLeftAlt, Key.A)],
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.controller.InsertAsync(context, save: true, cancellationToken);
    }

    /// <summary>Opens the tool window with a new label, filled with the last search term (FA02, FA16).</summary>
    [VisualStudioContribution]
    internal sealed class NewLabelCommand : Command
    {
        private readonly LabelWindowController controller;
        private readonly ErrorBoundary errorBoundary;

        /// <summary>Creates the command.</summary>
        /// <param name="controller">Starts the new label.</param>
        /// <param name="errorBoundary">Reports errors of opening the tool window.</param>
        public NewLabelCommand(LabelWindowController controller, ErrorBoundary errorBoundary)
        {
            this.controller = controller;
            this.errorBoundary = errorBoundary;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.NewLabelCommand.DisplayName%")
        {
            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu.WithPriority(0x0500)],
            Icon = new(ImageMoniker.KnownValues.Add, IconSettings.IconAndText),
            Shortcuts = [new CommandShortcutConfiguration(ModifierKey.ControlShiftLeftAlt, Key.N)],
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(
                "New label",
                async token =>
                {
                    await this.Extensibility.Shell().ShowToolWindowAsync<LabelToolWindow>(activate: true, token);
                    await this.controller.NewAsync();
                },
                cancellationToken);
    }
}
