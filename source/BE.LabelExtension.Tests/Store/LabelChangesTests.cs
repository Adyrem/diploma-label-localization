using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Search;
using BE.LabelExtension.Core.Sources;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Store
{
    public sealed class LabelChangesTests : IDisposable
    {
        private readonly TestPackages packages = new();
        private readonly RecordingSink messages = new();
        private readonly LabelStore store;
        private readonly LabelChanges changes;

        public LabelChangesTests()
        {
            this.store = new LabelStore(
                new ModelDiscovery(this.packages.ConfigurationFolder),
                new ILabelSource[] { new LabelFileSource(), new CompiledLabelSource() },
                new LabelFileWatcher(TimeSpan.FromMilliseconds(100)),
                this.messages);
            this.changes = new LabelChanges(this.store, this.messages);
        }

        public void Dispose()
        {
            this.store.Dispose();
            this.packages.Dispose();
        }

        /// <summary>TC17, in the label store: text and comment in two languages changed and saved.</summary>
        [Fact]
        public async Task EditAndSave_TwoLanguages_WritesOnlyTheseLabels()
        {
            await this.LoadAsync("de", "en-US");
            string de = this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de");
            string en = this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "en-US");
            string[] deBefore = File.ReadAllLines(de);
            Label label = this.store.Find("@BDM1:BDM110000003")!;

            Assert.True(this.changes.Edit(label, "de", "Lieferanschrift", "Neuer Kommentar"));
            Assert.True(this.changes.Edit(label, "en-US", "Shipping address", null));

            Assert.True(label.IsModified);
            Assert.Equal("Lieferanschrift", label.GetText("de"));
            Assert.Equal(deBefore, File.ReadAllLines(de));

            Assert.Equal(1, this.changes.Save());

            Assert.False(label.IsModified);
            Assert.Equal(0, this.changes.Count);
            string[] deAfter = File.ReadAllLines(de);
            Assert.Equal(deBefore.Length, deAfter.Length);
            Assert.Equal(deBefore.Where(l => !l.StartsWith("BDM110000003=") && !l.Contains("Adresse, an die geliefert wird")), deAfter.Where(l => !l.StartsWith("BDM110000003=") && !l.Contains("Neuer Kommentar")));
            Assert.Contains("BDM110000003=Lieferanschrift", deAfter);
            Assert.Equal(" ;Neuer Kommentar", deAfter[Array.IndexOf(deAfter, "BDM110000003=Lieferanschrift") + 1]);
            Assert.Contains("BDM110000003=Shipping address", File.ReadAllLines(en));
            Assert.DoesNotContain("Address the goods are delivered to", File.ReadAllText(en));
        }

        /// <summary>TC07, in the label store: labels of read-only models and compiled labels cannot be changed.</summary>
        [Fact]
        public async Task Edit_ReadOnlyLabel_IsRefused()
        {
            await this.LoadAsync("de");

            Assert.False(this.changes.Edit(this.store.Find("@DMO1001")!, "de", "Geändert", null));
            Assert.False(this.changes.Edit(this.store.Find("@DML:DML10000001")!, "de", "Geändert", null));
            Assert.Equal(0, this.changes.Count);
            Assert.False(this.store.Find("@DMO1001")!.IsModified);
        }

        /// <summary>
        /// RE1: saving reads the file anew and applies only the own change, so a change made
        /// outside in the meantime stays.
        /// </summary>
        [Fact]
        public async Task Save_FileChangedOutsideMeanwhile_KeepsThatChange()
        {
            await this.LoadAsync("de");
            string de = this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de");
            this.changes.Edit(this.store.Find("@BDM1:BDM110000001")!, "de", "Kundin", null);

            File.AppendAllText(de, "BDM1OUTSIDE=Von aussen\r\n", new UTF8Encoding(false));
            this.changes.Save();

            string[] lines = File.ReadAllLines(de);
            Assert.Contains("BDM110000001=Kundin", lines);
            Assert.Contains("BDM1OUTSIDE=Von aussen", lines);
        }

        [Fact]
        public async Task EditAndSave_MissingTranslation_IsAppendedToTheFileOfItsLanguage()
        {
            await this.LoadAsync("de", "en-US");
            Label label = this.store.Find("@BDM2:L3F2A9C15B8047DE1")!;
            Assert.Null(label.GetText("en-US"));

            Assert.True(this.changes.Edit(label, "en-US", "Customer delivery address", null));
            this.changes.Save();

            Assert.Equal("L3F2A9C15B8047DE1=Customer delivery address", File.ReadAllLines(this.packages.LabelFilePath("BEDemo2", "BEDemo2", "BDM2", "en-US")).Last());
        }

        /// <summary>
        /// A language the label file does not exist in goes to the _Extension file with the same
        /// IDs (F9): BDM1 has no it-CH file, BDM1_Extension has one.
        /// </summary>
        [Fact]
        public async Task EditAndSave_LanguageOnlyInExtensionFile_GoesToTheExtensionFile()
        {
            await this.LoadAsync("de", "it-CH");
            Label label = this.store.Find("@BDM1:BDM110000005")!;

            Assert.Equal("BDM1_Extension", this.changes.GetTarget(label, "it-CH")?.Name);
            Assert.True(this.changes.Edit(label, "it-CH", "Data della fattura", null));
            this.changes.Save();

            Assert.Equal("BDM110000005=Data della fattura", File.ReadAllLines(this.packages.LabelFilePath("BEDemo2", "BEDemo2", "BDM1_Extension", "it-CH")).Last());
            Assert.Null(this.changes.GetTarget(label, "pt-BR"));
        }

        [Fact]
        public async Task Save_FileWithoutByteOrderMark_StaysWithout()
        {
            string de = this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de");
            File.WriteAllBytes(de, File.ReadAllBytes(de).Skip(3).ToArray());
            await this.LoadAsync("de");

            this.changes.Edit(this.store.Find("@BDM1:BDM110000001")!, "de", "Kundin", null);
            this.changes.Save();

            Assert.NotEqual(0xEF, File.ReadAllBytes(de)[0]);
        }

        /// <summary>FA15: the own save is not reported as a change from outside.</summary>
        [Fact]
        public async Task Save_OwnWrite_IsNotReportedAsExternalChange()
        {
            await this.LoadAsync("de");
            int external = 0;
            this.store.ExternalChange += (_, _) => Interlocked.Increment(ref external);

            this.changes.Edit(this.store.Find("@BDM1:BDM110000001")!, "de", "Kundin", null);
            this.changes.Save();
            await Task.Delay(600);

            Assert.Equal(0, external);
        }

        [Fact]
        public async Task Edit_LineBreak_IsRejectedAndNothingChanges()
        {
            await this.LoadAsync("de");
            Label label = this.store.Find("@BDM1:BDM110000001")!;

            Assert.Throws<ArgumentException>(() => this.changes.Edit(label, "de", "Zwei\r\nZeilen", null));
            Assert.Equal("Kunde", label.GetText("de"));
            Assert.Equal(0, this.changes.Save());
        }

        [Fact]
        public async Task Save_DamagedFile_IsNotWrittenAndTheChangeStaysPending()
        {
            await this.LoadAsync("de");
            string de = this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de");
            Label label = this.store.Find("@BDM1:BDM110000001")!;
            this.changes.Edit(label, "de", "Kundin", null);
            File.AppendAllText(de, "this line is no label\r\n");

            Assert.Equal(0, this.changes.Save());

            Assert.True(label.IsModified);
            Assert.Equal(1, this.changes.Count);
            Assert.Contains("this line is no label", File.ReadAllText(de));
            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Error && m.Message.Contains("damaged"));
        }

        /// <summary>
        /// TC18 in the core logic: a new label in every language to create that the label file
        /// has, saved at once, with an ID in the new form. The missing language is named.
        /// </summary>
        [Fact]
        public async Task Create_LabelFileWithoutOneLanguage_WritesTheOthersAndNamesTheMissingOne()
        {
            await this.LoadAsync("de", "en-US");
            LabelFile bdm1 = this.store.LabelFiles.Single(f => f.Name == "BDM1");
            var texts = new[] { new LabelText("de", "Lieferschein", "Neu"), new LabelText("en-US", "Lieferschein", null), new LabelText("it-CH", "Lieferschein", null) };

            Label label = this.changes.Create(bdm1, texts)!;

            Assert.Matches("^@BDM1:L[0-9A-F]{16}$", label.Id.FullId);
            Assert.False(label.IsModified);
            Assert.Same(label, this.store.Find(label.Id));
            string[] de = File.ReadAllLines(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de"));
            Assert.Equal(label.Id.Key + "=Lieferschein", de[de.Length - 2]);
            Assert.Equal(" ;Neu", de.Last());
            Assert.Equal(label.Id.Key + "=Lieferschein", File.ReadAllLines(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "en-US")).Last());
            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Warning && m.Message.Contains("it-CH"));
        }

        /// <summary>A language that is not loaded is written as well, straight into its file.</summary>
        [Fact]
        public async Task Create_LanguageNotLoaded_IsWrittenToo()
        {
            await this.LoadAsync("de");
            LabelFile bdm1 = this.store.LabelFiles.Single(f => f.Name == "BDM1");

            Label label = this.changes.Create(bdm1, new[] { new LabelText("de", "Lieferschein", null), new LabelText("fr-CH", "Bulletin de livraison", null) })!;

            Assert.Null(label.GetText("fr-CH"));
            Assert.Equal(label.Id.Key + "=Bulletin de livraison", File.ReadAllLines(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "fr-CH")).Last());
        }

        /// <summary>TC07 and F9: read-only, compiled and _Extension label files are not offered for new labels.</summary>
        [Fact]
        public async Task CreatableLabelFiles_AreTheWritableBaseFiles()
        {
            await this.LoadAsync("de", "it-CH");

            Assert.Equal(new[] { "BDM1", "BDM2", "FieldDescriptions_Demo" }, this.changes.CreatableLabelFiles().Select(f => f.Name));
            Assert.Throws<ArgumentException>(() => this.changes.Create(this.store.LabelFiles.Single(f => f.Name == "BDM1_Extension"), new[] { new LabelText("it-CH", "x", null) }));
        }

        /// <summary>TC17 in the core logic: deleting removes the label in all languages of its label file, also those not loaded, once saved.</summary>
        [Fact]
        public async Task DeleteAndSave_RemovesTheLabelFromEveryFile()
        {
            await this.LoadAsync("de", "it-CH");
            Label label = this.store.Find("@BDM1:BDM110000003")!;

            Assert.True(this.changes.Delete(label));
            Assert.True(label.IsDeleted);
            Assert.DoesNotContain(new LabelSearch(this.store).Search(new SearchQuery("Lieferadresse", SearchMode.ExactMatch, true)), h => h.Label == label);
            Assert.Contains("BDM110000003=Lieferadresse", File.ReadAllLines(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de")));

            this.changes.Save();

            Assert.Null(this.store.Find("@BDM1:BDM110000003"));
            foreach (string language in new[] { "de", "en-US", "fr-CH" })
            {
                Assert.DoesNotContain(File.ReadAllLines(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", language)), l => l.StartsWith("BDM110000003="));
            }

            Assert.DoesNotContain(File.ReadAllLines(this.packages.LabelFilePath("BEDemo2", "BEDemo2", "BDM1_Extension", "it-CH")), l => l.StartsWith("BDM110000003="));
            Assert.Equal(0, this.changes.Count);
        }

        /// <summary>TC07 in the core logic: labels of read-only models cannot be deleted.</summary>
        [Fact]
        public async Task Delete_ReadOnlyLabel_IsRefused()
        {
            await this.LoadAsync("de");

            Assert.False(this.changes.Delete(this.store.Find("@DMO1001")!));
            Assert.Equal(0, this.changes.Count);
        }

        /// <summary>
        /// TC06 in the core logic: the copy has an ID in the new form and every language both
        /// label files have, with text and comment, also those not loaded.
        /// </summary>
        [Fact]
        public async Task Copy_ToAnotherLabelFile_TakesEveryCommonLanguage()
        {
            await this.LoadAsync("de");
            Label original = this.store.Find("@BDM1:BDM110000003")!;
            LabelFile bdm2 = this.store.LabelFiles.Single(f => f.Name == "BDM2");

            Label copy = this.changes.Copy(original, bdm2)!;

            Assert.Matches("^@BDM2:L[0-9A-F]{16}$", copy.Id.FullId);
            Assert.Equal("Lieferadresse", copy.GetText("de"));
            Assert.Equal("Adresse, an die geliefert wird", copy.GetTranslation("de")!.Comment);
            string[] en = File.ReadAllLines(this.packages.LabelFilePath("BEDemo2", "BEDemo2", "BDM2", "en-US"));
            Assert.Equal(copy.Id.Key + "=Delivery address", en[en.Length - 2]);
            Assert.Equal(" ;Address the goods are delivered to", en.Last());
            Assert.Contains(copy.Id.Key + "=Adresse de livraison", File.ReadAllLines(this.packages.LabelFilePath("BEDemo2", "BEDemo2", "BDM2", "fr-CH")));
            Assert.Same(original, this.store.Find("@BDM1:BDM110000003"));
        }

        /// <summary>TC07 in the core logic: labels of read-only models in the old form can be copied, and the copy has the new form.</summary>
        [Fact]
        public async Task Copy_ReadOnlyLabelInOldForm_GivesANewLabelInTheNewForm()
        {
            await this.LoadAsync("de", "en-US");
            LabelFile bdm1 = this.store.LabelFiles.Single(f => f.Name == "BDM1");

            Label copy = this.changes.Copy(this.store.Find("@DMO1001")!, bdm1)!;

            Assert.Matches("^@BDM1:L[0-9A-F]{16}$", copy.Id.FullId);
            Assert.Equal(this.store.Find("@DMO1001")!.GetText("en-US"), copy.GetText("en-US"));
        }

        /// <summary>TC06 in the core logic: moving is copying and deleting the original, saved at once.</summary>
        [Fact]
        public async Task Move_CopyThenDeleteAndSaveTheOriginal()
        {
            await this.LoadAsync("de");
            Label original = this.store.Find("@BDM1:BDM110000003")!;

            Label copy = this.changes.Copy(original, this.store.LabelFiles.Single(f => f.Name == "BDM2"))!;
            Assert.True(this.changes.Delete(original));
            this.changes.Save(new[] { original });

            Assert.Null(this.store.Find("@BDM1:BDM110000003"));
            Assert.Same(copy, this.store.Find(copy.Id));
            Assert.DoesNotContain(File.ReadAllLines(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de")), l => l.StartsWith("BDM110000003="));
        }

        private Task LoadAsync(params string[] languages)
        {
            var settings = new LabelSettings();
            settings.PackageDirectories.Add(this.packages.PackagesDirectory);
            foreach (string language in languages)
            {
                settings.LoadLanguages.Add(language);
            }

            return this.store.LoadAsync(settings, CancellationToken.None);
        }
    }
}
