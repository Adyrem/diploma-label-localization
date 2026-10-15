using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Core.TranslationServices;
using BE.LabelExtension.Settings;

namespace BE.LabelExtension.TranslationServices
{
    /// <summary>
    /// The translation services the settings can choose (FA10, FA11). A further service is a
    /// class of its own and one entry in <see cref="Entries"/> (NFA03).
    /// </summary>
    /// <remarks>
    /// The API key is read from the settings file and decrypted for every translation, so a key
    /// entered in the options counts at once, and the decrypted key stays in no field.
    /// </remarks>
    internal sealed class TranslationServiceCatalog : IDisposable
    {
        // Name and creation of each service. Each gets a function that reads its key.
        private static readonly (string Name, Func<Func<string?>, ITranslationService> Create)[] Entries =
        {
            (DeepLTranslationService.ServiceName, apiKey => new DeepLTranslationService(apiKey)),
        };

        private readonly IReadOnlyList<ITranslationService> services;

        /// <summary>Creates the services.</summary>
        public TranslationServiceCatalog()
        {
            this.services = Entries.Select(e => e.Create(ReadApiKey)).ToList();
        }

        /// <summary>The names of the services, for the options page.</summary>
        public static IReadOnlyList<string> Names { get; } = Entries.Select(e => e.Name).ToList();

        /// <summary>The service with the given name.</summary>
        /// <param name="name">The name from the settings.</param>
        /// <returns>The service, or <c>null</c> if there is none with this name.</returns>
        public ITranslationService? Find(string? name)
            => this.services.FirstOrDefault(s => string.Equals(s.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (IDisposable service in this.services.OfType<IDisposable>())
            {
                service.Dispose();
            }
        }

        private static string? ReadApiKey()
        {
            try
            {
                return ApiKeyProtection.Unprotect(LabelSettingsFile.Load(LabelSettingsFile.DefaultPath)?.ProtectedApiKey);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is SerializationException)
            {
                return null;
            }
        }
    }
}
