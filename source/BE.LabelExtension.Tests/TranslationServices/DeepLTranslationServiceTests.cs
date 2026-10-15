using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.TranslationServices;
using Xunit;

namespace BE.LabelExtension.Tests.TranslationServices
{
    /// <summary>TC11 and FA10: translation through DeepL with a simulated service.</summary>
    public sealed class DeepLTranslationServiceTests : IDisposable
    {
        private const string FreeKey = "00000000-0000-0000-0000-000000000000:fx";

        private readonly SimulatedDeepL deepL = new();
        private string? key = FreeKey;
        private readonly DeepLTranslationService service;

        public DeepLTranslationServiceTests()
        {
            this.service = new DeepLTranslationService(() => this.key, this.deepL);
        }

        public void Dispose()
        {
            this.service.Dispose();
            this.deepL.Dispose();
        }

        /// <summary>TC11: one request per target language, with the codes of DeepL.</summary>
        [Fact]
        public async Task TranslateAsync_SwissLanguages_OneRequestPerTargetWithTheCodesOfDeepL()
        {
            TranslationResult result = await this.service.TranslateAsync("Lieferadresse", "de-CH", new[] { "de-CH", "fr-CH" }, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("DE-CH: Lieferadresse", result.Translations["de-CH"]);
            Assert.Equal("FR: Lieferadresse", result.Translations["fr-CH"]);
            Assert.Equal(2, this.deepL.Requests.Count);
            Assert.All(this.deepL.Requests, r => Assert.Equal("https://api-free.deepl.com/v2/translate", r.Uri.ToString()));
            Assert.All(this.deepL.Requests, r => Assert.Equal("DeepL-Auth-Key " + FreeKey, r.Authorization));
            Assert.All(this.deepL.Requests, r => Assert.Contains("\"source_lang\":\"DE\"", r.Body));
            Assert.Contains("\"target_lang\":\"DE-CH\"", this.deepL.Requests[0].Body);
            Assert.Contains("\"target_lang\":\"FR\"", this.deepL.Requests[1].Body);
            Assert.Contains("\"text\":[\"Lieferadresse\"]", this.deepL.Requests[0].Body);
        }

        /// <summary>TC11: an error answer gives the reason instead of suggestions.</summary>
        [Theory]
        [InlineData(456, "the character quota of the DeepL account is used up")]
        [InlineData(429, "DeepL received too many requests; try again shortly")]
        [InlineData(403, "DeepL rejected the API key; check it in the options")]
        [InlineData(500, "DeepL answered with 500 InternalServerError: Internal error")]
        public async Task TranslateAsync_ErrorAnswer_IsAFailureWithReason(int status, string reason)
        {
            this.deepL.Status = (HttpStatusCode)status;

            TranslationResult result = await this.service.TranslateAsync("Lieferadresse", "de", new[] { "en-US", "fr-CH" }, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(reason, result.Error);
            Assert.Empty(result.Translations);
            Assert.DoesNotContain(FreeKey, result.Error);
            Assert.Single(this.deepL.Requests);
        }

        [Fact]
        public async Task TranslateAsync_WithoutKey_FailsWithoutRequest()
        {
            this.key = "  ";

            TranslationResult result = await this.service.TranslateAsync("Lieferadresse", "de", new[] { "en-US" }, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.StartsWith("no API key for DeepL is set in the options", result.Error);
            Assert.Empty(this.deepL.Requests);
        }

        [Fact]
        public async Task TranslateAsync_ServiceNotReachable_FailsWithReason()
        {
            this.deepL.Failure = new HttpRequestException("send failed", new WebException("The remote name could not be resolved: 'api-free.deepl.com'"));

            TranslationResult result = await this.service.TranslateAsync("Lieferadresse", "de", new[] { "en-US" }, CancellationToken.None);

            Assert.Equal("DeepL is not reachable (The remote name could not be resolved: 'api-free.deepl.com')", result.Error);
        }

        [Fact]
        public async Task TranslateAsync_ProKey_GoesToTheProEndpoint()
        {
            this.key = "00000000-0000-0000-0000-000000000000";

            await this.service.TranslateAsync("Lieferadresse", "de", new[] { "en-US" }, CancellationToken.None);

            Assert.Equal("https://api.deepl.com/v2/translate", this.deepL.Requests.Single().Uri.ToString());
        }

        [Fact]
        public async Task TranslateAsync_QuotesInTheText_AreSentAsJson()
        {
            await this.service.TranslateAsync("Kunde \"A\" \\ B", "de", new[] { "en-US" }, CancellationToken.None);

            Assert.Contains("\"text\":[\"Kunde \\\"A\\\" \\\\ B\"]", this.deepL.Requests.Single().Body);
        }

        /// <summary>Concept: de-CH keeps its variant, French and Italian lose it, the source takes only the base code.</summary>
        [Theory]
        [InlineData("de", "DE", "DE")]
        [InlineData("de-CH", "DE-CH", "DE")]
        [InlineData("en-US", "EN-US", "EN")]
        [InlineData("en-GB", "EN-GB", "EN")]
        [InlineData("fr-CH", "FR", "FR")]
        [InlineData("it-CH", "IT", "IT")]
        [InlineData("nl", "NL", "NL")]
        public void Codes_FollowTheConcept(string language, string target, string source)
        {
            Assert.Equal(target, DeepLTranslationService.TargetCode(language));
            Assert.Equal(source, DeepLTranslationService.SourceCode(language));
        }

        // Answers each request with "<target>: <text>", or with an error status.
        private sealed class SimulatedDeepL : HttpMessageHandler
        {
            public List<(Uri Uri, string? Authorization, string Body)> Requests { get; } = new();

            public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

            public Exception? Failure { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (this.Failure != null)
                {
                    throw this.Failure;
                }

                string body = await request.Content.ReadAsStringAsync();
                this.Requests.Add((request.RequestUri, request.Headers.Authorization?.ToString(), body));
                if (this.Status != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(this.Status) { Content = new StringContent("{\"message\":\"Internal error\"}", Encoding.UTF8, "application/json") };
                }

                string target = Regex.Match(body, "\"target_lang\":\"([^\"]+)\"").Groups[1].Value;
                string text = Regex.Match(body, "\"text\":\\[\"([^\"]*)\"\\]").Groups[1].Value;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($"{{\"translations\":[{{\"detected_source_language\":\"DE\",\"text\":\"{target}: {text}\"}}]}}", Encoding.UTF8, "application/json"),
                };
            }
        }
    }
}
