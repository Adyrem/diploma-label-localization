using BE.LabelExtension.Core.Store;
using Xunit;

namespace BE.LabelExtension.Tests.Store
{
    /// <summary>RE52: language codes of the settings are checked against the cultures Windows predefines.</summary>
    public class LanguageCodesTests
    {
        [Theory]
        [InlineData("de")]
        [InlineData("de-CH")]
        [InlineData("en-US")]
        [InlineData("fr-CH")]
        [InlineData("it-CH")]
        [InlineData("zh-Hans")]
        [InlineData("en-us")]
        [InlineData(" de-CH ")]
        public void IsPredefined_CultureOfWindows_IsTrue(string code)
        {
            Assert.True(LanguageCodes.IsPredefined(code));
        }

        /// <summary>Typos and well-formed codes Windows only accepts as unknown languages are refused.</summary>
        [Theory]
        [InlineData("de_CH")]
        [InlineData("de-XX")]
        [InlineData("abc")]
        [InlineData("deutsch")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void IsPredefined_TypoOrUnknownCode_IsFalse(string? code)
        {
            Assert.False(LanguageCodes.IsPredefined(code));
        }

        [Fact]
        public void Unknown_ListOfCodes_GivesTheUnknownInTheirOrder()
        {
            Assert.Equal(new[] { "de_CH", "abc" }, LanguageCodes.Unknown(new[] { "en-US", "de_CH", "de", "abc", "fr-CH" }));
            Assert.Empty(LanguageCodes.Unknown(new[] { "en-US", "de", "de-CH", "fr-CH", "it-CH" }));
        }
    }
}
