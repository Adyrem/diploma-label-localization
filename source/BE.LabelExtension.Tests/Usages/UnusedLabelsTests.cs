using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Sources;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Core.Usages;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Usages
{
    /// <summary>KA09: labels of writable models that no element of a writable model uses.</summary>
    public sealed class UnusedLabelsTests : IDisposable
    {
        private static readonly ISet<string> Candidates = new HashSet<string>(StringComparer.Ordinal) { "@BDM1:L1", "@SYS1", "@BDM1:L1.Old" };

        private readonly TestPackages packages = new();
        private readonly RecordingSink messages = new();
        private readonly LabelStore store;

        public UnusedLabelsTests()
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

        [Theory]
        [InlineData("<Label>@BDM1:L1</Label>", false, "@BDM1:L1")]
        [InlineData("return \"@BDM1:L1\";", false, "@BDM1:L1")]
        [InlineData("x = literalStr(@SYS1);", false, "@SYS1")]
        [InlineData("=Labels!@SYS1 & Fields!Name.Value", true, "@SYS1")]
        [InlineData("=Labels!@SYS1.", true, "@SYS1")]
        [InlineData("<Label>@BDM1:L1.Old</Label>", false, "@BDM1:L1.Old")]
        [InlineData("<Label>@BDM1:L10</Label>", false, "")]
        [InlineData("=Labels!@SYS1 &", false, "")]
        [InlineData("mail@BDM1:L1 x", false, "")]
        public void UsedIn_FollowsThePatternsOfTheUsageSearch(string text, bool isReport, string expected)
        {
            Assert.Equal(expected, string.Join("|", UnusedLabels.UsedIn(text, isReport, Candidates)));
        }

        [Fact]
        public async Task Find_LabelUsedNowhere_IsUnused()
        {
            File.AppendAllText(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de"), "BDM1UNUSED=Nie verwendet\r\n");
            var settings = new LabelSettings();
            settings.PackageDirectories.Add(this.packages.PackagesDirectory);
            settings.LoadLanguages.Add("de");
            await this.store.LoadAsync(settings, CancellationToken.None);
            List<ModelInfo> writable = this.store.Models.Where(m => !m.IsReadOnly).ToList();
            List<Label> labels = this.store.Labels.Where(l => !l.LabelFile.IsReadOnly).ToList();

            (IReadOnlyList<Label> unused, int files) = UnusedLabels.Find(labels, writable, CancellationToken.None);

            Assert.Contains(unused, l => l.Id.FullId == "@BDM1:BDM1UNUSED");
            Assert.DoesNotContain(unused, l => l.Id.FullId == "@BDM1:BDM110000003");
            Assert.True(files > 0);
        }
    }
}
