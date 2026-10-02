using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Sources;

namespace BE.LabelExtension.Core.Store
{
    /// <summary>
    /// Holds all loaded labels in memory, indexed by label ID.
    /// </summary>
    /// <remarks>
    /// Loading runs in the background and builds a new index, which replaces the old one only
    /// at the end. Searches during a load therefore see the previous state, also when
    /// reloading after an external change. Labels with changes not yet saved are taken over
    /// into the new index. A load started while another runs cancels the earlier one.
    /// </remarks>
    public sealed class LabelStore : IDisposable
    {
        private readonly ModelDiscovery discovery;
        private readonly IReadOnlyList<ILabelSource> sources;
        private readonly LabelFileWatcher watcher;
        private readonly IMessageSink messages;
        private Snapshot? snapshot;
        private CancellationTokenSource? currentLoad;
        private int loading;

        /// <summary>Creates an empty store.</summary>
        /// <param name="discovery">Finds the package directories and models.</param>
        /// <param name="sources">The label sources; earlier sources win over later ones.</param>
        /// <param name="watcher">Watches the writable label files after loading.</param>
        /// <param name="messages">Receives the messages of loading.</param>
        public LabelStore(ModelDiscovery discovery, IEnumerable<ILabelSource> sources, LabelFileWatcher watcher, IMessageSink messages)
        {
            this.discovery = discovery;
            this.sources = sources.ToList();
            this.watcher = watcher;
            this.messages = messages;
            this.watcher.ExternalChange += (sender, e) => this.ExternalChange?.Invoke(this, e);
        }

        /// <summary>Raised when a load has completed and the index has been replaced.</summary>
        public event EventHandler? Changed;

        /// <summary>Raised when loaded writable label files changed from outside.</summary>
        public event EventHandler<LabelFilesChangedEventArgs>? ExternalChange;

        /// <summary>Whether labels have been loaded at least once.</summary>
        public bool IsLoaded => Volatile.Read(ref this.snapshot) != null;

        /// <summary>Whether a load is running.</summary>
        public bool IsLoading => Volatile.Read(ref this.loading) > 0;

        /// <summary>The models found by the last load.</summary>
        public IReadOnlyList<ModelInfo> Models => this.Current.Models;

        /// <summary>The label files found by the last load, also those without a loaded language.</summary>
        public IReadOnlyList<LabelFile> LabelFiles => this.Current.LabelFiles;

        /// <summary>All loaded labels.</summary>
        public IReadOnlyCollection<Label> Labels => this.Current.Index.Values;

        /// <summary>The number of loaded labels.</summary>
        public int Count => this.Current.Index.Count;

        /// <summary>The watcher of the writable label files, suspended for own writes.</summary>
        public LabelFileWatcher Watcher => this.watcher;

        private Snapshot Current => Volatile.Read(ref this.snapshot) ?? Snapshot.Empty;

        /// <summary>Finds a label by its ID.</summary>
        /// <param name="id">The label ID.</param>
        /// <returns>The label, or <c>null</c> if the ID is unknown.</returns>
        public Label? Find(LabelId id) => id.FullId == null ? null : this.Find(id.FullId);

        /// <summary>Finds a label by its complete ID including the <c>@</c>.</summary>
        /// <param name="fullId">The complete label ID.</param>
        /// <returns>The label, or <c>null</c> if the ID is unknown.</returns>
        public Label? Find(string fullId) => this.Current.Index.TryGetValue(fullId, out Label? label) ? label : null;

        /// <summary>Finds the model a file belongs to, see <see cref="ModelDiscovery.FindModelFor"/>.</summary>
        /// <param name="filePath">Path of an element file or a <c>.xpp</c> file.</param>
        /// <returns>The model, or <c>null</c>.</returns>
        public ModelInfo? FindModelFor(string filePath) => ModelDiscovery.FindModelFor(filePath, this.Models);

        /// <summary>
        /// Loads the labels in the background. Problems with single files or directories are
        /// reported, and the other labels are loaded. No exception leaves the store except a
        /// requested cancellation.
        /// </summary>
        /// <param name="settings">Package directories and languages.</param>
        /// <param name="cancellationToken">Cancels the load, for example when Visual Studio closes.</param>
        /// <returns>A task that completes when the load has completed or failed.</returns>
        public Task LoadAsync(LabelSettings settings, CancellationToken cancellationToken)
        {
            var load = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Interlocked.Exchange(ref this.currentLoad, load)?.Cancel();
            return Task.Run(
                () =>
                {
                    Interlocked.Increment(ref this.loading);
                    try
                    {
                        this.Load(settings, load.Token);
                    }
                    catch (OperationCanceledException) when (load.Token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        this.messages.Report(MessageSeverity.Error, $"Loading the labels failed: {exception.Message}\r\n{exception}");
                    }
                    finally
                    {
                        Interlocked.Decrement(ref this.loading);
                    }
                },
                load.Token);
        }

        /// <summary>Determines the package directories: those of the metadata configuration, then the further ones.</summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The package directories, each once.</returns>
        public IReadOnlyList<PackageDirectory> GetPackageDirectories(LabelSettings settings)
        {
            var directories = new List<PackageDirectory>();
            if (!string.IsNullOrWhiteSpace(settings.MetadataConfiguration))
            {
                try
                {
                    directories.AddRange(this.discovery.ReadConfiguration(settings.MetadataConfiguration!).GetPackageDirectories());
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is SerializationException)
                {
                    this.messages.Report(MessageSeverity.Warning, $"Metadata configuration {settings.MetadataConfiguration} could not be read: {exception.Message}");
                }
            }

            foreach (string path in settings.PackageDirectories.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                if (!directories.Any(d => MetadataConfiguration.PathsEqual(d.Path, path)))
                {
                    directories.Add(new PackageDirectory(path, isReference: false));
                }
            }

            return directories;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Interlocked.Exchange(ref this.currentLoad, null)?.Cancel();
            this.watcher.Dispose();
        }

