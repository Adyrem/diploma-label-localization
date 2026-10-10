using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Search;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Core.Usages;
using BE.LabelExtension.Elements;
using BE.LabelExtension.Labels;
using BE.LabelExtension.Settings;
using BE.LabelExtension.Threading;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.Extensibility.UI;

namespace BE.LabelExtension.ToolWindow
{
    /// <summary>
    /// Behaviour of the tool window (FA01 to FA03, FA13, FA14): searches while the user types,
    /// shows the selected label with its translations, applies changes to the label store and
    /// saves them, creates, deletes, copies, moves and replaces labels and inserts their ID.
    /// </summary>
    /// <remarks>
    /// <para>One instance serves the tool window and the shortcuts, so both act on the label
    /// in the detail view.</para>
    /// <para>The hit list shows at most <see cref="MaxRows"/> labels. Remote UI transfers every
    /// row, and a term of one letter finds hundreds of thousands; the status line names the
    /// total.</para>
    /// </remarks>
    internal sealed class LabelWindowController : IDisposable
    {
        /// <summary>The most hits the list shows.</summary>
        public const int MaxRows = 1000;

        /// <summary>The most uses the area References shows, like the hit list.</summary>
        public const int MaxReferences = 1000;

        // Uses found in parallel go into the list in batches, not one message to Visual Studio each.
        private static readonly TimeSpan ReferenceBatch = TimeSpan.FromMilliseconds(250);

        // Index of the search mode Label id in Modes.
        private const int LabelIdMode = 6;

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

        private readonly VisualStudioExtensibility extensibility;
        private readonly LabelStore store;
        private readonly LabelLoader loader;
        private readonly LabelChanges changes;
        private readonly LabelOperations operations;
        private readonly ErrorBoundary errorBoundary;
        private readonly IMessageSink messages;
        private readonly ExtensionTasks tasks;
        private readonly object gate = new();
        private CancellationTokenSource? searchCancellation;
        private CancellationTokenSource? referenceSearch;
        private IReadOnlyList<Label> shown = Array.Empty<Label>();
        private IReadOnlyList<string> languages = Array.Empty<string>();
        private IReadOnlyList<LabelFile> labelFiles = Array.Empty<LabelFile>();
        private Label? detailLabel;
        private bool isDraft;
        private bool searched;
        private int busy;

        /// <summary>Creates the controller.</summary>
        /// <param name="extensibility">Shows prompts and edits the active editor.</param>
        /// <param name="store">The loaded labels.</param>
        /// <param name="loader">Starts loading and knows the languages.</param>
        /// <param name="changes">The changes not yet saved.</param>
        /// <param name="operations">Copies, moves and replaces labels with their references.</param>
        /// <param name="errorBoundary">Reports errors of the actions.</param>
        /// <param name="messages">Receives problems with single inputs.</param>
        /// <param name="tasks">Runs the work nobody waits for.</param>
        public LabelWindowController(VisualStudioExtensibility extensibility, LabelStore store, LabelLoader loader, LabelChanges changes, LabelOperations operations, ErrorBoundary errorBoundary, IMessageSink messages, ExtensionTasks tasks)
        {
            this.extensibility = extensibility;
            this.store = store;
            this.loader = loader;
            this.changes = changes;
            this.operations = operations;
            this.errorBoundary = errorBoundary;
            this.messages = messages;
            this.tasks = tasks;

            this.Data = new LabelToolWindowData(Modes.Select(m => m.Name).ToList(), DefaultMode)
            {
                SearchCommand = new AsyncCommand((_, _) => this.SearchNowAsync()),
                SaveCommand = new AsyncCommand((_, cancellationToken) => this.SaveAsync(cancellationToken)),
                SaveAndInsertCommand = new AsyncCommand((_, context, cancellationToken) => this.InsertAsync(context, save: true, cancellationToken)),
                InsertCommand = new AsyncCommand((_, context, cancellationToken) => this.InsertAsync(context, save: false, cancellationToken)),
                NewCommand = new AsyncCommand((_, _) => this.NewAsync()),
                SettingsCommand = new AsyncCommand((_, cancellationToken) => this.errorBoundary.RunAsync("Open settings", OptionsPage.ShowAsync, cancellationToken)),
                CreateCommand = new AsyncCommand((_, cancellationToken) => this.CreateAsync(cancellationToken)),
                CancelNewCommand = new AsyncCommand((_, _) => this.CancelNewAsync()),
                DeleteCommand = new AsyncCommand((_, _) => this.DeleteAsync()),
                CopyCommand = new AsyncCommand((_, cancellationToken) => this.RelocateAsync(move: false, cancellationToken)),
                MoveCommand = new AsyncCommand((_, cancellationToken) => this.RelocateAsync(move: true, cancellationToken)),
                ReplaceCommand = new AsyncCommand((_, cancellationToken) => this.ReplaceAsync(cancellationToken)),
                FindReferencesCommand = new AsyncCommand((_, _) => this.FindReferencesOfDetailAsync()),
            };
            this.UpdateCommands();
            this.Data.PropertyChanged += this.OnDataChanged;
            this.store.Changed += this.OnStoreChanged;
            this.changes.Changed += this.OnChangesChanged;
        }

