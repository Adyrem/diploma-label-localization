using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;

namespace BE.LabelExtension.Core.Store
{
    /// <summary>
    /// Changes to labels that are not yet in the label files, and saving them (FA03). A change
    /// stays in the label store until the developer saves.
    /// </summary>
    /// <remarks>
    /// Saving reads every affected file anew, applies only the changed labels and replaces the
    /// file through a temporary one. Changes from outside in the meantime and languages that are
    /// not loaded stay as they are (RE1). The file watcher rests during the write, so the own
    /// save is not reported as a change from outside (FA15).
    /// </remarks>
    public sealed class LabelChanges
    {
        private readonly LabelStore store;
        private readonly IMessageSink messages;
        private readonly Dictionary<Label, HashSet<string>> pending = new();

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

        /// <summary>The number of labels with changes not yet saved.</summary>
        public int Count
        {
            get
            {
                lock (this.pending)
                {
                    return this.pending.Count;
                }
            }
        }

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
        /// <returns>Whether the change is possible; not for read-only labels (TC07).</returns>
        /// <exception cref="ArgumentException">Text or comment contain a line break, which the format does not allow.</exception>
        public bool Edit(Label label, string language, string text, string? comment)
        {
            if (HasLineBreak(text) || HasLineBreak(comment))
            {
                throw new ArgumentException("Text and comment of a label cannot contain a line break.");
            }

            LabelFile? target = this.GetTarget(label, language);
            if (target == null)
            {
                return false;
            }

            label.SetTranslation(new Translation(language, text, string.IsNullOrEmpty(comment) ? null : comment, target));
            lock (this.pending)
            {
                if (!this.pending.TryGetValue(label, out HashSet<string>? languages))
                {
                    languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    this.pending.Add(label, languages);
                }

                languages.Add(language);
                label.IsModified = true;
            }

            this.Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Writes all changes into the label files. A file that cannot be written is reported,
        /// and its changes stay pending.
        /// </summary>
        /// <returns>The number of labels saved.</returns>
        public int Save()
        {
            List<(Label Label, string Language)> changes;
            lock (this.pending)
            {
                changes = this.pending.SelectMany(p => p.Value.Select(language => (p.Key, language))).ToList();
            }

            if (changes.Count == 0)
            {
                return 0;
            }

            var saved = new List<(Label Label, string Language)>();
            var files = changes
                .Select(c => (Change: c, Translation: c.Label.GetTranslation(c.Language)))
                .Where(c => c.Translation != null)
                .GroupBy(c => c.Translation!.LabelFile.GetPath(c.Change.Language) ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            using (this.store.Watcher.Suspend())
            {
                foreach (var file in files)
                {
                    if (WriteFile(file.Key, file.Select(c => (c.Change.Label, c.Translation!)).ToList()))
                    {
                        saved.AddRange(file.Select(c => c.Change));
                    }
                }
            }

            lock (this.pending)
            {
                foreach ((Label label, string language) in saved)
                {
                    if (this.pending.TryGetValue(label, out HashSet<string>? languages))
                    {
                        languages.Remove(language);
                        if (languages.Count == 0)
                        {
                            this.pending.Remove(label);
                            label.IsModified = false;
                        }
                    }
                }
            }

            int labels = saved.Select(s => s.Label).Distinct().Count();
            this.messages.Report(MessageSeverity.Message, $"Saved {saved.Count} translations of {labels} labels.");
            this.Changed?.Invoke(this, EventArgs.Empty);
            return labels;
        }

        // Reads the file anew, replaces or appends the changed labels and writes it with the
        // byte order mark as read.
        private bool WriteFile(string path, IReadOnlyList<(Label Label, Translation Translation)> changes)
        {
            try
            {
                if (path.Length == 0)
                {
                    throw new IOException("The label file has no file for this language.");
                }

                LabelFileContent content = LabelFileFormat.Read(path);
                if (content.IsDamaged)
                {
                    this.messages.Report(MessageSeverity.Error, $"Label file is damaged and is not written, so that none of its lines get lost: {path}");
                    return false;
                }

                var entries = content.Entries.ToList();
                var positions = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < entries.Count; i++)
                {
                    positions[entries[i].Key] = i;
                }

                foreach ((Label label, Translation translation) in changes)
                {
                    var entry = new LabelEntry(label.Id.Key, translation.Text, translation.Comment);
                    if (positions.TryGetValue(entry.Key, out int position))
                    {
                        entries[position] = entry;
                    }
                    else
                    {
                        positions[entry.Key] = entries.Count;
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

        private static bool HasLineBreak(string? value) => value != null && (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0);
    }
}
