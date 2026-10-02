using System.Collections.Generic;
using BE.LabelExtension.Core.Diagnostics;

namespace BE.LabelExtension.Tests.Diagnostics
{
    /// <summary>Message sink that keeps all reported messages for assertions.</summary>
    internal sealed class RecordingSink : IMessageSink
    {
        public List<(MessageSeverity Severity, string Message)> Messages { get; } = new();

        public void Report(MessageSeverity severity, string message) => this.Messages.Add((severity, message));
    }
}
