using System.IO;
using System.Linq;
using System.Threading;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Models
{
    public sealed class ModelDiscoveryTests : System.IDisposable
    {
        private readonly TestPackages packages = new();
        private readonly RecordingSink messages = new();

        public void Dispose() => this.packages.Dispose();

        /// <summary>TC14, part 1: the folders of a metadata configuration are found.</summary>
        [Fact]
        public void ReadConfiguration_FindsOwnAndReferenceFolders()
        {
            string store = Path.Combine(this.packages.Root, "Metadata");
            string framework = Path.Combine(this.packages.Root, "10.0.2527.197", "PackagesLocalDirectory");
            string reference = Path.Combine(this.packages.Root, "Reference");
            this.packages.WriteConfiguration("Demo-10.0.2527.197", store, framework, framework, reference);
            var discovery = new ModelDiscovery(this.packages.ConfigurationFolder);

            Assert.Equal(new[] { "Demo-10.0.2527.197" }, discovery.ListConfigurations());

            MetadataConfiguration configuration = discovery.ReadConfiguration("Demo-10.0.2527.197");
            Assert.Equal(store, configuration.ModelStoreFolder);
            Assert.Equal(Path.Combine(store, "XppSource"), configuration.DebugSourceFolder);
            Assert.Equal(framework, configuration.FrameworkDirectory);
            Assert.Equal(new[] { framework, reference }, configuration.ReferencePackagesPaths);

            var directories = configuration.GetPackageDirectories();
            Assert.Equal(new[] { store, framework, reference }, directories.Select(d => d.Path));
            Assert.Equal(new[] { false, true, true }, directories.Select(d => d.IsReference));
        }

        /// <summary>
        /// The Developer Tools do not write the fields in a fixed order, and many fields hold
        /// null, numbers or booleans. Seen on the test environment.
        /// </summary>
        [Fact]
        public void ReadConfiguration_OtherFieldOrderAndFurtherFields_IsRead()
        {
            string store = Path.Combine(this.packages.Root, "Metadata").Replace("\\", "\\\\");
            string framework = Path.Combine(this.packages.Root, "10.0.2527.197", "PackagesLocalDirectory").Replace("\\", "\\\\");
            string json = "{\r\n"
                + "  \"CrossReferencesDatabaseName\": \"XRef\",\r\n"
                + $"  \"ModelStoreFolder\": \"{store}\",\r\n"
                + "  \"ModuleExclusionList\": null,\r\n"
                + $"  \"DebugSourceFolder\": \"{store}\",\r\n"
                + $"  \"FrameworkDirectory\": \"{framework}\",\r\n"
                + "  \"AzureCR_Key\": null,\r\n"
                + "  \"EnableOfflineAuthentication\": true,\r\n"
                + "  \"RuntimeHostType\": 4,\r\n"
                + $"  \"ReferencePackagesPaths\": [ \"{framework}\" ],\r\n"
                + "  \"Description\": \"Demo\"\r\n"
                + "}\r\n";
            Directory.CreateDirectory(this.packages.ConfigurationFolder);
            File.WriteAllText(Path.Combine(this.packages.ConfigurationFolder, "Demo.json"), json, new System.Text.UTF8Encoding(true));

            MetadataConfiguration configuration = new ModelDiscovery(this.packages.ConfigurationFolder).ReadConfiguration("Demo");

            Assert.Equal(Path.Combine(this.packages.Root, "Metadata"), configuration.ModelStoreFolder);
            Assert.Equal(Path.Combine(this.packages.Root, "Metadata"), configuration.DebugSourceFolder);
            Assert.Equal(new[] { false, true }, configuration.GetPackageDirectories().Select(d => d.IsReference));
        }

        /// <summary>TC14, part 2: without a configuration the PackagesLocalDirectory of a classic VM is suggested, K: first.</summary>
        [Fact]
        public void FindClassicDirectory_SuggestsKFirstThenTheOtherDrivesInOrder()
        {
            string[] drives = { @"J:\", @"C:\", @"K:\" };

            Assert.Equal(@"K:\AOSService\PackagesLocalDirectory", ModelDiscovery.FindClassicDirectory(drives, _ => true));
            Assert.Equal(@"C:\AOSService\PackagesLocalDirectory", ModelDiscovery.FindClassicDirectory(drives, p => !p.StartsWith("K:")));
            Assert.Equal(@"J:\AOSService\PackagesLocalDirectory", ModelDiscovery.FindClassicDirectory(drives, p => p.StartsWith("J:")));
            Assert.Null(ModelDiscovery.FindClassicDirectory(drives, _ => false));
            Assert.Empty(new ModelDiscovery(Path.Combine(this.packages.Root, "missing")).ListConfigurations());
        }

        /// <summary>
        /// D1 on the classic VM: C:\AOSService\PackagesLocalDirectory existed but was almost
        /// empty, the packages lay on J:. Only a directory with packages counts.
        /// </summary>
        [Fact]
        public void FindClassicDirectory_EmptyDirectory_IsSkipped()
        {
            string empty = Path.Combine(this.packages.Root, "C");
            Directory.CreateDirectory(Path.Combine(empty, ModelDiscovery.ClassicFolder, "bin"));
            string real = Path.Combine(this.packages.Root, "J");
            string package = Path.Combine(real, ModelDiscovery.ClassicFolder, "BEDemo1");
            Directory.CreateDirectory(Path.Combine(package, "Descriptor"));
            File.Copy(Path.Combine(this.packages.PackagesDirectory, "BEDemo1", "Descriptor", "BEDemo1.xml"), Path.Combine(package, "Descriptor", "BEDemo1.xml"));

            Assert.False(ModelDiscovery.HasPackages(Path.Combine(empty, ModelDiscovery.ClassicFolder)));
            Assert.Equal(Path.Combine(real, ModelDiscovery.ClassicFolder), ModelDiscovery.FindClassicDirectory(new[] { empty, real }));
            Assert.True(ModelDiscovery.HasPackages(this.packages.PackagesDirectory));
        }

        [Fact]
        public void FindModels_SyntheticDirectory_FindsModelsAndTheirAccess()
        {
            var models = this.Find(new PackageDirectory(this.packages.PackagesDirectory, isReference: false));

            Assert.Equal(new[] { "BEDemo1", "BEDemo2", "DemoBase", "DemoLocked" }, models.Select(m => m.Name).OrderBy(n => n));
            Assert.False(models.Single(m => m.Name == "BEDemo1").IsReadOnly);
            Assert.False(models.Single(m => m.Name == "BEDemo2").IsReadOnly);
            Assert.True(models.Single(m => m.Name == "DemoBase").IsReadOnly);
            Assert.True(models.Single(m => m.Name == "DemoLocked").IsReadOnly);
            Assert.Equal(ModelLayer.CUS, models.Single(m => m.Name == "BEDemo2").Layer);
            Assert.Equal(this.packages.ModelDirectory("BEDemo1", "BEDemo1"), models.Single(m => m.Name == "BEDemo1").Directory);
            Assert.Empty(this.messages.Messages);
        }

        [Fact]
        public void FindModels_ReferenceDirectory_MakesEveryModelReadOnly()
        {
            var models = this.Find(new PackageDirectory(this.packages.PackagesDirectory, isReference: true));

            Assert.All(models, m => Assert.True(m.IsReadOnly));
        }

        [Fact]
        public void FindModels_MissingDirectory_IsReportedAndTheOthersAreSearched()
        {
            var models = this.Find(
                new PackageDirectory(Path.Combine(this.packages.Root, "missing"), isReference: false),
                new PackageDirectory(this.packages.PackagesDirectory, isReference: false));

            Assert.Equal(4, models.Count);
            Assert.Contains(this.messages.Messages, m => m.Severity == MessageSeverity.Warning && m.Message.Contains("missing"));
        }

        [Fact]
        public void FindModels_DescriptorWithoutLayer_IsNoModel()
        {
            this.ReplaceInDescriptor("BEDemo1", "<Layer>10</Layer>", string.Empty);

            Assert.DoesNotContain(this.Find(new PackageDirectory(this.packages.PackagesDirectory, false)), m => m.Name == "BEDemo1");
        }

        [Fact]
        public void FindModels_UnreadableLocked_CountsAsLocked()
        {
            this.ReplaceInDescriptor("BEDemo1", "<Locked>false</Locked>", "<Locked>maybe</Locked>");

            ModelInfo model = this.Find(new PackageDirectory(this.packages.PackagesDirectory, false)).Single(m => m.Name == "BEDemo1");

            Assert.True(model.IsLocked);
            Assert.True(model.IsReadOnly);
            Assert.Contains(this.messages.Messages, m => m.Message.Contains("Locked"));
        }

        [Fact]
        public void FindModels_MissingLocked_IsNotLocked()
        {
            ModelInfo model = this.Find(new PackageDirectory(this.packages.PackagesDirectory, false)).Single(m => m.Name == "BEDemo2");

            Assert.False(model.IsLocked);
        }

        /// <summary>TC13: the model of an XML file in a package directory and of a .xpp file in XppSource, on both kinds of environment.</summary>
        [Fact]
        public void FindModelFor_ElementFileAndXppSourceFile_FindTheModel()
        {
            var models = this.Find(new PackageDirectory(this.packages.PackagesDirectory, false));
            string xml = Path.Combine(this.packages.ModelDirectory("BEDemo2", "BEDemo2"), "AxTable", "BDMDelivery.xml");
            string xpp = Path.Combine(this.packages.Root, "Metadata", "XppSource", "BEDemo1", "AxClass_BDMDeliveryHelper.xpp");

            // On the classic development VM the X++ editor works below the PackagesLocalDirectory (D1).
            string classicXpp = Path.Combine(@"J:\AosService\PackagesLocalDirectory", "bin", "XppSource", "BEDemo2", "AxTable_BDMDelivery.xpp");

            Assert.Equal("BEDemo2", ModelDiscovery.FindModelFor(xml, models)?.Name);
            Assert.Equal("BEDemo1", ModelDiscovery.FindModelFor(xpp, models)?.Name);
            Assert.Equal("BEDemo2", ModelDiscovery.FindModelFor(classicXpp, models)?.Name);
            Assert.Null(ModelDiscovery.FindModelFor(Path.Combine(this.packages.Root, "elsewhere", "file.xml"), models));
        }

        private System.Collections.Generic.IReadOnlyList<ModelInfo> Find(params PackageDirectory[] directories)
            => new ModelDiscovery(this.packages.ConfigurationFolder).FindModels(directories, this.messages, CancellationToken.None);

        private void ReplaceInDescriptor(string model, string oldText, string newText)
        {
            string path = Path.Combine(this.packages.PackagesDirectory, model, "Descriptor", model + ".xml");
            File.WriteAllText(path, File.ReadAllText(path).Replace(oldText, newText));
        }
    }
}
