using BE.LabelExtension.Core.Labels;

namespace BE.LabelExtension.Core.Search
{
    /// <summary>
    /// A label found by a search, with its relevance.
    /// </summary>
    public sealed class SearchHit
    {
        /// <summary>Creates a hit.</summary>
        /// <param name="label">The label found.</param>
        /// <param name="score">The relevance, higher is better.</param>
        public SearchHit(Label label, int score)
        {
            this.Label = label;
            this.Score = score;
        }

        /// <summary>The label found.</summary>
        public Label Label { get; }

        /// <summary>The relevance, higher is better.</summary>
        public int Score { get; }

        /// <inheritdoc />
        public override string ToString() => $"{this.Label.Id.FullId} ({this.Score})";
    }
}
