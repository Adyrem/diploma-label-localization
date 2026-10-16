using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Commands;
using BE.LabelExtension.Core.Files;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Usages;
using BE.LabelExtension.Elements;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// Lists in the hit list that come from no search: the labels of the open element (KA04)
    /// and the unused labels of the writable models (KA09).
    /// </summary>
    /// <remarks>
    /// The hit list shows them like the hits of a search, with their translations, in the order
    /// they occur, and the column headers sort them. The next search replaces them.
    /// </remarks>
    internal sealed partial class LabelWindowController
    {
        /// <summary>
        /// Lists the labels used in the active element (KA04): in the X++ editor the IDs in its
        /// code as it stands, unsaved changes included; in the designer the IDs in the XML file
        /// of the element, as last saved.
        /// </summary>
        /// <param name="context">The state of Visual Studio when the command ran.</param>
        /// <param name="cancellationToken">Cancels the action.</param>
        /// <returns>A task that completes when the list is shown.</returns>
        public Task ShowElementLabelsAsync(IClientContext context, CancellationToken cancellationToken)
            => this.RunAsync(
                "Labels of the element",
                async token =>
                {
                    if (!this.store.IsLoaded)
                    {
                        await this.ShowMessageAsync("Labels are being loaded. List the labels of the element when they are loaded.", token);
                        return;
                    }

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                    string? document = DesignerSelection.Current?.ActiveDocument();
                    await TaskScheduler.Default;

                    string? text = null;
                    string? name = null;
                    if (document != null && document.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && File.Exists(LongPath.ForAccess(document)))
                    {
                        text = (await Task.Run(() => TextFile.Read(document), token)).Text;
                        name = Path.GetFileNameWithoutExtension(document);
                    }
                    else if (await this.extensibility.Editor().GetActiveTextViewAsync(context, token) is ITextViewSnapshot view)
                    {
                        text = EditorText.Text(view.Document.Text);
                        name = Path.GetFileNameWithoutExtension(view.FilePath ?? string.Empty);
                    }

                    if (text == null)
                    {
                        await this.ShowMessageAsync("Open an element in the X++ editor or in the designer, then list its labels again.", token);
                        return;
                    }

                    // Each ID once, in the order of its first use.
                    var ids = LabelId.FindAll(text).Select(m => m.Id).Distinct().ToList();
                    var labels = new List<Label>();
                    var unknown = new List<string>();
                    foreach (LabelId id in ids)
                    {
                        if (this.store.Find(id) is Label label)
                        {
                            labels.Add(label);
                        }
                        else
                        {
                            unknown.Add(id.FullId);
                        }
                    }

                    string status = (labels.Count == 1 ? "1 label" : $"{labels.Count} labels") + $" in {name}"
                        + (unknown.Count == 0 ? "." : $"; not known: {string.Join(", ", unknown.Take(5))}{(unknown.Count > 5 ? $" and {unknown.Count - 5} more" : string.Empty)}.");
                    this.ShowList(labels, status);
                },
                cancellationToken);

        /// <summary>
        /// Lists the labels of writable models that no element of a writable model uses (KA09),
        /// at most <see cref="MaxRows"/>. Uses in read-only models do not count, so a label of
        /// the own models that only a model of the platform used would appear; the own models
        /// are what the developer changes.
        /// </summary>
        /// <param name="cancellationToken">Cancels the search.</param>
        /// <returns>A task that completes when the list is shown.</returns>
        public Task ShowUnusedLabelsAsync(CancellationToken cancellationToken)
            => this.RunAsync(
                "Unused labels",
                async token =>
                {
                    if (!this.store.IsLoaded)
                    {
                        await this.ShowMessageAsync("Labels are being loaded. Search the unused labels when they are loaded.", token);
                        return;
                    }

                    IReadOnlyList<ModelInfo> models = this.operations.WritableModels();
                    List<Label> labels = this.store.Labels.Where(l => !l.LabelFile.IsReadOnly && !l.IsDeleted).ToList();
                    this.Data.StatusText = $"Searching the uses of {labels.Count} labels in the elements of {models.Count} writable models.";
                    var stopwatch = Stopwatch.StartNew();
                    (IReadOnlyList<Label> unused, int files) = await Task.Run(() => UnusedLabels.Find(labels, models, token), token);
                    stopwatch.Stop();

                    string status = (unused.Count == 1 ? "1 label" : $"{unused.Count} labels") + $" of writable models {(unused.Count == 1 ? "is" : "are")} used in no element of a writable model"
                        + $" ({files} files in {stopwatch.Elapsed.TotalSeconds:0.0} s)"
                        + (unused.Count > MaxRows ? $", the first {MaxRows} are shown." : ".");
                    this.ShowList(unused.Take(MaxRows).ToList(), status);
                    this.messages.Report(MessageSeverity.Message, status);
                },
                cancellationToken);

        // Shows labels that come from no search, and stops a search still waiting, which would
        // replace them.
        private void ShowList(IReadOnlyList<Label> labels, string status)
        {
            var cancellation = new CancellationTokenSource();
            Interlocked.Exchange(ref this.searchCancellation, cancellation)?.Cancel();
            this.Show(labels, status, cancellation.Token);
        }
    }
}
