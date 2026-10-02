using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Search;
using BE.LabelExtension.Core.Sources;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Search
{
    /// <summary>The search modes and the ranking of the existing tool (F3), on single labels and on the synthetic data.</summary>
    public sealed class LabelSearchTests : IDisposable
    {
        private static readonly ModelInfo Model = new("Test", "Test", "Test", ModelLayer.VAR, false, false, "Test", "Test");
        private static readonly LabelFile File = new("T", Model, isCompiled: false);

        private readonly TestPackages packages = new();
        private readonly LabelStore store;

        public LabelSearchTests()
        {
            this.store = new LabelStore(new ModelDiscovery(this.packages.ConfigurationFolder), new ILabelSource[] { new LabelFileSource() }, new LabelFileWatcher(), new RecordingSink());
        }

        public void Dispose()
        {
            this.store.Dispose();
            this.packages.Dispose();
        }

        [Fact]
        public void ExactMatch_TextBeforeId_TextScoresTwoIdThreeCommentOne()
        {
            Label text = Create("@T:A", ("de", "Kunde", null));
            Label id = Create("@T:Kunde", ("de", "Something else", null));
            Label comment = Create("@T:B", ("de", "Other", "Kunde"));
            Label both = Create("@T:Kunde2", ("de", "Kunde2", null));

            Assert.Equal(2, Score(text, "Kunde", SearchMode.ExactMatch));
            Assert.Equal(3, Score(id, "Kunde", SearchMode.ExactMatch));
            Assert.Equal(1, Score(comment, "Kunde", SearchMode.ExactMatch));
            Assert.Equal(2, Score(both, "Kunde2", SearchMode.ExactMatch));
        }

        [Fact]
        public void ExactMatch_ComparesTheIdWithAndWithoutLabelFile()
        {
            Label label = Create("@T:K1", ("de", "Text", null));

            Assert.Equal(3, Score(label, "@T:K1", SearchMode.ExactMatch));
            Assert.Equal(3, Score(label, "K1", SearchMode.ExactMatch));
            Assert.Equal(0, Score(label, "T:K1", SearchMode.ExactMatch));
            Assert.Equal(3, Score(label, "T:K", SearchMode.Substring));
        }

        [Fact]
        public void LabelScore_IsTheHighestOverItsLanguages()
        {
            Label label = Create("@T:Kunde", ("de", "Kunde", null), ("en-US", "Customer", null));

            // de: the text fits, 2; en-US: the ID fits, 3.
            Assert.Equal(3, Score(label, "Kunde", SearchMode.ExactMatch));
        }

        [Fact]
        public void CaseSensitivity_OnlyChangesTheComparison()
        {
            Label label = Create("@T:A", ("de", "Lieferadresse", null));

            Assert.Equal(2, Score(label, "lieferadresse", SearchMode.ExactMatch));
            Assert.Equal(0, Score(label, "lieferadresse", SearchMode.ExactMatch, caseSensitive: true));
            Assert.Equal(2, Score(label, "ADRESSE", SearchMode.Substring));
            Assert.Equal(0, Score(label, "ADRESSE", SearchMode.Substring, caseSensitive: true));
        }

        [Fact]
        public void MatchWord_IdTwoWholeWordThreeCommentOne()
        {
            Label word = Create("@T:A", ("de", "Es wurde kein Kunde angegeben.", null));
            Label part = Create("@T:B", ("de", "Kundenname", null));
            Label id = Create("@T:C", ("de", "Kunde", null));
            Label comment = Create("@T:D", ("de", "Text", "für jeden Kunde"));

            Assert.Equal(3, Score(word, "kunde", SearchMode.MatchWord, caseSensitive: true));
            Assert.Equal(0, Score(part, "kunde", SearchMode.MatchWord));
            Assert.Equal(2, Score(id, "@t:c", SearchMode.MatchWord));
            Assert.Equal(2, Score(id, "C", SearchMode.MatchWord));
            Assert.Equal(1, Score(comment, "Kunde", SearchMode.MatchWord));
        }

        [Fact]
        public void MatchWord_TermWithRegexCharacters_IsEscaped()
        {
            Label label = Create("@T:A", ("de", "Menge (Stück)", null));

            Assert.Equal(0, Score(label, "(Stück", SearchMode.MatchWord));
            Assert.Equal(0, Score(label, ".*", SearchMode.MatchWord));
        }

        [Theory]
        [InlineData("Lieferadresse", "Liefer Adresse", 500)]
        [InlineData("Lieferadresse Kunde", "Liefer Adresse", 342)]
        [InlineData("Lieferadresse", "lieferadresse", 1000)]
        [InlineData("Adresse Adresse", "adresse", 933)]
        [InlineData("Kunde", "Liefer", 0)]
        public void Coverage_IsTheMeanShareOfTheFieldTheWordsCover(string field, string term, int expected)
        {
            string[] words = term.Split(' ');
            Assert.Equal(expected, LabelSearch.Coverage(field, words, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void AnythingLike_SplitsAtSpaceCommaDotAndHyphen_AndIgnoresTheComment()
        {
            Label label = Create("@T:A", ("de", "Lieferadresse", "Liefer Adresse Kunde"));

            Assert.Equal(500, Score(label, "Liefer-Adresse, .", SearchMode.AnythingLike));
            Assert.Equal(0, Score(Create("@T:B", ("de", "Text", "Lieferadresse")), "Lieferadresse", SearchMode.AnythingLike));
            Assert.Empty(LabelSearch.Search(new[] { label }, new SearchQuery(" ,.-", SearchMode.AnythingLike, false)));
        }

        [Fact]
        public void AnythingLike_ScoresTheIdAsWell()
        {
            Label label = Create("@T:Kunde", ("de", "Something else entirely", null));

            // "@T:Kunde" has 8 characters, "kunde" covers 5 of them.
            Assert.Equal(625, Score(label, "kunde", SearchMode.AnythingLike));
            Assert.Equal(0, Score(label, "kunde", SearchMode.AnythingLike, caseSensitive: true));
        }

        [Fact]
        public void EmptyTerm_FindsNothing()
        {
            Assert.Empty(LabelSearch.Search(new[] { Create("@T:A", ("de", "A", null)) }, new SearchQuery(string.Empty, SearchMode.Substring, false)));
        }

        [Fact]
        public async Task ExactMatch_SyntheticData_FindsEachLabelOnceSortedById()
        {
            await this.LoadAsync();

            Assert.Equal(
                new[] { "@BDM1:BDM110000001", "@BDM2:BDM210000009", "@DMO1002" },
                this.Ids("kunde", SearchMode.ExactMatch));
        }

        [Fact]
        public async Task Substring_SyntheticData_TextHitsSortedById()
        {
            await this.LoadAsync();

            Assert.Equal(
                new[] { "@BDM1:BDM110000003", "@BDM1:BDM110000016", "@BDM2:L3F2A9C15B8047DE1", "@BDM2:LC0FFEE0000000001", "@DML:DML10000002", "@DMO1001", "@DMO10010" },
                this.Ids("Lieferadresse", SearchMode.Substring));
        }

        [Fact]
        public async Task Substring_SyntheticData_IdHitsRankAboveTextHits()
        {
            await this.LoadAsync();

            IReadOnlyList<SearchHit> hits = new LabelSearch(this.store).Search(new SearchQuery("DMO100", SearchMode.Substring, false));

            Assert.Equal(9, hits.Count);
            Assert.All(hits, h => Assert.Equal(3, h.Score));
            Assert.Equal("@DMO1001", hits[0].Label.Id.FullId);
        }

        [Fact]
        public async Task Substring_CommentOnly_ScoresOne()
        {
            await this.LoadAsync();

            SearchHit hit = Assert.Single(new LabelSearch(this.store).Search(new SearchQuery("delivered", SearchMode.Substring, false)));
            Assert.Equal("@BDM1:BDM110000003", hit.Label.Id.FullId);
            Assert.Equal(1, hit.Score);
        }

        [Fact]
        public async Task MatchWord_SyntheticData_FindsWholeWordsOnly()
        {
            await this.LoadAsync();

            Assert.Equal(
                new[] { "@BDM1:BDM110000001", "@BDM1:BDM110000014", "@BDM1:BDM110000015", "@BDM2:BDM210000009", "@BDM2:L3F2A9C15B8047DE1", "@DMO1002" },
                this.Ids("kunde", SearchMode.MatchWord));
        }

        [Fact]
        public async Task AnythingLike_SyntheticData_RanksByCoverage()
        {
            await this.LoadAsync();

            IReadOnlyList<SearchHit> hits = new LabelSearch(this.store).Search(new SearchQuery("liefer adresse", SearchMode.AnythingLike, false));

            // "Adresse" alone also scores 500: one of the two words covers it completely.
            Assert.Equal(new[] { "@BDM1:BDM110000003", "@BDM2:BDM210000006", "@DMO1001" }, hits.Take(3).Select(h => h.Label.Id.FullId));
            Assert.All(hits.Take(3), h => Assert.Equal(500, h.Score));
            Assert.Equal(464, hits.Single(h => h.Label.Id.FullId == "@DMO10010").Score);
            Assert.True(hits.Count > 10);
        }

        [Fact]
        public async Task Id_FindsExactlyThisLabel()
        {
            await this.LoadAsync();
            var search = new LabelSearch(this.store);

            Assert.Equal("@DMO1001", Assert.Single(search.Search(new SearchQuery("@DMO1001", SearchMode.Id, false))).Label.Id.FullId);
            Assert.Equal("@BDM1:BDM110000003", Assert.Single(search.Search(new SearchQuery("@BDM1:BDM110000003", SearchMode.Id, false))).Label.Id.FullId);
            Assert.Empty(search.Search(new SearchQuery("@dmo1001", SearchMode.Id, false)));
            Assert.Empty(search.Search(new SearchQuery("DMO1001", SearchMode.Id, false)));
        }

        [Fact]
        public async Task Search_EachLabelAppearsOnce()
        {
            await this.LoadAsync();

            IReadOnlyList<SearchHit> hits = new LabelSearch(this.store).Search(new SearchQuery("e", SearchMode.Substring, false));

            Assert.Equal(hits.Count, hits.Select(h => h.Label.Id.FullId).Distinct().Count());
        }

        private static int Score(Label label, string term, SearchMode mode, bool caseSensitive = false)
            => LabelSearch.Search(new[] { label }, new SearchQuery(term, mode, caseSensitive)).SingleOrDefault()?.Score ?? 0;

        private static Label Create(string id, params (string Language, string Text, string? Comment)[] translations)
        {
            var label = new Label(LabelId.Parse(id), File);
            foreach (var (language, text, comment) in translations)
            {
                label.TryAddTranslation(new Translation(language, text, comment, File));
            }

            return label;
        }

        private string[] Ids(string term, SearchMode mode, bool caseSensitive = false)
            => new LabelSearch(this.store).Search(new SearchQuery(term, mode, caseSensitive)).Select(h => h.Label.Id.FullId).ToArray();

        private Task LoadAsync()
        {
            var settings = new LabelSettings();
            settings.PackageDirectories.Add(this.packages.PackagesDirectory);
            foreach (string language in new[] { "de", "en-US", "fr-CH", "it-CH" })
            {
                settings.LoadLanguages.Add(language);
            }

            return this.store.LoadAsync(settings, CancellationToken.None);
        }
    }
}
