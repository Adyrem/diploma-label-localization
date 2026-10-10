using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Files;
using BE.LabelExtension.Core.Models;

namespace BE.LabelExtension.Core.Usages
{
    /// <summary>
    /// Finds and replaces the uses of a label ID in the XML files of the elements, with the
    /// patterns of the existing tool (RE7). Basis of the usage search (FA04), of replacing
    /// (FA13) and of the reference update when copying and moving (FA03).
    /// </summary>
    /// <remarks>
    /// <para>Searched are all <c>*.xml</c> in the folders <c>Ax*</c> of a model, also in
    /// subfolders, except <c>AxLabelFile</c>. The copies the build puts under
    /// <c>&lt;Package&gt;\XppMetadata</c> lie outside the model folder and are not touched
    /// (B21).</para>
    /// <para>A use is the complete ID, ordinal and case-sensitive, as <c>&gt;ID&lt;</c>,
    /// <c>"ID"</c>, <c>'ID'</c> or <c>(ID)</c>. In the files of reports, whose path contains
    /// <c>\AxReport\</c>, also as <c>!ID</c> followed by <c>&amp;</c>, comma, space, <c>+</c>,
    /// dot, <c>)</c> or <c>&lt;</c>, as in expressions such as <c>=Labels!@SYS12345 &amp;</c>.
    /// The existing tool uses only the report patterns there; the extension uses both, so that
    /// moving a label misses none of its uses. Every use in a line counts.</para>
    /// </remarks>
    public static class LabelReferences
    {
        private const string ReportFolder = @"\AxReport\";
        private const string LabelFileFolder = "AxLabelFile";
        private const string ReportFollowers = "&, +.)<";

