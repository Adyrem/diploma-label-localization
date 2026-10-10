using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Sources;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Core.Usages;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Store
{
    /// <summary>TC06 and TC05 through the label store: copying, moving and replacing with the references.</summary>
    public sealed class LabelOperationsTests : IDisposable
    {
        private const string Original = "@BDM1:BDM110000003";

        private readonly TestPackages packages = new();
        private readonly RecordingSink messages = new();
        private readonly LabelStore store;
        private readonly LabelChanges changes;
        private readonly LabelOperations operations;

        public LabelOperationsTests()
        {
            this.store = new LabelStore(
                new Core.Models.ModelDiscovery(this.packages.ConfigurationFolder),
                new ILabelSource[] { new LabelFileSource(), new CompiledLabelSource() },
                new LabelFileWatcher(TimeSpan.FromMilliseconds(100)),
                this.messages);
            this.changes = new LabelChanges(this.store, this.messages);
            this.operations = new LabelOperations(this.store, this.changes, this.messages);
        }

        private string ClassFile => Path.Combine(this.packages.ModelDirectory("BEDemo1", "BEDemo1"), "AxClass", "BDMDeliveryHelper.xml");

        private string ReadOnlyFile => Path.Combine(this.packages.ModelDirectory("DemoBase", "DemoBase"), "AxClass", "DMODeliveryHelper.xml");

        private LabelFile Bdm2 => this.store.LabelFiles.Single(f => f.Name == "BDM2");

        public void Dispose()
        {
            this.store.Dispose();
            this.packages.Dispose();
        }

        /// <summary>The references are searched in writable models only.</summary>
        [Fact]
        public async Task FindReferences_OnlyWritableModels()
        {
            await this.LoadAsync();

            var files = this.operations.FindReferences(Original, CancellationToken.None);

            Assert.Equal(3, files.Count);
            Assert.DoesNotContain(this.ReadOnlyFile, files);
        }

        /// <summary>TC06: the copy has the new form, the references in writable models point to it, the original stays.</summary>
        [Fact]
        public async Task Copy_WithReferences_PointsTheWritableModelsToTheCopy()
        {
            await this.LoadAsync();
            byte[] readOnlyBefore = File.ReadAllBytes(this.ReadOnlyFile);
            var files = this.operations.FindReferences(Original, CancellationToken.None);

            Label copy = this.operations.Copy(this.store.Find(Original)!, this.Bdm2, files)!;

            Assert.Matches("^@BDM2:L[0-9A-F]{16}$", copy.Id.FullId);
            Assert.Equal(6, LabelReferences.Find(this.operations.WritableModels(), copy.Id.FullId, CancellationToken.None).Count());
            Assert.Empty(LabelReferences.Find(this.operations.WritableModels(), Original, CancellationToken.None));
            Assert.Equal(readOnlyBefore, File.ReadAllBytes(this.ReadOnlyFile));
            Assert.NotNull(this.store.Find(Original));
            Assert.Contains(this.messages.Messages, m => m.Message == $"References to {Original} changed to {copy.Id.FullId} in 3 files.");
        }

        /// <summary>TC06: copying without references leaves the elements as they are.</summary>
        [Fact]
        public async Task Copy_WithoutReferences_ChangesNoElement()
        {
            await this.LoadAsync();
            byte[] classBefore = File.ReadAllBytes(this.ClassFile);

            Label copy = this.operations.Copy(this.store.Find(Original)!, this.Bdm2, null)!;

            Assert.NotNull(this.store.Find(copy.Id));
            Assert.Equal(classBefore, File.ReadAllBytes(this.ClassFile));
        }

        /// <summary>TC06: moving deletes the original in all languages and points the references to the copy.</summary>
        [Fact]
        public async Task Move_WithReferences_DeletesTheOriginalAndPointsToTheCopy()
        {
            await this.LoadAsync();
            var files = this.operations.FindReferences(Original, CancellationToken.None);

            Label copy = this.operations.Move(this.store.Find(Original)!, this.Bdm2, files)!;

            Assert.Null(this.store.Find(Original));
            Assert.Equal(0, this.changes.Count);
            foreach (string language in new[] { "de", "en-US" })
            {
                Assert.DoesNotContain(File.ReadAllLines(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", language)), l => l.StartsWith("BDM110000003="));
                Assert.Contains(copy.Id.Key + "=" + (language == "de" ? "Lieferadresse" : "Delivery address"), File.ReadAllLines(this.packages.LabelFilePath("BEDemo2", "BEDemo2", "BDM2", language)));
            }

            Assert.Contains("\"" + copy.Id.FullId + "\"", File.ReadAllText(this.ClassFile));
            Assert.Contains(Original, File.ReadAllText(this.ReadOnlyFile));
        }

        /// <summary>A reference that cannot be changed keeps the original, so that no element points to a deleted label.</summary>
        [Fact]
        public async Task Move_ReferenceCannotBeChanged_KeepsTheOriginal()
        {
            await this.LoadAsync();
            var files = this.operations.FindReferences(Original, CancellationToken.None);

            Label? copy;
            using (new FileStream(this.ClassFile, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                copy = this.operations.Move(this.store.Find(Original)!, this.Bdm2, files);
            }

            Assert.NotNull(copy);
            Assert.NotNull(this.store.Find(Original));
            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Error && m.Message.Contains(this.ClassFile));
            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Warning && m.Message.StartsWith(Original + " is kept"));
            Assert.Contains("BDM110000003=Lieferadresse", File.ReadAllLines(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de")));
        }

        /// <summary>TC07: a label of a read-only model cannot be moved, nothing is created.</summary>
        [Fact]
        public async Task Move_ReadOnlyLabel_IsRefused()
        {
            await this.LoadAsync();
            int before = this.store.Count;

            Assert.Null(this.operations.Move(this.store.Find("@DMO1001")!, this.Bdm2, null));
            Assert.Equal(before, this.store.Count);
        }

        /// <summary>TC05 and FA13: the Output Window says how many files were changed.</summary>
        [Fact]
        public async Task Replace_ReportsTheNumberOfChangedFiles()
        {
            await this.LoadAsync();
            var files = this.operations.FindReferences(Original, CancellationToken.None);

            Assert.Equal(3, this.operations.Replace(Original, "@BDM1:BDM110000016", files));

            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Message && m.Message == $"Replaced {Original} by @BDM1:BDM110000016 in 3 files.");
            Assert.Empty(this.operations.FindReferences(Original, CancellationToken.None));
        }

        /// <summary>FA13: only another, existing label can replace a label.</summary>
        [Theory]
        [InlineData("", "Enter the label ID to use instead.")]
        [InlineData("Lieferadresse", "Lieferadresse is not a label ID.")]
        [InlineData(" @BDM1:BDM110000003 ", "@BDM1:BDM110000003 is the label to replace. Enter another label ID.")]
        [InlineData("@BDM1:L0000000000000000", "@BDM1:L0000000000000000 is not a loaded label.")]
        public async Task CheckReplacement_InvalidInput_SaysWhy(string input, string reason)
        {
            await this.LoadAsync();

            Assert.Equal(reason, this.operations.CheckReplacement(this.store.Find(Original)!, input, out Label? replacement));
            Assert.Null(replacement);
        }

        [Fact]
        public async Task CheckReplacement_ExistingLabel_IsAccepted()
        {
            await this.LoadAsync();

            Assert.Null(this.operations.CheckReplacement(this.store.Find(Original)!, " @DMO1001 ", out Label? replacement));
            Assert.Equal("@DMO1001", replacement!.Id.FullId);
        }

        private Task LoadAsync()
        {
            var settings = new LabelSettings();
            settings.PackageDirectories.Add(this.packages.PackagesDirectory);
            settings.LoadLanguages.Add("de");
            settings.LoadLanguages.Add("en-US");
            return this.store.LoadAsync(settings, CancellationToken.None);
        }
    }
}