        // What the developer chooses for the references when copying or moving.
        private enum ReferenceChoice
        {
            Cancel,
            Change,
            Keep,
        }

        /// <summary>The data context of the tool window.</summary>
        public LabelToolWindowData Data { get; }

        /// <summary>Starts loading if needed and shows the current state.</summary>
        public void Open()
        {
            this.loader.EnsureLoaded();
            this.UpdateLanguages();
            this.UpdateLabelFiles();
            this.UpdateStatus();
            this.OnChangesChanged(this, EventArgs.Empty);
        }

        /// <summary>Writes all changes into the label files (FA03).</summary>
        /// <param name="cancellationToken">Cancels before saving.</param>
        /// <returns>A task that completes when the labels are saved.</returns>
        public Task SaveAsync(CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync("Save labels", this.SaveChangesAsync, cancellationToken);

        /// <summary>
        /// Inserts the complete ID of the label in the detail view at the cursor of the active
        /// editor, in place of a selection (FA14). For a new label, Save and insert creates it
        /// first. Without an active editor a hint appears.
        /// </summary>
        /// <param name="context">The state of Visual Studio when the command ran.</param>
        /// <param name="save">Whether to save all changes first.</param>
        /// <param name="cancellationToken">Cancels the action.</param>
        /// <returns>A task that completes when the ID is inserted.</returns>
        public Task InsertAsync(IClientContext context, bool save, CancellationToken cancellationToken)
            => this.RunAsync(
                save ? "Save and insert label ID" : "Insert label ID",
                async token =>
                {
                    if (save && this.isDraft && !await this.CreateDraftAsync(token))
                    {
                        return;
                    }

                    Label? label = this.detailLabel;
                    if (label == null || label.IsDeleted)
                    {
                        await this.ShowMessageAsync("Select a label first, then insert its ID.", token);
                        return;
                    }

                    if (save)
                    {
                        await this.SaveChangesAsync(token);
                        if (label.IsModified)
                        {
                            await this.ShowMessageAsync($"{label.Id.FullId} could not be saved and is not inserted. The Output Window, pane BE-LabelExtension, names the files.", token);
                            return;
                        }
                    }

                    ITextViewSnapshot? view = await this.extensibility.Editor().GetActiveTextViewAsync(context, token);
                    if (view == null)
                    {
                        await this.ShowMessageAsync("No editor is active. Place the cursor in the code where the label ID belongs, then insert again.", token);
                        return;
                    }

                    await this.extensibility.Editor().EditAsync(batch => view.Document.AsEditable(batch).Replace(view.Selection.Extent, label.Id.FullId), token);
                    this.Data.StatusText = $"{label.Id.FullId} inserted in {Path.GetFileName(view.FilePath)}.";
                },
                cancellationToken);

        /// <summary>
        /// Starts a new label in the selected label file (FA02): the detail view shows every
        /// language to create, filled with the last search term, until the developer creates it.
        /// </summary>
        /// <returns>A completed task.</returns>
        public Task NewAsync()
        {
            LabelFile? file = this.SelectedLabelFile();
            if (file == null)
            {
                this.Data.StatusText = this.store.IsLoaded
                    ? "There is no label file to create labels in. New labels go into label files of writable models."
                    : "Labels are being loaded. Create the label when they are loaded.";
                return Task.CompletedTask;
            }

            this.ShowDraft(file, this.Data.SearchText.Trim(), null);
            return Task.CompletedTask;
        }

        /// <summary>Searches for a text, for the search from the editor (FA07). The search mode stays.</summary>
        /// <param name="text">The text.</param>
        public void SearchFor(string text)
        {
            this.Data.SearchText = text;
            this.ScheduleSearch(TimeSpan.Zero);
        }

        /// <summary>
        /// Opens a label in the detail view, for the command on a label ID in the code (FA08).
        /// The hit list shows it as well, with the search mode Label id.
        /// </summary>
        /// <param name="id">The label ID.</param>
        /// <returns>Why the label cannot be shown, or <c>null</c> if it is.</returns>
        public string? OpenLabel(LabelId id)
        {
            if (!this.store.IsLoaded)
            {
                return "Labels are being loaded. Open the label again when they are loaded.";
            }

            if (this.store.Find(id) is not Label label || label.IsDeleted)
            {
                return $"{id.FullId} is not a known label.";
            }

            this.Data.SelectedModeIndex = LabelIdMode;
            this.Data.SearchText = id.FullId;
            this.ShowDetail(label);
            return null;
        }

        /// <summary>
        /// Searches the uses of a label in all models, the own ones first (FA04, F16). The uses
        /// appear while the search runs; a new search replaces the running one.
        /// </summary>
        /// <param name="id">The label ID.</param>
        public void FindReferences(LabelId id)
        {
            var cancellation = new CancellationTokenSource();
            Interlocked.Exchange(ref this.referenceSearch, cancellation)?.Cancel();
            var rows = new ObservableList<ReferenceRow>();
            this.Data.References = rows;
            this.Data.ReferencesTitle = $"References to {id.FullId}";
            this.Data.ReferencesStatus = "searching your models";

            var (own, others) = LabelUsageSearch.Split(this.store.Models, this.loader.PackageDirectories(), !string.IsNullOrWhiteSpace(this.loader.Settings.MetadataConfiguration));
            string? debugSourceFolder = this.loader.DebugSourceFolder();
            _ = this.tasks.Factory.RunAsync(() => this.errorBoundary.RunAsync(
                "Find references",
                token => this.SearchReferencesAsync(id, own, others, rows, debugSourceFolder, token),
                cancellation.Token));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Data.PropertyChanged -= this.OnDataChanged;
            this.store.Changed -= this.OnStoreChanged;
            this.changes.Changed -= this.OnChangesChanged;
            Interlocked.Exchange(ref this.searchCancellation, null)?.Cancel();
            Interlocked.Exchange(ref this.referenceSearch, null)?.Cancel();
        }

        private Task FindReferencesOfDetailAsync()
        {
            if (this.detailLabel is Label label)
            {
                this.FindReferences(label.Id);
            }

            return Task.CompletedTask;
        }

        private async Task SearchReferencesAsync(LabelId id, IReadOnlyList<ModelInfo> own, IReadOnlyList<ModelInfo> others, ObservableList<ReferenceRow> rows, string? debugSourceFolder, CancellationToken cancellationToken)
        {
            var queue = new ConcurrentQueue<LabelUsage>();
            int found = 0;
            int ownDone = 0;
            var stopwatch = Stopwatch.StartNew();
            Task search = Task.Run(
                () => LabelUsageSearch.Run(
                    own,
                    others,
                    id.FullId,
                    usage =>
                    {
                        queue.Enqueue(usage);
                        Interlocked.Increment(ref found);
                    },
                    () => Interlocked.Exchange(ref ownDone, 1),
                    cancellationToken),
                cancellationToken);

            while (!search.IsCompleted && !cancellationToken.IsCancellationRequested)
            {
                await Task.WhenAny(search, Task.Delay(ReferenceBatch, cancellationToken));
                this.AddReferences(queue, rows, id, debugSourceFolder);
                this.Data.ReferencesStatus = Volatile.Read(ref ownDone) == 0
                    ? "searching your models"
                    : $"searching the other models, {Uses(Volatile.Read(ref found))} found so far";
            }

            await search;
            this.AddReferences(queue, rows, id, debugSourceFolder);
            int total = Volatile.Read(ref found);
            this.Data.ReferencesStatus = $"{Uses(total)} found in all models in {stopwatch.Elapsed.TotalSeconds:0.0} s"
                + (total > MaxReferences ? $", the first {MaxReferences} are shown" : string.Empty);
        }

        private static string Uses(int count) => count == 1 ? "1 use" : $"{count} uses";

        private void AddReferences(ConcurrentQueue<LabelUsage> queue, ObservableList<ReferenceRow> rows, LabelId id, string? debugSourceFolder)
        {
            var batch = new List<ReferenceRow>();
            while (rows.Count + batch.Count < MaxReferences && queue.TryDequeue(out LabelUsage? usage))
            {
                batch.Add(this.CreateReferenceRow(usage, id, debugSourceFolder));
            }

            // Beyond the limit only the count goes on.
            while (rows.Count + batch.Count >= MaxReferences && queue.TryDequeue(out _))
            {
            }

            if (batch.Count > 0)
            {
                rows.AddRange(batch);
            }
        }

        private ReferenceRow CreateReferenceRow(LabelUsage usage, LabelId id, string? debugSourceFolder)
        {
            string directory = usage.Model.Directory.TrimEnd('\\') + "\\";
            string file = usage.Path.StartsWith(directory, StringComparison.OrdinalIgnoreCase) ? usage.Path.Substring(directory.Length) : Path.GetFileName(usage.Path);
            var go = new AsyncCommand((_, cancellationToken) => this.errorBoundary.RunAsync(
                "Go to reference",
                async token => this.Data.StatusText = await ElementNavigator.OpenAsync(usage, id.FullId, debugSourceFolder, token),
                cancellationToken));
            return new ReferenceRow(usage.Model.Name, file, usage.Line, usage.Column, usage.LineText, go);
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
                case nameof(LabelToolWindowData.SelectedLabelFileIndex):
                    this.OnLabelFileChanged();
                    break;
            }
        }

