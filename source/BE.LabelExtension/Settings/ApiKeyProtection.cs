using System;
using System.Security.Cryptography;
using System.Text;

namespace BE.LabelExtension.Settings
{
    /// <summary>
    /// Encrypts the API key of the translation service with the Data Protection API of Windows
    /// for the current user (NFA06). Only the encrypted form goes into the settings file; the
    /// key itself never appears on disk, in the Output Window or in the repository.
    /// </summary>
    internal static class ApiKeyProtection
    {
        // Ties the encrypted key to this extension, so another program of the same user cannot
        // simply decrypt it with an empty entropy.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("BE.LabelExtension.ApiKey");

        /// <summary>Encrypts a key.</summary>
        /// <param name="key">The key, empty for none.</param>
        /// <returns>The encrypted key as Base64, or <c>null</c> for none.</returns>
        public static string? Protect(string? key)
            => string.IsNullOrEmpty(key)
                ? null
                : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(key), Entropy, DataProtectionScope.CurrentUser));

        /// <summary>Decrypts a key.</summary>
        /// <param name="protectedKey">The encrypted key from <see cref="Protect"/>.</param>
        /// <returns>The key, or <c>null</c> if there is none or it cannot be decrypted, for example for another user.</returns>
        public static string? Unprotect(string? protectedKey)
        {
            if (string.IsNullOrEmpty(protectedKey))
            {
                return null;
            }

            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protectedKey), Entropy, DataProtectionScope.CurrentUser));
            }
            catch (Exception exception) when (exception is CryptographicException || exception is FormatException)
            {
                return null;
            }
        }
    }
}
