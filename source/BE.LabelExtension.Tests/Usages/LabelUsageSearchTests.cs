using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Usages;
using BE.LabelExtension.Tests.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Usages
{
    /// <summary>FA04 with F16: the usage search covers all models, the own ones first.</summary>
    public sealed class LabelUsageSearchTests : IDisposable
    {
        private readonly TestPackages packages = new();
        private readonly IReadOnlyList<ModelInfo> models;

        public LabelUsageSearchTests()
        {
            this.models = new ModelDiscovery(this.packages.ConfigurationFolder)
                .FindModels(new[] { new PackageDirectory(this.packages.PackagesDirectory, isReference: false) }, new RecordingSink(), CancellationToken.None);
        }

        public void Dispose() => this.packages.Dispose();

        /// <summary>Classic VM: one directory for all, so own are the models from layer VAR up.</summary>
        [Fact]
        public void Split_ClassicVm_OwnFromLayerVar()
        {
            var (own, others) = LabelUsageSearch.Split(this.models, this.Directories(isReference: false), fromConfiguration: false);

            Assert.Equal(new[] { "BEDemo1", "BEDemo2", "DemoLocked" }, own.Select(m => m.Name).OrderBy(n => n));
            Assert.Equal(new[] { "DemoBase" }, others.Select(m => m.Name));
        }

        /// <summary>Unified Developer Experience: own are all models of the ModelStoreFolder, references are others.</summary>
        [Fact]
        public void Split_Configuration_OwnAreTheModelStore()
        {
            Assert.Equal(4, LabelUsageSearch.Split(this.models, this.Directories(isReference: false), fromConfiguration: true).Own.Count);
            Assert.Empty(LabelUsageSearch.Split(this.models, this.Directories(isReference: true), fromConfiguration: true).Own);
        }

        /// <summary>All models are searched, the own ones are done before the others start.</summary>
        [Fact]
        public void Run_AllModels_OwnFirst()
        {
            var (own, others) = LabelUsageSearch.Split(this.models, this.Directories(isReference: false), fromConfiguration: false);
            var found = new ConcurrentQueue<(LabelUsage Usage, bool OwnDone)>();
            bool ownDone = false;

            LabelUsageSearch.Run(own, others, "@BDM1:BDM110000003", u => found.Enqueue((u, Volatile.Read(ref ownDone))), () => Volatile.Write(ref ownDone, true), CancellationToken.None);

            Assert.Equal(7, found.Count);
            Assert.All(found.Where(f => f.Usage.Model.Name == "DemoBase"), f => Assert.True(f.OwnDone));
            Assert.All(found.Where(f => f.Usage.Model.Name != "DemoBase"), f => Assert.False(f.OwnDone));
        }

        [Fact]
        public void Run_Cancelled_Stops()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.ThrowsAny<OperationCanceledException>(() => LabelUsageSearch.Run(this.models, Array.Empty<ModelInfo>(), "@BDM1:BDM110000003", _ => { }, null, cancellation.Token));
        }

        private IReadOnlyList<PackageDirectory> Directories(bool isReference) => new[] { new PackageDirectory(this.packages.PackagesDirectory, isReference) };
    }
}
