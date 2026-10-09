using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using BE.LabelExtension.Core.Diagnostics;

namespace BE.LabelExtension.Core.Models
{
    /// <summary>
    /// Finds the package directories through the metadata configuration and the models in
    /// them through their descriptors, and assigns a file to its model.
    /// </summary>
    public sealed class ModelDiscovery
    {
        /// <summary>Where a drive of the classic development VM holds the PackagesLocalDirectory.</summary>
        public const string ClassicFolder = @"AOSService\PackagesLocalDirectory";

        private const string XppSourceFolder = "XppSource";

        /// <summary>Creates the discovery.</summary>
        /// <param name="configurationFolder">
        /// Folder of the metadata configurations, usually <see cref="MetadataConfiguration.DefaultFolder"/>.
        /// </param>
        public ModelDiscovery(string configurationFolder)
        {
            this.ConfigurationFolder = configurationFolder;
        }

        /// <summary>Folder of the metadata configurations.</summary>
        public string ConfigurationFolder { get; }

        /// <summary>Lists the names of the metadata configurations.</summary>
        /// <returns>The names, sorted; empty on a classic development VM.</returns>
        public IReadOnlyList<string> ListConfigurations() => MetadataConfiguration.List(this.ConfigurationFolder);

        /// <summary>Reads a metadata configuration, anew on every call.</summary>
        /// <param name="name">Name of the configuration.</param>
        /// <returns>The configuration.</returns>
        public MetadataConfiguration ReadConfiguration(string name) => MetadataConfiguration.Read(this.ConfigurationFolder, name);