        /// <summary>The XML files of the elements of a model.</summary>
        /// <param name="model">The model.</param>
        /// <returns>The files; empty if the model folder cannot be read.</returns>
        public static IReadOnlyList<string> ElementFiles(ModelInfo model)
        {
            var files = new List<string>();
            try
            {
                foreach (string folder in LongPath.GetDirectories(model.Directory))
                {
                    string name = Path.GetFileName(folder);
                    if (name.StartsWith("Ax", StringComparison.OrdinalIgnoreCase) && !string.Equals(name, LabelFileFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        AddXmlFiles(folder, files);
                    }
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                // A model that cannot be read has no files to search.
            }

            return files;
        }

        /// <summary>Finds the uses of a label ID in the elements of the given models, model by model.</summary>
        /// <param name="models">The models, in the order to search them.</param>
        /// <param name="labelId">The complete label ID.</param>
        /// <param name="cancellationToken">Cancels the search.</param>
        /// <returns>The uses, file by file as they are found.</returns>
        public static IEnumerable<LabelUsage> Find(IEnumerable<ModelInfo> models, string labelId, CancellationToken cancellationToken)
        {
            foreach (ModelInfo model in models)
            {
                foreach (string file in ElementFiles(model))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string text;
                    try
                    {
                        text = TextFile.Read(file).Text;
                    }
                    catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                    {
                        continue;
                    }

                    foreach (LabelUsage usage in FindInText(model, file, text, labelId))
                    {
                        yield return usage;
                    }
                }
            }
        }

        /// <summary>Finds the uses of a label ID in the text of one file.</summary>
        /// <param name="model">The model of the file.</param>
        /// <param name="path">The path of the file.</param>
        /// <param name="text">Its text.</param>
        /// <param name="labelId">The complete label ID.</param>
        /// <returns>The uses with line and column, both starting at 1.</returns>
        public static IReadOnlyList<LabelUsage> FindInText(ModelInfo model, string path, string text, string labelId)
        {
            var usages = new List<LabelUsage>();
            int line = 1;
            int lineStart = 0;
            int counted = 0;
            foreach (int position in Positions(text, labelId, IsReport(path)))
            {
                for (; counted < position; counted++)
                {
                    if (text[counted] == '\n')
                    {
                        line++;
                        lineStart = counted + 1;
                    }
                }

                int lineEnd = text.IndexOf('\n', position);
                string lineText = text.Substring(lineStart, (lineEnd < 0 ? text.Length : lineEnd) - lineStart).Trim();
                usages.Add(new LabelUsage(model, path, line, position - lineStart + 1, lineText));
            }

            return usages;
        }

        /// <summary>
        /// Finds the files in the given models that use a label ID, without changing them, for
        /// checking them before replacing.
        /// </summary>
        /// <param name="models">The models, usually the writable ones.</param>
        /// <param name="labelId">The complete label ID.</param>
        /// <param name="cancellationToken">Cancels the search.</param>
        /// <returns>The files with at least one use.</returns>
        public static IReadOnlyList<string> FindFiles(IEnumerable<ModelInfo> models, string labelId, CancellationToken cancellationToken)
            => Find(models, labelId, cancellationToken).Select(u => u.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>
        /// Replaces the uses of one label ID by another in the given files (FA13). Every file
        /// keeps its encoding and byte order mark.
        /// </summary>
        /// <param name="files">The files, usually from <see cref="FindFiles"/>.</param>
        /// <param name="oldId">The complete label ID used so far.</param>
        /// <param name="newId">The complete label ID to use instead.</param>
        /// <param name="messages">Receives every file that could not be changed.</param>
        /// <returns>The files changed.</returns>
        public static IReadOnlyList<string> Replace(IEnumerable<string> files, string oldId, string newId, IMessageSink messages)
            => Replace(files, oldId, newId, messages, out _);

        /// <summary>
        /// Replaces the uses of one label ID by another in the given files and names the files
        /// that could not be changed, for example before moving deletes the original.
        /// </summary>
        /// <param name="files">The files, usually from <see cref="FindFiles"/>.</param>
        /// <param name="oldId">The complete label ID used so far.</param>
        /// <param name="newId">The complete label ID to use instead.</param>
        /// <param name="messages">Receives every file that could not be changed.</param>
        /// <param name="failed">The files that could not be read or written.</param>
        /// <returns>The files changed.</returns>
        public static IReadOnlyList<string> Replace(IEnumerable<string> files, string oldId, string newId, IMessageSink messages, out IReadOnlyList<string> failed)
        {
            var changed = new List<string>();
            var notChanged = new List<string>();
            failed = notChanged;
            foreach (string file in files)
            {
                try
                {
                    (string text, Encoding encoding) = TextFile.Read(file);
                    string replaced = ReplaceInText(text, oldId, newId, IsReport(file), out int count);
                    if (count > 0)
                    {
                        TextFile.Write(file, replaced, encoding);
                        changed.Add(file);
                    }
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    messages.Report(MessageSeverity.Error, $"References to {oldId} could not be changed in {file} ({exception.Message})");
                    notChanged.Add(file);
                }
            }

            return changed;
        }

        /// <summary>
        /// Replaces the uses of one label ID by another in a text. Like the existing tool,
        /// <c>'ID'</c> becomes <c>"NEW"</c> in double quotes; the other patterns keep their
        /// characters around the ID.
        /// </summary>
        internal static string ReplaceInText(string text, string oldId, string newId, bool isReport, out int count)
        {
            var result = new StringBuilder(text.Length);
            int copied = 0;
            count = 0;
            foreach (int position in Positions(text, oldId, isReport))
            {
                bool singleQuotes = text[position - 1] == '\'' && position + oldId.Length < text.Length && text[position + oldId.Length] == '\'';
                result.Append(text, copied, position - copied - (singleQuotes ? 1 : 0));
                result.Append(singleQuotes ? "\"" + newId + "\"" : newId);
                copied = position + oldId.Length + (singleQuotes ? 1 : 0);
                count++;
            }

            result.Append(text, copied, text.Length - copied);
            return result.ToString();
        }

        /// <summary>The positions of the ID within one of the patterns, in order.</summary>
        internal static IEnumerable<int> Positions(string text, string id, bool isReport)
        {
            int position = text.IndexOf(id, StringComparison.Ordinal);
            while (position >= 0)
            {
                if (IsUse(text, position, id.Length, isReport))
                {
                    yield return position;
                }

                position = text.IndexOf(id, position + 1, StringComparison.Ordinal);
            }
        }

        private static bool IsUse(string text, int position, int length, bool isReport)
        {
            if (position == 0 || position + length >= text.Length)
            {
                return false;
            }

            char before = text[position - 1];
            char after = text[position + length];
            return (before == '>' && after == '<')
                || (before == '"' && after == '"')
                || (before == '\'' && after == '\'')
                || (before == '(' && after == ')')
                || (isReport && before == '!' && ReportFollowers.IndexOf(after) >= 0);
        }

        private static bool IsReport(string path) => path.IndexOf(ReportFolder, StringComparison.OrdinalIgnoreCase) >= 0;

        private static void AddXmlFiles(string folder, List<string> files)
        {
            files.AddRange(LongPath.GetFiles(folder, "*.xml"));
            foreach (string subfolder in LongPath.GetDirectories(folder))
            {
                AddXmlFiles(subfolder, files);
            }
        }
    }
}
