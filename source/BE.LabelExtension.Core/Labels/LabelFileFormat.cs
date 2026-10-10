using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BE.LabelExtension.Core.Files;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// Reads and writes the format of the label files, <c>&lt;Label file&gt;.&lt;Language&gt;.label.txt</c>.
    /// No other class knows the format.
    /// </summary>
    /// <remarks>
    /// <para>Reading follows the existing tool. A line <c>ID=Text</c> starts a label, the ID
    /// reaches up to the first equals sign, without spaces and tabs at its end; the existing
    /// tool keeps those. A line that starts with <c>;</c> or <c>#</c> after
    /// spaces is a comment. Only the first comment line after a label counts, comment lines
    /// before the first label are ignored, and empty lines do not count. If an ID appears
    /// twice, the first one wins.</para>
    /// <para>Writing is UTF-8 with Windows line endings: each label as <c>ID=Text</c>, an
    /// empty text as one space, the comment as the following line <c> ;Comment</c>. A file
    /// keeps its byte order mark as read, a new file gets one. The existing tool always
    /// writes one.</para>
    /// </remarks>
    public static class LabelFileFormat
    {
        private const string LineBreak = "\r\n";

        private static readonly Encoding WithoutByteOrderMark = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>UTF-8 with byte order mark, for new files and for files read with one.</summary>
        public static Encoding Encoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        /// <summary>Reads a label file.</summary>
        /// <param name="path">Path of the file of one language.</param>
        /// <returns>The labels and the problems found.</returns>
        public static LabelFileContent Read(string path)
        {
            // Other programs, the existing tool for one, may hold the file open.
            using var stream = new FileStream(LongPath.ForAccess(path), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return Read(stream);
        }

        /// <summary>Reads a label file.</summary>
        /// <param name="stream">The content. A byte order mark selects the encoding, otherwise UTF-8.</param>
        /// <returns>The labels and the problems found.</returns>
        public static LabelFileContent Read(Stream stream)
        {
            if (!stream.CanSeek)
            {
                var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                buffer.Position = 0;
                stream = buffer;
            }

            bool hasByteOrderMark = StartsWithByteOrderMark(stream);
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

                // Spaces and tabs before the equals sign are not part of the ID. Files of the
                // platform contain lines such as "ID =Text"; the existing tool keeps the space
                // in the ID, so the ID used in code does not find such a label there.
                int equals = line.IndexOf('=');
                string id = equals > 0 ? line.Substring(0, equals).TrimEnd(' ', '\t') : string.Empty;
                if (id.Length == 0)
                {
                    issues.Add(new LabelFileIssue(LabelFileIssueKind.InvalidLine, lineNumber, null));
                    continue;
                }

                Flush();
                key = id;
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
            return new LabelFileContent(entries, issues, hasByteOrderMark);
        }

        /// <summary>Writes the labels in the given order, with byte order mark, as for a new file.</summary>
        /// <param name="stream">Receives the content.</param>
        /// <param name="entries">The labels to write.</param>
        /// <exception cref="ArgumentException">A key, text or comment contains a line break, or a key an equals sign.</exception>
        public static void Write(Stream stream, IEnumerable<LabelEntry> entries) => Write(stream, entries, byteOrderMark: true);

        /// <summary>Writes the labels in the given order.</summary>
        /// <param name="stream">Receives the content.</param>
        /// <param name="entries">The labels to write.</param>
        /// <param name="byteOrderMark">Whether to start with the byte order mark, as <see cref="LabelFileContent.HasByteOrderMark"/> of the file read.</param>
        /// <exception cref="ArgumentException">A key, text or comment contains a line break, or a key an equals sign.</exception>
        public static void Write(Stream stream, IEnumerable<LabelEntry> entries, bool byteOrderMark)
        {
            using var writer = new StreamWriter(stream, byteOrderMark ? Encoding : WithoutByteOrderMark, bufferSize: 65536, leaveOpen: true) { NewLine = LineBreak };
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
        /// Writes the labels with byte order mark, as for a new file, see
        /// <see cref="WriteFile(string, IEnumerable{LabelEntry}, bool)"/>.
        /// </summary>
        /// <param name="path">Path of the file of one language.</param>
        /// <param name="entries">The labels to write.</param>
        public static void WriteFile(string path, IEnumerable<LabelEntry> entries) => WriteFile(path, entries, byteOrderMark: true);

        /// <summary>
        /// Writes the labels into a temporary file next to <paramref name="path"/>, which then
        /// replaces the old file. A failure leaves the old file untouched.
        /// </summary>
        /// <param name="path">Path of the file of one language.</param>
        /// <param name="entries">The labels to write.</param>
        /// <param name="byteOrderMark">Whether to start with the byte order mark, as <see cref="LabelFileContent.HasByteOrderMark"/> of the file read.</param>
        public static void WriteFile(string path, IEnumerable<LabelEntry> entries, bool byteOrderMark)
        {
            string fullPath = Path.GetFullPath(path);
            string target = LongPath.ForAccess(fullPath);

            // The temporary name is longer than the target and may need the prefix on its own.
            string temporary = LongPath.ForAccess(Path.Combine(Path.GetDirectoryName(fullPath)!, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp"));
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    Write(stream, entries, byteOrderMark);
                }

                if (File.Exists(target))
                {
                    File.Replace(temporary, target, destinationBackupFileName: null, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporary, target);
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

        // Looks at the first bytes and goes back, so that the reader sees them as well.
        private static bool StartsWithByteOrderMark(Stream stream)
        {
            long start = stream.Position;
            byte[] preamble = Encoding.GetPreamble();
            var head = new byte[preamble.Length];
            int read = 0;
            int count;
            while (read < head.Length && (count = stream.Read(head, read, head.Length - read)) > 0)
            {
                read += count;
            }

            stream.Position = start;
            return read == preamble.Length && head.SequenceEqual(preamble);
        }
    }
}
