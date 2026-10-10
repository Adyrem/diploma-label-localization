using System;
using System.IO;
using System.Linq;
using System.Text;
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
