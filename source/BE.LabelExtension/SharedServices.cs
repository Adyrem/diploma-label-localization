using System;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Labels;

namespace BE.LabelExtension
{
    /// <summary>
    /// The services of the new model that the classic parts need: the package for the question
    /// about unsaved labels when Visual Studio closes, the tooltip and the inline display for the
    /// labels. Neither reaches the dependency injection of the new model; the services enter
    /// themselves here when they are created. Before that, there is nothing to save or show.
    /// </summary>
    internal static class SharedServices
    {
        private static LabelChanges? changes;
        private static LabelLoader? loader;

        /// <summary>Raised on any thread when a service has entered itself.</summary>
        public static event EventHandler? Available;

        /// <summary>The changes not yet saved, once the label store exists.</summary>
        public static LabelChanges? Changes
        {
            get => changes;
            set
            {
                changes = value;
                Available?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>The loader with the label store and the settings, once it exists.</summary>
        public static LabelLoader? Loader
        {
            get => loader;
            set
            {
                loader = value;
                Available?.Invoke(null, EventArgs.Empty);
            }
        }
    }
}
