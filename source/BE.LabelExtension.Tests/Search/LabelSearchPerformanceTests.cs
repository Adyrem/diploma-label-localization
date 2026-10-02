using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Search;
using Xunit;
using Xunit.Abstractions;

namespace BE.LabelExtension.Tests.Search
{
    /// <summary>
    /// Rough check of NFA02 on generated labels. The binding measurement is TC35 on the test
    /// environment; this test only catches a search mode that is far too slow.
    /// </summary>
    public class LabelSearchPerformanceTests
    {
        private const int LabelCount = 400_000;

        private static readonly string[] Words = { "Kunde", "Lieferadresse", "Rechnung", "Betrag", "Menge", "Artikel", "Lager", "Auftrag", "Zahlung", "Datum" };

        private static readonly List<Label> Labels = Generate();

        private readonly ITestOutputHelper output;

        public LabelSearchPerformanceTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData(SearchMode.ExactMatch, false, "Lieferadresse 42")]
        [InlineData(SearchMode.ExactMatch, true, "Lieferadresse 42")]
        [InlineData(SearchMode.Substring, false, "adresse 4")]
        [InlineData(SearchMode.Substring, true, "adresse 4")]
        [InlineData(SearchMode.AnythingLike, false, "liefer adresse kunde")]
        [InlineData(SearchMode.AnythingLike, true, "Liefer adresse Kunde")]
        [InlineData(SearchMode.MatchWord, false, "kunde")]
        public void Search_ManyLabels_StaysWellWithinTheLimit(SearchMode mode, bool caseSensitive, string term)
        {
            var query = new SearchQuery(term, mode, caseSensitive);
            LabelSearch.Search(Labels, query);

            var stopwatch = Stopwatch.StartNew();
            int hits = LabelSearch.Search(Labels, query).Count;
            stopwatch.Stop();

            this.output.WriteLine($"{query}: {hits} hits in {stopwatch.ElapsedMilliseconds} ms over {LabelCount} labels in two languages");
            Assert.True(stopwatch.ElapsedMilliseconds < 3000, $"{query} took {stopwatch.ElapsedMilliseconds} ms.");
        }

        private static List<Label> Generate()
        {
            var model = new ModelInfo("Perf", "Perf", "Perf", ModelLayer.VAR, false, false, "Perf", "Perf");
            var file = new LabelFile("PRF", model, isCompiled: false);
            var labels = new List<Label>(LabelCount);
            for (int i = 0; i < LabelCount; i++)
            {
                string number = i.ToString(CultureInfo.InvariantCulture);
                var label = new Label(LabelId.Parse("@PRF:L" + i.ToString("X16", CultureInfo.InvariantCulture)), file);
                string word = Words[i % Words.Length];
                string other = Words[(i / Words.Length) % Words.Length];
                label.TryAddTranslation(new Translation("de", $"{word} {other} {number}", i % 7 == 0 ? "Kommentar " + number : null, file));
                label.TryAddTranslation(new Translation("en-US", $"Text {number} for {word}", null, file));
                labels.Add(label);
            }

            return labels;
        }
    }
}
