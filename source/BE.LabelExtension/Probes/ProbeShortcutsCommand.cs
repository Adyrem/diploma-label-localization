#if DEBUG
// The probe runs on the main thread from start to end. The analyzer does not follow that
// into the lambdas passed to ProbeReport.Safe, which are called synchronously.
#pragma warning disable VSTHRD010

using System;
using System.Collections.Generic;
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
    /// Probe for FA16 and D2: lists which commands already use the shortcuts of the extension
    /// and a few alternatives, in every scope, together with all bindings on Ctrl+Shift+Alt.
    /// The Developer Tools bring commands of their own, so the result on the test environment
    /// can differ from a plain Visual Studio.
    /// </summary>
    /// <remarks>
    /// <para>DTE knows the classic commands only, those of Visual Studio and of the Developer
    /// Tools among them. Commands of the new model, those of this extension included, are
    /// missing, so the shortcuts of the extension show as free unless a classic command uses
    /// them too.</para>
    /// <para>Command names of Visual Studio and the Developer Tools appear as they are. Any
    /// other name could belong to an internal extension and appears as "(other command)".</para>
    /// </remarks>
    [VisualStudioContribution]
    internal sealed class ProbeShortcutsCommand : Command
    {
        private const string Title = "Probe: List shortcut conflicts";

        // The shortcuts of the extension first, then alternatives.
        private static readonly string[] Candidates =
        {
            "Ctrl+Shift+Alt+S", "Ctrl+Shift+Alt+I", "Ctrl+Shift+Alt+A", "Ctrl+Shift+Alt+N",
            "Ctrl+Shift+Alt+L", "Ctrl+Shift+Alt+D", "Ctrl+Shift+Alt+E", "Ctrl+Shift+Alt+R",
            "Alt+Shift+S", "Alt+Shift+I", "Alt+Shift+N",
        };

        // First parts of command names of Visual Studio and the Developer Tools.
        private static readonly string[] KnownCategories =
        {
            "Analyze", "Build", "ClassViewContextMenus", "Data", "Debug", "Dynamics365", "Edit", "File", "Format",
            "Git", "Help", "LiveShare", "OtherContextMenus", "Project", "ProjectandSolutionContextMenus",
            "Refactor", "Resources", "SolutionExplorer", "Team", "Test", "TestExplorer", "Tools", "View",
            "Window", "XML",
        };

        private readonly ErrorBoundary errorBoundary;
        private readonly IMessageSink sink;

        public ProbeShortcutsCommand(ErrorBoundary errorBoundary, IMessageSink sink)
        {
            this.errorBoundary = errorBoundary;
            this.sink = sink;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.ProbeShortcutsCommand.DisplayName%")
        {
            Icon = new(ImageMoniker.KnownValues.Keyboard, IconSettings.IconAndText),
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(Title, this.RunAsync, cancellationToken);

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            DTE2 dte = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SDTE, DTE2>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Keys, such as "Ctrl+Shift+Alt+S" or "Ctrl+K, Ctrl+C", to scope and command.
            var bindings = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            int commands = 0;
            foreach (EnvDTE.Command command in dte.Commands)
            {
                commands++;
                string name = ProbeReport.Safe(() => command.Name ?? string.Empty);
                object[] list;
                try
                {
                    list = command.Bindings as object[] ?? Array.Empty<object>();
                }
                catch (Exception exception) when (exception is System.Runtime.InteropServices.COMException || exception is NotImplementedException)
                {
                    // Some commands have no bindings to read.
                    continue;
                }

                foreach (object binding in list)
                {
                    string text = binding as string ?? string.Empty;
                    int separator = text.IndexOf("::", StringComparison.Ordinal);
                    if (separator < 0)
                    {
                        continue;
                    }

                    string keys = text.Substring(separator + 2);
                    if (!bindings.TryGetValue(keys, out List<string>? users))
                    {
                        users = new List<string>();
                        bindings.Add(keys, users);
                    }

                    users.Add($"{text.Substring(0, separator)}: {Shown(name)}");
                }
            }

            var report = new ProbeReport(Title);
            report.Line($"{commands} commands, {bindings.Count} different shortcuts.");
            report.Line("Candidates (also as the first key of a chord):");
            foreach (string candidate in Candidates)
            {
                var users = bindings.Where(b => b.Key.Equals(candidate, StringComparison.OrdinalIgnoreCase) || b.Key.StartsWith(candidate + ",", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(b => b.Value.Select(u => $"{b.Key} [{u}]"))
                    .ToList();
                report.Line($"  {candidate,-18} {(users.Count == 0 ? "free" : "TAKEN: " + string.Join("; ", users))}");
            }


            report.Line("All bindings on Ctrl+Shift+Alt:");
            foreach (var binding in bindings.Where(b => b.Key.StartsWith("Ctrl+Shift+Alt+", StringComparison.OrdinalIgnoreCase)).OrderBy(b => b.Key, StringComparer.OrdinalIgnoreCase))
            {
                report.Line($"  {binding.Key,-22} {string.Join("; ", binding.Value)}");
            }

            report.Send(this.sink);
        }

        private static string Shown(string name)
        {
            int dot = name.IndexOf('.');
            string category = dot < 0 ? name : name.Substring(0, dot);
            return name.Length == 0 ? "(unnamed command)"
                : KnownCategories.Contains(category, StringComparer.OrdinalIgnoreCase) ? name
                : "(other command)";
        }
    }
}
#endif
