using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Store;

namespace BE.LabelExtension.Core.Search
{
    /// <summary>
    /// Searches the loaded labels with the modes and the ranking of the existing tool (FA01, FA12).
    /// </summary>
    /// <remarks>
    /// <para>Every language of a label gets a score; the label counts with its highest one.
    /// Hits are the labels scoring above 0, sorted by score descending and then by complete ID,
    /// ordinal.</para>
    /// <list type="bullet">
    /// <item>Exact match and Substring: text 2, otherwise ID 3, otherwise comment 1. The text is
    /// checked first, so a label whose text and ID fit scores 2 in that language. Exact match
    /// compares the ID with and without its label file, Substring only the complete ID.</item>
    /// <item>MatchWord, ignoring case: term equal to the ID with or without label file 2,
    /// otherwise a whole word of the text 3, otherwise a whole word of the comment 1. Unlike the
    /// existing tool the term is escaped before it goes into the regular expression.</item>
    /// <item>Anything like that: the term is split into words at spaces, commas, dots and
    /// hyphens. ID and text are scored separately, empty fields not at all: each word is removed
    /// from the field on its own and the rest is measured, and the score is
    /// <c>1000 * (words - sum(rest / field length)) / words</c>, the mean share of the field the
    /// words cover. The comment does not count.</item>
    /// <item>Label id: exactly the label with this complete ID.</item>
    /// </list>
    /// </remarks>
    public sealed class LabelSearch
    {
        // The highest score of any mode, that of Anything like that.
        private const int MaxScore = 1000;

        private static readonly char[] WordSeparators = { ' ', ',', '.', '-' };

        private readonly LabelStore store;

        /// <summary>Creates a search over the labels of a store.</summary>
        /// <param name="store">The store with the loaded labels.</param>
        public LabelSearch(LabelStore store)
        {
            this.store = store;
        }

        /// <summary>Searches the labels currently loaded.</summary>
        /// <param name="query">Term, mode and case.</param>
        /// <param name="cancellationToken">Cancels a search that is no longer needed, for example while typing.</param>
        /// <returns>The hits, the most relevant first; each label once.</returns>
        public IReadOnlyList<SearchHit> Search(SearchQuery query, CancellationToken cancellationToken = default)
        {
            if (query.Mode == SearchMode.Id)
            {
                Label? label = this.store.Find(query.Term);
                return label == null ? Array.Empty<SearchHit>() : new[] { new SearchHit(label, 1) };
            }

            return SearchSorted(this.store.SortedLabels, query, cancellationToken);
        }

        /// <summary>
        /// Searches the given labels. For <see cref="SearchMode.Id"/> the label with exactly the
        /// complete ID is found.
        /// </summary>
        /// <param name="labels">The labels to search.</param>
        /// <param name="query">Term, mode and case.</param>
        /// <param name="cancellationToken">Cancels the search.</param>
        /// <returns>The hits, the most relevant first.</returns>
        public static IReadOnlyList<SearchHit> Search(IEnumerable<Label> labels, SearchQuery query, CancellationToken cancellationToken = default)
        {
            Label[] sorted = labels.ToArray();
            Array.Sort(sorted, (a, b) => string.CompareOrdinal(a.Id.FullId, b.Id.FullId));
            return SearchSorted(sorted, query, cancellationToken);
        }

        // The labels are sorted by complete ID, ordinal. Each hit becomes one number, the score
        // descending in the upper half and the position in the lower one, so sorting numbers
        // gives score descending and then complete ID, without comparing strings (F14).
        private static IReadOnlyList<SearchHit> SearchSorted(Label[] labels, SearchQuery query, CancellationToken cancellationToken)
        {
            if (query.Term.Length == 0)
            {
                return Array.Empty<SearchHit>();
            }

            Func<Label, int>? score = CreateScorer(query);
            if (score == null)
            {
                return Array.Empty<SearchHit>();
            }

            var parts = new List<List<long>>();
            if (labels.Length > 0)
            {
                Parallel.ForEach(
                    Partitioner.Create(0, labels.Length, Math.Max(1024, labels.Length / (Environment.ProcessorCount * 8))),
                    new ParallelOptions { CancellationToken = cancellationToken },
                    () => new List<long>(),
                    (range, state, found) =>
                    {
                        for (int i = range.Item1; i < range.Item2; i++)
                        {
                            int value = score(labels[i]);
                            if (value > 0)
                            {
                                found.Add(((long)(MaxScore - value) << 32) | (uint)i);
                            }
                        }

                        return found;
                    },
                    found =>
                    {
                        lock (parts)
                        {
                            parts.Add(found);
                        }
                    });
            }

            long[] keys = new long[parts.Sum(p => p.Count)];
            int next = 0;
            foreach (List<long> part in parts)
            {
                part.CopyTo(keys, next);
                next += part.Count;
            }

            Array.Sort(keys);
            var hits = new SearchHit[keys.Length];
            for (int k = 0; k < keys.Length; k++)
            {
                hits[k] = new SearchHit(labels[(int)(keys[k] & uint.MaxValue)], MaxScore - (int)(keys[k] >> 32));
            }

            return hits;
        }

