using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using BE.LabelExtension.Core.Labels;
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
    /// Rough check of NFA02 on generated labels in the volume of the test environment: about
    /// 490 000 labels in five languages, each translation with a comment (B14, B16). The labels
    /// go through the label store as when loading, so load time and memory are measured too.
    /// The binding measurement is TC35 on the test environment; this test only catches a search
    /// mode that is far too slow.
    /// </summary>
    public sealed class LabelSearchPerformanceTests : IClassFixture<LabelSearchPerformanceTests.GeneratedStore>
    {
        private readonly GeneratedStore generated;
        private readonly ITestOutputHelper output;

        public LabelSearchPerformanceTests(GeneratedStore generated, ITestOutputHelper output)
        {
            this.generated = generated;
            this.output = output;
        }

        [Fact]
        public void Load_ManyLabels_ReportsTimeAndMemory()
        {
            this.output.WriteLine($"{this.generated.Store.Count} labels in {GeneratedStore.Languages.Length} languages loaded in {this.generated.LoadTime.TotalSeconds:0.0} s, {this.generated.MemoryBytes / (1024 * 1024)} MB managed memory");
            Assert.Equal(GeneratedStore.LabelCount, this.generated.Store.Count);
        }

        [Theory]
        [InlineData(SearchMode.ExactMatch, true, "Lieferadresse 42")]
        [InlineData(SearchMode.ExactMatch, false, "Lieferadresse 42")]
        [InlineData(SearchMode.Substring, true, "adresse 4")]
        [InlineData(SearchMode.Substring, false, "adresse 4")]
        [InlineData(SearchMode.AnythingLike, true, "liefer adresse kunde")]
        [InlineData(SearchMode.AnythingLike, false, "liefer adresse kunde")]
        [InlineData(SearchMode.MatchWord, false, "kunde")]
        public void Search_ManyLabels_StaysWellWithinTheLimit(SearchMode mode, bool caseSensitive, string term)
        {
            var search = new LabelSearch(this.generated.Store);
            var query = new SearchQuery(term, mode, caseSensitive);
            int hits = search.Search(query).Count;

            var times = new long[3];
            for (int run = 0; run < times.Length; run++)
            {
                var stopwatch = Stopwatch.StartNew();
                search.Search(query);
                times[run] = stopwatch.ElapsedMilliseconds;
            }

            this.output.WriteLine($"{query}: {hits} hits in {string.Join(" / ", times)} ms over {this.generated.Store.Count} labels in {GeneratedStore.Languages.Length} languages, {Environment.ProcessorCount} logical processors");
            Assert.All(times, time => Assert.True(time < 3000, $"{query} took {time} ms."));
        }

        /// <summary>The labels, loaded once for all tests of the class.</summary>
        public sealed class GeneratedStore : IDisposable
        {
            public const int LabelCount = 490_000;

            public static readonly string[] Languages = { "en-US", "de", "de-CH", "fr-CH", "it-CH" };

            public GeneratedStore()
            {
                var source = new GeneratedSource();
                this.Store = new LabelStore(
                    new ModelDiscovery(Path.Combine(Path.GetTempPath(), "BE.LabelExtension.Tests", "no-configuration")),
                    new ILabelSource[] { source },
                    new LabelFileWatcher(),
                    new RecordingSink());

                var settings = new LabelSettings();
                foreach (string language in Languages)
                {
                    settings.LoadLanguages.Add(language);
                }

                // Memory: what remains after loading, once the documents read are gone. Time:
                // only the label store, without generating the documents.
                long before = GC.GetTotalMemory(forceFullCollection: true);
                source.Generate();
                var stopwatch = Stopwatch.StartNew();
                this.Store.LoadAsync(settings, CancellationToken.None).GetAwaiter().GetResult();
                this.LoadTime = stopwatch.Elapsed;
                source.Release();
                this.MemoryBytes = GC.GetTotalMemory(forceFullCollection: true) - before;
            }

            public LabelStore Store { get; }

            public TimeSpan LoadTime { get; }

            public long MemoryBytes { get; }

            public void Dispose() => this.Store.Dispose();
        }

        /// <summary>
        /// Delivers the labels as the label files would: one document per language, every string
        /// a new instance as read from a file. As in the files of the platform, the comment is
        /// the same in every language and the de-CH text mostly equals the de text.
        /// </summary>
        private sealed class GeneratedSource : ILabelSource
        {
            private static readonly string[] Words = { "Kunde", "Lieferadresse", "Rechnung", "Betrag", "Menge", "Artikel", "Lager", "Auftrag", "Zahlung", "Datum" };

            private LabelSourceResult? result;

            public LabelSourceResult Load(LabelLoadRequest request, CancellationToken cancellationToken)
                => this.result ?? throw new InvalidOperationException("Generate first.");

            public void Release() => this.result = null;

            public void Generate()
            {
                var model = new ModelInfo("Perf", "Perf", "Perf", ModelLayer.SYS, false, true, "Perf", "Perf");
                var file = new LabelFile("PRF", model, isCompiled: false);
                var documents = new List<LabelDocument>();
                foreach (string language in GeneratedStore.Languages)
                {
                    file.AddLanguage(language, $"Perf\\PRF.{language}.label.txt");
                    var entries = new List<LabelEntry>(GeneratedStore.LabelCount);
                    for (int i = 0; i < GeneratedStore.LabelCount; i++)
                    {
                        string number = i.ToString(CultureInfo.InvariantCulture);
                        string word = Words[i % Words.Length];
                        string other = Words[(i / Words.Length) % Words.Length];
                        string text = language switch
                        {
                            "en-US" => $"Text {number} for {word}",
                            "fr-CH" => $"Texte {number} pour {word}",
                            "it-CH" => $"Testo {number} per {word}",
                            "de-CH" when i % 10 == 0 => $"{other} {word} {number}",
                            _ => $"{word} {other} {number}",
                        };
                        entries.Add(new LabelEntry("L" + i.ToString("X16", CultureInfo.InvariantCulture), text, $"Comment on {word} and {other}, number {number}"));
                    }

                    documents.Add(new LabelDocument(file, language, $"Perf\\PRF.{language}.label.txt", entries));
                }

                this.result = new LabelSourceResult(new[] { file }, documents);
            }
        }
    }
}
