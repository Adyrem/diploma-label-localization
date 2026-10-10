using System;
using System.IO;
using System.Linq;
using System.Threading;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Usages;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Usages
{
    /// <summary>TC05 in the core logic: finding the uses of a label ID and replacing them.</summary>
    public sealed class LabelReferencesTests : IDisposable
    {
        private readonly TestPackages packages = new();
        private readonly RecordingSink messages = new();
        private readonly System.Collections.Generic.IReadOnlyList<ModelInfo> models;

        public LabelReferencesTests()
        {
            this.models = new ModelDiscovery(this.packages.ConfigurationFolder)
                .FindModels(new[] { new PackageDirectory(this.packages.PackagesDirectory, isReference: false) }, this.messages, CancellationToken.None);
        }

        private string ClassFile => Path.Combine(this.packages.ModelDirectory("BEDemo1", "BEDemo1"), "AxClass", "BDMDeliveryHelper.xml");

        private string TableFile => Path.Combine(this.packages.ModelDirectory("BEDemo2", "BEDemo2"), "AxTable", "BDMDelivery.xml");

        private string ReportFile => Path.Combine(this.packages.ModelDirectory("BEDemo2", "BEDemo2"), "AxReport", "BDMDeliveryNote.xml");

        private string ReadOnlyFile => Path.Combine(this.packages.ModelDirectory("DemoBase", "DemoBase"), "AxClass", "DMODeliveryHelper.xml");

        [Fact]
        public void Find_AllModels_FindsEveryUseWithModelLineAndColumn()
        {
            var usages = LabelReferences.Find(this.models, "@BDM1:BDM110000003", CancellationToken.None).ToList();

            Assert.Equal(7, usages.Count);
            Assert.Equal(2, usages.Count(u => u.Path == this.ClassFile));
            Assert.Equal(2, usages.Count(u => u.Path == this.TableFile));
            Assert.Equal(2, usages.Count(u => u.Path == this.ReportFile));
            Assert.Equal(1, usages.Count(u => u.Path == this.ReadOnlyFile));
            Assert.DoesNotContain(usages, u => u.Path.IndexOf("XppMetadata", StringComparison.OrdinalIgnoreCase) >= 0);

            LabelUsage helpText = usages.Single(u => u.Path == this.TableFile && u.LineText.StartsWith("<HelpText>"));
            Assert.Equal("BEDemo2", helpText.Model.Name);
            Assert.Equal(17, helpText.Line);
            Assert.Equal(14, helpText.Column);
            Assert.Equal("<HelpText>@BDM1:BDM110000003</HelpText>", helpText.LineText);
        }

        /// <summary>TC05: the search for @SYS12345 does not report @SYS123456, and the other way round.</summary>
        [Fact]
        public void Find_OldForm_MatchesOnlyTheCompleteId()
        {
            Assert.Equal(2, LabelReferences.Find(this.models, "@SYS12345", CancellationToken.None).Count());
            Assert.Single(LabelReferences.Find(this.models, "@SYS123456", CancellationToken.None));
            Assert.Empty(LabelReferences.Find(this.models, "@SYS1234", CancellationToken.None));
        }

        /// <summary>TC05: replaced only in writable models, and the number of changed files is right.</summary>
        [Fact]
        public void Replace_WritableModels_ChangesOnlyTheirFilesAndKeepsTheEncoding()
        {
            byte[] readOnlyBefore = File.ReadAllBytes(this.ReadOnlyFile);
            var writable = this.models.Where(m => !m.IsReadOnly).ToList();

            var files = LabelReferences.FindFiles(writable, "@BDM1:BDM110000003", CancellationToken.None);
            var changed = LabelReferences.Replace(files, "@BDM1:BDM110000003", "@BDM2:L0123456789ABCDEF", this.messages);

            Assert.Equal(3, files.Count);
            Assert.Equal(3, changed.Count);
            Assert.Empty(LabelReferences.Find(writable, "@BDM1:BDM110000003", CancellationToken.None));
            Assert.Equal(6, LabelReferences.Find(writable, "@BDM2:L0123456789ABCDEF", CancellationToken.None).Count());
            Assert.Equal(readOnlyBefore, File.ReadAllBytes(this.ReadOnlyFile));

            // As in the existing tool, single quotes become double quotes.
            string code = File.ReadAllText(this.ClassFile);
            Assert.Contains("return \"@BDM2:L0123456789ABCDEF\" + literalStr(@SYS123456);", code);
            Assert.Contains("=Labels!@BDM2:L0123456789ABCDEF & \": \"", File.ReadAllText(this.ReportFile));

            Assert.Equal(0xEF, File.ReadAllBytes(this.TableFile)[0]);
            Assert.NotEqual(0xEF, File.ReadAllBytes(this.ClassFile)[0]);
            Assert.Contains("\r\n", code);
        }

        [Theory]
        [InlineData("x>@A:B<x", true)]
        [InlineData("x\"@A:B\"x", true)]
        [InlineData("x'@A:B'x", true)]
        [InlineData("x(@A:B)x", true)]
        [InlineData("x>@A:BC<x", false)]
        [InlineData("x@A:B x", false)]
        [InlineData("x!@A:B x", false)]
        [InlineData("x\"@A:B'x", false)]
        public void Positions_OnlyTheFourPatterns_CountOutsideReports(string text, bool isUse)
        {
            Assert.Equal(isUse ? 1 : 0, LabelReferences.Positions(text, "@A:B", isReport: false).Count());
        }

        [Theory]
        [InlineData("!@A:B&")]
        [InlineData("!@A:B,")]
        [InlineData("!@A:B ")]
        [InlineData("!@A:B+")]
        [InlineData("!@A:B.")]
        [InlineData("!@A:B)")]
        [InlineData("!@A:B<")]
        public void Positions_ReportExpression_CountsInReports(string text)
        {
            Assert.Single(LabelReferences.Positions(text, "@A:B", isReport: true));
            Assert.Empty(LabelReferences.Positions(text, "@A:B", isReport: false));
        }

        /// <summary>
        /// TC29 in the core logic: uses in code are numbered in the order of the XML file, uses in
        /// properties and in the design of a report are not code.
        /// </summary>
        [Fact]
        public void Find_CodeAndProperties_NumbersTheUsesInCode()
        {
            var usages = LabelReferences.Find(this.models, "@BDM1:BDM110000003", CancellationToken.None).ToList();

            Assert.Equal(new[] { 1, 2 }, usages.Where(u => u.Path == this.ClassFile).Select(u => u.CodeOccurrence));
            Assert.All(usages.Where(u => u.Path == this.TableFile || u.Path == this.ReportFile), u => Assert.False(u.IsInCode));
        }

        /// <summary>The same uses in the code the X++ editor shows, so the n-th use leads to the same place.</summary>
        [Fact]
        public void FindInCode_CodeOfTheEditor_FindsTheSameUses()
        {
            const string Code = "public static str caption()\r\n{\r\n    return strFmt(\"%1 %2\", \"@BDM1:BDM110000003\", \"@SYS12345\");\r\n}\r\n"
                + "public static str legacyCaption()\r\n{\r\n    return '@BDM1:BDM110000003' + literalStr(@SYS123456);\r\n}\r\n";

            var positions = LabelReferences.FindInCode(Code, "@BDM1:BDM110000003");

            Assert.Equal(2, positions.Count);
            Assert.Equal(Code.IndexOf("@BDM1"), positions[0]);
            Assert.Single(LabelReferences.FindInCode(Code, "@SYS123456"));
        }

        [Fact]
        public void CodeRanges_OnlySourceAndDeclaration()
        {
            const string Xml = "<Declaration><![CDATA[a]]></Declaration><Text><![CDATA[b]]></Text><Source>\n<![CDATA[c]]></Source>";

            var ranges = LabelReferences.CodeRanges(Xml);

            Assert.Equal(new[] { "a", "c" }, ranges.Select(r => Xml.Substring(r.Start, r.End - r.Start)));
        }

        public void Dispose() => this.packages.Dispose();
    }
}
