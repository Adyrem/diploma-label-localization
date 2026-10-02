using System.Collections.Generic;
using System.Linq;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// Content of the file of one language: the labels in their order and the problems found.
    /// </summary>
    public sealed class LabelFileContent
    {
        /// <summary>Creates the content.</summary>
        /// <param name="entries">The labels in their order, each ID once.</param>
        /// <param name="issues">The problems found while reading.</param>
        public LabelFileContent(IReadOnlyList<LabelEntry> entries, IReadOnlyList<LabelFileIssue> issues)
        {
            this.Entries = entries;
            this.Issues = issues;
        }

        /// <summary>The labels in their order, each ID once.</summary>
        public IReadOnlyList<LabelEntry> Entries { get; }

        /// <summary>The problems found while reading.</summary>
        public IReadOnlyList<LabelFileIssue> Issues { get; }

        /// <summary>
        /// Whether the file contains a line that is neither a label nor a comment. Such a file
        /// is not loaded, because saving it would lose that line.
        /// </summary>
        public bool IsDamaged => this.Issues.Any(i => i.Kind == LabelFileIssueKind.InvalidLine);

        /// <summary>
        /// Whether the next save changes the file even without edits, because it drops
        /// duplicate IDs or further comment lines.
        /// </summary>
        public bool NeedsCleanup => this.Issues.Any(i => i.Kind != LabelFileIssueKind.InvalidLine);
    }
}
