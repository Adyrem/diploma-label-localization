using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Files;
using BE.LabelExtension.Core.Models;

namespace BE.LabelExtension.Core.Usages
{
    /// <summary>
    /// The usage search of the tool window (FA04) over all models, with the decision F16: the
    /// own models first, then the others. On the test environment the own models take well under
    /// a second, all models up to a minute (D1, B26), so the uses that matter most come first.
    /// </summary>
    public static class LabelUsageSearch
    {
        /// <summary>
        /// Splits the models into the own ones and the others. Own are the models of package
        /// directories that are no reference: on the Unified Developer Experience those of the
        /// ModelStoreFolder; on a classic VM, where all models share one directory, only those
        /// from layer VAR up.
        /// </summary>
        /// <param name="models">The loaded models.</param>
        /// <param name="directories">The package directories they come from.</param>
        /// <param name="fromConfiguration">Whether the directories come from a metadata configuration.</param>
        /// <returns>The own models and the others, each in the order given.</returns>
        public static (IReadOnlyList<ModelInfo> Own, IReadOnlyList<ModelInfo> Others) Split(IReadOnlyList<ModelInfo> models, IReadOnlyList<PackageDirectory> directories, bool fromConfiguration)
        {
            var own = new List<ModelInfo>();
            var others = new List<ModelInfo>();
            foreach (ModelInfo model in models)
            {
                PackageDirectory? directory = directories.FirstOrDefault(d => IsBelow(model.PackageDirectory, d.Path));
                bool isOwn = directory != null && !directory.IsReference && (fromConfiguration || model.Layer >= ModelLayer.VAR);
                (isOwn ? own : others).Add(model);
            }

            return (own, others);
        }

        /// <summary>
        /// Searches the uses of a label ID: the own models one after the other, then the others
        /// in parallel. Each use is reported as soon as it is found.
        /// </summary>
        /// <param name="own">The own models.</param>
        /// <param name="others">The other models.</param>
        /// <param name="labelId">The complete label ID.</param>
        /// <param name="found">Receives each use; for the other models from several threads at once.</param>
        /// <param name="ownSearched">Called when the own models are done.</param>
        /// <param name="cancellationToken">Cancels the search, for example when a new one starts.</param>
        public static void Run(IReadOnlyList<ModelInfo> own, IReadOnlyList<ModelInfo> others, string labelId, Action<LabelUsage> found, Action? ownSearched, CancellationToken cancellationToken)
        {
            foreach (LabelUsage usage in LabelReferences.Find(own, labelId, cancellationToken))
            {
                found(usage);
            }

            ownSearched?.Invoke();

            var options = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount };
            IEnumerable<(ModelInfo Model, string File)> files = others.SelectMany(m => LabelReferences.ElementFiles(m).Select(f => (m, f)));
            Parallel.ForEach(files, options, item =>
            {
                string text;
                try
                {
                    text = TextFile.Read(item.File).Text;
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    return;
                }

                foreach (LabelUsage usage in LabelReferences.FindInText(item.Model, item.File, text, labelId))
                {
                    found(usage);
                }
            });
        }

        private static bool IsBelow(string path, string directory)
        {
            string normalized = directory.Replace('/', '\\').TrimEnd('\\');
            string candidate = path.Replace('/', '\\').TrimEnd('\\');
            return candidate.Equals(normalized, StringComparison.OrdinalIgnoreCase)
                || candidate.StartsWith(normalized + "\\", StringComparison.OrdinalIgnoreCase);
        }
    }
}