        private static Func<Label, int>? CreateScorer(SearchQuery query)
        {
            string term = query.Term;
            switch (query.Mode)
            {
                case SearchMode.ExactMatch:
                    StringComparison comparison = query.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    return label => ScoreLanguages(
                        label,
                        string.Equals(label.Id.FullId, term, comparison) || label.Id.KeyEquals(term, comparison),
                        text => string.Equals(text, term, comparison));

                case SearchMode.Substring:
                    Func<string, bool> contains = Contains(term, query.CaseSensitive);
                    return label => ScoreLanguages(label, contains(label.Id.FullId), contains);

                case SearchMode.MatchWord:
                    var word = new Regex(@"\b" + Regex.Escape(term) + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                    // The expression can only match where the term occurs under its case rule,
                    // which is quick to check. Most fields fail that check.
                    CharFolding.FoldedTerm lowered = CharFolding.RegexIgnoreCase.Prepare(term);
                    Func<string, bool> isWord = field => lowered.IndexOf(field, 0) >= 0 && word.IsMatch(field);
                    return label => ScoreMatchWord(
                        label,
                        string.Equals(label.Id.FullId, term, StringComparison.OrdinalIgnoreCase) || label.Id.KeyEquals(term, StringComparison.OrdinalIgnoreCase),
                        isWord);

                case SearchMode.AnythingLike:
                    string[] words = term.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 0)
                    {
                        return null;
                    }

                    CharFolding folding = query.CaseSensitive ? CharFolding.Ordinal : CharFolding.OrdinalIgnoreCase;
                    CharFolding.FoldedTerm[] prepared = words.Select(folding.Prepare).ToArray();
                    return label =>
                    {
                        int best = Coverage(label.Id.FullId, prepared);
                        string? previous = null;
                        foreach (Translation translation in label.TranslationArray)
                        {
                            // A text shared with the language before scores the same.
                            if (!ReferenceEquals(translation.Text, previous))
                            {
                                previous = translation.Text;
                                best = Math.Max(best, Coverage(previous, prepared));
                            }
                        }

                        return best;
                    };

                case SearchMode.Id:
                    return label => string.Equals(label.Id.FullId, term, StringComparison.Ordinal) ? 1 : 0;

                default:
                    throw new ArgumentOutOfRangeException(nameof(query), query.Mode, "Unknown search mode.");
            }
        }

        private static Func<string, bool> Contains(string term, bool caseSensitive)
        {
            CharFolding.FoldedTerm prepared = (caseSensitive ? CharFolding.Ordinal : CharFolding.OrdinalIgnoreCase).Prepare(term);
            return field => prepared.IndexOf(field, 0) >= 0;
        }

        // Exact match and Substring: text 2, otherwise ID 3, otherwise comment 1, per language.
        private static int ScoreLanguages(Label label, bool idMatches, Func<string, bool> fits)
        {
            var text = default(FieldResult);
            var comment = default(FieldResult);
            int best = 0;
            foreach (Translation translation in label.TranslationArray)
            {
                int score = text.Fits(translation.Text, fits) ? 2
                    : idMatches ? 3
                    : translation.Comment != null && comment.Fits(translation.Comment, fits) ? 1
                    : 0;
                best = Math.Max(best, score);
            }

            return best;
        }

        // MatchWord: ID 2, otherwise whole word of the text 3, otherwise of the comment 1, per language.
        private static int ScoreMatchWord(Label label, bool idMatches, Func<string, bool> isWord)
        {
            var text = default(FieldResult);
            var comment = default(FieldResult);
            int best = 0;
            foreach (Translation translation in label.TranslationArray)
            {
                int score = idMatches ? 2
                    : text.Fits(translation.Text, isWord) ? 3
                    : translation.Comment != null && comment.Fits(translation.Comment, isWord) ? 1
                    : 0;
                best = Math.Max(best, score);
            }

            return best;
        }

        // Remembers the last field checked. The labels share equal texts and comments among
        // their languages (Label.Share), and the comment is usually the same in every language,
        // so most of them need checking only once per label.
        private struct FieldResult
        {
            private string? field;
            private bool fits;

            public bool Fits(string value, Func<string, bool> check)
            {
                if (!ReferenceEquals(value, this.field))
                {
                    this.field = value;
                    this.fits = check(value);
                }

                return this.fits;
            }
        }

        /// <summary>
        /// Anything like that for one field: the mean share of the field the words cover, times
        /// 1000 and truncated. Removing a word means removing all of its occurrences, as
        /// <see cref="string.Replace(string, string)"/> does from left to right.
        /// </summary>
        /// <param name="field">The field, ID or text.</param>
        /// <param name="words">The words of the term.</param>
        /// <param name="comparison">Ordinal, or ordinal without case.</param>
        /// <returns>The score of the field, 0 to 1000.</returns>
        internal static int Coverage(string field, IReadOnlyList<string> words, StringComparison comparison)
        {
            CharFolding folding = comparison switch
            {
                StringComparison.Ordinal => CharFolding.Ordinal,
                StringComparison.OrdinalIgnoreCase => CharFolding.OrdinalIgnoreCase,
                _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Only ordinal comparisons are supported."),
            };
            return Coverage(field, words.Select(folding.Prepare).ToArray());
        }

        private static int Coverage(string field, CharFolding.FoldedTerm[] words)
        {
            if (field.Length == 0)
            {
                return 0;
            }

            long restSum = 0;
            foreach (CharFolding.FoldedTerm word in words)
            {
                int occurrences = 0;
                int index = word.IndexOf(field, 0);
                while (index >= 0)
                {
                    occurrences++;
                    index = word.IndexOf(field, index + word.Length);
                }

                restSum += field.Length - (occurrences * word.Length);
            }

            // 1000 * (n - restSum / length) / n, in integers so the truncation is exact.
            long total = (long)words.Length * field.Length;
            return (int)(1000L * (total - restSum) / total);
        }
    }
}
