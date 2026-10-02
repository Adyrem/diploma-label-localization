namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// A label ID found in a text, see <see cref="LabelId.FindAll"/>.
    /// </summary>
    public sealed class LabelIdMatch
    {
        /// <summary>Creates a match.</summary>
        /// <param name="id">The ID found.</param>
        /// <param name="index">Position of the <c>@</c> in the text.</param>
        public LabelIdMatch(LabelId id, int index)
        {
            this.Id = id;
            this.Index = index;
        }

        /// <summary>The ID found.</summary>
        public LabelId Id { get; }

        /// <summary>Position of the <c>@</c> in the text.</summary>
        public int Index { get; }

        /// <summary>Length of the ID in the text.</summary>
        public int Length => this.Id.FullId.Length;
    }
}
