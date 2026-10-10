using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Elements;
using BE.LabelExtension.Core.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BE.LabelExtension.Elements
{
    /// <summary>
    /// Finds and saves the elements that are open in Visual Studio with unsaved changes, before
    /// the extension writes into element files (B25). Saving such an element afterwards would
    /// overwrite what the extension wrote; D1 showed that for the X++ editor.
    /// </summary>
    /// <remarks>
    /// The running document table names the XML file for the designer and the <c>.xpp</c> under
    /// XppSource for the X++ editor; <see cref="ElementDocuments"/> traces both back to the
    /// element.
    /// </remarks>
    internal static class UnsavedElements
    {
        /// <summary>Finds the open elements of the given models with unsaved changes.</summary>
        /// <param name="models">The models the extension writes into.</param>
        /// <param name="cancellationToken">Cancels the search.</param>
        /// <returns>The elements, one per open document.</returns>
        public static async Task<IReadOnlyList<UnsavedElement>> FindAsync(IReadOnlyList<ModelInfo> models, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var unsaved = new List<UnsavedElement>();
            foreach (RunningDocumentInfo info in new RunningDocumentTable(ServiceProvider.GlobalProvider))
            {
                if (IsDirty(info.DocData) && ElementDocuments.ElementFile(info.Moniker, models) is string file)
                {
                    unsaved.Add(new UnsavedElement(file, info.DocCookie));
                }
            }

            return unsaved;
        }

        /// <summary>Saves the given documents, as File, Save would.</summary>
        /// <param name="elements">Elements from <see cref="FindAsync"/>.</param>
        /// <param name="cancellationToken">Cancels before the next document.</param>
        /// <returns>A task that completes when the documents are saved.</returns>
        public static async Task SaveAsync(IEnumerable<UnsavedElement> elements, CancellationToken cancellationToken)
        {
            IVsRunningDocumentTable table = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsRunningDocumentTable, IVsRunningDocumentTable>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            foreach (UnsavedElement element in elements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                table.SaveDocuments((uint)__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty, null, VSConstants.VSITEMID_NIL, element.DocumentCookie);
            }
        }

        private static bool IsDirty(object? documentData)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int dirty;
            if (documentData is IVsPersistDocData persist && persist.IsDocDataDirty(out dirty) == VSConstants.S_OK)
            {
                return dirty != 0;
            }

            return documentData is IPersistFileFormat file && file.IsDirty(out dirty) == VSConstants.S_OK && dirty != 0;
        }
    }

    /// <summary>An element open in Visual Studio with unsaved changes.</summary>
    internal sealed class UnsavedElement
    {
        /// <summary>Creates the entry.</summary>
        /// <param name="elementFile">The XML file of the element.</param>
        /// <param name="documentCookie">The document in the running document table.</param>
        public UnsavedElement(string elementFile, uint documentCookie)
        {
            this.ElementFile = elementFile;
            this.DocumentCookie = documentCookie;
        }

        /// <summary>The XML file of the element.</summary>
        public string ElementFile { get; }

        /// <summary>The document in the running document table.</summary>
        public uint DocumentCookie { get; }
    }
}
