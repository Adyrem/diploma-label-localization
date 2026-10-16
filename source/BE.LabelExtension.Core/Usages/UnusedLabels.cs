using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Files;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;

namespace BE.LabelExtension.Core.Usages
{
    /// <summary>
    /// Finds labels that no element uses (KA09), limited to the given models, usually the
    /// writable ones, so that one pass over their element files is enough.
    /// </summary>
    /// <remarks>
    /// Every file is read once. At each <c>@</c> the text up to the next character that ends a
    /// use is taken as a candidate and counts only within the patterns of the usage search
    /// (<see cref="LabelReferences"/>). An ID with a space in it is therefore not recognized
    /// as used; the strict form of new IDs has none.
    /// </remarks>
    public static class UnusedLabels
    {
        // What follows an ID in one of the patterns, and line breaks.
        private static readonly char[] Terminators = { '<', '"', '\'', ')', '&', ',', ' ', '+', '\r', '\n', '\t' };

        /// <summary>The labels that no element of the given models uses.</summary>
        /// <param name="labels">The labels to check.</param>
        /// <param name="models">The models whose elements count, usually the writable ones.</param>
        /// <param name="cancellationToken">Cancels the search.</param>
        /// <returns>The unused labels in the order given, and how many files were searched.</returns>
        public static (IReadOnlyList<Label> Unused, int Files) Find(IReadOnlyList<Label> labels, IReadOnlyList<ModelInfo> models, CancellationToken cancellationToken)
        {
            var candidates = new HashSet<string>(labels.Select(l => l.Id.FullId), StringComparer.Ordinal);
            var used = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
            List<string> files = models.SelectMany(LabelReferences.ElementFiles).ToList();

            Parallel.ForEach(files, new ParallelOptions { CancellationToken = cancellationToken }, file =>
            {
                string text;
                try
                {
                    text = TextFile.Read(file).Text;
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    return;
                }

                foreach (string id in UsedIn(text, LabelReferences.IsReport(file), candidates))
                {
                    used.TryAdd(id, true);
                }
            });

            return (labels.Where(l => !used.ContainsKey(l.Id.FullId)).ToList(), files.Count);
        }

        /// <summary>The candidates used in one text.</summary>
        /// <param name="text">The text of an element file.</param>
        /// <param name="isReport">Whether it is the file of a report, with the patterns of reports.</param>
        /// <param name="candidates">The complete IDs looked for.</param>
        /// <returns>The candidates used, each once per occurrence.</returns>
        internal static IEnumerable<string> UsedIn(string text, bool isReport, ISet<string> candidates)
        {
            int at = text.IndexOf('@');
            while (at >= 0)
            {
                int end = text.IndexOfAny(Terminators, at + 1);
                if (end < 0)
                {
                    end = text.Length;
                }

                // In a report a dot may follow the ID, as in "!@SYS1.", and an ID may hold dots
                // itself: the whole token first, then shorter at each dot from the right.
                string token = text.Substring(at, end - at);
                int length = token.Length;
                while (length > 1)
                {
                    if (candidates.Contains(token.Substring(0, length)) && LabelReferences.IsUse(text, at, length, isReport))
                    {
                        yield return token.Substring(0, length);
                        break;
                    }

                    int dot = token.LastIndexOf('.', length - 1);
                    if (dot <= 0)
                    {
                        break;
                    }

                    length = dot;
                }

                at = text.IndexOf('@', at + 1);
            }
        }
    }
}
