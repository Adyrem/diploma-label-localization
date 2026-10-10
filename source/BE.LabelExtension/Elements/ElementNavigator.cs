using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Elements;
using BE.LabelExtension.Core.Files;
using BE.LabelExtension.Core.Usages;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace BE.LabelExtension.Elements
{
    /// <summary>
    /// Opens an element at a use of a label ID (FA04), as the concept lays down under the jump
    /// to a use and D1 tried out.
    /// </summary>
    /// <remarks>
    /// <para>A use in code opens the <c>.xpp</c> file of the element in XppSource, which the X++
    /// editor shows, with the cursor on the same use: the third one if the use is the third in
    /// the code of the XML file. Opening the XML file would open the designer instead (D1).</para>
    /// <para>The Developer Tools write the <c>.xpp</c> file when the element is opened in code. If
    /// there is none yet, or it is older than the XML file and not open, the element opens in
    /// the designer, so that nobody edits code that no longer matches the element.</para>
    /// <para>A use in a property opens the element in the designer. The Developer Tools have no
    /// command to select a node there (D1), so the result names the property instead.</para>
    /// </remarks>
    internal static class ElementNavigator
    {
        // The Developer Tools write XML and .xpp one after the other when saving.
        private static readonly TimeSpan SaveTolerance = TimeSpan.FromSeconds(5);

        /// <summary>Opens the element of a use.</summary>
        /// <param name="usage">The use.</param>
        /// <param name="labelId">The complete label ID.</param>
        /// <param name="debugSourceFolder">The DebugSourceFolder of the metadata configuration, if one is used.</param>
        /// <param name="cancellationToken">Cancels before opening.</param>
        /// <returns>What happened, for the status line.</returns>
        public static async Task<string> OpenAsync(LabelUsage usage, string labelId, string? debugSourceFolder, CancellationToken cancellationToken)
        {
            string element = Path.GetFileNameWithoutExtension(usage.Path);
            if (usage.IsInCode)
            {
                string? xpp = ElementDocuments.XppFile(usage.Path, usage.Model, ElementDocuments.XppSourceFolder(usage.Model, debugSourceFolder));
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                if (xpp != null && (IsOpen(xpp) || IsCurrent(xpp, usage.Path)))
                {
                    IVsWindowFrame frame = Open(xpp);
                    return PlaceCaret(frame, labelId, usage.CodeOccurrence)
                        ? $"{element}: use {usage.CodeOccurrence} of {labelId} in the code."
                        : $"{element} is open, but its code has no use {usage.CodeOccurrence} of {labelId}; it may have changed since the search.";
                }

                OpenWithCaret(usage);
                return $"{element} is open. Its code is not yet open in the X++ editor, so the cursor cannot go to the use. Open the code once from the Application Explorer, then go to the use again.";
            }

            string? property = await Task.Run(() => DescribeProperty(usage, labelId), cancellationToken);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            OpenWithCaret(usage);
            return property == null ? $"{element} is open; {labelId} is in line {usage.Line} of its XML file." : $"{element} is open; {labelId} is in {property}.";
        }

        private static string? DescribeProperty(LabelUsage usage, string labelId)
        {
            try
            {
                return ElementXmlLocator.DescribeProperty(TextFile.Read(usage.Path).Text, usage.Line, labelId);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static bool IsOpen(string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, path, Guid.Empty, out _, out _, out _);
        }

        private static bool IsCurrent(string xpp, string xml)
            => File.Exists(xpp) && File.GetLastWriteTimeUtc(xpp) >= File.GetLastWriteTimeUtc(xml) - SaveTolerance;

        private static IVsWindowFrame Open(string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, path, VSConstants.LOGVIEWID.Primary_guid, out _, out _, out IVsWindowFrame frame);
            ErrorHandler.ThrowOnFailure(frame.Show());
            return frame;
        }

        // The XML file opens in the designer of the Developer Tools. Where it opens as text
        // instead, as without the Developer Tools, the cursor goes to line and column.
        private static void OpenWithCaret(LabelUsage usage)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsWindowFrame frame = Open(usage.Path);
            IVsTextView? view = VsShellUtilities.GetTextView(frame);
            if (view != null)
            {
                view.SetCaretPos(usage.Line - 1, usage.Column - 1);
                view.CenterLines(usage.Line - 1, 1);
            }
        }

        private static bool PlaceCaret(IVsWindowFrame frame, string labelId, int occurrence)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsTextView? view = VsShellUtilities.GetTextView(frame);
            if (view == null
                || view.GetBuffer(out IVsTextLines lines) != VSConstants.S_OK
                || lines.GetLastLineIndex(out int lastLine, out int lastIndex) != VSConstants.S_OK
                || lines.GetLineText(0, 0, lastLine, lastIndex, out string text) != VSConstants.S_OK)
            {
                return false;
            }

            IReadOnlyList<int> found = LabelReferences.FindInCode(text, labelId);
            if (occurrence < 1 || occurrence > found.Count
                || lines.GetLineIndexOfPosition(found[occurrence - 1], out int line, out int column) != VSConstants.S_OK)
            {
                return false;
            }

            view.SetCaretPos(line, column);
            view.CenterLines(line, 1);
            return true;
        }
    }
}
