using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Usages;

namespace BE.LabelExtension.Core.Store
{
    /// <summary>
    /// Copies, moves and replaces labels together with their references in the elements
    /// (FA03, FA13). References are changed only in writable models.
    /// </summary>
    /// <remarks>
    /// Finding the references and changing them are two steps, so that the tool window can
    /// make sure in between that none of the elements has unsaved changes in Visual Studio
    /// (B25). Saving such an element afterwards would bring back the old ID.
    /// </remarks>
    public sealed class LabelOperations
    {
        private readonly LabelStore store;
        private readonly LabelChanges changes;
        private readonly IMessageSink messages;

        /// <summary>Creates the operations on a label store.</summary>
        /// <param name="store">The label store.</param>
        /// <param name="changes">Creates and deletes the labels.</param>
        /// <param name="messages">Receives the results and every file that could not be changed.</param>
        public LabelOperations(LabelStore store, LabelChanges changes, IMessageSink messages)
        {
            this.store = store;
            this.changes = changes;
            this.messages = messages;
        }

        /// <summary>The models whose elements may be changed.</summary>
        /// <returns>The writable models.</returns>
        public IReadOnlyList<ModelInfo> WritableModels() => this.store.Models.Where(m => !m.IsReadOnly).ToList();

        /// <summary>Finds the element files in writable models that use a label.</summary>
        /// <param name="labelId">The complete label ID.</param>
        /// <param name="cancellationToken">Cancels the search.</param>
        /// <returns>The files with at least one use.</returns>
        public IReadOnlyList<string> FindReferences(string labelId, CancellationToken cancellationToken)
            => LabelReferences.FindFiles(this.WritableModels(), labelId, cancellationToken);

        /// <summary>
        /// Copies a label into another label file, see <see cref="LabelChanges.Copy"/>, and
        /// changes the given references to the copy.
        /// </summary>
        /// <param name="original">The label to copy.</param>
        /// <param name="target">One of <see cref="LabelChanges.CreatableLabelFiles"/>.</param>
        /// <param name="references">Files from <see cref="FindReferences"/>, or <c>null</c> to keep the references.</param>
        /// <returns>The copy, or <c>null</c> if nothing was copied.</returns>
        public Label? Copy(Label original, LabelFile target, IReadOnlyList<string>? references)
        {
            Label? copy = this.changes.Copy(original, target);
            if (copy != null && references != null)
            {
                this.ChangeReferences(original.Id.FullId, copy.Id.FullId, references);
            }

            return copy;
        }

        /// <summary>
        /// Moves a label into another label file: copies it, changes the given references and
        /// deletes the original, saved at once. If a reference could not be changed, the
        /// original stays, so that no element points to a label that no longer exists.
        /// </summary>
        /// <param name="original">The label to move, from a writable label file.</param>
        /// <param name="target">One of <see cref="LabelChanges.CreatableLabelFiles"/>, not the label file of the original.</param>
        /// <param name="references">Files from <see cref="FindReferences"/>, or <c>null</c> to keep the references.</param>
        /// <returns>The copy, or <c>null</c> if nothing was moved.</returns>
        public Label? Move(Label original, LabelFile target, IReadOnlyList<string>? references)
        {
            if (original.LabelFile.IsReadOnly)
            {
                this.messages.Report(MessageSeverity.Warning, $"{original.Id.FullId} is read-only and cannot be moved. Copy it instead.");
                return null;
            }

            if (ReferenceEquals(original.LabelFile, target))
            {
                this.messages.Report(MessageSeverity.Warning, $"{original.Id.FullId} is already in {target.Name}.");
                return null;
            }

            Label? copy = this.changes.Copy(original, target);
            if (copy == null)
            {
                return null;
            }

            if (references != null && this.ChangeReferences(original.Id.FullId, copy.Id.FullId, references) > 0)
            {
                this.messages.Report(MessageSeverity.Warning, $"{original.Id.FullId} is kept, because not all references could be changed to {copy.Id.FullId}.");
                return copy;
            }

            this.changes.Delete(original);
            this.changes.Save(new[] { original });
            return copy;
        }

        /// <summary>
        /// Checks the label ID a developer enters to replace a label with (FA13).
        /// </summary>
        /// <param name="original">The label to replace.</param>
        /// <param name="input">The text entered.</param>
        /// <param name="replacement">The label to use instead, if the input is valid.</param>
        /// <returns>Why the input cannot be used, or <c>null</c> if it can.</returns>
        public string? CheckReplacement(Label original, string? input, out Label? replacement)
        {
            replacement = null;
            string text = (input ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                return "Enter the label ID to use instead.";
            }

            if (!LabelId.TryParse(text, out LabelId id))
            {
                return $"{text} is not a label ID.";
            }

            if (id == original.Id)
            {
                return $"{text} is the label to replace. Enter another label ID.";
            }

            replacement = this.store.Find(id);
            return replacement == null || replacement.IsDeleted ? $"{text} is not a loaded label." : null;
        }

        /// <summary>
        /// Replaces the uses of one label by another in the given files (FA13) and reports how
        /// many files were changed.
        /// </summary>
        /// <param name="oldId">The complete label ID used so far.</param>
        /// <param name="newId">The complete label ID to use instead.</param>
        /// <param name="files">Files from <see cref="FindReferences"/>.</param>
        /// <returns>The number of files changed.</returns>
        public int Replace(string oldId, string newId, IReadOnlyList<string> files)
            => this.ReplaceInFiles(oldId, newId, files, $"Replaced {oldId} by {newId}").Changed;

        private static string Files(int count) => count == 1 ? "1 file" : $"{count} files";

        // Returns the number of files that could not be changed.
        private int ChangeReferences(string oldId, string newId, IReadOnlyList<string> files)
            => this.ReplaceInFiles(oldId, newId, files, $"References to {oldId} changed to {newId}").Failed;

        private (int Changed, int Failed) ReplaceInFiles(string oldId, string newId, IReadOnlyList<string> files, string what)
        {
            IReadOnlyList<string> changed = LabelReferences.Replace(files, oldId, newId, this.messages, out IReadOnlyList<string> failed);
            this.messages.Report(
                failed.Count == 0 ? MessageSeverity.Message : MessageSeverity.Warning,
                $"{what} in {Files(changed.Count)}" + (failed.Count == 0 ? "." : $"; {Files(failed.Count)} could not be changed."));
            return (changed.Count, failed.Count);
        }
    }
}
