using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Store;
using Xunit;

namespace BE.LabelExtension.Tests.Store
{
    /// <summary>TC09, the part of the file watcher: changes from outside are reported, own writes are not.</summary>
    public sealed class LabelFileWatcherTests : IDisposable
    {
        private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private readonly TestPackages packages = new();
        private readonly LabelFileWatcher watcher = new(Delay);
        private readonly BlockingCollection<string> reported = new();
        private readonly string file;

        public LabelFileWatcherTests()
        {
            this.file = this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "de");
            this.watcher.ExternalChange += (_, e) =>
            {
                foreach (string path in e.Paths)
                {
                    this.reported.Add(path);
                }
            };
            this.watcher.Start(new[] { this.file });
        }

        public void Dispose()
        {
            this.watcher.Dispose();
            this.packages.Dispose();
        }

        [Fact]
        public void ChangeFromOutside_IsReported()
        {
            File.AppendAllText(this.file, "EXTERNAL=Von aussen\r\n");

            Assert.True(this.reported.TryTake(out string? path, Patience));
            Assert.Equal(Path.GetFullPath(this.file), path);
        }

        [Fact]
        public void OwnWriteInsideSuspension_IsNotReported()
        {
            using (this.watcher.Suspend())
            {
                LabelFileFormat.WriteFile(this.file, new[] { new LabelEntry("OWN", "Eigene Änderung", null) });
            }

            Assert.False(this.reported.TryTake(out _, TimeSpan.FromSeconds(1.5)));
        }

        [Fact]
        public void ChangeAfterOwnWrite_IsReportedAgain()
        {
            using (this.watcher.Suspend())
            {
                LabelFileFormat.WriteFile(this.file, new[] { new LabelEntry("OWN", "Eigene Änderung", null) });
            }

            Thread.Sleep(Delay + Delay);
            File.AppendAllText(this.file, "EXTERNAL=Von aussen\r\n");

            Assert.True(this.reported.TryTake(out _, Patience));
        }

        [Fact]
        public void ChangeOfUnwatchedFile_IsNotReported()
        {
            File.AppendAllText(this.packages.LabelFilePath("BEDemo1", "BEDemo1", "BDM1", "en-US"), "EXTERNAL=From outside\r\n");

            Assert.False(this.reported.TryTake(out _, TimeSpan.FromSeconds(1.5)));
        }
    }
}
