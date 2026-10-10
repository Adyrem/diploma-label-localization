using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Search;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Labels;
using BE.LabelExtension.Threading;
using Microsoft.VisualStudio.Extensibility.UI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// Behaviour of the tool window (FA01, FA03): searches while the user types, shows the
    /// selected label with its translations, applies changes to the label store and saves them.
    /// </summary>
    /// <remarks>
    /// The hit list shows at most <see cref="MaxRows"/> labels. Remote UI transfers every row,
    /// and a term of one letter finds hundreds of thousands; the status line names the total.
    /// </remarks>
    internal sealed class LabelWindowController : IDisposable
    {
        /// <summary>The most hits the list shows.</summary>
        public const int MaxRows = 1000;

        private static readonly TimeSpan TypingDelay = TimeSpan.FromMilliseconds(250);

        // The modes in the order and with the names of the existing tool.
        private static readonly (string Name, SearchMode Mode, bool CaseSensitive)[] Modes =
        {
            ("Exact match", SearchMode.ExactMatch, true),
            ("Exact match, ignore case", SearchMode.ExactMatch, false),
            ("Substring", SearchMode.Substring, true),
            ("Substring, ignore case", SearchMode.Substring, false),
            ("Anything like that", SearchMode.AnythingLike, true),
            ("Anything like that, ignore case", SearchMode.AnythingLike, false),
            ("Label id", SearchMode.Id, true),
            ("Match word", SearchMode.MatchWord, false),
        };

        private const int DefaultMode = 3;

        private readonly LabelStore store;
        private readonly LabelLoader loader;
        private readonly LabelChanges changes;
        private readonly ErrorBoundary errorBoundary;
        private readonly IMessageSink messages;
        private readonly ExtensionTasks tasks;
        private readonly object gate = new();
        private CancellationTokenSource? searchCancellation;
        private IReadOnlyList<Label> shown = Array.Empty<Label>();
        private IReadOnlyList<string> languages = Array.Empty<string>();
        private bool searched;

        /// <summary>Creates the controller.</summary>
        /// <param name="store">The loaded labels.</param>
        /// <param name="loader">Starts loading and knows the languages.</param>
        /// <param name="changes">The changes not yet saved.</param>
        /// <param name="errorBoundary">Reports errors of searching and saving.</param>
        /// <param name="messages">Receives problems with single inputs.</param>
        /// <param name="tasks">Runs the work nobody waits for.</param>
        public LabelWindowController(LabelStore store, LabelLoader loader, LabelChanges changes, ErrorBoundary errorBoundary, IMessageSink messages, ExtensionTasks tasks)
        {
            this.store = store;
            this.loader = loader;
            this.changes = changes;
            this.errorBoundary = errorBoundary;
            this.messages = messages;
            this.tasks = tasks;

            this.Data = new LabelToolWindowData(
                Modes.Select(m => m.Name).ToList(),
                DefaultMode,
                new AsyncCommand((_, _) => this.SearchNowAsync()),
                new AsyncCommand((_, cancellationToken) => this.SaveAsync(cancellationToken)) { CanExecute = false });
            this.Data.PropertyChanged += this.OnDataChanged;
            this.store.Changed += this.OnStoreChanged;
            this.changes.Changed += this.OnChangesChanged;
        }

        /// <summary>The data context of the tool window.</summary>
        public LabelToolWindowData Data { get; }

        /// <summary>Starts loading if needed and shows the current state.</summary>
        public void Open()
        {
            this.loader.EnsureLoaded();
            this.UpdateLanguages();
            this.UpdateStatus();
            this.OnChangesChanged(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Data.PropertyChanged -= this.OnDataChanged;
            this.store.Changed -= this.OnStoreChanged;
            this.changes.Changed -= this.OnChangesChanged;
            Interlocked.Exchange(ref this.searchCancellation, null)?.Cancel();
        }

        private void OnDataChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(LabelToolWindowData.SearchText):
                    this.ScheduleSearch(TypingDelay);
                    break;
                case nameof(LabelToolWindowData.SelectedModeIndex):
                    this.ScheduleSearch(TimeSpan.Zero);
                    break;
                case nameof(LabelToolWindowData.SelectedHitIndex):
                    this.OnSelectionChanged();
                    break;
            }
        }

        // A reload replaces the labels: search again and show the selected label anew.
        private void OnStoreChanged(object? sender, EventArgs e)
        {
            this.UpdateLanguages();
            if (this.searched)
            {
                this.ScheduleSearch(TimeSpan.Zero);
            }
            else
            {
                this.UpdateStatus();
            }

            string id = this.Data.Detail.Id;
            if (id.Length > 0 && this.store.Find(id) is Label label)
            {
                this.ShowDetail(label);
            }
        }

        private void OnChangesChanged(object? sender, EventArgs e)
        {
            int count = this.changes.Count;
            this.Data.SaveCommand.CanExecute = count > 0;
            this.Data.PendingText = count == 0 ? string.Empty : count == 1 ? "1 label not saved" : $"{count} labels not saved";
        }

        private Task SearchNowAsync()
        {
            this.ScheduleSearch(TimeSpan.Zero);
            return Task.CompletedTask;
        }

        // A new search cancels the one before, also while it waits for the user to stop typing.
        private void ScheduleSearch(TimeSpan delay)
        {
            var cancellation = new CancellationTokenSource();
            Interlocked.Exchange(ref this.searchCancellation, cancellation)?.Cancel();
            string term = this.Data.SearchText;
            int mode = this.Data.SelectedModeIndex;
            _ = this.tasks.Factory.RunAsync(() => this.errorBoundary.RunAsync(
                "Search labels",
                async cancellationToken =>
                {
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }

                    await Task.Run(() => this.Search(term, mode, cancellationToken), cancellationToken);
                },
                cancellation.Token));
        }

        private void Search(string term, int mode, CancellationToken cancellationToken)
        {
            if (!this.store.IsLoaded)
            {
                this.Data.StatusText = "Labels are being loaded. Search again when they are loaded.";
                return;
            }

            if (term.Trim().Length == 0 || mode < 0 || mode >= Modes.Length)
            {
                this.Show(Array.Empty<Label>(), null, cancellationToken);
                return;
            }

            var query = new SearchQuery(term, Modes[mode].Mode, Modes[mode].CaseSensitive);
            var stopwatch = Stopwatch.StartNew();
            IReadOnlyList<SearchHit> hits = new LabelSearch(this.store).Search(query, cancellationToken);
            stopwatch.Stop();

            string status = (hits.Count == 1 ? "1 label found" : $"{hits.Count} labels found") + $" in {stopwatch.ElapsedMilliseconds} ms"
                + (hits.Count > MaxRows ? $", the first {MaxRows} are shown" : string.Empty);
            this.Show(hits.Take(MaxRows).Select(h => h.Label).ToList(), status, cancellationToken);
        }

        private void Show(IReadOnlyList<Label> labels, string? status, CancellationToken cancellationToken)
        {
            var rows = new ObservableList<HitRow>(labels.Select(this.CreateRow));
            lock (this.gate)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                this.searched = status != null;
                this.shown = labels;
                this.Data.Hits = rows;
            }

            if (status == null)
            {
                this.UpdateStatus();
            }
            else
            {
                this.Data.StatusText = status;
            }
        }

        private HitRow CreateRow(Label label)
            => new(label.Id.FullId, label.LabelFile.IsReadOnly, label.IsModified, new ObservableList<HitCell>(this.languages.Select(language => new HitCell(label.GetText(language)))));

        // The detail view keeps its label while the list changes; only another selection replaces it.
        private void OnSelectionChanged()
        {
            Label? label;
            lock (this.gate)
            {
                int index = this.Data.SelectedHitIndex;
                label = index >= 0 && index < this.shown.Count ? this.shown[index] : null;
            }

            if (label != null)
            {
                this.ShowDetail(label);
            }
        }

        private void ShowDetail(Label label)
        {
            var rows = new List<TranslationRow>();
            foreach (string language in this.languages)
            {
                Translation? translation = label.GetTranslation(language);
                bool canChange = this.changes.GetTarget(label, language) != null;

                // A read-only label says so once in the header; a row says only what differs.
                string hint = canChange
                    ? (translation == null ? "missing" : string.Empty)
                    : translation == null ? "missing, the label file does not exist in this language"
                    : ReferenceEquals(translation.LabelFile, label.LabelFile) && label.LabelFile.IsReadOnly ? string.Empty
                    : ReadOnlyReason(translation.LabelFile);
                var row = new TranslationRow(language, translation?.Text ?? string.Empty, translation?.Comment ?? string.Empty, !canChange, hint);
                row.PropertyChanged += (_, _) => this.OnTranslationEdited(label, row);
                rows.Add(row);
            }

            LabelFile file = label.LabelFile;
            string info = $"Label file {file.Name}, model {file.Model.Name}" + (file.IsReadOnly ? $", {ReadOnlyReason(file)}" : string.Empty);
            this.Data.Detail = new LabelDetailData(label.Id.FullId, info, rows);
        }

        private void OnTranslationEdited(Label label, TranslationRow row)
        {
            try
            {
                if (!this.changes.Edit(label, row.Language, row.Text, row.Comment))
                {
                    return;
                }
            }
            catch (ArgumentException exception)
            {
                this.messages.Report(MessageSeverity.Warning, $"{label.Id.FullId} in {row.Language}: {exception.Message}");
                return;
            }

            lock (this.gate)
            {
                int index = IndexOf(this.shown, label);
                int column = IndexOf(this.languages, row.Language);
                if (index >= 0 && index < this.Data.Hits.Count)
                {
                    HitRow hit = this.Data.Hits[index];
                    hit.IsModified = true;
                    if (column >= 0 && column < hit.Cells.Count)
                    {
                        hit.Cells[column].Update(label.GetText(row.Language));
                    }
                }
            }
        }

        private Task SaveAsync(CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(
                "Save labels",
                async token =>
                {
                    await Task.Run(() => this.changes.Save(), token);
                    lock (this.gate)
                    {
                        for (int i = 0; i < this.shown.Count && i < this.Data.Hits.Count; i++)
                        {
                            this.Data.Hits[i].IsModified = this.shown[i].IsModified;
                        }
                    }
                },
                cancellationToken);

        private void UpdateLanguages()
        {
            this.languages = this.loader.Settings.LoadLanguages.ToList();
            this.Data.Columns = this.languages.Select(language => $"Text {language}").ToList();
        }

        private void UpdateStatus()
        {
            this.Data.StatusText = this.store.IsLoaded
                ? $"{this.store.Count} labels loaded from {this.store.LabelFiles.Count} label files."
                : "Labels are being loaded.";
        }

        private static string ReadOnlyReason(LabelFile file)
            => file.IsCompiled ? "compiled, read-only"
                : file.Model.IsLocked ? "model locked, read-only"
                : "read-only model";

        private static int IndexOf<T>(IReadOnlyList<T> list, T item)
            where T : class
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item) || (item is string text && string.Equals(list[i] as string, text, StringComparison.OrdinalIgnoreCase)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
