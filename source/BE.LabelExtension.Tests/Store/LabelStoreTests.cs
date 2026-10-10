using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Sources;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Store
{
    public sealed class LabelStoreTests : IDisposable
    {
        private readonly TestPackages packages = new();
        private readonly RecordingSink messages = new();
        private readonly LabelStore store;

        public LabelStoreTests()
        {
            this.store = new LabelStore(
                new ModelDiscovery(this.packages.ConfigurationFolder),
                new ILabelSource[] { new LabelFileSource(), new CompiledLabelSource() },
                new LabelFileWatcher(),
                this.messages);
        }

        public void Dispose()
        {
            this.store.Dispose();
            this.packages.Dispose();
        }

        [Fact]
        public async Task LoadAsync_SyntheticDirectory_LoadsAllLabelsOfTheLoadedLanguages()
        {
            await this.LoadAsync("de", "en-US", "fr-CH", "it-CH");

            Assert.True(this.store.IsLoaded);
            Assert.Equal(29 + 4 + 12 + 9 + 3, this.store.Count);
            Assert.Equal("Lieferadresse", this.store.Find("@BDM1:BDM110000003")?.GetText("de"));
            Assert.Equal("Address the goods are delivered to", this.store.Find("@BDM1:BDM110000003")?.Translations["en-US"].Comment);
            Assert.Equal(string.Empty, this.store.Find("@BDM1:BDM110000021")?.GetText("de"));
            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Message && m.Message.StartsWith("57 labels loaded"));
        }

        [Fact]
        public async Task LoadAsync_OldForm_IsFoundByItsWholeIdAndKnowsItsLabelFileOnlyFromTheFile()
        {
            await this.LoadAsync("de", "en-US");

            Label? label = this.store.Find("@DMO1001");
            Assert.NotNull(label);
            Assert.True(label!.Id.IsLegacy);
            Assert.Equal("DMO", label.LabelFile.Name);
            Assert.Equal("Delivery addresses", this.store.Find("@DMO10010")?.GetText("en-US"));
        }

        [Fact]
        public async Task LoadAsync_ExtensionFile_AddsItsTranslationsToTheLabelsOfTheBaseFile()
        {
            await this.LoadAsync("de", "it-CH");

            Label label = this.store.Find("@BDM1:BDM110000001")!;
            Assert.Equal("Cliente", label.GetText("it-CH"));
            Assert.Equal("BDM1_Extension", label.Translations["it-CH"].LabelFile.Name);
            Assert.Equal("BDM1", label.Translations["de"].LabelFile.Name);
            Assert.Null(this.store.Find("@BDM1_Extension:BDM110000001"));
        }

        [Fact]
        public async Task LoadAsync_MissingTranslation_HasNoText()
        {
            await this.LoadAsync("de", "en-US");

            Label label = this.store.Find("@BDM2:L3F2A9C15B8047DE1")!;
            Assert.Equal("Lieferadresse Kunde", label.GetText("de"));
            Assert.Null(label.GetText("en-US"));
        }

        [Fact]
        public async Task LoadAsync_LanguagesNotLoaded_AreStillKnownToTheLabelFile()
        {
            await this.LoadAsync("de");

            LabelFile bdm2 = this.store.LabelFiles.Single(f => f.Name == "BDM2");
            Assert.Equal(new[] { "de", "en-US", "fr-CH", "it-CH" }, bdm2.Languages);
            Assert.Null(this.store.Find("@BDM2:BDM210000001")?.GetText("en-US"));
            Assert.Contains(this.store.LabelFiles, f => f.Name == "BDM1_Extension");
        }

        /// <summary>TC12: en-US is recognized from folder and file name, although its XML description has no Language.</summary>
        [Fact]
        public async Task LoadAsync_EnUs_IsRecognizedFromFolderAndFileName()
        {
            await this.LoadAsync("en-US");

            Assert.Equal("Delivery address", this.store.Find("@BDM1:BDM110000003")?.GetText("en-US"));
        }

        [Fact]
        public async Task LoadAsync_ReadOnlyModels_GiveReadOnlyLabelFiles()
        {
            await this.LoadAsync("de");

            Assert.False(this.store.Find("@BDM1:BDM110000001")!.LabelFile.IsReadOnly);
            Assert.True(this.store.Find("@DMO1002")!.LabelFile.IsReadOnly);
            Assert.True(this.store.Find("@DML:DML10000001")!.LabelFile.IsReadOnly);
        }

        /// <summary>TC08: compiled labels are read without loading the assembly, unless a .label.txt exists.</summary>
        [Fact]
        public async Task LoadAsync_CompiledLabels_AreReadAndTheFileStaysFree()
        {
            string compiledPackage = Path.Combine(this.packages.PackagesDirectory, "DemoCompiled", "Resources");
            string de = this.packages.WriteCompiledLabels(compiledPackage, "DMC", "de", new Dictionary<string, string> { ["DMC1"] = "Kompiliert", ["@DMC2001"] = "Alte Form kompiliert" });
            this.packages.WriteCompiledLabels(compiledPackage, "DMC", "en-US", new Dictionary<string, string> { ["DMC1"] = "Compiled" });
            string bdm2 = this.packages.WriteCompiledLabels(Path.Combine(this.packages.PackagesDirectory, "BEDemo2", "Resources"), "BDM2", "de", new Dictionary<string, string> { ["BDM210000001"] = "aus der Ressource" });

            await this.LoadAsync("de", "en-US");

            Label compiled = this.store.Find("@DMC:DMC1")!;
            Assert.Equal("Kompiliert", compiled.GetText("de"));
            Assert.Equal("Compiled", compiled.GetText("en-US"));
            Assert.True(compiled.LabelFile.IsCompiled);
            Assert.True(compiled.LabelFile.IsReadOnly);
            Assert.Equal("Alte Form kompiliert", this.store.Find("@DMC2001")?.GetText("de"));

            Assert.Equal("Lieferung", this.store.Find("@BDM2:BDM210000001")?.GetText("de"));
            Assert.DoesNotContain(this.store.LabelFiles, f => f.IsCompiled && f.Name == "BDM2");
            Assert.Contains(this.messages.Messages, m => m.Message.Contains("label files (1 of them only compiled)"));

            File.Delete(de);
            File.Delete(bdm2);
            Assert.False(File.Exists(de));
        }

        /// <summary>
        /// Where a .label.txt exists, the compiled resource of the same label file and language
        /// is not even read. A damaged one therefore gives no warning.
        /// </summary>
        [Fact]
        public async Task LoadAsync_CompiledResourceWithLabelTxt_IsNotRead()
        {
            string folder = Path.Combine(this.packages.PackagesDirectory, "BEDemo2", "Resources", "de");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "BDM2.Resources.dll"), new byte[] { 1, 2, 3 });

            await this.LoadAsync("de");

            Assert.Equal("Lieferung", this.store.Find("@BDM2:BDM210000001")?.GetText("de"));
            Assert.DoesNotContain(this.messages.Messages, m => m.Message.Contains("BDM2.Resources.dll"));
        }

        /// <summary>
        /// Only Resources\&lt;Language&gt; directly below the package counts. The bin folders
        /// hold satellite assemblies of .NET with the same name pattern, which are no labels.
        /// </summary>
        [Fact]
        public async Task LoadAsync_ResourcesOutsideThePackageResourcesFolder_AreIgnored()
        {
            string package = Path.Combine(this.packages.PackagesDirectory, "DemoCompiled");
            this.packages.WriteCompiledLabels(Path.Combine(package, "bin"), "Satellite", "de", new Dictionary<string, string> { ["Message"] = "Keine Label" });
            this.packages.WriteCompiledLabels(Path.Combine(package, "Resources", "de", "deeper"), "Deeper", "de", new Dictionary<string, string> { ["DEEP1"] = "Zu tief" });
            this.packages.WriteCompiledLabels(Path.Combine(package, "Resources"), "DMC", "de", new Dictionary<string, string> { ["DMC1"] = "Kompiliert" });

            await this.LoadAsync("de");

            Assert.Equal("Kompiliert", this.store.Find("@DMC:DMC1")?.GetText("de"));
            Assert.Null(this.store.Find("@Satellite:Message"));
            Assert.Null(this.store.Find("@Deeper:DEEP1"));
            Assert.DoesNotContain(this.store.LabelFiles, f => f.Name == "Satellite" || f.Name == "Deeper");
        }

        /// <summary>
        /// D1: a label file whose path is longer than 260 characters is loaded, not reported as
        /// unreadable.
        /// </summary>
        [Fact]
        public async Task LoadAsync_LabelFileWithPathLongerThan260Characters_IsLoaded()
        {
            string name = "Long" + new string('x', 180);
            string path = Path.Combine(this.packages.ModelDirectory("BEDemo1", "BEDemo1"), "AxLabelFile", "LabelResources", "de", name + ".de.label.txt");
            File.WriteAllText(Core.Files.LongPath.ForAccess(path), "LONG1=Langer Pfad\r\n", new System.Text.UTF8Encoding(true));

            await this.LoadAsync("de");

            Assert.True(path.Length > 260);
            Assert.Equal("Langer Pfad", this.store.Find($"@{name}:LONG1")?.GetText("de"));
            Assert.DoesNotContain(this.messages.Messages, m => m.Severity != MessageSeverity.Message);
        }

        /// <summary>F17: a label written "ID =Text" is found by the ID without the space.</summary>
        [Fact]
        public async Task LoadAsync_SpaceBeforeEqualsSign_LabelIsFoundByItsId()
        {
            File.AppendAllText(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de"), "BDM1SPACE =Mit Leerzeichen\r\n");

            await this.LoadAsync("de");

            Assert.Equal("Mit Leerzeichen", this.store.Find("@BDM1:BDM1SPACE")?.GetText("de"));
            Assert.DoesNotContain(this.messages.Messages, m => m.Severity != MessageSeverity.Message);
        }

        /// <summary>F14: the message after loading names the memory.</summary>
        [Fact]
        public async Task LoadAsync_Message_NamesTheMemory()
        {
            await this.LoadAsync("de");

            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Message && m.Message.Contains("MB managed") && m.Message.Contains("MB private bytes"));
        }

        /// <summary>TC10: a damaged label file, a missing package directory and a locked file give a message each, the rest is loaded.</summary>
        [Fact]
        public async Task LoadAsync_DamagedMissingAndLockedFiles_AreReportedAndTheRestIsLoaded()
        {
            File.AppendAllText(this.packages.LabelFilePath("BEDemo2", "BEDemo2", "BDM2", "de"), "this line is no label\r\n");
            var settings = Settings("de", "en-US");
            settings.PackageDirectories.Insert(0, Path.Combine(this.packages.Root, "missing"));

            string locked = this.packages.LabelFilePath("BEDemo1", "BEDemo1", "FieldDescriptions_Demo", "de");
            using (new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                await this.store.LoadAsync(settings, CancellationToken.None);
            }

            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Warning && m.Message.Contains("damaged") && m.Message.Contains("BDM2.de.label.txt"));
            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Warning && m.Message.Contains("missing"));
            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Warning && m.Message.Contains("FieldDescriptions_Demo.de.label.txt"));
            Assert.Null(this.store.Find("@BDM2:BDM210000001")?.GetText("de"));
            Assert.Equal("Delivery", this.store.Find("@BDM2:BDM210000001")?.GetText("en-US"));
            Assert.Equal("Kunde", this.store.Find("@BDM1:BDM110000001")?.GetText("de"));
            Assert.DoesNotContain(this.messages.Messages, m => m.Severity == MessageSeverity.Error);
        }

        [Fact]
        public async Task LoadAsync_DuplicateIdAndExtraComment_AreReportedAndMarkTheFileForCleanup()
        {
            File.AppendAllText(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de"), "BDM110000001=Doppelt\r\n");

            await this.LoadAsync("de");

            Assert.Equal("Kunde", this.store.Find("@BDM1:BDM110000001")?.GetText("de"));
            Assert.Contains(this.messages.Messages, m => m.Message.Contains("appears a second time"));
            Assert.Equal(new[] { "de" }, this.store.LabelFiles.Single(f => f.Name == "BDM1").LanguagesNeedingCleanup);
        }

        [Fact]
        public async Task LoadAsync_UnsavedLabel_SurvivesTheReload()
        {
            await this.LoadAsync("de");
            Label unsaved = this.store.Find("@BDM1:BDM110000001")!;
            unsaved.IsModified = true;

            await this.LoadAsync("de");

            Assert.Same(unsaved, this.store.Find("@BDM1:BDM110000001"));
            Assert.NotSame(unsaved, this.store.Find("@BDM1:BDM110000002"));
        }

        [Fact]
        public async Task LoadAsync_MetadataConfiguration_TreatsReferenceFoldersAsReadOnly()
        {
            string reference = Path.Combine(this.packages.Root, "Reference");
            Directory.Move(Path.Combine(this.packages.PackagesDirectory, "BEDemo2"), Path.Combine(Directory.CreateDirectory(reference).FullName, "BEDemo2"));
            this.packages.WriteConfiguration("Demo", this.packages.PackagesDirectory, reference);
            var settings = Settings("de");
            settings.PackageDirectories.Clear();
            settings.MetadataConfiguration = "Demo";

            await this.store.LoadAsync(settings, CancellationToken.None);

            Assert.Equal("Lieferung", this.store.Find("@BDM2:BDM210000001")?.GetText("de"));
            Assert.True(this.store.Models.Single(m => m.Name == "BEDemo2").IsReadOnly);
            Assert.True(this.store.Find("@BDM2:BDM210000001")!.LabelFile.IsReadOnly);
            Assert.False(this.store.Models.Single(m => m.Name == "BEDemo1").IsReadOnly);
        }

        [Fact]
        public async Task Find_UnknownId_ReturnsNull()
        {
            Assert.Null(this.store.Find("@BDM1:BDM110000001"));
            await this.LoadAsync("de");
            Assert.Null(this.store.Find("@BDM1:UNKNOWN"));
            Assert.Null(this.store.Find(default(LabelId)));
        }

        [Fact]
        public async Task LoadAsync_RaisesChangedAfterReplacingTheIndex()
        {
            int countWhenChanged = -1;
            this.store.Changed += (_, _) => countWhenChanged = this.store.Count;

            await this.LoadAsync("de");

            Assert.Equal(this.store.Count, countWhenChanged);
        }

        private Task LoadAsync(params string[] languages) => this.store.LoadAsync(this.Settings(languages), CancellationToken.None);

        private LabelSettings Settings(params string[] languages)
        {
            var settings = new LabelSettings();
            settings.PackageDirectories.Add(this.packages.PackagesDirectory);
            foreach (string language in languages)
            {
                settings.LoadLanguages.Add(language);
            }

            return settings;
        }
    }
}
