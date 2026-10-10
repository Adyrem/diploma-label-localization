using System;
using System.IO;
using BE.LabelExtension.Core.Store;
using Xunit;

namespace BE.LabelExtension.Tests.Store
{
    /// <summary>FA11 and TC21 in the core logic: the settings last over a restart.</summary>
    public sealed class LabelSettingsFileTests : IDisposable
    {
        private readonly string folder = Path.Combine(Path.GetTempPath(), "BE.LabelExtension.Tests", Guid.NewGuid().ToString("N"));

        private string File => Path.Combine(this.folder, "settings.json");

        public void Dispose()
        {
            if (Directory.Exists(this.folder))
            {
                Directory.Delete(this.folder, recursive: true);
            }
        }

        [Fact]
        public void SaveAndLoad_AllSettings_ComeBack()
        {
            var settings = new LabelSettings { MetadataConfiguration = "Demo-10.0.2527.197", SourceLanguage = "de", ProtectedApiKey = "AQAAANCMnd8B", InlineDisplay = false };
            settings.PackageDirectories.Add(@"J:\AosService\PackagesLocalDirectory");
            settings.LoadLanguages.Add("de");
            settings.LoadLanguages.Add("en-US");
            settings.CreateLanguages.Add("de-CH");

            LabelSettingsFile.Save(this.File, settings);
            LabelSettings loaded = LabelSettingsFile.Load(this.File)!;

            Assert.Equal("Demo-10.0.2527.197", loaded.MetadataConfiguration);
            Assert.Equal(new[] { @"J:\AosService\PackagesLocalDirectory" }, loaded.PackageDirectories);
            Assert.Equal(new[] { "de", "en-US" }, loaded.LoadLanguages);
            Assert.Equal(new[] { "de-CH" }, loaded.CreateLanguages);
            Assert.Equal("de", loaded.SourceLanguage);
            Assert.Equal("DeepL", loaded.TranslationService);
            Assert.Equal("AQAAANCMnd8B", loaded.ProtectedApiKey);
            Assert.False(loaded.InlineDisplay);
            Assert.Equal(new[] { this.File }, Directory.GetFiles(this.folder));
        }

        [Fact]
        public void Load_MissingFile_IsNull()
        {
            Assert.Null(LabelSettingsFile.Load(this.File));
        }

        [Fact]
        public void Load_FileWithByteOrderMarkAndMissingFields_UsesTheDefaults()
        {
            Directory.CreateDirectory(this.folder);
            System.IO.File.WriteAllText(this.File, "{ \"LoadLanguages\": [ \"de\" ] }", new System.Text.UTF8Encoding(true));

            LabelSettings loaded = LabelSettingsFile.Load(this.File)!;

            Assert.Equal(new[] { "de" }, loaded.LoadLanguages);
            Assert.Null(loaded.MetadataConfiguration);
            Assert.True(loaded.InlineDisplay);
            Assert.Equal("DeepL", loaded.TranslationService);
        }

        [Theory]
        [InlineData("de, en-US", new[] { "de", "en-US" })]
        [InlineData("de;en-US de-CH", new[] { "de", "en-US", "de-CH" })]
        [InlineData(" de , DE ,, ", new[] { "de" })]
        [InlineData("", new string[0])]
        public void ParseLanguages_SplitsAndRemovesDuplicates(string text, string[] expected)
        {
            Assert.Equal(expected, LabelSettings.ParseLanguages(text));
        }

        /// <summary>F4: languages, configuration and directories reload, the other settings do not.</summary>
        [Fact]
        public void RequiresReload_OnlyForWhatTheLoadUses()
        {
            var before = new LabelSettings { MetadataConfiguration = "A" };
            before.LoadLanguages.Add("de");

            LabelSettings sourceLanguage = before.Clone();
            sourceLanguage.SourceLanguage = "en-US";
            LabelSettings languages = before.Clone();
            languages.LoadLanguages.Add("fr-CH");
            LabelSettings configuration = before.Clone();
            configuration.MetadataConfiguration = "B";
            LabelSettings directories = before.Clone();
            directories.PackageDirectories.Add(@"K:\AOSService\PackagesLocalDirectory");

            Assert.False(before.RequiresReload(sourceLanguage));
            Assert.True(before.RequiresReload(languages));
            Assert.True(before.RequiresReload(configuration));
            Assert.True(before.RequiresReload(directories));
        }
    }
}
