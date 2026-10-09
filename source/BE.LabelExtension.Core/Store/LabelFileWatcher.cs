using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BE.LabelExtension.Core.Files;

namespace BE.LabelExtension.Core.Store
{
    /// <summary>
    /// Reports changes to the writable label files from outside, for example by the existing
    /// tool (FA15). Own writes do not count: they run inside <see cref="Suspend"/>.
    /// </summary>
    /// <remarks>
    /// The file system reports a write several times and sometimes late, after the write has
    /// finished. The watcher therefore remembers time and size of every file and reports a
    /// file only if they differ from the state it knows, collected over a short delay. After
    /// a suspension it takes the current state as known, so late events of the own write are
    /// ignored as well.
    /// </remarks>
    public sealed class LabelFileWatcher : IDisposable
    {
        private readonly object gate = new();
        private readonly TimeSpan delay;
        private readonly Timer timer;
        private readonly List<FileSystemWatcher> watchers = new();
        private readonly Dictionary<string, Stamp> known = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pending = new(StringComparer.OrdinalIgnoreCase);
        private int suspended;
        private bool disposed;

        /// <summary>Creates a watcher that collects changes for half a second.</summary>
        public LabelFileWatcher()
            : this(TimeSpan.FromMilliseconds(500))
        {
        }

        /// <summary>Creates a watcher.</summary>
        /// <param name="delay">How long changes are collected before they are reported.</param>
        public LabelFileWatcher(TimeSpan delay)
        {
            this.delay = delay;
            this.timer = new Timer(_ => this.ReportPending(), null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Raised on a thread of the thread pool when watched files changed from outside.
        /// </summary>
        public event EventHandler<LabelFilesChangedEventArgs>? ExternalChange;

        /// <summary>Watches the given files instead of the ones watched so far.</summary>
        /// <param name="files">Paths of the writable label files.</param>
        public void Start(IEnumerable<string> files)
        {
            lock (this.gate)
            {
                this.StopWatchers();
                foreach (string file in files)
                {
                    this.known[Path.GetFullPath(file)] = Stamp.Of(file);
                }

                foreach (string folder in this.known.Keys.Select(Path.GetDirectoryName).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (folder == null || !Directory.Exists(folder))
                    {
                        continue;
                    }

                    var watcher = new FileSystemWatcher(folder, "*.label.txt")
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                        IncludeSubdirectories = false,
                    };
                    watcher.Changed += (_, e) => this.OnFileEvent(e.FullPath);
                    watcher.Created += (_, e) => this.OnFileEvent(e.FullPath);
                    watcher.Deleted += (_, e) => this.OnFileEvent(e.FullPath);
                    watcher.Renamed += (_, e) => this.OnFileEvent(e.FullPath);
                    watcher.EnableRaisingEvents = true;
                    this.watchers.Add(watcher);
                }
            }
        }

        /// <summary>Stops watching.</summary>
        public void Stop()
        {
            lock (this.gate)
            {
                this.StopWatchers();
            }
        }

        /// <summary>
        /// Suspends the watching for an own write. Disposing the result resumes it and takes
        /// the state after the write as known.
        /// </summary>
        /// <returns>Resumes the watching when disposed, also after an exception.</returns>
        public IDisposable Suspend()
        {
            lock (this.gate)
            {
                this.suspended++;
            }

            return new Suspension(this);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (this.gate)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                this.StopWatchers();
            }

            this.timer.Dispose();
        }

        private void Resume()
        {
            lock (this.gate)
            {
                this.suspended--;
                if (this.suspended > 0)
                {
                    return;
                }

                foreach (string file in this.known.Keys.ToList())
                {
                    this.known[file] = Stamp.Of(file);
                }

                this.pending.Clear();
            }
        }

        private void OnFileEvent(string path)
        {
            lock (this.gate)
            {
                if (this.disposed || this.suspended > 0 || !this.known.ContainsKey(path))
                {
                    return;
                }

                this.pending.Add(path);
                this.timer.Change(this.delay, Timeout.InfiniteTimeSpan);
            }
        }

        private void ReportPending()
        {
            List<string> changed;
            lock (this.gate)
            {
                if (this.disposed || this.suspended > 0)
                {
                    return;
                }

                changed = new List<string>();
                foreach (string path in this.pending)
                {
                    Stamp now = Stamp.Of(path);
                    if (!now.Equals(this.known[path]))
                    {
                        this.known[path] = now;
                        changed.Add(path);
                    }
                }

                this.pending.Clear();
            }

            if (changed.Count > 0)
            {
                this.ExternalChange?.Invoke(this, new LabelFilesChangedEventArgs(changed));
            }
        }

        private void StopWatchers()
        {
            foreach (FileSystemWatcher watcher in this.watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            this.watchers.Clear();
            this.known.Clear();
            this.pending.Clear();
        }

        private readonly struct Stamp : IEquatable<Stamp>
        {
            private readonly long ticks;
            private readonly long length;

            private Stamp(long ticks, long length)
            {
                this.ticks = ticks;
                this.length = length;
            }

            public static Stamp Of(string path)
            {
                try
                {
                    var info = new FileInfo(LongPath.ForAccess(path));
                    return info.Exists ? new Stamp(info.LastWriteTimeUtc.Ticks, info.Length) : new Stamp(0, -1);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    return new Stamp(0, -2);
                }
            }

            public bool Equals(Stamp other) => this.ticks == other.ticks && this.length == other.length;

            public override bool Equals(object? obj) => obj is Stamp other && this.Equals(other);

            public override int GetHashCode() => this.ticks.GetHashCode() ^ this.length.GetHashCode();
        }

        private sealed class Suspension : IDisposable
        {
            private LabelFileWatcher? owner;

            public Suspension(LabelFileWatcher owner)
            {
                this.owner = owner;
            }

            public void Dispose() => Interlocked.Exchange(ref this.owner, null)?.Resume();
        }
    }
}
