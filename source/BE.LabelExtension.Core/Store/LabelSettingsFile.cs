using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace BE.LabelExtension.Core.Store
{
    /// <summary>
    /// Keeps the settings in a JSON file in the user profile, so they last over a restart of
    /// Visual Studio (FA11). The options page of the extension and the extension itself both
    /// read it, without depending on each other.
    /// </summary>
    public static class LabelSettingsFile
    {
        /// <summary>The file of the current user.</summary>
        public static string DefaultPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BE.LabelExtension", "settings.json");

        /// <summary>Raised after <see cref="Save"/> wrote the settings, with the path of the file.</summary>
        public static event EventHandler<string>? Saved;

        /// <summary>Reads the settings.</summary>
        /// <param name="path">The file, usually <see cref="DefaultPath"/>.</param>
        /// <returns>The settings, or <c>null</c> if the file does not exist yet.</returns>
        /// <exception cref="IOException">The file cannot be read.</exception>
        /// <exception cref="SerializationException">The file is not valid JSON of the settings.</exception>
        public static LabelSettings? Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            // Read as text first: the serializer fails on a byte order mark.
            string json = File.ReadAllText(path);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var file = (SettingsData?)new DataContractJsonSerializer(typeof(SettingsData)).ReadObject(stream) ?? new SettingsData();

            var settings = new LabelSettings
            {
                MetadataConfiguration = string.IsNullOrWhiteSpace(file.MetadataConfiguration) ? null : file.MetadataConfiguration,
                SourceLanguage = file.SourceLanguage,
                TranslationService = string.IsNullOrWhiteSpace(file.TranslationService) ? "DeepL" : file.TranslationService!,
                ProtectedApiKey = string.IsNullOrEmpty(file.ProtectedApiKey) ? null : file.ProtectedApiKey,
                InlineDisplay = file.InlineDisplay ?? true,
                InlineCount = file.InlineCount ?? true,
                SideBySide = file.SideBySide ?? false,
            };
            AddAll(file.PackageDirectories, settings.PackageDirectories);
            AddAll(file.LoadLanguages, settings.LoadLanguages);
            AddAll(file.CreateLanguages, settings.CreateLanguages);
            AddAll(file.InlineLanguages, settings.InlineLanguages);
            return settings;
        }

        /// <summary>Writes the settings, through a temporary file.</summary>
        /// <param name="path">The file, usually <see cref="DefaultPath"/>.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="notify">
        /// Whether to raise <see cref="Saved"/>; <c>false</c> for the layout of the tool window,
        /// which changes nothing the extension has to apply.
        /// </param>
        public static void Save(string path, LabelSettings settings, bool notify = true)
        {
            var file = new SettingsData
            {
                MetadataConfiguration = settings.MetadataConfiguration,
                PackageDirectories = new List<string>(settings.PackageDirectories),
                LoadLanguages = new List<string>(settings.LoadLanguages),
                CreateLanguages = new List<string>(settings.CreateLanguages),
                SourceLanguage = settings.SourceLanguage,
                TranslationService = settings.TranslationService,
                ProtectedApiKey = settings.ProtectedApiKey,
                InlineDisplay = settings.InlineDisplay,
                InlineLanguages = new List<string>(settings.InlineLanguages),
                InlineCount = settings.InlineCount,
                SideBySide = settings.SideBySide,
            };

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, new UTF8Encoding(false), ownsStream: false, indent: true))
                {
                    new DataContractJsonSerializer(typeof(SettingsData)).WriteObject(writer, file);
                }

                if (File.Exists(path))
                {
                    File.Replace(temporary, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }

            if (notify)
            {
                Saved?.Invoke(null, path);
            }
        }

        private static void AddAll(IEnumerable<string>? values, IList<string> target)
        {
            foreach (string value in values ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    target.Add(value.Trim());
                }
            }
        }

        [DataContract]
        private sealed class SettingsData
        {
            [DataMember(Order = 1)]
            public string? MetadataConfiguration { get; set; }

            [DataMember(Order = 2)]
            public List<string>? PackageDirectories { get; set; }

            [DataMember(Order = 3)]
            public List<string>? LoadLanguages { get; set; }

            [DataMember(Order = 4)]
            public List<string>? CreateLanguages { get; set; }

            [DataMember(Order = 5)]
            public string? SourceLanguage { get; set; }

            [DataMember(Order = 6)]
            public string? TranslationService { get; set; }

            [DataMember(Order = 7)]
            public string? ProtectedApiKey { get; set; }

            [DataMember(Order = 8)]
            public bool? InlineDisplay { get; set; }

            [DataMember(Order = 9)]
            public List<string>? InlineLanguages { get; set; }

            [DataMember(Order = 10)]
            public bool? InlineCount { get; set; }

            [DataMember(Order = 11)]
            public bool? SideBySide { get; set; }
        }
    }
}
