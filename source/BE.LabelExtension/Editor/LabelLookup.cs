using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using BE.LabelExtension.Core.Display;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Labels;
using BE.LabelExtension.Threading;

namespace BE.LabelExtension.Editor
{
    /// <summary>
    /// Access of the editor parts to the label store (FA05, FA06). They are MEF parts of the
    /// classic editor and reach the store only through <see cref="SharedServices"/>, once the
    /// new model has created it, which happens at the latest when a <c>.xpp</c> file opens
    /// (<see cref="XppDocumentListener"/>).
    /// </summary>
    [Export(typeof(LabelLookup))]
    internal sealed class LabelLookup
    {
        private readonly object gate = new();
        private LabelLoader? loader;
        private LabelChanges? changes;

        /// <summary>Creates the lookup and connects it as soon as the services exist.</summary>
        public LabelLookup()
        {
            SharedServices.Available += (_, _) => this.Connect();
            this.Connect();
        }

        /// <summary>
        /// Raised on any thread when what the editor shows may have changed: the labels are
        /// loaded or changed, or the settings were saved.
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>Whether the inline display is switched on in the settings (FA06).</summary>
        public bool InlineDisplay => this.loader?.Settings.InlineDisplay ?? false;

        /// <summary>Starts loading the labels unless it has started already.</summary>
        public void EnsureLoaded() => this.loader?.EnsureLoaded();

        /// <summary>
        /// Runs an action on the main thread later, tracked like the other work of the extension
        /// so that it does not outlive Visual Studio. Before the services exist nothing runs, and
        /// nothing needs to: <see cref="Changed"/> is not raised before.
        /// </summary>
        /// <param name="action">The action.</param>
        public void RunOnMainThread(Action action)
        {
            ExtensionTasks? tasks = this.loader?.Tasks;
            if (tasks == null)
            {
                return;
            }

            _ = tasks.Factory.RunAsync(async () =>
            {
                await tasks.Factory.SwitchToMainThreadAsync(tasks.ShutdownToken);
                action();
            });
        }

        /// <summary>
        /// Summarizes a label ID in the loaded languages. While the labels are not loaded there
        /// is nothing to show (UC07 3a).
        /// </summary>
        /// <param name="id">The ID in the code.</param>
        /// <returns>The summary, or <c>null</c> while the labels are not loaded.</returns>
        public LabelSummary? Summarize(LabelId id)
        {
            LabelLoader? current = this.loader;
            if (current == null || !current.Store.IsLoaded)
            {
                return null;
            }

            return LabelSummary.Create(id, current.Store.Find(id), current.Settings.LoadLanguages.ToList());
        }

        /// <summary>Summarizes several label IDs, see <see cref="Summarize(LabelId)"/>.</summary>
        /// <param name="ids">The IDs in the code.</param>
        /// <returns>The summaries, or <c>null</c> while the labels are not loaded.</returns>
        public IReadOnlyList<LabelSummary>? Summarize(IReadOnlyList<LabelId> ids)
        {
            var summaries = new List<LabelSummary>(ids.Count);
            foreach (LabelId id in ids)
            {
                LabelSummary? summary = this.Summarize(id);
                if (summary == null)
                {
                    return null;
                }

                summaries.Add(summary);
            }

            return summaries;
        }

        // Each service is connected once, when it has entered itself.
        private void Connect()
        {
            bool connected = false;
            lock (this.gate)
            {
                if (this.loader == null && SharedServices.Loader is LabelLoader newLoader)
                {
                    this.loader = newLoader;
                    newLoader.Store.Changed += this.OnChanged;
                    newLoader.SettingsChanged += this.OnChanged;
                    connected = true;
                }

                if (this.changes == null && SharedServices.Changes is LabelChanges newChanges)
                {
                    this.changes = newChanges;
                    newChanges.Changed += this.OnChanged;
                    connected = true;
                }
            }

            if (connected)
            {
                this.OnChanged(this, EventArgs.Empty);
            }
        }

        private void OnChanged(object? sender, EventArgs e) => this.Changed?.Invoke(this, EventArgs.Empty);
    }
}