        // A reload replaces the labels: search again and show the selected label anew.
        private void OnStoreChanged(object? sender, EventArgs e)
        {
            this.UpdateLanguages();
            this.UpdateLabelFiles();
            if (this.searched)
            {
                this.ScheduleSearch(TimeSpan.Zero);
            }
            else
            {
                this.UpdateStatus();
            }

            // A new label follows the label files in UpdateLabelFiles.
            Label? label = this.detailLabel;
            if (this.isDraft)
            {
                return;
            }

            if (label != null && this.store.Find(label.Id) is Label current)
            {
                this.ShowDetail(current);
            }
            else if (label != null)
            {
                // Deleted and saved, or gone with the reload.
                this.detailLabel = null;
                this.Data.Detail = LabelDetailData.Empty;
                this.UpdateCommands();
            }
        }

        private void OnChangesChanged(object? sender, EventArgs e)
        {
            int count = this.changes.Count;
            this.Data.PendingText = count == 0 ? string.Empty : count == 1 ? "1 label not saved" : $"{count} labels not saved";
            this.UpdateCommands();
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
            => new(label.Id.FullId, label.LabelFile.IsReadOnly, label.IsModified, label.IsDeleted, new ObservableList<HitCell>(this.languages.Select(language => new HitCell(label.GetText(language)))));

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
            // A command from the editor can show a label before the window has opened.
            if (this.languages.Count == 0)
            {
                this.UpdateLanguages();
            }

            var rows = new List<TranslationRow>();
            foreach (string language in this.languages)
            {
                Translation? translation = label.GetTranslation(language);
                bool canChange = !label.IsDeleted && this.changes.GetTarget(label, language) != null;

                // A read-only or deleted label says so once in the header; a row says only what differs.
                string hint = label.IsDeleted ? string.Empty
                    : canChange ? (translation == null ? "missing" : string.Empty)
                    : translation == null ? "missing, the label file does not exist in this language"
                    : ReferenceEquals(translation.LabelFile, label.LabelFile) && label.LabelFile.IsReadOnly ? string.Empty
                    : ReadOnlyReason(translation.LabelFile);
                var row = new TranslationRow(language, translation?.Text ?? string.Empty, translation?.Comment ?? string.Empty, !canChange, canChange || translation != null, hint);
                row.PropertyChanged += (_, _) => this.OnTranslationEdited(label, row);
                rows.Add(row);
            }

            LabelFile file = label.LabelFile;
            string info = $"Label file {file.Name}, model {file.Model.Name}"
                + (label.IsDeleted ? ", deleted; it leaves the label files when you save" : file.IsReadOnly ? $", {ReadOnlyReason(file)}" : string.Empty);
            this.isDraft = false;
            this.detailLabel = label;
            this.Data.Detail = new LabelDetailData(label.Id.FullId, info, rows, isDraft: false);
            this.UpdateCommands();
        }

