using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Store;
using Microsoft.VisualStudio.Shell;

namespace BE.LabelExtension.Settings
{
    /// <summary>
    /// Page BE-LabelExtension in the options of Visual Studio (FA11). The settings go into the
    /// settings file of the user, not into the registry, so the extension reads them without
    /// the package. The API key is stored encrypted only (NFA06).
    /// </summary>
    /// <remarks>
    /// The page is a property grid. It offers the fields of the wireframe in the concept with
    /// lists to choose from where the wireframe has them, and needs no own controls.
    /// </remarks>
    [Guid(PageGuidString)]
    public sealed class LabelOptionsPage : DialogPage
    {
        /// <summary>The GUID of the page, to open it from the tool window.</summary>
        public const string PageGuidString = "38c23fdf-3ec8-4706-a723-8ef934401680";

        /// <summary>The choice for the most recently changed metadata configuration.</summary>
        public const string Newest = "(most recently changed)";

        private const string LabelsCategory = "Labels";
        private const string TranslationCategory = "Translation";
        private const string EditorCategory = "Editor";

        /// <summary>The metadata configuration of the Unified Developer Experience.</summary>
        [Category(LabelsCategory)]
        [DisplayName("Metadata configuration")]
        [Description("Configuration of the Dynamics 365 Developer Tools whose package directories are loaded. Empty on a classic development VM.")]
        [TypeConverter(typeof(ConfigurationConverter))]
        public string MetadataConfiguration { get; set; } = Newest;

        /// <summary>Further package directories, for example the PackagesLocalDirectory of a classic development VM.</summary>
        [Category(LabelsCategory)]
        [DisplayName("Additional package directories")]
        [Description("Further package directories to load, for example J:\\AosService\\PackagesLocalDirectory on a classic development VM.")]
        public string[] PackageDirectories { get; set; } = Array.Empty<string>();

        /// <summary>The languages to load, separated by commas.</summary>
        [Category(LabelsCategory)]
        [DisplayName("Languages to load")]
        [Description("Languages to load and to show, separated by commas, for example de, en-US. They take effect with the next load, without restarting Visual Studio.")]
        public string LoadLanguages { get; set; } = string.Join(", ", LabelSettingsDefaults.Languages);

        /// <summary>The languages new labels are created in, separated by commas.</summary>
        [Category(LabelsCategory)]
        [DisplayName("Languages to create")]
        [Description("Languages a new label is created in, separated by commas.")]
        public string CreateLanguages { get; set; } = string.Join(", ", LabelSettingsDefaults.Languages);

        /// <summary>The language the translation service translates from.</summary>
        [Category(TranslationCategory)]
        [DisplayName("Source language")]
        [Description("Language the translation service translates from.")]
        public string SourceLanguage { get; set; } = "de";

        /// <summary>The translation service.</summary>
        [Category(TranslationCategory)]
        [DisplayName("Translation service")]
        [Description("Service that suggests translations for new labels.")]
        [TypeConverter(typeof(ServiceConverter))]
        public string TranslationService { get; set; } = "DeepL";

        /// <summary>The API key of the translation service; stored encrypted only.</summary>
        [Category(TranslationCategory)]
        [DisplayName("API key")]
        [Description("Key of the translation service. It is stored encrypted for your Windows user.")]
        [PasswordPropertyText(true)]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Whether the translations appear above the lines with a label ID.</summary>
        [Category(EditorCategory)]
        [DisplayName("Inline display")]
        [Description("Shows the translations above every line of X++ code with a label ID.")]
        public bool InlineDisplay { get; set; } = true;

        /// <inheritdoc />
        public override void LoadSettingsFromStorage()
        {
            LabelSettings settings;
            try
            {
                settings = LabelSettingsDefaults.Complete(LabelSettingsFile.Load(LabelSettingsFile.DefaultPath));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is SerializationException)
            {
                settings = LabelSettingsDefaults.Complete(null);
            }

            this.MetadataConfiguration = settings.MetadataConfiguration ?? Newest;
            this.PackageDirectories = settings.PackageDirectories.ToArray();
            this.LoadLanguages = string.Join(", ", settings.LoadLanguages);
            this.CreateLanguages = string.Join(", ", settings.CreateLanguages);
            this.SourceLanguage = settings.SourceLanguage ?? string.Empty;
            this.TranslationService = settings.TranslationService;
            this.ApiKey = ApiKeyProtection.Unprotect(settings.ProtectedApiKey) ?? string.Empty;
            this.InlineDisplay = settings.InlineDisplay;
        }

        /// <inheritdoc />
        public override void SaveSettingsToStorage()
        {
            var settings = new LabelSettings
            {
                MetadataConfiguration = string.IsNullOrWhiteSpace(this.MetadataConfiguration) || this.MetadataConfiguration == Newest ? null : this.MetadataConfiguration.Trim(),
                SourceLanguage = string.IsNullOrWhiteSpace(this.SourceLanguage) ? null : this.SourceLanguage.Trim(),
                TranslationService = string.IsNullOrWhiteSpace(this.TranslationService) ? "DeepL" : this.TranslationService.Trim(),
                ProtectedApiKey = ApiKeyProtection.Protect(this.ApiKey),
                InlineDisplay = this.InlineDisplay,
            };

            foreach (string directory in (this.PackageDirectories ?? Array.Empty<string>()).Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                settings.PackageDirectories.Add(directory.Trim());
            }

            foreach (string language in LabelSettings.ParseLanguages(this.LoadLanguages))
            {
                settings.LoadLanguages.Add(language);
            }

            foreach (string language in LabelSettings.ParseLanguages(this.CreateLanguages))
            {
                settings.CreateLanguages.Add(language);
            }

            LabelSettingsFile.Save(LabelSettingsFile.DefaultPath, settings);
        }

        // The configurations in the folder of the Developer Tools, to choose from.
        private sealed class ConfigurationConverter : StringConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
                => new(new[] { Newest }.Concat(Core.Models.MetadataConfiguration.List(Core.Models.MetadataConfiguration.DefaultFolder)).ToArray());
        }

        private sealed class ServiceConverter : StringConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(new[] { "DeepL" });
        }
    }
}
