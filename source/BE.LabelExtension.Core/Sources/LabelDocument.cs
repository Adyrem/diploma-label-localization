using System.Collections.Generic;
using BE.LabelExtension.Core.Labels;

namespace BE.LabelExtension.Core.Sources
{
    /// <summary>
    /// The labels of one label file in one language, as a source has loaded them.
    /// </summary>
    public sealed class LabelDocument
    {
        /// <summary>Creates a document.</summary>
        /// <param name="labelFile">The label file.</param>
        /// <param name="language">The language.</param>
        /// <param name="path">The file the labels were read from.</param>
        /// <param name="entries">The labels in their order.</param>
        public LabelDocument(LabelFile labelFile, string language, string path, IReadOnlyList<LabelEntry> entries)
        {
            this.LabelFile = labelFile;
            this.Language = language;
            this.Path = path;
            this.Entries = entries;
        }

        /// <summary>The label file.</summary>
        public LabelFile LabelFile { get; }

        /// <summary>The language.</summary>
        public string Language { get; }

        /// <summary>The file the labels were read from.</summary>
        public string Path { get; }

        /// <summary>The labels in their order.</summary>
        public IReadOnlyList<LabelEntry> Entries { get; }
    }
}
