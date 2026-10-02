using System.Collections.Generic;
using System.Threading;

namespace BE.LabelExtension.Core.Sources
{
    /// <summary>
    /// A source of label files (strategy). The label store asks every source and merges the
    /// results, so a further source, for example through the Metadata API, can be added
    /// without changing the store.
    /// </summary>
    public interface ILabelSource
    {
        /// <summary>
        /// Loads the label files in the requested languages. Problems with single files are
        /// reported to <see cref="LabelLoadRequest.Messages"/> and the file is skipped.
        /// </summary>
        /// <param name="request">Models, package directories and languages to load.</param>
        /// <param name="cancellationToken">Cancels the load.</param>
        /// <returns>The label files found and one document per label file and loaded language.</returns>
        LabelSourceResult Load(LabelLoadRequest request, CancellationToken cancellationToken);
    }
}
