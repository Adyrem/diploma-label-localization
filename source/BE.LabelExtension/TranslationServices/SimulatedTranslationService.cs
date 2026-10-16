#if DEBUG
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.TranslationServices;

namespace BE.LabelExtension.TranslationServices
{
    /// <summary>
    /// A translation service of the Debug build that answers without a network, with
    /// "&lt;language&gt;: &lt;text&gt;", so the suggestions can be tried without an API key.
    /// </summary>
    internal sealed class SimulatedTranslationService : ITranslationService
    {
        /// <summary>The name of the service in the settings.</summary>
        public const string ServiceName = "Simulated (Debug build)";

        /// <inheritdoc />
        public string Name => ServiceName;

        /// <inheritdoc />
        public bool IsConfigured => true;

        /// <inheritdoc />
        public async Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, IReadOnlyList<string> targetLanguages, CancellationToken cancellationToken)
        {
            // About the time a service takes to answer.
            await Task.Delay(300, cancellationToken).ConfigureAwait(false);
            return TranslationResult.Succeeded(targetLanguages.Distinct().ToDictionary(language => language, language => $"{language}: {text}"));
        }
    }
}
#endif
