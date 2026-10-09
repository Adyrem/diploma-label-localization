using System;
using System.IO;
using System.Text;
using BE.LabelExtension.Core.Files;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Sources;
using Xunit;

namespace BE.LabelExtension.Tests.Files
{
    /// <summary>
    /// D1: label files of the platform have paths longer than 260 characters, and Visual Studio
    /// is not aware of long paths. The test host is not either, so these tests fail without
    /// the prefix.
    /// </summary>
    public sealed class LongPathTests : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "BE.LabelExtension.Tests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(this.root))
            {
                Directory.Delete(@"\\?\" + this.root, recursive: true);
            }
        }

        [Theory]
        [InlineData(@"C:\short\path.txt")]
        [InlineData(@"\\?\C:\already\prefixed.txt")]
        [InlineData(@"relative\path.txt")]
        public void ForAccess_ShortPrefixedOrRelativePath_StaysAsItIs(string path)
        {
            Assert.Equal(path, LongPath.ForAccess(path));
        }

        [Fact]
        public void ForAccess_LongPath_GetsThePrefixAndLosesItAgain()
        {
            string local = @"C:\" + new string('a', 300) + @"\file.label.txt";
            string unc = @"\\server\share\" + new string('b', 300) + @"\file.label.txt";

            Assert.Equal(@"\\?\" + local, LongPath.ForAccess(local));
            Assert.Equal(@"\\?\UNC\server\share\" + new string('b', 300) + @"\file.label.txt", LongPath.ForAccess(unc));
            Assert.Equal(local, LongPath.WithoutPrefix(LongPath.ForAccess(local)));
            Assert.Equal(unc, LongPath.WithoutPrefix(LongPath.ForAccess(unc)));
        }

        [Fact]
        public void ReadAndWriteFile_PathLongerThan260Characters_Works()
        {
            string path = this.LongFile("de", "BDM1.de.label.txt");

            LabelFileFormat.WriteFile(path, new[] { new LabelEntry("BDM110000001", "Kunde", null) });

            Assert.True(path.Length > 260);
            Assert.Equal("Kunde", LabelFileFormat.Read(path).Entries[0].Text);
            Assert.Equal(new[] { path }, LongPath.GetFiles(Path.GetDirectoryName(path)!, "*.label.txt"));
        }

        [Fact]
        public void ReadFirstResource_PathLongerThan260Characters_Works()
        {
            using var packages = new TestPackages();
            string compiled = packages.WriteCompiledLabels(packages.Root, "DMC", "de", new System.Collections.Generic.Dictionary<string, string> { ["DMC1"] = "Kompiliert" });
            string path = this.LongFile("de", "DMC.Resources.dll");
            File.Copy(compiled, LongPath.ForAccess(path));

            Assert.Contains(ResourceAssemblyReader.ReadFirstResource(path), e => e.Key == "DMC1" && e.Text == "Kompiliert");
        }

        // A file in a folder below 248 characters whose own path is longer than 260, as on the
        // test environment.
        private string LongFile(string language, string fileName)
        {
            string folder = Path.Combine(this.root, new string('m', 220 - this.root.Length), language);
            Directory.CreateDirectory(LongPath.ForAccess(folder));
            return Path.Combine(folder, new string('x', 60) + "_" + fileName);
        }
    }
}