        // A new label: every language to create, those the label file lacks disabled. Texts
        // already entered stay when the developer picks another label file.
        private void ShowDraft(LabelFile file, string text, IReadOnlyList<TranslationRow>? entered)
        {
            var rows = new List<TranslationRow>();
            foreach (string language in this.loader.Settings.CreateLanguages)
            {
                TranslationRow? before = entered?.FirstOrDefault(r => string.Equals(r.Language, language, StringComparison.OrdinalIgnoreCase));
                bool hasFile = file.GetPath(language) != null;
                rows.Add(new TranslationRow(
                    language,
                    before?.Text ?? text,
                    before?.Comment ?? string.Empty,
                    isReadOnly: !hasFile,
                    hasFile,
                    hasFile ? string.Empty : "the label file does not exist in this language; the label is created without it"));
            }

            this.isDraft = true;
            this.detailLabel = null;
            this.Data.Detail = new LabelDetailData(string.Empty, $"New label in label file {file.Name}, model {file.Model.Name}. It gets its ID when you create it.", rows, isDraft: true);
            this.UpdateCommands();
        }

        private void OnLabelFileChanged()
        {
            LabelFile? file = this.SelectedLabelFile();
            this.Data.CopyText = file == null ? "Copy to" : $"Copy to {file.Name}";
            this.Data.MoveText = file == null ? "Move to" : $"Move to {file.Name}";
            if (this.isDraft && file != null)
            {
                this.ShowDraft(file, this.Data.SearchText.Trim(), this.Data.Detail.Translations);
            }

            this.UpdateCommands();
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

        private async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => this.changes.Save(), cancellationToken);
            this.UpdateHitStates();
        }

