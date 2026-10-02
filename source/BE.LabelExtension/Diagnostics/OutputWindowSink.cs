using System;
using System.Collections.Generic;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace BE.LabelExtension.Diagnostics
{
    /// <summary>
    /// Writes messages to the pane BE-LabelExtension of the Output Window, with time and
    /// kind (FA17). Uses the stable VSSDK interface instead of the output window of the
    /// new model, which is still a preview (VSEXTPREVIEW_OUTPUTWINDOW).
    /// </summary>
    /// <remarks>
    /// Messages may arrive from any thread. The pane is created on the main thread the
    /// first time a message arrives; messages that arrive meanwhile are kept and written
    /// in their order once the pane exists.
    /// </remarks>
    internal sealed class OutputWindowSink : IMessageSink
    {
        private const string PaneTitle = "BE-LabelExtension";

        private static readonly Guid PaneGuid = new("6b0e8f63-2a4c-4f7e-9d1b-58c3a7e2f104");

        private readonly ExtensionTasks tasks;
        private readonly object gate = new();
        private readonly List<string> pending = new();
        private IVsOutputWindowPane? pane;
        private JoinableTask? paneCreation;

        /// <summary>Creates the sink. The pane itself is created with the first message.</summary>
        /// <param name="tasks">Runs the creation of the pane, which needs the main thread.</param>
        public OutputWindowSink(ExtensionTasks tasks)
        {
            this.tasks = tasks;
        }

        /// <inheritdoc />
        public void Report(MessageSeverity severity, string message)
        {
            string text = MessageFormatter.Format(severity, DateTime.Now, message);

            lock (this.gate)
            {
                if (this.pane != null)
                {
                    Write(this.pane, text);
                    return;
                }

                this.pending.Add(text);
                this.paneCreation ??= this.tasks.Factory.RunAsync(this.CreatePaneAsync);
            }
        }

        // OutputStringThreadSafe is documented as callable from any thread; the analyzer
        // cannot tell it apart from the members of the pane that need the main thread.
#pragma warning disable VSTHRD010
        private static void Write(IVsOutputWindowPane pane, string text) => pane.OutputStringThreadSafe(text);
#pragma warning restore VSTHRD010

        private async Task CreatePaneAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                IVsOutputWindow outputWindow = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsOutputWindow, IVsOutputWindow>();
                Guid guid = PaneGuid;
                outputWindow.CreatePane(ref guid, PaneTitle, fInitVisible: 1, fClearWithSolution: 0);
                outputWindow.GetPane(ref guid, out IVsOutputWindowPane created);

                lock (this.gate)
                {
                    foreach (string text in this.pending)
                    {
                        Write(created, text);
                    }

                    this.pending.Clear();
                    this.pane = created;
                }
            }
            catch (Exception exception)
            {
                // Without a pane the messages have nowhere else to go than the activity log.
                ActivityLog.TryLogError(PaneTitle, $"Output pane could not be created: {exception}");
                lock (this.gate)
                {
                    foreach (string text in this.pending)
                    {
                        ActivityLog.TryLogInformation(PaneTitle, text);
                    }

                    this.pending.Clear();
                    this.paneCreation = null;
                }
            }
        }
    }
}
