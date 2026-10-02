namespace BE.LabelExtension.Core.Search
{
    /// <summary>
    /// The search modes of the existing tool. Together with <see cref="SearchQuery.CaseSensitive"/>
    /// they give its eight modes; for <see cref="Id"/> and <see cref="MatchWord"/> the case
    /// setting has no effect.
    /// </summary>
    public enum SearchMode
    {
        /// <summary>ID, text or comment equal to the term (FA01).</summary>
        ExactMatch,

        /// <summary>ID, text or comment containing the term (FA01).</summary>
        Substring,

        /// <summary>The words of the term in ID and text, ranked by how much of the field they cover (FA12).</summary>
        AnythingLike,

        /// <summary>The term as a whole word in text or comment, or as the complete ID (FA12).</summary>
        MatchWord,

        /// <summary>Exactly the label with this complete ID (FA01).</summary>
        Id,
    }
}