        private Task CreateAsync(CancellationToken cancellationToken)
            => this.RunAsync("Create label", async token => await this.CreateDraftAsync(token), cancellationToken);

        // Creates the new label in the detail view and shows it with its ID (FA02). A language
        // the label file lacks is named in the Output Window.
        private async Task<bool> CreateDraftAsync(CancellationToken cancellationToken)
        {
            LabelFile? file = this.SelectedLabelFile();
            if (!this.isDraft || file == null)
            {
                return false;
            }

            var texts = this.Data.Detail.Translations.Select(r => new LabelText(r.Language, r.Text, r.Comment)).ToList();
            Label? label;
            try
            {
                label = await Task.Run(() => this.changes.Create(file, texts), cancellationToken);
            }
            catch (ArgumentException exception)
            {
                await this.ShowMessageAsync(exception.Message, cancellationToken);
                return false;
            }

            if (label == null)
            {
                return false;
            }

            this.ShowDetail(label);
            return true;
        }

        private Task CancelNewAsync()
        {
            if (this.isDraft)
            {
                this.isDraft = false;
                this.Data.Detail = LabelDetailData.Empty;
                this.UpdateCommands();
            }

            return Task.CompletedTask;
        }

        // Deleting waits for Save like a change of text (FA03); until then the label is struck through.
        private Task DeleteAsync()
        {
            Label? label = this.detailLabel;
            if (label != null && this.changes.Delete(label))
            {
                this.UpdateHitStates();
                this.ShowDetail(label);
            }

            return Task.CompletedTask;
        }

        // Copy to and Move to (FA03). The target is the label file selected in the toolbar.
        private Task RelocateAsync(bool move, CancellationToken cancellationToken)
            => this.RunAsync(
                move ? "Move label" : "Copy label",
                async token =>
                {
                    Label? label = this.detailLabel;
                    LabelFile? target = this.SelectedLabelFile();
                    if (label == null || target == null)
                    {
                        return;
                    }

                    string question = move
                        ? $"Move {label.Id.FullId} to label file {target.Name}, model {target.Model.Name}? The label gets a new ID. Change its uses in writable models to the new ID? Keep them only if no element uses the label."
                        : $"Copy {label.Id.FullId} to label file {target.Name}, model {target.Model.Name}? The copy gets a new ID. Change the uses in writable models to the copy?";
                    var choices = new ChoiceResultCollection<ReferenceChoice>(new[]
                    {
                        new KeyValuePair<ChoiceDescription, ReferenceChoice>("Change references", ReferenceChoice.Change),
                        new KeyValuePair<ChoiceDescription, ReferenceChoice>("Keep references", ReferenceChoice.Keep),
                        new KeyValuePair<ChoiceDescription, ReferenceChoice>("Cancel", ReferenceChoice.Cancel),
                    });
                    ReferenceChoice choice = await this.extensibility.Shell().ShowPromptAsync(question, new PromptOptions<ReferenceChoice>(choices, 0, ReferenceChoice.Cancel), token);
                    if (choice == ReferenceChoice.Cancel)
                    {
                        return;
                    }

                    IReadOnlyList<string>? references = null;
                    if (choice == ReferenceChoice.Change)
                    {
                        references = await this.FindReferencesAsync(label, token);
                        if (references == null)
                        {
                            return;
                        }
                    }

                    Label? copy = await Task.Run(() => move ? this.operations.Move(label, target, references) : this.operations.Copy(label, target, references), token);
                    if (copy != null)
                    {
                        this.ShowDetail(copy);
                    }
                },
                cancellationToken);

