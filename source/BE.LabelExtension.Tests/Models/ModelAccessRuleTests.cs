using BE.LabelExtension.Core.Models;
using Xunit;

namespace BE.LabelExtension.Tests.Models
{
    /// <summary>The rule for read-only models, per layer (F1).</summary>
    public class ModelAccessRuleTests
    {
        [Theory]
        [InlineData(ModelLayer.SYS, true)]
        [InlineData(ModelLayer.SYP, true)]
        [InlineData(ModelLayer.GLS, true)]
        [InlineData(ModelLayer.GLP, true)]
        [InlineData(ModelLayer.FPK, true)]
        [InlineData(ModelLayer.FPP, true)]
        [InlineData(ModelLayer.SLN, true)]
        [InlineData(ModelLayer.SLP, true)]
        [InlineData(ModelLayer.ISV, true)]
        [InlineData(ModelLayer.ISP, true)]
        [InlineData(ModelLayer.VAR, false)]
        [InlineData(ModelLayer.VAP, false)]
        [InlineData(ModelLayer.CUS, false)]
        [InlineData(ModelLayer.CUP, false)]
        [InlineData(ModelLayer.USR, true)]
        [InlineData(ModelLayer.USP, true)]
        public void IsReadOnly_UnlockedModelOutsideReferences_DependsOnLayer(ModelLayer layer, bool expected)
        {
            Assert.Equal(expected, ModelAccessRule.IsReadOnly(layer, isLocked: false, isInReferenceDirectory: false));
        }

        [Theory]
        [InlineData(ModelLayer.VAR)]
        [InlineData(ModelLayer.CUP)]
        public void IsReadOnly_LockedModel_IsReadOnlyInEveryLayer(ModelLayer layer)
        {
            Assert.True(ModelAccessRule.IsReadOnly(layer, isLocked: true, isInReferenceDirectory: false));
        }

        [Theory]
        [InlineData(ModelLayer.VAR)]
        [InlineData(ModelLayer.CUS)]
        public void IsReadOnly_ModelInReferenceDirectory_IsReadOnlyInEveryLayer(ModelLayer layer)
        {
            Assert.True(ModelAccessRule.IsReadOnly(layer, isLocked: false, isInReferenceDirectory: true));
        }

        [Fact]
        public void Layers_HaveTheNumbersOfTheDescriptor()
        {
            Assert.Equal(0, (int)ModelLayer.SYS);
            Assert.Equal(10, (int)ModelLayer.VAR);
            Assert.Equal(13, (int)ModelLayer.CUP);
            Assert.Equal(15, (int)ModelLayer.USP);
        }
    }
}
