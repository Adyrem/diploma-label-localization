using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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

            return Search(this.store.Labels, query, cancellationToken);
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
            if (query.Term.Length == 0)
            {
                return Array.Empty<SearchHit>();
            }

            Func<Label, int>? score = CreateScorer(query);
            if (score == null)
            {
                return Array.Empty<SearchHit>();
            }

            List<SearchHit> hits = labels
                .AsParallel()
                .WithCancellation(cancellationToken)
                .Select(label => (Label: label, Score: score(label)))
                .Where(scored => scored.Score > 0)
                .Select(scored => new SearchHit(scored.Label, scored.Score))
                .ToList();

            hits.Sort((a, b) => a.Score != b.Score
                ? b.Score.CompareTo(a.Score)
                : string.CompareOrdinal(a.Label.Id.FullId, b.Label.Id.FullId));
            return hits;
        }

        private static Func<Label, int>? CreateScorer(SearchQuery query)
        {
            string term = query.Term;
            StringComparison comparison = query.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            switch (query.Mode)
            {
                case SearchMode.ExactMatch:
                    return label => ScoreLanguages(
                        label,
                        string.Equals(label.Id.FullId, term, comparison) || string.Equals(label.Id.Key, term, comparison),
                        text => string.Equals(text, term, comparison));

                case SearchMode.Substring:
                    return label => ScoreLanguages(
                        label,
                        label.Id.FullId.IndexOf(term, comparison) >= 0,
                        text => text.IndexOf(term, comparison) >= 0);

                case SearchMode.MatchWord:
                    var word = new Regex(@"\b" + Regex.Escape(term) + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    return label => ScoreMatchWord(
                        label,
                        string.Equals(label.Id.FullId, term, StringComparison.OrdinalIgnoreCase) || string.Equals(label.Id.Key, term, StringComparison.OrdinalIgnoreCase),
                        word);

                case SearchMode.AnythingLike:
                    string[] words = term.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 0)
                    {
                        return null;
                    }

                    return label =>
                    {
                        int best = Coverage(label.Id.FullId, words, comparison);
                        foreach (Translation translation in label.Translations.Values)
                        {
                            best = Math.Max(best, Coverage(translation.Text, words, comparison));
                        }

                        return best;
                    };

                case SearchMode.Id:
                    return label => string.Equals(label.Id.FullId, term, StringComparison.Ordinal) ? 1 : 0;

                default:
                    throw new ArgumentOutOfRangeException(nameof(query), query.Mode, "Unknown search mode.");
            }
        }

        // Exact match and Substring: text 2, otherwise ID 3, otherwise comment 1, per language.
        private static int ScoreLanguages(Label label, bool idMatches, Func<string, bool> fits)
        {
            int best = 0;
            foreach (Translation translation in label.Translations.Values)
            {
                int score = fits(translation.Text) ? 2
                    : idMatches ? 3
                    : translation.Comment != null && fits(translation.Comment) ? 1
                    : 0;
                best = Math.Max(best, score);
            }

            return best;
        }

        // MatchWord: ID 2, otherwise whole word of the text 3, otherwise of the comment 1, per language.
        private static int ScoreMatchWord(Label label, bool idMatches, Regex word)
        {
            int best = 0;
            foreach (Translation translation in label.Translations.Values)
            {
                int score = idMatches ? 2
                    : word.IsMatch(translation.Text) ? 3
                    : translation.Comment != null && word.IsMatch(translation.Comment) ? 1
                    : 0;
                best = Math.Max(best, score);
            }

            return best;
        }

        /// <summary>
        /// Anything like that for one field: the mean share of the field the words cover, times
        /// 1000 and truncated. Removing a word means removing all of its occurrences, as
        /// <see cref="string.Replace(string, string)"/> does from left to right.
        /// </summary>
        internal static int Coverage(string field, IReadOnlyList<string> words, StringComparison comparison)
        {
            if (field.Length == 0)
            {
                return 0;
            }

            long restSum = 0;
            foreach (string word in words)
            {
                int occurrences = 0;
                int index = field.IndexOf(word, comparison);
                while (index >= 0)
                {
                    occurrences++;
                    index = field.IndexOf(word, index + word.Length, comparison);
                }

                restSum += field.Length - (occurrences * word.Length);
            }

            // 1000 * (n - restSum / length) / n, in integers so the truncation is exact.
            long total = (long)words.Count * field.Length;
            return (int)(1000L * (total - restSum) / total);
        }
    }
}
