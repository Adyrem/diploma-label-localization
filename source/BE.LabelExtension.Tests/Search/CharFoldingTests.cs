using System;
using System.Text.RegularExpressions;
using BE.LabelExtension.Core.Search;
using Xunit;

namespace BE.LabelExtension.Tests.Search
{
    /// <summary>
    /// The table must find exactly what the framework finds, so that the faster search (F14)
    /// keeps the results and TC04 stays green. Each test checks every pair of field and term.
    /// </summary>
    public class CharFoldingTests
    {
        // Umlauts, sharp s, Kelvin sign, Turkish i, long s, Greek and Cyrillic: the characters
        // where upper and lower case behave unusually.
        private static readonly string[] Fields =
        {
            "Lieferadresse", "LIEFERADRESSE", "lieferAdresse Kunde", "Grösse und Größe", "STRASSE Straße",
            "Kelvin K und k", "Istanbul ıİiI", "ſtatus", "Σίσυφος ΣΊΣΥΦΟΣ", "Счёт СЧЁТ", "", "a", "aaa aa a",
        };

        private static readonly string[] Terms =
        {
            "adresse", "ADRESSE", "Kunde", "ö", "Ö", "ß", "SS", "k", "K", "K", "i", "I", "ı", "İ", "s", "S", "ſ",
            "σ", "Σ", "ς", "ё", "Ё", "a", "aa", "xyz",
        };

        [Fact]
        public void OrdinalIgnoreCase_FindsWhatTheFrameworkFinds()
        {
            foreach (string term in Terms)
            {
                CharFolding.FoldedTerm prepared = CharFolding.OrdinalIgnoreCase.Prepare(term);
                foreach (string field in Fields)
                {
                    for (int start = 0; start <= field.Length; start++)
                    {
                        Assert.True(
                            field.IndexOf(term, start, StringComparison.OrdinalIgnoreCase) == prepared.IndexOf(field, start),
                            $"'{term}' in '{field}' from {start}");
                    }
                }
            }
        }

        [Fact]
        public void Ordinal_FindsWhatTheFrameworkFinds()
        {
            foreach (string term in Terms)
            {
                CharFolding.FoldedTerm prepared = CharFolding.Ordinal.Prepare(term);
                foreach (string field in Fields)
                {
                    for (int start = 0; start <= field.Length; start++)
                    {
                        Assert.True(
                            field.IndexOf(term, start, StringComparison.Ordinal) == prepared.IndexOf(field, start),
                            $"'{term}' in '{field}' from {start}");
                    }
                }
            }
        }

        /// <summary>
        /// MatchWord checks with this table before the regular expression. The check must never
        /// reject a field the expression matches.
        /// </summary>
        [Fact]
        public void RegexIgnoreCase_NeverRejectsAMatchOfTheExpression()
        {
            foreach (string term in Terms)
            {
                var expression = new Regex(Regex.Escape(term), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                CharFolding.FoldedTerm prepared = CharFolding.RegexIgnoreCase.Prepare(term);
                foreach (string field in Fields)
                {
                    Assert.True(!expression.IsMatch(field) || prepared.IndexOf(field, 0) >= 0, $"'{term}' in '{field}'");
                }
            }
        }
    }
}
