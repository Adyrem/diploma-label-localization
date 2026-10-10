using System;
using System.Collections.Generic;
using System.IO;
using BE.LabelExtension.Core.Models;

namespace BE.LabelExtension.Core.Elements
{
    /// <summary>
    /// Finds the element file a document open in Visual Studio belongs to, so the extension
    /// does not write into an element with unsaved changes (B25).
    /// </summary>
    /// <remarks>
    /// The designer opens the XML file of the element itself. The X++ editor opens a copy the
    /// Developer Tools put under <c>XppSource\&lt;Model&gt;\Ax&lt;Type&gt;_&lt;Name&gt;.xpp</c>, on the
    /// Unified Developer Experience as on a classic VM under
    /// <c>PackagesLocalDirectory\bin\XppSource</c>, and saving it writes the XML file.
    /// </remarks>
    public static class ElementDocuments
    {
        /// <summary>The XML file of the element a document belongs to.</summary>
        /// <param name="documentPath">Path of the open document.</param>
        /// <param name="models">The models to consider, usually the writable ones.</param>
        /// <returns>The path of the XML file, or <c>null</c> if the document is no element of these models.</returns>
        public static string? ElementFile(string? documentPath, IEnumerable<ModelInfo> models)
        {
            // Some documents have a moniker that is no path, such as those of projects.
            if (string.IsNullOrEmpty(documentPath) || documentPath!.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return null;
            }

            ModelInfo? model = ModelDiscovery.FindModelFor(documentPath, models);
            if (model == null)
            {
                return null;
            }

            string extension = Path.GetExtension(documentPath);
            if (string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase))
            {
                return IsBelow(documentPath, model.Directory) ? documentPath : null;
            }

            if (!string.Equals(extension, ".xpp", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // The type has no underscore, the name may have one: split at the first.
            string name = Path.GetFileNameWithoutExtension(documentPath);
            int separator = name.IndexOf('_');
            if (separator <= 2 || separator == name.Length - 1 || !name.StartsWith("Ax", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return Path.Combine(model.Directory, name.Substring(0, separator), name.Substring(separator + 1) + ".xml");
        }

        /// <summary>
        /// The folder XppSource the X++ editor works in: on the Unified Developer Experience the
        /// DebugSourceFolder of the metadata configuration, on a classic VM
        /// <c>PackagesLocalDirectory\bin\XppSource</c> beside the packages (D1).
        /// </summary>
        /// <param name="model">The model of the element.</param>
        /// <param name="debugSourceFolder">The DebugSourceFolder of the metadata configuration, if one is used.</param>
        /// <returns>The folder.</returns>
        public static string XppSourceFolder(ModelInfo model, string? debugSourceFolder)
            => !string.IsNullOrWhiteSpace(debugSourceFolder)
                ? debugSourceFolder!
                : Path.Combine(Path.GetDirectoryName(model.PackageDirectory) ?? model.PackageDirectory, "bin", "XppSource");

        /// <summary>
        /// The <c>.xpp</c> file the X++ editor shows for an element:
        /// <c>XppSource\&lt;Model&gt;\Ax&lt;Type&gt;_&lt;Name&gt;.xpp</c>. The Developer Tools create it
        /// when the element is opened in code, so it may not exist yet.
        /// </summary>
        /// <param name="elementFile">The XML file of the element, <c>&lt;Model&gt;\Ax&lt;Type&gt;\&lt;Name&gt;.xml</c>.</param>
        /// <param name="model">The model of the element.</param>
        /// <param name="xppSourceFolder">The folder XppSource, see <see cref="XppSourceFolder"/>.</param>
        /// <returns>The path, or <c>null</c> if the file is no element of the model.</returns>
        public static string? XppFile(string elementFile, ModelInfo model, string xppSourceFolder)
        {
            string? folder = Path.GetDirectoryName(elementFile);
            string type = Path.GetFileName(folder ?? string.Empty);
            if (folder == null
                || !type.StartsWith("Ax", StringComparison.OrdinalIgnoreCase)
                || !IsBelow(folder, model.Directory)
                || !string.Equals(Path.GetDirectoryName(folder)?.TrimEnd('\\', '/'), model.Directory.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return Path.Combine(xppSourceFolder, model.Name, $"{type}_{Path.GetFileNameWithoutExtension(elementFile)}.xpp");
        }

        private static bool IsBelow(string path, string directory)
        {
            string normalized = directory.Replace('/', '\\').TrimEnd('\\') + "\\";
            return path.Replace('/', '\\').StartsWith(normalized, StringComparison.OrdinalIgnoreCase);
        }
    }
}