        /// <summary>
        /// Returns the PackagesLocalDirectory of a classic development VM, for when there is no
        /// metadata configuration: K: first, as in the existing tool, then the other drives in
        /// alphabetical order. Only a directory that holds a package with a descriptor counts.
        /// On the test environment an empty one lay on C: and the real one on J:.
        /// </summary>
        /// <param name="drives">Root folders of the drives; the fixed drives if <c>null</c>.</param>
        /// <param name="hasPackages">Checks whether a directory holds packages; replaceable for tests.</param>
        /// <returns>The directory, or <c>null</c> if no drive has one with packages.</returns>
        public static string? FindClassicDirectory(IEnumerable<string>? drives = null, Func<string, bool>? hasPackages = null)
            => (drives ?? FixedDrives())
                .OrderBy(drive => drive.StartsWith("K", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(drive => drive, StringComparer.OrdinalIgnoreCase)
                .Select(drive => Path.Combine(drive, ClassicFolder))
                .FirstOrDefault(hasPackages ?? HasPackages);

        /// <summary>Whether a directory holds at least one package with a model descriptor.</summary>
        /// <param name="directory">The directory.</param>
        /// <returns>Whether it holds packages; <c>false</c> if it cannot be read.</returns>
        public static bool HasPackages(string directory)
        {
            try
            {
                return Directory.Exists(directory)
                    && Directory.EnumerateDirectories(directory).Any(package =>
                    {
                        string descriptorFolder = Path.Combine(package, "Descriptor");
                        return Directory.Exists(descriptorFolder) && Directory.EnumerateFiles(descriptorFolder, "*.xml").Any();
                    });
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Finds the models in the package directories. A model is a descriptor
        /// <c>&lt;Package&gt;\Descriptor\&lt;Model&gt;.xml</c> with the elements Id, ModelModule,
        /// DisplayName and Layer; its folder is <c>&lt;Package&gt;\&lt;Model&gt;</c>.
        /// </summary>
        /// <param name="directories">The package directories, own models first.</param>
        /// <param name="messages">Receives a warning for every missing directory and unreadable descriptor.</param>
        /// <param name="cancellationToken">Cancels the search.</param>
        /// <returns>The models in the order of the directories. A model name found again later is skipped.</returns>
        public IReadOnlyList<ModelInfo> FindModels(IEnumerable<PackageDirectory> directories, IMessageSink messages, CancellationToken cancellationToken)
        {
            var models = new List<ModelInfo>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PackageDirectory directory in directories)
            {
                if (!Directory.Exists(directory.Path))
                {
                    messages.Report(MessageSeverity.Warning, $"Package directory not found: {directory.Path}");
                    continue;
                }

                foreach (string package in SafeGetDirectories(directory.Path, messages))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string descriptorFolder = Path.Combine(package, "Descriptor");
                    if (!Directory.Exists(descriptorFolder))
                    {
                        continue;
                    }

                    foreach (string descriptor in Directory.GetFiles(descriptorFolder, "*.xml"))
                    {
                        ModelInfo? model = ReadDescriptor(descriptor, package, directory.IsReference, messages);
                        if (model == null)
                        {
                            continue;
                        }

                        if (names.Add(model.Name))
                        {
                            models.Add(model);
                        }
                        else
                        {
                            messages.Report(MessageSeverity.Warning, $"Model {model.Name} appears again in {package}; the first one is used.");
                        }
                    }
                }
            }

            return models;
        }

        /// <summary>
        /// Finds the model a file belongs to, for the label file to suggest when extracting a
        /// text. Understands paths in the package directories and in XppSource, where the X++
        /// editor of the Unified Developer Experience works.
        /// </summary>
        /// <param name="filePath">Path of an element file or a <c>.xpp</c> file.</param>
        /// <param name="models">The known models.</param>
        /// <returns>The model, or <c>null</c> if the file belongs to none.</returns>
        public static ModelInfo? FindModelFor(string filePath, IEnumerable<ModelInfo> models)
        {
            string path = Normalize(filePath);
            List<ModelInfo> candidates = models.ToList();

            ModelInfo? byFolder = candidates
                .Where(m => path.StartsWith(Normalize(m.Directory) + "/", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.Directory.Length)
                .FirstOrDefault();
            if (byFolder != null)
            {
                return byFolder;
            }

            // <...>\XppSource\<Model>\AxClass_<Name>.xpp
            string[] segments = path.Split('/');
            int xppSource = Array.FindLastIndex(segments, s => string.Equals(s, XppSourceFolder, StringComparison.OrdinalIgnoreCase));
            if (xppSource >= 0 && xppSource + 2 < segments.Length)
            {
                string modelName = segments[xppSource + 1];
                return candidates.FirstOrDefault(m => string.Equals(m.Name, modelName, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private static ModelInfo? ReadDescriptor(string path, string package, bool isReference, IMessageSink messages)
        {
            XElement root;
            try
            {
                using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
                root = XDocument.Load(reader).Root!;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is XmlException)
            {
                messages.Report(MessageSeverity.Warning, $"Model descriptor could not be read: {path} ({exception.Message})");
                return null;
            }

            string? Element(string name) => root.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;

            string? id = Element("Id");
            string? module = Element("ModelModule");
            string? displayName = Element("DisplayName");
            string? layerText = Element("Layer");
            if (id == null || module == null || displayName == null || layerText == null)
            {
                return null;
            }

            if (!int.TryParse(layerText.Trim(), out int layerNumber) || !Enum.IsDefined(typeof(ModelLayer), layerNumber))
            {
                messages.Report(MessageSeverity.Warning, $"Model descriptor has an unknown layer '{layerText}' and is skipped: {path}");
                return null;
            }

            // A missing Locked means not locked. A value that cannot be read counts as locked,
            // so the extension never writes into a model it does not understand.
            string? lockedText = Element("Locked");
            bool isLocked = false;
            if (lockedText != null && !bool.TryParse(lockedText.Trim(), out isLocked))
            {
                messages.Report(MessageSeverity.Warning, $"Model descriptor has an unknown value '{lockedText}' for Locked, the model is treated as locked: {path}");
                isLocked = true;
            }

            var layer = (ModelLayer)layerNumber;
            string name = Path.GetFileNameWithoutExtension(path);
            return new ModelInfo(
                name,
                displayName,
                Path.GetFileName(package),
                layer,
                isLocked,
                ModelAccessRule.IsReadOnly(layer, isLocked, isReference),
                Path.Combine(package, name),
                package);
        }

        private static IEnumerable<string> SafeGetDirectories(string path, IMessageSink messages)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                messages.Report(MessageSeverity.Warning, $"Package directory could not be read: {path} ({exception.Message})");
                return Array.Empty<string>();
            }
        }

        private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');

        private static IEnumerable<string> FixedDrives()
        {
            try
            {
                return DriveInfo.GetDrives()
                    .Where(drive => drive.DriveType == DriveType.Fixed && drive.IsReady)
                    .Select(drive => drive.RootDirectory.FullName)
                    .ToList();
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }
    }
}
