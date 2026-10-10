using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BE.LabelExtension.Core.Store
{
    /// <summary>
    /// Checks language codes from the settings against the cultures Windows predefines, as the
    /// structure script does (RE52). The languages are free text on the options page, so a typo
    /// such as <c>de_CH</c> for <c>de-CH</c> would otherwise go unnoticed.
    /// </summary>
    /// <remarks>
    /// <see cref="CultureInfo.GetCultureInfo(string)"/> alone does not suffice: Windows accepts
    /// every well-formed code such as <c>abc</c> and reports it as an unknown language.
    /// </remarks>
    public static class LanguageCodes
    {
        private static readonly Lazy<HashSet<string>> Predefined = new(() => new HashSet<string>(
            CultureInfo.GetCultures(CultureTypes.AllCultures).Select(c => c.Name).Where(n => n.Length > 0),
            StringComparer.OrdinalIgnoreCase));

        /// <summary>Whether a code names a culture Windows predefines, such as <c>de</c> or <c>de-CH</c>.</summary>
        /// <param name="code">The code.</param>
        /// <returns>Whether it is known; upper and lower case do not matter.</returns>
        public static bool IsPredefined(string? code)
            => !string.IsNullOrWhiteSpace(code) && Predefined.Value.Contains(code!.Trim());

        /// <summary>The codes of a list that Windows does not predefine, in their order.</summary>
        /// <param name="codes">The codes.</param>
        /// <returns>The unknown codes.</returns>
        public static IReadOnlyList<string> Unknown(IEnumerable<string?> codes)
            => codes.Where(c => !IsPredefined(c)).Select(c => c ?? string.Empty).ToList();
    }
}
