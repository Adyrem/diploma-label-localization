using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BE.LabelExtension.Core.Elements;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Elements
{
    /// <summary>B25: open documents are traced back to the XML file of their element.</summary>
    public sealed class ElementDocumentsTests : IDisposable
    {
        private readonly TestPackages packages = new();
        private readonly IReadOnlyList<ModelInfo> writable;

        public ElementDocumentsTests()
        {
            this.writable = new ModelDiscovery(this.packages.ConfigurationFolder)
                .FindModels(new[] { new PackageDirectory(this.packages.PackagesDirectory, isReference: false) }, new RecordingSink(), CancellationToken.None)
                .Where(m => !m.IsReadOnly)
                .ToList();
        }

        public void Dispose() => this.packages.Dispose();

        /// <summary>FA04: the X++ editor shows an element from XppSource\Model\AxType_Name.xpp.</summary>
        [Fact]
        public void XppFile_ElementOfTheModel_IsInXppSource()
        {
            ModelInfo model = this.writable.Single(m => m.Name == "BEDemo1");
            string xml = Path.Combine(model.Directory, "AxClass", "BDMDeliveryHelper.xml");

            Assert.Equal(@"C:\XppSource\BEDemo1\AxClass_BDMDeliveryHelper.xpp", ElementDocuments.XppFile(xml, model, @"C:\XppSource"));
            Assert.Null(ElementDocuments.XppFile(Path.Combine(model.Directory, "readme.xml"), model, @"C:\XppSource"));
            Assert.Null(ElementDocuments.XppFile(Path.Combine(model.Directory, "AxClass", "Sub", "X.xml"), model, @"C:\XppSource"));
        }

        /// <summary>XppSource comes from the metadata configuration, on a classic VM it lies in bin beside the packages.</summary>
        [Fact]
        public void XppSourceFolder_ConfigurationOrClassicVm()
        {
            ModelInfo model = this.writable.Single(m => m.Name == "BEDemo1");

            Assert.Equal(@"C:\Store\XppSource", ElementDocuments.XppSourceFolder(model, @"C:\Store\XppSource"));
            Assert.Equal(Path.Combine(this.packages.PackagesDirectory, "bin", "XppSource"), ElementDocuments.XppSourceFolder(model, null));
        }

        /// <summary>The designer opens the XML file itself.</summary>
        [Fact]
        public void ElementFile_XmlOfAWritableModel_IsTheFileItself()
        {
            string xml = Path.Combine(this.packages.ModelDirectory("BEDemo2", "BEDemo2"), "AxTable", "BDMDelivery.xml");

            Assert.Equal(xml, ElementDocuments.ElementFile(xml, this.writable));
        }

        /// <summary>The X++ editor opens a copy under XppSource, on both kinds of environment.</summary>
        [Fact]
        public void ElementFile_XppInXppSource_IsTheXmlOfTheElement()
        {
            string xpp = Path.Combine(this.packages.Root, "Metadata", "XppSource", "BEDemo1", "AxClass_BDMDeliveryHelper.xpp");
            string classicXpp = Path.Combine(@"J:\AosService\PackagesLocalDirectory", "bin", "XppSource", "BEDemo2", "AxTable_BDMDelivery.xpp");

            Assert.Equal(
                Path.Combine(this.packages.ModelDirectory("BEDemo1", "BEDemo1"), "AxClass", "BDMDeliveryHelper.xml"),
                ElementDocuments.ElementFile(xpp, this.writable));
            Assert.Equal(
                Path.Combine(this.packages.ModelDirectory("BEDemo2", "BEDemo2"), "AxTable", "BDMDelivery.xml"),
                ElementDocuments.ElementFile(classicXpp, this.writable));
        }

        /// <summary>The type has no underscore, so the first one separates it from the name.</summary>
        [Fact]
        public void ElementFile_NameWithUnderscore_SplitsAtTheFirst()
        {
            string xpp = Path.Combine(this.packages.Root, "Metadata", "XppSource", "BEDemo1", "AxClass_BDM_Delivery_Helper.xpp");

            Assert.Equal(
                Path.Combine(this.packages.ModelDirectory("BEDemo1", "BEDemo1"), "AxClass", "BDM_Delivery_Helper.xml"),
                ElementDocuments.ElementFile(xpp, this.writable));
        }

        /// <summary>Read-only models, build copies under XppMetadata and other files are no elements to check.</summary>
        [Fact]
        public void ElementFile_OtherDocuments_AreNull()
        {
            string readOnly = Path.Combine(this.packages.ModelDirectory("DemoBase", "DemoBase"), "AxClass", "DMODeliveryHelper.xml");
            string buildCopy = Path.Combine(this.packages.PackagesDirectory, "BEDemo1", "XppMetadata", "BEDemo1", "AxClass", "BDMDeliveryHelper.xml");
            string readOnlyXpp = Path.Combine(this.packages.Root, "Metadata", "XppSource", "DemoBase", "AxClass_DMODeliveryHelper.xpp");

            Assert.Null(ElementDocuments.ElementFile(readOnly, this.writable));
            Assert.Null(ElementDocuments.ElementFile(buildCopy, this.writable));
            Assert.Null(ElementDocuments.ElementFile(readOnlyXpp, this.writable));
            Assert.Null(ElementDocuments.ElementFile(Path.Combine(this.packages.Root, "Metadata", "XppSource", "BEDemo1", "Notes.xpp"), this.writable));
            Assert.Null(ElementDocuments.ElementFile(Path.Combine(this.packages.ModelDirectory("BEDemo1", "BEDemo1"), "readme.txt"), this.writable));
            Assert.Null(ElementDocuments.ElementFile(@"C:\Projects\Solution\Program.cs", this.writable));
            Assert.Null(ElementDocuments.ElementFile("<Solution|Misc Files>", this.writable));
            Assert.Null(ElementDocuments.ElementFile(null, this.writable));
        }
    }
}
