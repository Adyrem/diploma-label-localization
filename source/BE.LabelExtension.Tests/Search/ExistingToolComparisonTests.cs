using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Search;
using BE.LabelExtension.Core.Sources;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace BE.LabelExtension.Tests.Search
{
    /// <summary>
    /// TC04: every search mode with fixed terms on the synthetic data gives the same hits as
    /// the existing tool, and the same top hit. The hit lists of the existing tool were captured
    /// once on the same data and the same languages (TestData/ExpectedSearchResults.json).
    /// </summary>
    public sealed class ExistingToolComparisonTests : IDisposable
    {
        private static readonly char[] RegexCharacters = @".$^{[(|)*+?\".ToCharArray();

        private readonly TestPackages packages = new();
        private readonly LabelStore store;
        private readonly ITestOutputHelper output;

        public ExistingToolComparisonTests(ITestOutputHelper output)
        {
            this.output = output;
            this.store = new LabelStore(new ModelDiscovery(this.packages.ConfigurationFolder), new ILabelSource[] { new LabelFileSource() }, new LabelFileWatcher(), new RecordingSink());
        }

        public void Dispose()
        {
            this.store.Dispose();
            this.packages.Dispose();
        }

        [Fact]
        public async Task EveryCapturedSearch_GivesTheSameHitsAndTheSameTopHit()
        {
            var settings = new LabelSettings();
            settings.PackageDirectories.Add(this.packages.PackagesDirectory);
            foreach (string language in new[] { "de", "en-US", "fr-CH", "it-CH" })
            {
                settings.LoadLanguages.Add(language);
            }

            await this.store.LoadAsync(settings, CancellationToken.None);
            var search = new LabelSearch(this.store);

            var failures = new List<string>();
            int orderDifferences = 0;
            int skipped = 0;
            List<CapturedSearch> captured = LoadCapturedSearches();
            foreach (CapturedSearch expected in captured)
            {
                SearchQuery query = ToQuery(expected);

                // Deviation A2: the existing tool puts the MatchWord term unescaped into a
                // regular expression, so "Kunde." also finds "Kunden". The extension escapes it.
                if (query.Mode == SearchMode.MatchWord && query.Term.IndexOfAny(RegexCharacters) >= 0)
                {
                    skipped++;
                    continue;
                }

                IReadOnlyList<SearchHit> actual = search.Search(query);
                string[] expectedIds = expected.Hits.Select(h => h.Id).ToArray();
                string[] actualIds = actual.Select(h => h.Label.Id.FullId).ToArray();

                string[] missing = expectedIds.Except(actualIds).ToArray();
                string[] extra = actualIds.Except(expectedIds).ToArray();
                if (missing.Length > 0 || extra.Length > 0)
                {
                    failures.Add($"{query}: missing [{string.Join(", ", missing)}], extra [{string.Join(", ", extra)}]");
                    continue;
                }

                if (expectedIds.Length > 0 && expectedIds[0] != actualIds[0])
                {
                    failures.Add($"{query}: top hit {actualIds[0]} instead of {expectedIds[0]}");
                }

                // TC04 asks for the same hits and top hit, not the same order; differences in
                // order are only reported. The captured scores are not compared: the existing
                // tool shows the score of the language it displays, not that of the label.
                if (!expectedIds.SequenceEqual(actualIds))
                {
                    orderDifferences++;
                    this.output.WriteLine($"order differs for {query}: expected {string.Join(", ", expectedIds)}; actual {string.Join(", ", actualIds)}");
                }
            }

            this.output.WriteLine($"{captured.Count} searches, {skipped} skipped as deviation A2, {failures.Count} failures, {orderDifferences} with another order.");
            Assert.True(captured.Count >= 300, "The captured results are incomplete.");
            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        }

        private static SearchQuery ToQuery(CapturedSearch search) => search.Mode switch
        {
            "ExactMatch" => new SearchQuery(search.Term, SearchMode.ExactMatch, caseSensitive: true),
            "ExactMatchIgnoreCase" => new SearchQuery(search.Term, SearchMode.ExactMatch, caseSensitive: false),
            "SubString" => new SearchQuery(search.Term, SearchMode.Substring, caseSensitive: true),
            "SubStringIgnoreCase" => new SearchQuery(search.Term, SearchMode.Substring, caseSensitive: false),
            "AnythingLikeThat" => new SearchQuery(search.Term, SearchMode.AnythingLike, caseSensitive: true),
            "AnythingLikeThatIgnoreCase" => new SearchQuery(search.Term, SearchMode.AnythingLike, caseSensitive: false),
            "LabelId" => new SearchQuery(search.Term, SearchMode.Id, caseSensitive: false),
            "MatchWord" => new SearchQuery(search.Term, SearchMode.MatchWord, caseSensitive: false),
            _ => throw new InvalidDataException($"Unknown mode {search.Mode}."),
        };

        private static List<CapturedSearch> LoadCapturedSearches()
        {
            using FileStream stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "ExpectedSearchResults.json"));
            var serializer = new DataContractJsonSerializer(typeof(List<CapturedSearch>));
            return (List<CapturedSearch>)serializer.ReadObject(stream)!;
        }

        [DataContract]
        private sealed class CapturedSearch
        {
            [DataMember(Name = "mode")]
            public string Mode { get; set; } = string.Empty;

            [DataMember(Name = "term")]
            public string Term { get; set; } = string.Empty;

            [DataMember(Name = "hits")]
            public List<CapturedHit> Hits { get; set; } = new();
        }

        [DataContract]
        private sealed class CapturedHit
        {
            [DataMember(Name = "id")]
            public string Id { get; set; } = string.Empty;

            [DataMember(Name = "score")]
            public int Score { get; set; }

            [DataMember(Name = "language")]
            public string Language { get; set; } = string.Empty;
        }
    }
}
