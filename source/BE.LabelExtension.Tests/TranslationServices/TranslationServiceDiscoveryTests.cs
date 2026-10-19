using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.TranslationServices;
using Xunit;

namespace BE.LabelExtension.Tests.TranslationServices
{
    /// <summary>
    /// TC38 and NFA03: a second translation service, here classes of the test assembly, is found
    /// beside DeepL without a change to an existing class.
    /// </summary>
    public sealed class TranslationServiceDiscoveryTests
    {
        private static readonly System.Reflection.Assembly[] Assemblies = { typeof(ITranslationService).Assembly, typeof(TranslationServiceDiscoveryTests).Assembly };

        [Fact]
        public void Create_CoreAndAnotherAssembly_FindsDeepLFirstAndTheFurtherServices()
        {
            IReadOnlyList<ITranslationService> services = TranslationServiceDiscovery.Create(Assemblies, () => null);

            Assert.Equal(new[] { DeepLTranslationService.ServiceName, KeyedTestService.ServiceName, SecondTestService.ServiceName }, services.Select(s => s.Name));
            Assert.IsType<DeepLTranslationService>(services[0]);
        }

        [Fact]
        public void Create_ConstructorWithKey_GetsTheFunctionThatReadsTheKey()
        {
            string? key = null;

            IReadOnlyList<ITranslationService> services = TranslationServiceDiscovery.Create(Assemblies, () => key);

            Assert.False(services.Single(s => s.Name == DeepLTranslationService.ServiceName).IsConfigured);
            Assert.False(services.Single(s => s.Name == KeyedTestService.ServiceName).IsConfigured);
            key = "0000:fx";
            Assert.True(services.Single(s => s.Name == DeepLTranslationService.ServiceName).IsConfigured);
            Assert.True(services.Single(s => s.Name == KeyedTestService.ServiceName).IsConfigured);
            Assert.Equal(3, ((KeyedTestService)services.Single(s => s.Name == KeyedTestService.ServiceName)).Retries);
        }

        [Fact]
        public async Task Create_SecondService_TranslatesWithoutFurtherSetup()
        {
            ITranslationService second = TranslationServiceDiscovery.Create(Assemblies, () => null).Single(s => s.Name == SecondTestService.ServiceName);

            TranslationResult result = await second.TranslateAsync("Lieferadresse", "de", new[] { "en-US" }, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("[en-US] Lieferadresse", result.Translations["en-US"]);
        }

        [Fact]
        public void Create_SameNameAsAServiceFoundBefore_IsLeftOut()
        {
            IReadOnlyList<ITranslationService> services = TranslationServiceDiscovery.Create(Assemblies, () => null);

            Assert.Single(services, s => s.Name == DeepLTranslationService.ServiceName);
            Assert.DoesNotContain(services, s => s is DuplicateDeepLTestService);
        }

        [Fact]
        public void Create_WithoutUsableConstructorOrAbstract_IsLeftOut()
        {
            IReadOnlyList<ITranslationService> services = TranslationServiceDiscovery.Create(Assemblies, () => null);

            Assert.DoesNotContain(services, s => s is NoUsableConstructorTestService);
            Assert.DoesNotContain(services, s => s.Name == "Abstract (test)");
        }

        /// <summary>A second service with a parameterless constructor.</summary>
        internal sealed class SecondTestService : ITranslationService
        {
            public const string ServiceName = "Second (test)";

            public string Name => ServiceName;

            public bool IsConfigured => true;

            public Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, IReadOnlyList<string> targetLanguages, CancellationToken cancellationToken)
                => Task.FromResult(TranslationResult.Succeeded(targetLanguages.ToDictionary(l => l, l => $"[{l}] {text}")));
        }

        /// <summary>A service that reads its key, with a further optional parameter.</summary>
        internal sealed class KeyedTestService : ITranslationService
        {
            public const string ServiceName = "Keyed (test)";

            private readonly Func<string?> apiKey;

            public KeyedTestService(Func<string?> apiKey, int retries = 3)
            {
                this.apiKey = apiKey;
                this.Retries = retries;
            }

            public int Retries { get; }

            public string Name => ServiceName;

            public bool IsConfigured => !string.IsNullOrEmpty(this.apiKey());

            public Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, IReadOnlyList<string> targetLanguages, CancellationToken cancellationToken)
                => Task.FromResult(TranslationResult.Failed("not needed"));
        }

        /// <summary>A service named like DeepL; the one of the core counts.</summary>
        internal sealed class DuplicateDeepLTestService : ITranslationService
        {
            public string Name => DeepLTranslationService.ServiceName;

            public bool IsConfigured => true;

            public Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, IReadOnlyList<string> targetLanguages, CancellationToken cancellationToken)
                => Task.FromResult(TranslationResult.Failed("not needed"));
        }

        /// <summary>A service whose constructor needs something the discovery cannot give.</summary>
        internal sealed class NoUsableConstructorTestService : ITranslationService
        {
            public NoUsableConstructorTestService(string endpoint)
            {
                this.Name = endpoint;
            }

            public string Name { get; }

            public bool IsConfigured => true;

            public Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, IReadOnlyList<string> targetLanguages, CancellationToken cancellationToken)
                => Task.FromResult(TranslationResult.Failed("not needed"));
        }

        /// <summary>An abstract service is no service.</summary>
        internal abstract class AbstractTestService : ITranslationService
        {
            public string Name => "Abstract (test)";

            public bool IsConfigured => true;

            public abstract Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, IReadOnlyList<string> targetLanguages, CancellationToken cancellationToken);
        }
    }
}
