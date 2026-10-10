using System.Collections.Generic;
using System.Text.RegularExpressions;
using BE.LabelExtension.Core.Labels;
using Xunit;

namespace BE.LabelExtension.Tests.Labels
{
    public class LabelKeyGeneratorTests
    {
        /// <summary>TC03: 10 000 new label parts, each L and 16 hexadecimal digits in upper case, none twice.</summary>
        [Fact]
        public void NewKey_TenThousand_AreWellFormedAndUnique()
        {
            var format = new Regex("^L[0-9A-F]{16}$");
            var keys = new HashSet<string>();

            for (int i = 0; i < 10_000; i++)
            {
                string key = LabelKeyGenerator.NewKey();
                Assert.Matches(format, key);
                Assert.True(keys.Add(key), $"{key} came twice.");
            }
        }

        [Fact]
        public void NewKey_TakenKey_IsNotReturned()
        {
            var taken = new HashSet<string>();
            string first = LabelKeyGenerator.NewKey();
            taken.Add(first);
            int asked = 0;

            string key = LabelKeyGenerator.NewKey(candidate => asked++ == 0 || taken.Contains(candidate));

            Assert.NotEqual(first, key);
            Assert.True(asked >= 2);
            Assert.True(LabelId.TryCreate("BDM1", key, out _));
        }
    }
}
