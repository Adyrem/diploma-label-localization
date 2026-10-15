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
            var settings = new LabelSettings { MetadataConfiguration = "Demo-10.0.2527.197", SourceLanguage = "de", ProtectedApiKey = "AQAAANCMnd8B", InlineDisplay = false, InlineCount = false, SideBySide = true };
            settings.PackageDirectories.Add(@"J:\AosService\PackagesLocalDirectory");
            settings.LoadLanguages.Add("de");
            settings.LoadLanguages.Add("en-US");
            settings.CreateLanguages.Add("de-CH");
            settings.InlineLanguages.Add("en-US");
            settings.InlineLanguages.Add("de");

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
            Assert.Equal(new[] { "en-US", "de" }, loaded.InlineLanguages);
            Assert.False(loaded.InlineCount);
            Assert.True(loaded.SideBySide);
            Assert.Equal(new[] { this.File }, Directory.GetFiles(this.folder));
        }

        /// <summary>The layout of the tool window is saved without making the extension apply the settings again.</summary>
        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public void Save_RaisesSavedOnlyWhenAskedTo(bool notify, int expected)
        {
            int raised = 0;
            EventHandler<string> handler = (_, path) =>
            {
                if (path == this.File)
                {
                    raised++;
                }
            };
            LabelSettingsFile.Saved += handler;
            try
            {
                LabelSettingsFile.Save(this.File, new LabelSettings { SideBySide = true }, notify);
            }
            finally
            {
                LabelSettingsFile.Saved -= handler;
            }

            Assert.Equal(expected, raised);
            Assert.True(LabelSettingsFile.Load(this.File)!.SideBySide);
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
            Assert.Empty(loaded.InlineLanguages);
            Assert.True(loaded.InlineCount);
            Assert.False(loaded.SideBySide);
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
            LabelSettings display = before.Clone();
            display.InlineLanguages.Add("de");
            display.InlineCount = false;
            display.SideBySide = true;

            Assert.False(before.RequiresReload(sourceLanguage));
            Assert.False(before.RequiresReload(display));
            Assert.True(before.RequiresReload(languages));
            Assert.True(before.RequiresReload(configuration));
            Assert.True(before.RequiresReload(directories));
        }
    }
}
