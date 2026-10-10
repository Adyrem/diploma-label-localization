using System;
using System.IO;
using System.Text;

namespace BE.LabelExtension.Core.Files
{
    /// <summary>
    /// Reads and writes a text file, such as the XML file of an element, in the encoding and
    /// with the byte order mark it has. Writing goes through a temporary file that replaces the
    /// old one, so a failure leaves the old file untouched.
    /// </summary>
    public static class TextFile
    {
        /// <summary>Reads a text file.</summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>The text and the encoding to write it back with.</returns>
        public static (string Text, Encoding Encoding) Read(string path)
        {
            byte[] bytes = File.ReadAllBytes(LongPath.ForAccess(path));
            Encoding encoding = Detect(bytes, out int preamble);
            return (encoding.GetString(bytes, preamble, bytes.Length - preamble), encoding);
        }

        /// <summary>Writes a text file through a temporary file next to it.</summary>
        /// <param name="path">Path of the file.</param>
        /// <param name="text">The text.</param>
        /// <param name="encoding">The encoding from <see cref="Read"/>; its preamble is the byte order mark.</param>
        public static void Write(string path, string text, Encoding encoding)
        {
            string fullPath = Path.GetFullPath(path);
            string target = LongPath.ForAccess(fullPath);
            string temporary = LongPath.ForAccess(Path.Combine(Path.GetDirectoryName(fullPath)!, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp"));
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, encoding))
                {
                    writer.Write(text);
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

        private static Encoding Detect(byte[] bytes, out int preamble)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                preamble = 3;
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                preamble = 2;
                return new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                preamble = 2;
                return new UnicodeEncoding(bigEndian: true, byteOrderMark: true);
            }

            preamble = 0;
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }
    }
}