        // Replace (FA13): every use of the label in writable models gets another, existing label.
        private Task ReplaceAsync(CancellationToken cancellationToken)
            => this.RunAsync(
                "Replace label",
                async token =>
                {
                    Label? label = this.detailLabel;
                    if (label == null)
                    {
                        return;
                    }

                    string? input = await this.extensibility.Shell().ShowPromptAsync(
                        $"Replace every use of {label.Id.FullId} in writable models by this label ID:",
                        InputPromptOptions.Default,
                        token);
                    if (input == null)
                    {
                        return;
                    }

                    string? problem = this.operations.CheckReplacement(label, input, out Label? replacement);
                    if (problem != null || replacement == null)
                    {
                        await this.ShowMessageAsync(problem ?? "The label ID cannot be used.", token);
                        return;
                    }

                    IReadOnlyList<string>? files = await this.FindReferencesAsync(label, token);
                    if (files == null)
                    {
                        return;
                    }

                    if (files.Count == 0)
                    {
                        string none = $"{label.Id.FullId} is not used in writable models; nothing was replaced.";
                        this.messages.Report(MessageSeverity.Message, none);
                        this.Data.StatusText = none;
                        return;
                    }

                    string question = $"{label.Id.FullId} is used in {Files(files.Count)} of writable models. Replace it by {replacement.Id.FullId}?";
                    if (!await this.extensibility.Shell().ShowPromptAsync(question, PromptOptions.OKCancel, token))
                    {
                        return;
                    }

                    int changed = await Task.Run(() => this.operations.Replace(label.Id.FullId, replacement.Id.FullId, files), token);
                    this.Data.StatusText = $"Replaced {label.Id.FullId} by {replacement.Id.FullId} in {Files(changed)}.";
                },
                cancellationToken);

        // Finds the element files to change, after making sure no element of a writable model
        // has unsaved changes (B25). Returns null if the developer cancels.
        private async Task<IReadOnlyList<string>?> FindReferencesAsync(Label label, CancellationToken cancellationToken)
        {
            if (!await this.EnsureElementsSavedAsync(cancellationToken))
            {
                return null;
            }

            this.Data.StatusText = $"Searching the uses of {label.Id.FullId} in writable models.";
            return await Task.Run(() => this.operations.FindReferences(label.Id.FullId, cancellationToken), cancellationToken);
        }

        // Every element of a writable model counts, not only those that use the label so far:
        // an unsaved editor may just have got the ID.
        private async Task<bool> EnsureElementsSavedAsync(CancellationToken cancellationToken)
        {
            var models = this.operations.WritableModels();
            IReadOnlyList<UnsavedElement> unsaved = await UnsavedElements.FindAsync(models, cancellationToken);
            if (unsaved.Count == 0)
            {
                return true;
            }

            var names = unsaved.Select(u => Path.GetFileNameWithoutExtension(u.ElementFile)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            string list = string.Join(", ", names.Take(5)) + (names.Count > 5 ? $" and {names.Count - 5} more" : string.Empty);
            var choices = new ChoiceResultCollection<bool>(new[]
            {
                new KeyValuePair<ChoiceDescription, bool>("Save and continue", true),
                new KeyValuePair<ChoiceDescription, bool>("Cancel", false),
            });
            string question = (names.Count == 1 ? $"The element {list} has" : $"The elements {list} have")
                + " unsaved changes. The extension changes element files only when they are saved, so that saving them later does not undo the change.";
            if (!await this.extensibility.Shell().ShowPromptAsync(question, new PromptOptions<bool>(choices, 0, false), cancellationToken))
            {
                return false;
            }

            await UnsavedElements.SaveAsync(unsaved, cancellationToken);
            if ((await UnsavedElements.FindAsync(models, cancellationToken)).Count > 0)
            {
                await this.ShowMessageAsync("Not all elements could be saved. Nothing was changed.", cancellationToken);
                return false;
            }

            return true;
        }

        // One action at a time; the buttons are disabled meanwhile.
        private async Task RunAsync(string entryPoint, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref this.busy, 1) == 1)
            {
                return;
            }

