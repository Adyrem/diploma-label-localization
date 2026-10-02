using System.Collections.Generic;
using BE.LabelExtension.Core.Labels;

namespace BE.LabelExtension.Core.Sources
{
    /// <summary>
    /// What a <see cref="ILabelSource"/> has loaded.
    /// </summary>
    public sealed class LabelSourceResult
    {
        /// <summary>Creates a result.</summary>
        /// <param name="labelFiles">All label files found, also those without a loaded language.</param>
        /// <param name="documents">One document per label file and loaded language.</param>
        public LabelSourceResult(IReadOnlyList<LabelFile> labelFiles, IReadOnlyList<LabelDocument> documents)
        {
            this.LabelFiles = labelFiles;
            this.Documents = documents;
        }

        /// <summary>All label files found, also those without a loaded language.</summary>
        public IReadOnlyList<LabelFile> LabelFiles { get; }

        /// <summary>One document per label file and loaded language.</summary>
        public IReadOnlyList<LabelDocument> Documents { get; }
    }
}
