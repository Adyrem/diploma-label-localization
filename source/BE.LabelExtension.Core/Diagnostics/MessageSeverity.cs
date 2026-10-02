namespace BE.LabelExtension.Core.Diagnostics
{
    /// <summary>
    /// Kind of a message, matching the categories Errors, Warnings and Messages of the
    /// existing tool's console (FA17).
    /// </summary>
    public enum MessageSeverity
    {
        /// <summary>An operation failed.</summary>
        Error,

        /// <summary>An operation succeeded only partially, for example a file was skipped.</summary>
        Warning,

        /// <summary>Information without a problem, for example the number of loaded labels.</summary>
        Message,
    }
}
