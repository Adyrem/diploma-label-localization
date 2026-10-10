using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Sources;

namespace BE.LabelExtension.Core.Store
{
    /// <summary>
    /// Creates, changes, deletes and copies labels and saves them (FA02, FA03).
    /// </summary>
    /// <remarks>
    /// <para>Changes and deletions stay in the label store until the developer saves. Creating
    /// and copying save at once, because code may point to the new label right after.</para>
    /// <para>Saving reads every affected file anew, applies only the own changes and replaces
    /// the file through a temporary one. Changes from outside in the meantime and languages
    /// that are not loaded stay as they are (RE1). The file watcher rests during the write, so
    /// the own save is not reported as a change from outside (FA15).</para>
    /// </remarks>
    public sealed class LabelChanges
    {
        private const string ExtensionSuffix = "_Extension";

        private readonly LabelStore store;
        private readonly IMessageSink messages;
        private readonly Dictionary<Label, HashSet<string>> edits = new();
        private readonly HashSet<Label> deletions = new();

        /// <summary>Creates the changes of a label store.</summary>
        /// <param name="store">The label store.</param>
        /// <param name="messages">Receives the result of saving and every file that could not be written.</param>
        public LabelChanges(LabelStore store, IMessageSink messages)
        {
            this.store = store;
            this.messages = messages;
        }

        /// <summary>Raised when changes are added or saved.</summary>
        public event EventHandler? Changed;

        /// <summary>The number of labels with changes or a deletion not yet saved.</summary>
        public int Count
        {
            get
            {
                lock (this.edits)
                {
                    return this.edits.Keys.Concat(this.deletions).Distinct().Count();
                }
            }
        }

        /// <summary>
        /// The label files new labels can be created in: writable, not only compiled, and not an
        /// <c>_Extension</c>, which only holds translations of existing labels (F9, TC07).
        /// </summary>
        /// <returns>The label files, sorted by name.</returns>
        public IReadOnlyList<LabelFile> CreatableLabelFiles()
            => this.store.LabelFiles
                .Where(f => !f.IsReadOnly && !IsExtension(f))
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.Model.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>
        /// Returns the label file a translation in this language is written to: the file of the
        /// existing translation, otherwise the label file of the label, otherwise another
        /// writable label file with the same IDs, such as its <c>_Extension</c>. The file must
        /// exist in this language; the extension does not create new files (FA02).
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="language">The language.</param>
        /// <returns>The label file, or <c>null</c> if the translation cannot be changed.</returns>
        public LabelFile? GetTarget(Label label, string language)
        {
            Translation? existing = label.GetTranslation(language);
            if (existing != null)
            {
                return !existing.IsReadOnly && existing.LabelFile.GetPath(language) != null ? existing.LabelFile : null;
            }

            if (!label.LabelFile.IsReadOnly && label.LabelFile.GetPath(language) != null)
            {
                return label.LabelFile;
            }

            return this.store.LabelFiles.FirstOrDefault(f =>
                !f.IsReadOnly
                && string.Equals(f.IdPrefix, label.LabelFile.IdPrefix, StringComparison.OrdinalIgnoreCase)
                && f.GetPath(language) != null);
        }

