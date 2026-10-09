using System;
using System.IO;
using System.Linq;

namespace BE.LabelExtension.Core.Files
{
    /// <summary>
    /// Paths for file access beyond 260 characters. Visual Studio does not declare itself
    /// aware of long paths, so Windows accepts such a path only with the prefix <c>\\?\</c>.
    /// Label files of the platform reach that length on the test environment (D1).
    /// </summary>
    /// <remarks>
    /// Only the call that touches the file system gets the prefix. Everywhere else, in the
    /// label store, in comparisons and in messages, paths stay without it.
    /// </remarks>
    public static class LongPath
    {
        // Directories may be 12 characters shorter than files (MAX_PATH less an 8.3 name).
        // From this length on, the prefix is added for both.
        private const int Limit = 248;
        private const string Prefix = @"\\?\";
        private const string UncPrefix = @"\\?\UNC\";
        private const string DevicePrefix = @"\\.\";

        /// <summary>The path to hand to a file or directory operation.</summary>
        /// <param name="path">An absolute path without prefix.</param>
        /// <returns>The path with <c>\\?\</c> if it is long, otherwise the path as it is.</returns>
        public static string ForAccess(string path)
        {
            if (path.Length < Limit
                || path.StartsWith(Prefix, StringComparison.Ordinal)
                || path.StartsWith(DevicePrefix, StringComparison.Ordinal))
            {
                return path;
            }

            // The prefix switches off the normalization of Windows, so only backslashes.
            string normalized = path.Replace('/', '\\');
            if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return UncPrefix + normalized.Substring(2);
            }

            if (normalized.Length >= 3 && normalized[1] == ':' && normalized[2] == '\\')
            {
                return Prefix + normalized;
            }

            return path;
        }

        /// <summary>Whether a folder exists, also with a long path.</summary>
        /// <param name="folder">Path of the folder without prefix.</param>
        /// <returns>Whether it exists.</returns>
        public static bool DirectoryExists(string folder) => Directory.Exists(ForAccess(folder));

        /// <summary>Lists the subfolders of a folder, also with a long path.</summary>
        /// <param name="folder">Path of the folder without prefix.</param>
        /// <returns>The subfolders, without prefix.</returns>
        public static string[] GetDirectories(string folder) => Directory.GetDirectories(ForAccess(folder)).Select(WithoutPrefix).ToArray();

        /// <summary>Lists the files of a folder that match a pattern, also with a long path.</summary>
        /// <param name="folder">Path of the folder without prefix.</param>
        /// <param name="pattern">Pattern such as <c>*.label.txt</c>.</param>
        /// <returns>The files, without prefix.</returns>
        public static string[] GetFiles(string folder, string pattern) => Directory.GetFiles(ForAccess(folder), pattern).Select(WithoutPrefix).ToArray();

        /// <summary>The path without the prefix, for example from a directory listing of a long path.</summary>
        /// <param name="path">A path with or without prefix.</param>
        /// <returns>The path without prefix.</returns>
        public static string WithoutPrefix(string path)
        {
            if (path.StartsWith(UncPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return @"\\" + path.Substring(UncPrefix.Length);
            }

            return path.StartsWith(Prefix, StringComparison.Ordinal) ? path.Substring(Prefix.Length) : path;
        }
    }
}
