using System;
using System.Linq;
using BE.LabelExtension.Core.Store;

namespace BE.LabelExtension.Settings
{
    /// <summary>
    /// Fills in what the user has not set, the same way for the options page and for loading.
    /// The metadata configuration stays empty for "the most recently changed one"; the loader
    /// resolves it on every load, because names change with updates.
    /// </summary>
    internal static class LabelSettingsDefaults
    {
        /// <summary>The languages loaded and created when nothing is set.</summary>
        public static readonly string[] Languages = { "en-US", "de", "de-CH", "fr-CH", "it-CH" };

        /// <summary>The settings of the file with defaults for what is missing.</summary>
        /// <param name="saved">The settings of the file, <c>null</c> if there is none yet.</param>
        /// <returns>Complete settings.</returns>
        public static LabelSettings Complete(LabelSettings? saved)
        {
            LabelSettings settings = saved?.Clone() ?? new LabelSettings();
            if (settings.LoadLanguages.Count == 0)
            {
                foreach (string language in Languages)
                {
                    settings.LoadLanguages.Add(language);
                }
            }

            if (settings.CreateLanguages.Count == 0)
            {
                foreach (string language in settings.LoadLanguages)
                {
                    settings.CreateLanguages.Add(language);
                }
            }

            if (string.IsNullOrWhiteSpace(settings.SourceLanguage))
            {
                settings.SourceLanguage = settings.CreateLanguages.FirstOrDefault(l => string.Equals(l, "de", StringComparison.OrdinalIgnoreCase))
                    ?? settings.CreateLanguages.FirstOrDefault();
            }

            return settings;
        }
    }
}