        private void Load(LabelSettings settings, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            IReadOnlyList<PackageDirectory> directories = this.GetPackageDirectories(settings);
            if (directories.Count == 0)
            {
                this.messages.Report(MessageSeverity.Warning, "No package directory is configured, so no labels are loaded.");
            }

            IReadOnlyList<ModelInfo> models = this.discovery.FindModels(directories, this.messages, cancellationToken);
            var request = new LabelLoadRequest(models, directories, settings.LoadLanguages, this.messages);

            var results = new List<LabelSourceResult>();
            foreach (ILabelSource source in this.sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(source.Load(request, cancellationToken));
            }

            Snapshot next = Merge(models, results, this.messages);
            cancellationToken.ThrowIfCancellationRequested();

            // Changes not yet saved survive the reload.
            foreach (Label unsaved in this.Current.Index.Values.Where(l => l.IsModified))
            {
                next.Index[unsaved.Id.FullId] = unsaved;
            }

            Volatile.Write(ref this.snapshot, next);
            this.watcher.Start(next.WatchedFiles);

            this.messages.Report(
                MessageSeverity.Message,
                $"{next.Index.Count} labels loaded from {next.DocumentCount} files of {next.LabelFiles.Count} label files in {models.Count} models, in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
            this.Changed?.Invoke(this, EventArgs.Empty);
        }

        private static Snapshot Merge(IReadOnlyList<ModelInfo> models, IReadOnlyList<LabelSourceResult> results, IMessageSink messages)
        {
            // A .label.txt wins over the compiled resources of the same label file and language.
            List<LabelFile> textFiles = results.SelectMany(r => r.LabelFiles).Where(f => !f.IsCompiled).ToList();
            var textLanguages = new HashSet<string>(
                textFiles.SelectMany(f => f.Languages.Select(language => Key(f.Name, language))),
                StringComparer.OrdinalIgnoreCase);

            var documents = new List<LabelDocument>();
            foreach (LabelDocument document in results.SelectMany(r => r.Documents))
            {
                if (!document.LabelFile.IsCompiled || !textLanguages.Contains(Key(document.LabelFile.Name, document.Language)))
                {
                    documents.Add(document);
                }
            }

            List<LabelFile> labelFiles = textFiles
                .Concat(results.SelectMany(r => r.LabelFiles).Where(f => f.IsCompiled && f.Languages.Any(language => !textLanguages.Contains(Key(f.Name, language)))))
                .ToList();

            var index = new Dictionary<string, Label>(StringComparer.Ordinal);
            int duplicates = 0;
            string? duplicateExample = null;
            foreach (LabelDocument document in documents)
            {
                int invalid = 0;
                string? invalidExample = null;
                foreach (LabelEntry entry in document.Entries)
                {
                    if (!LabelId.TryCreate(document.LabelFile.IdPrefix, entry.Key, out LabelId id))
                    {
                        invalid++;
                        invalidExample ??= entry.Key;
                        continue;
                    }

                    if (!index.TryGetValue(id.FullId, out Label? label))
                    {
                        label = new Label(id, document.LabelFile);
                        index.Add(id.FullId, label);
                    }

                    if (!label.TryAddTranslation(new Translation(document.Language, entry.Text, entry.Comment, document.LabelFile)))
                    {
                        duplicates++;
                        duplicateExample ??= $"{id.FullId} in {document.Language} from {document.Path}";
                    }
                }

                if (invalid > 0)
                {
                    messages.Report(MessageSeverity.Warning, $"{invalid} labels with an invalid ID are skipped, for example '{invalidExample}': {document.Path}");
                }
            }

            if (duplicates > 0)
            {
                messages.Report(MessageSeverity.Warning, $"{duplicates} translations exist in more than one label file; the first one found is used, for example {duplicateExample}.");
            }

            List<string> watched = documents
                .Where(d => !d.LabelFile.IsReadOnly)
                .Select(d => d.Path)
                .ToList();

            return new Snapshot(models, labelFiles, index, documents.Count, watched);
        }

        private static string Key(string labelFile, string language) => labelFile + "|" + language;

        private sealed class Snapshot
        {
            public static readonly Snapshot Empty = new(Array.Empty<ModelInfo>(), Array.Empty<LabelFile>(), new Dictionary<string, Label>(), 0, Array.Empty<string>());

            public Snapshot(IReadOnlyList<ModelInfo> models, IReadOnlyList<LabelFile> labelFiles, Dictionary<string, Label> index, int documentCount, IReadOnlyList<string> watchedFiles)
            {
                this.Models = models;
                this.LabelFiles = labelFiles;
                this.Index = index;
                this.DocumentCount = documentCount;
                this.WatchedFiles = watchedFiles;
            }

            public IReadOnlyList<ModelInfo> Models { get; }

            public IReadOnlyList<LabelFile> LabelFiles { get; }

            public Dictionary<string, Label> Index { get; }

            public int DocumentCount { get; }

            public IReadOnlyList<string> WatchedFiles { get; }
        }
    }
}
