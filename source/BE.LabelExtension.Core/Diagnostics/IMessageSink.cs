namespace BE.LabelExtension.Core.Diagnostics
{
    /// <summary>
    /// Receives the messages of the core logic. The core defines this interface and the
    /// extension implements it with the Output Window, so the core needs no reference to
    /// Visual Studio (dependency inversion, Z8).
    /// </summary>
    public interface IMessageSink
    {
        /// <summary>
        /// Reports a message. Implementations must not throw, because callers report
        /// from error handlers.
        /// </summary>
        /// <param name="severity">Kind of the message.</param>
        /// <param name="message">Text of the message, may span several lines.</param>
        void Report(MessageSeverity severity, string message);
    }
}
