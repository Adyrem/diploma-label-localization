using System;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using Xunit;

namespace BE.LabelExtension.Tests.Diagnostics
{
    public class ErrorBoundaryTests
    {
        private readonly RecordingSink sink = new();
        private readonly ErrorBoundary boundary;

        public ErrorBoundaryTests()
        {
            this.boundary = new ErrorBoundary(this.sink);
        }

        [Fact]
        public async Task RunAsync_Exception_IsReportedAsErrorWithEntryPoint()
        {
            await this.boundary.RunAsync("Open label window", _ => throw new InvalidOperationException("broken"), CancellationToken.None);

            var (severity, message) = Assert.Single(this.sink.Messages);
            Assert.Equal(MessageSeverity.Error, severity);
            Assert.StartsWith("Open label window failed: broken\r\n", message);
            Assert.Contains(nameof(InvalidOperationException), message);
        }

        [Fact]
        public async Task RunAsync_Success_ReportsNothing()
        {
            bool ran = false;

            await this.boundary.RunAsync("Command", _ => { ran = true; return Task.CompletedTask; }, CancellationToken.None);

            Assert.True(ran);
            Assert.Empty(this.sink.Messages);
        }

        [Fact]
        public async Task RunAsync_RequestedCancellation_IsNotAnError()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await this.boundary.RunAsync("Load", token => Task.FromCanceled(token), cancellation.Token);

            Assert.Empty(this.sink.Messages);
        }

        [Fact]
        public async Task RunAsync_CancellationNotRequested_IsAnError()
        {
            await this.boundary.RunAsync("Load", _ => throw new OperationCanceledException(), CancellationToken.None);

            Assert.Equal(MessageSeverity.Error, Assert.Single(this.sink.Messages).Severity);
        }

        [Fact]
        public void Run_Exception_IsReported()
        {
            this.boundary.Run("Tooltip", () => throw new FormatException("bad id"));

            Assert.StartsWith("Tooltip failed: bad id", Assert.Single(this.sink.Messages).Message);
        }

        [Fact]
        public void RunWithResult_Exception_ReturnsFallback()
        {
            int result = this.boundary.Run<int>("Tagger", () => throw new InvalidOperationException(), fallback: -1);

            Assert.Equal(-1, result);
            Assert.Single(this.sink.Messages);
        }

        [Fact]
        public void RunWithResult_Success_ReturnsResult()
        {
            Assert.Equal(42, this.boundary.Run("Tagger", () => 42, fallback: -1));
            Assert.Empty(this.sink.Messages);
        }

        [Fact]
        public void Report_SinkThrows_DoesNotThrow()
        {
            var failing = new ErrorBoundary(new ThrowingSink());

            failing.Run("Command", () => throw new InvalidOperationException());
        }

        private sealed class ThrowingSink : IMessageSink
        {
            public void Report(MessageSeverity severity, string message) => throw new InvalidOperationException("sink down");
        }
    }
}
