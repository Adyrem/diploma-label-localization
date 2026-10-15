using System;
using System.Collections.Generic;

namespace BE.LabelExtension.Core.TranslationServices
{
    /// <summary>
    /// The suggestions of a translation service, or why there are none. A failure has no
    /// suggestions at all: the new label then keeps its text in every language (FA10).
    /// </summary>
    public sealed class TranslationResult
    {
        private TranslationResult(IReadOnlyDictionary<string, string> translations, string? error)
        {
            this.Translations = translations;
            this.Error = error;
        }

        /// <summary>The translation per target language, by the language as Dynamics 365 names it.</summary>
        public IReadOnlyDictionary<string, string> Translations { get; }

        /// <summary>Why the service gave no translations, <c>null</c> if it did.</summary>
        public string? Error { get; }

        /// <summary>Whether the service gave the translations.</summary>
        public bool IsSuccess => this.Error == null;

        /// <summary>A result with translations.</summary>
        /// <param name="translations">The translation per target language.</param>
        /// <returns>The result.</returns>
        public static TranslationResult Succeeded(IReadOnlyDictionary<string, string> translations)
            => new(translations ?? throw new ArgumentNullException(nameof(translations)), null);

        /// <summary>A result without translations.</summary>
        /// <param name="reason">Why, for the Output Window; never containing the API key.</param>
        /// <returns>The result.</returns>
        public static TranslationResult Failed(string reason)
            => new(new Dictionary<string, string>(), string.IsNullOrWhiteSpace(reason) ? "unknown reason" : reason);
    }
}
