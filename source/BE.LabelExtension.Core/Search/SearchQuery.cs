using System;

namespace BE.LabelExtension.Core.Search
{
    /// <summary>
    /// A search: term, mode and whether case matters.
    /// </summary>
    public sealed class SearchQuery
    {
        /// <summary>Creates a search.</summary>
        /// <param name="term">The search term.</param>
        /// <param name="mode">The search mode.</param>
        /// <param name="caseSensitive">Whether case matters; no effect for <see cref="SearchMode.Id"/> and <see cref="SearchMode.MatchWord"/>.</param>
        public SearchQuery(string term, SearchMode mode, bool caseSensitive)
        {
            this.Term = term ?? throw new ArgumentNullException(nameof(term));
            this.Mode = mode;
            this.CaseSensitive = caseSensitive;
        }

        /// <summary>The search term.</summary>
        public string Term { get; }

        /// <summary>The search mode.</summary>
        public SearchMode Mode { get; }

        /// <summary>Whether case matters.</summary>
        public bool CaseSensitive { get; }

        /// <inheritdoc />
        public override string ToString() => $"{this.Mode}{(this.CaseSensitive ? ", case sensitive" : string.Empty)}: {this.Term}";
    }
}
