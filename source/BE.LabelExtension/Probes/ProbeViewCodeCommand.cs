#if DEBUG
// The probe runs on the main thread from start to end. The analyzer does not follow that
// into the lambdas passed to ProbeReport.Safe, which are called synchronously.
#pragma warning disable VSTHRD010

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using EnvDTE80;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BE.LabelExtension.Probes
{
    /// <summary>
    /// Probe for RE56 in D2: whether the standard command View.ViewCode (F7) opens the code of
    /// the element shown in the designer of the Developer Tools. None of their own commands does
    /// (D1, B22), but their list only held names with Dynamics, X++ or Label. If View.ViewCode
    /// works, the jump to a use in code could open the code itself instead of pointing to the
    /// Application Explorer.
    /// </summary>
    /// <remarks>
    /// Reports the active document before and after as a shape and all commands with ViewCode in
    /// their name with their availability. Names outside known categories appear as
    /// "(other command)".
    /// </remarks>
    [VisualStudioContribution]
    internal sealed class ProbeViewCodeCommand : Command
    {
        private const string Title = "Probe: View code of the designer";

        private static readonly string[] KnownCategories = { "View", "Dynamics365", "OtherContextMenus", "EditorContextMenus", "Project", "ClassViewContextMenus", "ProjectandSolutionContextMenus" };

        private readonly ErrorBoundary errorBoundary;
        private readonly IMessageSink sink;

        public ProbeViewCodeCommand(ErrorBoundary errorBoundary, IMessageSink sink)
        {
            this.errorBoundary = errorBoundary;
            this.sink = sink;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.ProbeViewCodeCommand.DisplayName%")
        {
            Icon = new(ImageMoniker.KnownValues.GoToSourceCode, IconSettings.IconAndText),
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(Title, this.RunAsync, cancellationToken);

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            DTE2 dte = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SDTE, DTE2>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var report = new ProbeReport(Title);
            report.Line($"active document before: {ProbeReport.Safe(() => ProbeReport.PathShape(dte.ActiveDocument?.FullName))}");

            report.Line("commands with ViewCode in their name (name: available):");
            foreach (EnvDTE.Command command in dte.Commands)
            {
                string name = ProbeReport.Safe(() => command.Name ?? string.Empty);
                if (name.IndexOf("ViewCode", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string category = name.Split('.')[0];
                    string shown = KnownCategories.Contains(category, StringComparer.OrdinalIgnoreCase) ? name : "(other command)";
                    report.Line($"  {shown}: {ProbeReport.Safe(() => command.IsAvailable.ToString())}");
                }
            }

            bool available;
            try
            {
                available = dte.Commands.Item("View.ViewCode").IsAvailable;
            }
            catch (ArgumentException)
            {
                available = false;
            }

            if (!available)
            {
                report.Line("View.ViewCode is not available for the active document; nothing was run.");
                report.Send(this.sink);
                return;
            }

            try
            {
                dte.ExecuteCommand("View.ViewCode");
                report.Line("View.ViewCode ran.");
            }
            catch (Exception exception) when (exception is System.Runtime.InteropServices.COMException || exception is ArgumentException)
            {
                report.Line($"View.ViewCode failed: {exception.GetType().Name}: {ProbeReport.Shorten(exception.Message, 200)}");
            }

            // Opening the code may take a moment.
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            report.Line($"active document after: {ProbeReport.Safe(() => ProbeReport.PathShape(dte.ActiveDocument?.FullName))}");
            report.Send(this.sink);
        }
    }
}
#endif