        /// <summary>
        /// Changes text and comment of a label in one language, in the label store only. An
        /// empty comment removes the comment.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="language">The language.</param>
        /// <param name="text">The new text.</param>
        /// <param name="comment">The new comment, empty or <c>null</c> for none.</param>
        /// <returns>Whether the change is possible; not for read-only or deleted labels (TC07).</returns>
        /// <exception cref="ArgumentException">Text or comment contain a line break, which the format does not allow.</exception>
        public bool Edit(Label label, string language, string text, string? comment)
        {
            CheckLineBreaks(text, comment);
            LabelFile? target = this.GetTarget(label, language);
            if (target == null || label.IsDeleted)
            {
                return false;
            }

            label.SetTranslation(new Translation(language, text, string.IsNullOrEmpty(comment) ? null : comment, target));
            lock (this.edits)
            {
                this.AddEdit(label, language);
            }

            this.Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Creates a new label in a label file and saves it at once (FA02). Its ID has the new
        /// form with a new label part. A language the label file does not exist in is left out
        /// and named in the Output Window.
        /// </summary>
        /// <param name="labelFile">One of <see cref="CreatableLabelFiles"/>.</param>
        /// <param name="translations">Text and comment per language to create.</param>
        /// <returns>The new label, or <c>null</c> if the label file exists in none of the languages.</returns>
        /// <exception cref="ArgumentException">The label file cannot take new labels, or a text contains a line break.</exception>
        public Label? Create(LabelFile labelFile, IReadOnlyList<LabelText> translations)
        {
            if (labelFile.IsReadOnly || IsExtension(labelFile))
            {
                throw new ArgumentException($"New labels cannot be created in {labelFile.Name}.", nameof(labelFile));
            }

            foreach (LabelText translation in translations)
            {
                CheckLineBreaks(translation.Text, translation.Comment);
            }

            string key = LabelKeyGenerator.NewKey(candidate => this.store.Find($"@{labelFile.IdPrefix}:{candidate}") != null);
            LabelId id = LabelId.Parse($"@{labelFile.IdPrefix}:{key}");
            return this.CreateLabel(id, labelFile, translations, "created");
        }

        /// <summary>
        /// Copies a label into another label file and saves it at once (FA03): a new label with
        /// an ID in the new form, in every language both label files have, with text and comment
        /// of the original. Languages that are not loaded are read from the files of the
        /// original. Labels of read-only models and compiled labels can be copied (TC07).
        /// </summary>
        /// <param name="original">The label to copy.</param>
        /// <param name="target">One of <see cref="CreatableLabelFiles"/>.</param>
        /// <returns>The copy, or <c>null</c> if the two label files have no language in common.</returns>
        public Label? Copy(Label original, LabelFile target)
        {
            var translations = new List<LabelText>();
            foreach (string language in target.Languages)
            {
                Translation? loaded = original.GetTranslation(language);
                LabelEntry? entry = loaded != null ? new LabelEntry(original.Id.Key, loaded.Text, loaded.Comment) : this.ReadFromFile(original, language);
                if (entry != null)
                {
                    translations.Add(new LabelText(language, entry.Text, entry.Comment));
                }
            }

            if (translations.Count == 0)
            {
                this.messages.Report(MessageSeverity.Warning, $"{original.Id.FullId} has no language in common with {target.Name} and is not copied.");
                return null;
            }

            return this.Create(target, translations);
        }

        /// <summary>
        /// Deletes a label in all languages of its label file, in the label store only, until
        /// the developer saves. The search no longer finds it. A translation in another
        /// writable file, such as the <c>_Extension</c>, goes as well, so the label does not
        /// come back with the next load.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <returns>Whether the label can be deleted; not for read-only labels (TC07).</returns>
        public bool Delete(Label label)
        {
            if (label.LabelFile.IsReadOnly)
            {
                return false;
            }

            lock (this.edits)
            {
                this.edits.Remove(label);
                this.deletions.Add(label);
                label.IsDeleted = true;
                label.IsModified = true;
            }

            this.Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Writes all changes and deletions into the label files. A file that cannot be written
        /// is reported, and its changes stay pending.
        /// </summary>
        /// <returns>The number of labels saved.</returns>
        public int Save() => this.Save(null);

        /// <summary>Writes the changes and deletions of some labels, for example after moving one.</summary>
        /// <param name="labels">The labels to save; <c>null</c> for all.</param>
        /// <returns>The number of labels saved.</returns>
        public int Save(IEnumerable<Label>? labels)
        {
            List<(Label Label, string Language)> edited;
            List<Label> deleted;
            lock (this.edits)
            {
                var only = labels == null ? null : new HashSet<Label>(labels);
                edited = this.edits.Where(e => only == null || only.Contains(e.Key)).SelectMany(e => e.Value.Select(language => (e.Key, language))).ToList();
                deleted = this.deletions.Where(d => only == null || only.Contains(d)).ToList();
            }

            if (edited.Count == 0 && deleted.Count == 0)
            {
                return 0;
            }

            // What to write, by file: the entry to put in, or null to remove the label.
            var operations = new Dictionary<string, List<(string Key, LabelEntry? Entry)>>(StringComparer.OrdinalIgnoreCase);
            var editPaths = new Dictionary<(Label, string), string>();
            var deletePaths = new Dictionary<Label, List<string>>();
            foreach ((Label label, string language) in edited)
            {
                Translation? translation = label.GetTranslation(language);
                string? path = translation?.LabelFile.GetPath(language);
                if (translation != null && path != null)
                {
                    Add(operations, path, label.Id.Key, new LabelEntry(label.Id.Key, translation.Text, translation.Comment));
                    editPaths[(label, language)] = path;
                }
            }

            foreach (Label label in deleted)
            {
                IEnumerable<string?> paths = label.LabelFile.Languages.Select(label.LabelFile.GetPath)
                    .Concat(label.Translations.Where(t => !t.IsReadOnly).Select(t => t.LabelFile.GetPath(t.Language)));
                deletePaths[label] = paths.Where(p => p != null).Select(p => p!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                foreach (string path in deletePaths[label])
                {
                    Add(operations, path, label.Id.Key, null);
                }
            }

            var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (this.store.Watcher.Suspend())
            {
                foreach (var file in operations)
                {
                    if (this.WriteFile(file.Key, file.Value))
                    {
                        written.Add(file.Key);
                    }
                }
            }

            int saved;
            var removed = new List<Label>();
            lock (this.edits)
            {
                foreach (var edit in editPaths.Where(e => written.Contains(e.Value)))
                {
                    (Label label, string language) = edit.Key;
                    if (this.edits.TryGetValue(label, out HashSet<string>? languages) && languages.Remove(language) && languages.Count == 0)
                    {
                        this.edits.Remove(label);
                        label.IsModified = this.deletions.Contains(label);
                    }
                }

                foreach (var deletion in deletePaths.Where(d => d.Value.All(written.Contains)))
                {
                    this.deletions.Remove(deletion.Key);
                    deletion.Key.IsModified = false;
                    removed.Add(deletion.Key);
                }

                saved = editPaths.Where(e => written.Contains(e.Value)).Select(e => e.Key.Item1).Concat(removed).Distinct().Count();
            }

            if (removed.Count > 0)
            {
                this.store.Apply(Array.Empty<Label>(), removed);
            }

            this.messages.Report(MessageSeverity.Message, $"Saved {Counted(saved, "label")}, {removed.Count} of them deleted, in {Counted(written.Count, "file")}.");
            this.Changed?.Invoke(this, EventArgs.Empty);
            return saved;
        }

        private static string Counted(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

        private static bool IsExtension(LabelFile labelFile)
            => labelFile.Name.EndsWith(ExtensionSuffix, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(labelFile.IdPrefix, labelFile.Name, StringComparison.OrdinalIgnoreCase);

        private static void Add(Dictionary<string, List<(string Key, LabelEntry? Entry)>> operations, string path, string key, LabelEntry? entry)
        {
            if (!operations.TryGetValue(path, out List<(string Key, LabelEntry? Entry)>? list))
            {
                list = new List<(string Key, LabelEntry? Entry)>();
                operations.Add(path, list);
            }

            list.Add((key, entry));
        }

        private static void CheckLineBreaks(string text, string? comment)
        {
            if (HasLineBreak(text) || HasLineBreak(comment))
            {
                throw new ArgumentException("Text and comment of a label cannot contain a line break.");
            }
        }

        private static bool HasLineBreak(string? value) => value != null && (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0);

        // Writes a new label into every language given, adds it to the store and saves it.
        // A file that cannot be written leaves the label unsaved in that language (UC02 5a).
        private Label? CreateLabel(LabelId id, LabelFile labelFile, IReadOnlyList<LabelText> translations, string verb)
        {
            List<LabelText> present = translations.Where(t => labelFile.GetPath(t.Language) != null).ToList();
            foreach (LabelText missing in translations.Where(t => labelFile.GetPath(t.Language) == null))
            {
                this.messages.Report(MessageSeverity.Warning, $"{labelFile.Name} does not exist in {missing.Language}; {id.FullId} is {verb} without this language.");
            }

            if (present.Count == 0)
            {
                this.messages.Report(MessageSeverity.Warning, $"{labelFile.Name} exists in none of the languages; no label is {verb}.");
                return null;
            }

            var label = new Label(id, labelFile);
            IReadOnlyList<string> loaded = this.store.Languages;
            var unloaded = new List<(string Path, LabelEntry Entry)>();
            foreach (LabelText translation in present)
            {
                string comment = translation.Comment ?? string.Empty;
                if (loaded.Contains(translation.Language, StringComparer.OrdinalIgnoreCase))
                {
                    label.TryAddTranslation(new Translation(translation.Language, translation.Text, comment.Length == 0 ? null : comment, labelFile));
                }
                else
                {
                    unloaded.Add((labelFile.GetPath(translation.Language)!, new LabelEntry(id.Key, translation.Text, comment.Length == 0 ? null : comment)));
                }
            }

            lock (this.edits)
            {
                foreach (Translation translation in label.Translations)
                {
                    this.AddEdit(label, translation.Language);
                }
            }

            // Languages that are not loaded are not in the store; they are written directly.
            using (this.store.Watcher.Suspend())
            {
                foreach ((string path, LabelEntry entry) in unloaded)
                {
                    this.WriteFile(path, new[] { (entry.Key, (LabelEntry?)entry) });
                }
            }

            // Saved before it enters the store, so that a search started by the store shows it saved.
            this.Save(new[] { label });
            this.store.Apply(new[] { label }, Array.Empty<Label>());
            this.messages.Report(MessageSeverity.Message, $"{id.FullId} {verb} in {labelFile.Name}, model {labelFile.Model.Name}.");
            return label;
        }

        private void AddEdit(Label label, string language)
        {
            if (!this.edits.TryGetValue(label, out HashSet<string>? languages))
            {
                languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                this.edits.Add(label, languages);
            }

            languages.Add(language);
            label.IsModified = true;
        }

        // Text and comment of a language that is not loaded, from the file of the label file.
        private LabelEntry? ReadFromFile(Label label, string language)
        {
            string? path = label.LabelFile.GetPath(language);
            if (path == null)
            {
                return null;
            }

            try
            {
                IReadOnlyList<LabelEntry> entries = label.LabelFile.IsCompiled
                    ? ResourceAssemblyReader.ReadFirstResource(path)
                    : LabelFileFormat.Read(path).Entries;
                return entries.FirstOrDefault(e => string.Equals(e.Key, label.Id.Key, StringComparison.Ordinal));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is BadImageFormatException || exception is InvalidOperationException)
            {
                this.messages.Report(MessageSeverity.Warning, $"{label.Id.FullId} could not be read in {language}: {path} ({exception.Message})");
                return null;
            }
        }

        // Reads the file anew, puts in, replaces or removes the given labels and writes it with
        // the byte order mark as read.
        private bool WriteFile(string path, IReadOnlyList<(string Key, LabelEntry? Entry)> changes)
        {
            try
            {
                LabelFileContent content = LabelFileFormat.Read(path);
                if (content.IsDamaged)
                {
                    this.messages.Report(MessageSeverity.Error, $"Label file is damaged and is not written, so that none of its lines get lost: {path}");
                    return false;
                }

                var entries = content.Entries.ToList();
                foreach ((string key, LabelEntry? entry) in changes)
                {
                    int position = entries.FindIndex(e => string.Equals(e.Key, key, StringComparison.Ordinal));
                    if (entry == null)
                    {
                        if (position >= 0)
                        {
                            entries.RemoveAt(position);
                        }
                    }
                    else if (position >= 0)
                    {
                        entries[position] = entry;
                    }
                    else
                    {
                        entries.Add(entry);
                    }
                }

                LabelFileFormat.WriteFile(path, entries, content.HasByteOrderMark);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                this.messages.Report(MessageSeverity.Error, $"Label file could not be saved, its changes stay pending: {path} ({exception.Message})");
                return false;
            }
        }
    }
}