            this.UpdateCommands();
            try
            {
                await this.errorBoundary.RunAsync(entryPoint, action, cancellationToken);
            }
            finally
            {
                Volatile.Write(ref this.busy, 0);
                this.UpdateCommands();
            }
        }

        private Task ShowMessageAsync(string message, CancellationToken cancellationToken)
        {
            this.Data.StatusText = message;
            return this.extensibility.Shell().ShowPromptAsync(message, PromptOptions.OK, cancellationToken);
        }

        private LabelFile? SelectedLabelFile()
        {
            lock (this.gate)
            {
                int index = this.Data.SelectedLabelFileIndex;
                return index >= 0 && index < this.labelFiles.Count ? this.labelFiles[index] : null;
            }
        }

        private void UpdateLabelFiles()
        {
            if (!this.store.IsLoaded)
            {
                return;
            }

            IReadOnlyList<LabelFile> files = this.changes.CreatableLabelFiles();
            var names = files.Select(f => files.Count(o => string.Equals(o.Name, f.Name, StringComparison.OrdinalIgnoreCase)) > 1 ? $"{f.Name} ({f.Model.Name})" : f.Name).ToList();
            int index;
            lock (this.gate)
            {
                LabelFile? selected = this.SelectedLabelFile();
                index = selected == null ? -1 : IndexOfFile(files, selected);
                this.labelFiles = files;
            }

            this.Data.LabelFiles = names;
            this.Data.SelectedLabelFileIndex = index >= 0 ? index : files.Count > 0 ? 0 : -1;
            this.OnLabelFileChanged();
        }

        private void UpdateCommands()
        {
            bool idle = Volatile.Read(ref this.busy) == 0;
            Label? label = this.detailLabel;
            bool usable = !this.isDraft && label != null && !label.IsDeleted;
            LabelFile? target = this.SelectedLabelFile();
            bool canCopy = usable && target != null && !ReferenceEquals(target, label!.LabelFile);

            this.Data.SaveCommand.CanExecute = idle && this.changes.Count > 0;
            this.Data.SaveAndInsertCommand.CanExecute = idle && (usable || this.isDraft);
            this.Data.InsertCommand.CanExecute = idle && usable;
            this.Data.NewCommand.CanExecute = idle && target != null;
            this.Data.CreateCommand.CanExecute = idle && this.isDraft && target != null;
            this.Data.CancelNewCommand.CanExecute = idle && this.isDraft;
            this.Data.DeleteCommand.CanExecute = idle && usable && !label!.LabelFile.IsReadOnly;
            this.Data.CopyCommand.CanExecute = idle && canCopy;
            this.Data.MoveCommand.CanExecute = idle && canCopy && !label!.LabelFile.IsReadOnly;
            this.Data.ReplaceCommand.CanExecute = idle && usable;
            this.Data.FindReferencesCommand.CanExecute = usable;
        }

        // Saved and deleted labels change their marks in the list.
        private void UpdateHitStates()
        {
            lock (this.gate)
            {
                for (int i = 0; i < this.shown.Count && i < this.Data.Hits.Count; i++)
                {
                    this.Data.Hits[i].IsModified = this.shown[i].IsModified;
                    this.Data.Hits[i].IsDeleted = this.shown[i].IsDeleted;
                }
            }
        }

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

        private static string Files(int count) => count == 1 ? "1 file" : $"{count} files";

        private static string ReadOnlyReason(LabelFile file)
            => file.IsCompiled ? "compiled, read-only"
                : file.Model.IsLocked ? "model locked, read-only"
                : "read-only model";

        // After a reload the label files are new objects; the same name and model is the same file.
        private static int IndexOfFile(IReadOnlyList<LabelFile> files, LabelFile file)
        {
            for (int i = 0; i < files.Count; i++)
            {
                if (string.Equals(files[i].Name, file.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(files[i].Model.Name, file.Model.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

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
