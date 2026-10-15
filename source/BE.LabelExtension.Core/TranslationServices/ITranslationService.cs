using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BE.LabelExtension.Core.TranslationServices
{
    /// <summary>
    /// A service that suggests translations for a new label (FA10). A further service comes as
    /// a class of its own beside the existing ones (NFA03).
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>The name the settings choose the service by, such as <c>DeepL</c>.</summary>
        string Name { get; }

        /// <summary>
        /// Whether the service has what it needs to translate, such as its API key. Without, the
        /// new label keeps its text in every language and a message names the reason (FA10).
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Translates a text into several languages. Problems such as a missing key, a refused
        /// request or an unreachable service come back as a failed result with the reason, not
        /// as an exception.
        /// </summary>
        /// <param name="text">The text in the source language.</param>
        /// <param name="sourceLanguage">The language of the text, as Dynamics 365 names it, such as <c>de</c>.</param>
        /// <param name="targetLanguages">The languages to translate into, as Dynamics 365 names them.</param>
        /// <param name="cancellationToken">Cancels the translation.</param>
        /// <returns>The translation per target language, or the reason why there is none.</returns>
        Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, IReadOnlyList<string> targetLanguages, CancellationToken cancellationToken);
    }
}
