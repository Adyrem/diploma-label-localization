using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Core.TranslationServices;
using BE.LabelExtension.Settings;

namespace BE.LabelExtension.TranslationServices
{
    /// <summary>
    /// The translation services the settings can choose (FA10, FA11). They are found in the
    /// core and in the extension, so a further service is a class of its own in one of the two
    /// projects, and no existing class changes (NFA03, TC38). See
    /// <see cref="TranslationServiceDiscovery"/> for what counts as a service.
    /// </summary>
    /// <remarks>
    /// The API key is read from the settings file and decrypted for every translation, so a key
    /// entered in the options counts at once, and the decrypted key stays in no field.
    /// </remarks>
    internal sealed class TranslationServiceCatalog : IDisposable
    {
        private static readonly Assembly[] Assemblies = { typeof(ITranslationService).Assembly, typeof(TranslationServiceCatalog).Assembly };

        private static readonly Lazy<IReadOnlyList<string>> ServiceNames = new(ReadNames);

        private readonly IReadOnlyList<ITranslationService> services;

        /// <summary>Creates the services.</summary>
        public TranslationServiceCatalog()
        {
            this.services = TranslationServiceDiscovery.Create(Assemblies, ReadApiKey);
        }

        /// <summary>The names of the services, for the options page.</summary>
        public static IReadOnlyList<string> Names => ServiceNames.Value;

        /// <summary>The service with the given name.</summary>
        /// <param name="name">The name from the settings.</param>
        /// <returns>The service, or <c>null</c> if there is none with this name.</returns>
        public ITranslationService? Find(string? name)
            => this.services.FirstOrDefault(s => string.Equals(s.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <inheritdoc />
        public void Dispose()
        {
            Release(this.services);
        }

        // The names are properties of the services; the instances made for them are released at once.
        private static IReadOnlyList<string> ReadNames()
        {
            IReadOnlyList<ITranslationService> found = TranslationServiceDiscovery.Create(Assemblies, () => null);
            List<string> names = found.Select(s => s.Name).ToList();
            Release(found);
            return names;
        }

        private static void Release(IEnumerable<ITranslationService> services)
        {
            foreach (IDisposable service in services.OfType<IDisposable>())
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
