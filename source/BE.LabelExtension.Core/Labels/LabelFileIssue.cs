namespace BE.LabelExtension.Core.Labels
{
    /// <summary>Kind of a problem found while reading a label file.</summary>
    public enum LabelFileIssueKind
    {
        /// <summary>
        /// The ID appeared before. The first one is kept, this one is dropped on the next save.
        /// </summary>
        DuplicateId,

        /// <summary>
        /// A further comment line of a label. Only the first one counts, this one is dropped on
        /// the next save.
        /// </summary>
        ExtraCommentLine,

        /// <summary>
        /// A line that is neither a label, a comment nor empty. The file counts as damaged and
        /// is not loaded, so that saving cannot lose the line.
        /// </summary>
        InvalidLine,
    }

    /// <summary>
    /// A problem found while reading a label file.
    /// </summary>
    public sealed class LabelFileIssue
    {
        /// <summary>Creates an issue.</summary>
        /// <param name="kind">Kind of the problem.</param>
        /// <param name="line">Line number, starting at 1.</param>
        /// <param name="key">Label concerned, if known.</param>
        public LabelFileIssue(LabelFileIssueKind kind, int line, string? key)
        {
            this.Kind = kind;
            this.Line = line;
            this.Key = key;
        }

        /// <summary>Kind of the problem.</summary>
        public LabelFileIssueKind Kind { get; }

        /// <summary>Line number, starting at 1.</summary>
        public int Line { get; }

        /// <summary>Label concerned, if known.</summary>
        public string? Key { get; }

        /// <inheritdoc />
        public override string ToString() => this.Kind switch
        {
            LabelFileIssueKind.DuplicateId => $"line {this.Line}: label {this.Key} appears a second time and will be removed on the next save",
            LabelFileIssueKind.ExtraCommentLine => $"line {this.Line}: further comment line of label {this.Key}, only the first one is kept",
            _ => $"line {this.Line}: neither a label nor a comment",
        };
    }
}
