using System;
using System.Collections.Generic;
using System.Linq;

namespace BE.LabelExtension.Core.Store
{
    /// <summary>
    /// The settings of the extension (FA11): what the label store loads, the languages new
    /// labels are created in, and the translation service.
    /// </summary>
    public sealed class LabelSettings
    {
        /// <summary>
        /// Name of the metadata configuration of the Unified Developer Experience, <c>null</c> on a
        /// classic development VM.
        /// </summary>
        public string? MetadataConfiguration { get; set; }

        /// <summary>Further package directories, for example the PackagesLocalDirectory of a classic development VM.</summary>
        public IList<string> PackageDirectories { get; } = new List<string>();

        /// <summary>Languages to load and to show, for example en-US and de-CH.</summary>
        public IList<string> LoadLanguages { get; } = new List<string>();

        /// <summary>Languages a new label is created in.</summary>
        public IList<string> CreateLanguages { get; } = new List<string>();

        /// <summary>Language the translation service translates from.</summary>
        public string? SourceLanguage { get; set; }

        /// <summary>The translation service, for now only DeepL.</summary>
        public string TranslationService { get; set; } = "DeepL";

        /// <summary>
        /// The API key of the translation service, encrypted for the current Windows user. The
        /// key itself never appears in the settings (NFA06).
        /// </summary>
        public string? ProtectedApiKey { get; set; }

        /// <summary>Whether the translations appear above the lines with a label ID (FA06).</summary>
        public bool InlineDisplay { get; set; } = true;

        /// <summary>
        /// Splits a list of languages as entered, for example "de, en-US", into its languages,
        /// each once, ignoring case.
        /// </summary>
        /// <param name="text">The languages, separated by commas, semicolons or spaces.</param>
        /// <returns>The languages in their order.</returns>
        public static IReadOnlyList<string> ParseLanguages(string? text)
            => (text ?? string.Empty)
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>
        /// Whether the labels must be loaded again after changing from these settings to
        /// others: the languages to load, the metadata configuration or the package
        /// directories differ (F4).
        /// </summary>
        /// <param name="other">The new settings.</param>
        /// <returns>Whether to reload.</returns>
        public bool RequiresReload(LabelSettings other)
            => !string.Equals(this.MetadataConfiguration ?? string.Empty, other.MetadataConfiguration ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                || !this.LoadLanguages.SequenceEqual(other.LoadLanguages, StringComparer.OrdinalIgnoreCase)
                || !this.PackageDirectories.SequenceEqual(other.PackageDirectories, StringComparer.OrdinalIgnoreCase);

        /// <summary>A copy, so the settings of a load stay as they were when it started.</summary>
        /// <returns>The copy.</returns>
        public LabelSettings Clone()
        {
            var copy = new LabelSettings
            {
                MetadataConfiguration = this.MetadataConfiguration,
                SourceLanguage = this.SourceLanguage,
                TranslationService = this.TranslationService,
                ProtectedApiKey = this.ProtectedApiKey,
                InlineDisplay = this.InlineDisplay,
            };
            Copy(this.PackageDirectories, copy.PackageDirectories);
            Copy(this.LoadLanguages, copy.LoadLanguages);
            Copy(this.CreateLanguages, copy.CreateLanguages);
            return copy;
        }

        private static void Copy(IEnumerable<string> from, IList<string> to)
        {
            foreach (string value in from)
            {
                to.Add(value);
            }
        }
    }
}
