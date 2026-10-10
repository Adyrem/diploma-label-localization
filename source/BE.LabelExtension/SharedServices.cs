using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Labels;

namespace BE.LabelExtension
{
    /// <summary>
    /// The services of the new model that the classic package needs, for the question about
    /// unsaved labels when Visual Studio closes. The package cannot reach the dependency
    /// injection of the new model; the services enter themselves here when they are created.
    /// Before that, there is nothing to save.
    /// </summary>
    internal static class SharedServices
    {
        /// <summary>The changes not yet saved, once the label store exists.</summary>
        public static LabelChanges? Changes { get; set; }

        /// <summary>The loader, once it exists.</summary>
        public static LabelLoader? Loader { get; set; }
    }
}
