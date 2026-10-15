using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BE.LabelExtension.Core.TranslationServices
{
    /// <summary>
    /// Translation through the REST interface of DeepL (FA10, concept: technical decisions).
    /// The key goes in the header <c>Authorization</c> as <c>DeepL-Auth-Key</c>. Keys of DeepL
    /// API Free end in <c>:fx</c> and go to <c>api-free.deepl.com</c>, all others to
    /// <c>api.deepl.com</c>.
    /// </summary>
    /// <remarks>
    /// One request per target language, because DeepL takes only one target per request. The
    /// key is read for every translation, so a key changed in the options counts at once, and
    /// it never appears in a result or message.
    /// </remarks>
    public sealed class DeepLTranslationService : ITranslationService, IDisposable
    {
        /// <summary>The name of the service in the settings.</summary>
        public const string ServiceName = "DeepL";

        private static readonly Uri FreeEndpoint = new("https://api-free.deepl.com/v2/translate");
        private static readonly Uri ProEndpoint = new("https://api.deepl.com/v2/translate");
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

        private readonly Func<string?> apiKey;
        private readonly HttpClient client;

        /// <summary>Creates the service.</summary>
        /// <param name="apiKey">Reads the current API key, <c>null</c> or empty if none is set.</param>
        /// <param name="handler">Sends the requests; a simulated one in tests, otherwise that of the framework.</param>
        public DeepLTranslationService(Func<string?> apiKey, HttpMessageHandler? handler = null)
        {
            this.apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            this.client = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            this.client.Timeout = Timeout;
        }

        /// <inheritdoc />
        public string Name => ServiceName;

        /// <inheritdoc />
        public bool IsConfigured => !string.IsNullOrWhiteSpace(this.apiKey());

        /// <summary>
        /// The target language of DeepL for a language of Dynamics 365. <c>de-CH</c> keeps its
        /// variant, because it writes "Strasse" instead of "Straße"; French and Italian have no
        /// Swiss variant at DeepL; English keeps its variant; any other language goes by its
        /// base code.
        /// </summary>
        /// <param name="language">The language, such as <c>fr-CH</c>.</param>
        /// <returns>The code for DeepL, such as <c>FR</c>.</returns>
        public static string TargetCode(string language)
        {
            string code = (language ?? throw new ArgumentNullException(nameof(language))).Trim().ToUpperInvariant();
            return code switch
            {
                "DE-CH" => "DE-CH",
                "EN-US" => "EN-US",
                "EN-GB" => "EN-GB",
                _ => BaseCode(code),
            };
        }

        /// <summary>The source language of DeepL, which takes only the base code: <c>de-CH</c> goes out as <c>DE</c>.</summary>
        /// <param name="language">The language, such as <c>de-CH</c>.</param>
        /// <returns>The code for DeepL, such as <c>DE</c>.</returns>
        public static string SourceCode(string language)
            => BaseCode((language ?? throw new ArgumentNullException(nameof(language))).Trim().ToUpperInvariant());

        /// <summary>The address for a key: DeepL API Free for keys ending in <c>:fx</c>, otherwise DeepL API Pro.</summary>
        /// <param name="key">The API key.</param>
        /// <returns>The address of the translation.</returns>
        public static Uri EndpointFor(string key)
            => key.Trim().EndsWith(":fx", StringComparison.OrdinalIgnoreCase) ? FreeEndpoint : ProEndpoint;

        /// <inheritdoc />
        public async Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, IReadOnlyList<string> targetLanguages, CancellationToken cancellationToken)
        {
            string? key = this.apiKey()?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                return TranslationResult.Failed("no API key for DeepL is set in the options (Tools > Options > BE-LabelExtension)");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return TranslationResult.Failed("the text to translate is empty");
            }

            Uri endpoint = EndpointFor(key!);
            var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string target in targetLanguages.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                (string? translation, string? error) = await this.RequestAsync(endpoint, key!, text, SourceCode(sourceLanguage), TargetCode(target), cancellationToken).ConfigureAwait(false);
                if (error != null)
                {
                    return TranslationResult.Failed(error);
                }

                translations[target] = translation!;
            }

            return TranslationResult.Succeeded(translations);
        }

        /// <inheritdoc />
        public void Dispose() => this.client.Dispose();

        private static string BaseCode(string code)
        {
            int dash = code.IndexOf('-');
            return dash > 0 ? code.Substring(0, dash) : code;
        }

        private async Task<(string? Translation, string? Error)> RequestAsync(Uri endpoint, string key, string text, string source, string target, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(Serialize(new TranslateRequest { Text = new[] { text }, SourceLang = source, TargetLang = target }), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", key);

            HttpResponseMessage response;
            try
            {
                response = await this.client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return (null, $"DeepL did not answer within {Timeout.TotalSeconds:0} s");
            }
            catch (HttpRequestException exception)
            {
                return (null, $"DeepL is not reachable ({Innermost(exception).Message})");
            }

            using (response)
            {
                string body = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return (null, Describe(response.StatusCode, body, target));
                }

                TranslateResponse? parsed = Deserialize(body);
                string? translation = parsed?.Translations?.FirstOrDefault()?.Text;
                return translation == null
                    ? (null, $"DeepL answered for {target} without a translation")
                    : (translation, null);
            }
        }

        // The status codes DeepL documents, with its own message where it gives one.
        private static string Describe(HttpStatusCode status, string body, string target)
        {
            string? message = DeserializeError(body)?.Message;
            string detail = string.IsNullOrWhiteSpace(message) ? string.Empty : $": {message}";
            return (int)status switch
            {
                400 => $"DeepL rejected the request for {target}{detail}",
                403 => "DeepL rejected the API key; check it in the options",
                413 => "the text is too long for DeepL",
                429 => "DeepL received too many requests; try again shortly",
                456 => "the character quota of the DeepL account is used up",
                _ => $"DeepL answered with {(int)status} {status}{detail}",
            };
        }

        private static Exception Innermost(Exception exception)
        {
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            return exception;
        }

        private static string Serialize(TranslateRequest request)
        {
            using var stream = new MemoryStream();
            new DataContractJsonSerializer(typeof(TranslateRequest)).WriteObject(stream, request);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static TranslateResponse? Deserialize(string body)
        {
            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
                return (TranslateResponse?)new DataContractJsonSerializer(typeof(TranslateResponse)).ReadObject(stream);
            }
            catch (SerializationException)
            {
                return null;
            }
        }

        private static ErrorResponse? DeserializeError(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
                return (ErrorResponse?)new DataContractJsonSerializer(typeof(ErrorResponse)).ReadObject(stream);
            }
            catch (SerializationException)
            {
                return null;
            }
        }

        [DataContract]
        private sealed class TranslateRequest
        {
            [DataMember(Name = "text", Order = 1)]
            public string[] Text { get; set; } = Array.Empty<string>();

            [DataMember(Name = "source_lang", Order = 2)]
            public string SourceLang { get; set; } = string.Empty;

            [DataMember(Name = "target_lang", Order = 3)]
            public string TargetLang { get; set; } = string.Empty;
        }

        [DataContract]
        private sealed class TranslateResponse
        {
            [DataMember(Name = "translations")]
            public TranslatedText[]? Translations { get; set; }
        }

        [DataContract]
        private sealed class TranslatedText
        {
            [DataMember(Name = "text")]
            public string? Text { get; set; }
        }

        [DataContract]
        private sealed class ErrorResponse
        {
            [DataMember(Name = "message")]
            public string? Message { get; set; }
        }
    }
}
