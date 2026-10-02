using System.Collections.Generic;

namespace BE.LabelExtension.Core.Store
{
    /// <summary>
    /// What the label store loads: the package directories and the languages. The extension
    /// stores these settings, together with the API key of the translation service.
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
    }
}
