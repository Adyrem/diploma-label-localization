using System;
using System.Threading;
using System.Threading.Tasks;

namespace BE.LabelExtension.Core.Diagnostics
{
    /// <summary>
    /// Error boundary for the entry points of the extension, that is commands, MEF parts
    /// and background tasks (NFA04). An exception is reported as an error and goes no
    /// further, so Visual Studio keeps running.
    /// </summary>
    public sealed class ErrorBoundary
    {
        private readonly IMessageSink sink;

        /// <summary>
        /// Creates an error boundary that reports to the given sink.
        /// </summary>
        /// <param name="sink">Receives the errors.</param>
        public ErrorBoundary(IMessageSink sink)
        {
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        /// <summary>
        /// Runs an asynchronous action and reports any exception. A cancellation requested
        /// through <paramref name="cancellationToken"/> is not an error and is not reported.
        /// </summary>
        /// <param name="entryPoint">Name of the entry point, shown in the message.</param>
        /// <param name="action">The action to run.</param>
        /// <param name="cancellationToken">Cancels the action.</param>
        /// <returns>A task that completes when the action has completed or failed.</returns>
        public async Task RunAsync(string entryPoint, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            try
            {
                await action(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                this.Report(entryPoint, exception);
            }
        }

        /// <summary>
        /// Runs an action and reports any exception.
        /// </summary>
        /// <param name="entryPoint">Name of the entry point, shown in the message.</param>
        /// <param name="action">The action to run.</param>
        public void Run(string entryPoint, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                this.Report(entryPoint, exception);
            }
        }

        /// <summary>
        /// Runs a function and reports any exception. MEF parts use this, because Visual
        /// Studio expects a result from them.
        /// </summary>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <param name="entryPoint">Name of the entry point, shown in the message.</param>
        /// <param name="function">The function to run.</param>
        /// <param name="fallback">Result if the function fails.</param>
        /// <returns>The result of the function, or <paramref name="fallback"/> if it failed.</returns>
        public T Run<T>(string entryPoint, Func<T> function, T fallback)
        {
            try
            {
                return function();
            }
            catch (Exception exception)
            {
                this.Report(entryPoint, exception);
                return fallback;
            }
        }

        /// <summary>
        /// Reports an exception as an error: first line with the entry point and the
        /// message, then the details. Never throws, even if the sink does.
        /// </summary>
        /// <param name="entryPoint">Name of the entry point, shown in the message.</param>
        /// <param name="exception">The exception to report.</param>
        public void Report(string entryPoint, Exception exception)
        {
            try
            {
                this.sink.Report(MessageSeverity.Error, $"{entryPoint} failed: {exception.Message}\r\n{exception}");
            }
            catch
            {
                // Nothing left to report to. An error boundary must not throw itself.
            }
        }
    }
}
