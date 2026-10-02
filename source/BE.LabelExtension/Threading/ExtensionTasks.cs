using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace BE.LabelExtension.Threading
{
    /// <summary>
    /// Tracks the asynchronous work the extension starts without awaiting it, such as
    /// creating the output pane or loading the labels in the background. On shutdown it
    /// cancels <see cref="ShutdownToken"/> and waits for the work, so nothing runs on
    /// after Visual Studio has released the extension.
    /// </summary>
    /// <remarks>
    /// The extension has no AsyncPackage, whose factory would do this. The factory of
    /// <see cref="Microsoft.VisualStudio.Shell.ThreadHelper"/> does not block shutdown
    /// and is therefore unsuitable for fire and forget (analyzer rule VSSDK007).
    /// </remarks>
    internal sealed class ExtensionTasks : IDisposable
    {
        private readonly CancellationTokenSource shutdown = new();
        private readonly JoinableTaskCollection tasks;
        private bool disposed;

        /// <summary>Creates the tracker on the joinable task context of Visual Studio.</summary>
        public ExtensionTasks()
        {
            this.tasks = ThreadHelper.JoinableTaskContext.CreateCollection();
            this.Factory = ThreadHelper.JoinableTaskContext.CreateFactory(this.tasks);
        }

        /// <summary>Factory for work that is started and not awaited by its caller.</summary>
        public JoinableTaskFactory Factory { get; }

        /// <summary>Cancelled when Visual Studio shuts the extension down.</summary>
        public CancellationToken ShutdownToken => this.shutdown.Token;

        /// <summary>Cancels the running work and waits until it has completed.</summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.shutdown.Cancel();
            ThreadHelper.JoinableTaskFactory.Run(() => this.tasks.JoinTillEmptyAsync());
            this.shutdown.Dispose();
        }
    }
}
