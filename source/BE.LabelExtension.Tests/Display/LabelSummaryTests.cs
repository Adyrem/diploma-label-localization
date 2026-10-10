using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Display;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Sources;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Display
{
    /// <summary>FA05 and FA06 in the core logic: what tooltip and inline display show for a label ID.</summary>
    public sealed class LabelSummaryTests : IDisposable
    {
        // it-CH comes from BDM1_Extension, which BDM110000016 lacks.
        private static readonly string[] Languages = { "de", "en-US", "it-CH" };

        private readonly TestPackages packages = new();
        private readonly LabelStore store;

        public LabelSummaryTests()
        {
            this.store = new LabelStore(
                new ModelDiscovery(this.packages.ConfigurationFolder),
                new ILabelSource[] { new LabelFileSource(), new CompiledLabelSource() },
                new LabelFileWatcher(TimeSpan.FromMilliseconds(100)),
                new RecordingSink());
        }

        public void Dispose()
        {
            this.store.Dispose();
            this.packages.Dispose();
        }

        [Fact]
        public async Task Create_AllLanguagesTranslated_ListsThemInTheOrderOfTheSettings()
        {
            await this.LoadAsync();

            LabelSummary summary = this.Summary("@BDM1:BDM110000003");

            Assert.True(summary.IsKnown);
            Assert.Equal(Languages, summary.Lines.Select(l => l.Language));
            Assert.Equal(new[] { "Lieferadresse", "Delivery address", "Indirizzo di consegna" }, summary.Lines.Select(l => l.Text));
            Assert.Equal("all 3 languages translated", summary.Status);
            Assert.Equal("3 translations | de: Lieferadresse | en-US: Delivery address | it-CH: Indirizzo di consegna", LabelSummary.InlineText(new[] { summary }));
        }

        /// <summary>UC07 4a: a missing translation is marked.</summary>
        [Fact]
        public async Task Create_MissingTranslation_IsMarked()
        {
            await this.LoadAsync();

            LabelSummary summary = this.Summary("@BDM1:BDM110000016");
            var inline = LabelSummary.Inline(new[] { summary });

            Assert.Equal(1, summary.MissingCount);
            Assert.True(summary.Lines.Single(l => l.Language == "it-CH").IsMissing);
            Assert.Equal("2 of 3 languages translated, 1 missing", summary.Status);
            Assert.Equal("2 of 3 translations | de: Lieferadresse des Kunden | en-US: Delivery address of the customer | it-CH missing", LabelSummary.InlineText(new[] { summary }));
            Assert.Equal("it-CH missing", Assert.Single(inline, p => p.IsMarked).Text);
        }

        /// <summary>UC07 3b: an unknown ID is named as such, in both forms.</summary>
        [Theory]
        [InlineData("@BDM1:L0000000000000000")]
        [InlineData("@SYS99999")]
        public async Task Create_UnknownId_SaysSo(string id)
        {
            await this.LoadAsync();

            LabelSummary summary = this.Summary(id);

            Assert.False(summary.IsKnown);
            Assert.Empty(summary.Lines);
            Assert.Equal("not a known label", summary.Status);
            Assert.Equal("unknown label", Assert.Single(LabelSummary.Inline(new[] { summary }), p => p.IsMarked).Text);
        }

        /// <summary>The old form is found as well, here a label of a read-only model.</summary>
        [Fact]
        public async Task Create_OldForm_IsFound()
        {
            await this.LoadAsync();

            LabelSummary summary = this.Summary("@DMO1001");

            Assert.Equal("Delivery address", summary.Lines.Single(l => l.Language == "en-US").Text);
        }

        /// <summary>Several labels in one line: each part starts with its ID.</summary>
        [Fact]
        public async Task InlineText_SeveralLabels_NamesEachId()
        {
            await this.LoadAsync();

            string text = LabelSummary.InlineText(new[] { this.Summary("@BDM1:BDM110000003"), this.Summary("@SYS99999") });

            Assert.Equal("@BDM1:BDM110000003: 3 translations | de: Lieferadresse | en-US: Delivery address | it-CH: Indirizzo di consegna    @SYS99999: unknown label", text);
        }

        private LabelSummary Summary(string id)
        {
            LabelId labelId = LabelId.Parse(id);
            return LabelSummary.Create(labelId, this.store.Find(labelId), Languages);
        }

        private Task LoadAsync()
        {
            var settings = new LabelSettings();
            settings.PackageDirectories.Add(this.packages.PackagesDirectory);
            foreach (string language in Languages)
            {
                settings.LoadLanguages.Add(language);
            }

            return this.store.LoadAsync(settings, CancellationToken.None);
        }
    }
}
