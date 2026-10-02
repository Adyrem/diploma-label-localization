using System;
using System.Collections.Generic;

namespace BE.LabelExtension.Core.Store
{
    /// <summary>
    /// Label files changed outside the extension.
    /// </summary>
    public sealed class LabelFilesChangedEventArgs : EventArgs
    {
        /// <summary>Creates the event data.</summary>
        /// <param name="paths">The changed files.</param>
        public LabelFilesChangedEventArgs(IReadOnlyList<string> paths)
        {
            this.Paths = paths;
        }

        /// <summary>The changed files.</summary>
        public IReadOnlyList<string> Paths { get; }
    }
}
