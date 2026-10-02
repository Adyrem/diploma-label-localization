using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// Reads and writes the format of the label files, <c>&lt;Label file&gt;.&lt;Language&gt;.label.txt</c>.
    /// No other class knows the format.
    /// </summary>
    /// <remarks>
    /// <para>Reading follows the existing tool. A line <c>ID=Text</c> starts a label, the ID
    /// reaches up to the first equals sign. A line that starts with <c>;</c> or <c>#</c> after
    /// spaces is a comment. Only the first comment line after a label counts, comment lines
    /// before the first label are ignored, and empty lines do not count. If an ID appears
    /// twice, the first one wins.</para>
    /// <para>Writing is always UTF-8 with byte order mark and Windows line endings: each label
    /// as <c>ID=Text</c>, an empty text as one space, the comment as the following line
    /// <c> ;Comment</c>.</para>
    /// </remarks>
    public static class LabelFileFormat
    {
        private const string LineBreak = "\r\n";

        /// <summary>UTF-8 with byte order mark, as the existing tool writes.</summary>
        public static Encoding Encoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        /// <summary>Reads a label file.</summary>
        /// <param name="path">Path of the file of one language.</param>
        /// <returns>The labels and the problems found.</returns>
        public static LabelFileContent Read(string path)
        {
            // Other programs, the existing tool for one, may hold the file open.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return Read(stream);
        }

        /// <summary>Reads a label file.</summary>
        /// <param name="stream">The content. A byte order mark selects the encoding, otherwise UTF-8.</param>
        /// <returns>The labels and the problems found.</returns>
        public static LabelFileContent Read(Stream stream)
        {
            var entries = new List<LabelEntry>();
            var issues = new List<LabelFileIssue>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            string? key = null;
            string? text = null;
            string? comment = null;
            bool keep = false;
            int lineNumber = 0;

            void Flush()
            {
                if (key != null && keep)
                {
                    entries.Add(new LabelEntry(key, text!, comment));
                }
            }

            using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                string trimmed = line.TrimStart();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (trimmed[0] == ';' || trimmed[0] == '#')
                {
                    if (key == null)
                    {
                        continue;
                    }

                    if (comment == null)
                    {
                        comment = trimmed.Substring(1);
                    }
                    else
                    {
                        issues.Add(new LabelFileIssue(LabelFileIssueKind.ExtraCommentLine, lineNumber, key));
                    }

                    continue;
                }

                int equals = line.IndexOf('=');
                if (equals <= 0)
                {
                    issues.Add(new LabelFileIssue(LabelFileIssueKind.InvalidLine, lineNumber, null));
                    continue;
                }

                Flush();
                key = line.Substring(0, equals);
                text = line.Substring(equals + 1);
                if (text == " ")
                {
                    text = string.Empty;
                }

                comment = null;
                keep = seen.Add(key);
                if (!keep)
                {
                    issues.Add(new LabelFileIssue(LabelFileIssueKind.DuplicateId, lineNumber, key));
                }
            }

            Flush();
            return new LabelFileContent(entries, issues);
        }

        /// <summary>Writes the labels in the given order.</summary>
        /// <param name="stream">Receives the content.</param>
        /// <param name="entries">The labels to write.</param>
        /// <exception cref="ArgumentException">A key, text or comment contains a line break, or a key an equals sign.</exception>
        public static void Write(Stream stream, IEnumerable<LabelEntry> entries)
        {
            using var writer = new StreamWriter(stream, Encoding, bufferSize: 65536, leaveOpen: true) { NewLine = LineBreak };
            foreach (LabelEntry entry in entries)
            {
                Validate(entry);
                writer.Write(entry.Key);
                writer.Write('=');
                writer.Write(entry.Text.Length == 0 ? " " : entry.Text);
                writer.Write(LineBreak);
                if (entry.Comment != null)
                {
                    writer.Write(" ;");
                    writer.Write(entry.Comment);
                    writer.Write(LineBreak);
                }
            }
        }

        /// <summary>
        /// Writes the labels into a temporary file next to <paramref name="path"/>, which then
        /// replaces the old file. A failure leaves the old file untouched.
        /// </summary>
        /// <param name="path">Path of the file of one language.</param>
        /// <param name="entries">The labels to write.</param>
        public static void WriteFile(string path, IEnumerable<LabelEntry> entries)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
            string temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    Write(stream, entries);
                }

                if (File.Exists(path))
                {
                    File.Replace(temporary, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private static void Validate(LabelEntry entry)
        {
            if (entry.Key.Length == 0 || entry.Key.IndexOf('=') >= 0 || HasLineBreak(entry.Key))
            {
                throw new ArgumentException($"Invalid label key '{entry.Key}'.", nameof(entry));
            }

            if (HasLineBreak(entry.Text) || (entry.Comment != null && HasLineBreak(entry.Comment)))
            {
                throw new ArgumentException($"Text or comment of label '{entry.Key}' contains a line break.", nameof(entry));
            }
        }

        private static bool HasLineBreak(string value) => value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0;
    }
}
