using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace BE.LabelExtension.Core.Models
{
    /// <summary>
    /// A metadata configuration of the Developer Tools in the Unified Developer Experience,
    /// one JSON file in <c>%LOCALAPPDATA%\Microsoft\Dynamics365\XPPConfig</c>. The file name
    /// without extension is the name of the configuration.
    /// </summary>
    public sealed class MetadataConfiguration
    {
        private MetadataConfiguration(string name, string? modelStoreFolder, string? debugSourceFolder, string? frameworkDirectory, IReadOnlyList<string> referencePackagesPaths)
        {
            this.Name = name;
            this.ModelStoreFolder = modelStoreFolder;
            this.DebugSourceFolder = debugSourceFolder;
            this.FrameworkDirectory = frameworkDirectory;
            this.ReferencePackagesPaths = referencePackagesPaths;
        }

        /// <summary>The folder holding the configurations of the current user.</summary>
        public static string DefaultFolder { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Dynamics365", "XPPConfig");

        /// <summary>Name of the configuration, the file name without extension.</summary>
        public string Name { get; }

        /// <summary>Folder of the own models.</summary>
        public string? ModelStoreFolder { get; }

        /// <summary>Folder of the source files the X++ editor works on.</summary>
        public string? DebugSourceFolder { get; }

        /// <summary>Folder with the reference metadata of the platform.</summary>
        public string? FrameworkDirectory { get; }

        /// <summary>Further folders with reference metadata.</summary>
        public IReadOnlyList<string> ReferencePackagesPaths { get; }

        /// <summary>
        /// The package directories of this configuration: first the own models, then the
        /// reference directories, which are read-only. Each directory appears once.
        /// </summary>
        /// <returns>The package directories in this order.</returns>
        public IReadOnlyList<PackageDirectory> GetPackageDirectories()
        {
            var result = new List<PackageDirectory>();
            void Add(string? path, bool isReference)
            {
                if (!string.IsNullOrWhiteSpace(path) && !result.Any(d => PathsEqual(d.Path, path!)))
                {
                    result.Add(new PackageDirectory(path!, isReference));
                }
            }

            Add(this.ModelStoreFolder, isReference: false);
            Add(this.FrameworkDirectory, isReference: true);
            foreach (string path in this.ReferencePackagesPaths)
            {
                Add(path, isReference: true);
            }

            return result;
        }

        /// <summary>Lists the names of the configurations in a folder.</summary>
        /// <param name="folder">The folder, usually <see cref="DefaultFolder"/>.</param>
        /// <returns>The names, sorted; empty if the folder does not exist.</returns>
        public static IReadOnlyList<string> List(string folder)
            => Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList()
                : (IReadOnlyList<string>)Array.Empty<string>();

        /// <summary>
        /// Reads a configuration. It is read anew on every load, because names and paths
        /// change with updates.
        /// </summary>
        /// <param name="folder">The folder, usually <see cref="DefaultFolder"/>.</param>
        /// <param name="name">Name of the configuration.</param>
        /// <returns>The configuration.</returns>
        /// <exception cref="IOException">The file cannot be read.</exception>
        /// <exception cref="SerializationException">The file is not valid JSON of a configuration.</exception>
        public static MetadataConfiguration Read(string folder, string name)
        {
            // Read as text first: the serializer fails on a byte order mark.
            string json;
            using (var reader = new StreamReader(new FileStream(Path.Combine(folder, name + ".json"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite), detectEncodingFromByteOrderMarks: true))
            {
                json = reader.ReadToEnd();
            }

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var serializer = new DataContractJsonSerializer(typeof(ConfigurationFile));
            var file = (ConfigurationFile?)serializer.ReadObject(stream) ?? new ConfigurationFile();
            return new MetadataConfiguration(
                name,
                file.ModelStoreFolder,
                file.DebugSourceFolder,
                file.FrameworkDirectory,
                (file.ReferencePackagesPaths ?? Array.Empty<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList());
        }

        internal static bool PathsEqual(string left, string right)
            => string.Equals(left.TrimEnd('\\', '/'), right.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

        [DataContract]
        private sealed class ConfigurationFile
        {
            [DataMember(IsRequired = false)]
            public string? ModelStoreFolder { get; set; }

            [DataMember(IsRequired = false)]
            public string? DebugSourceFolder { get; set; }

            [DataMember(IsRequired = false)]
            public string? FrameworkDirectory { get; set; }

            [DataMember(IsRequired = false)]
            public string[]? ReferencePackagesPaths { get; set; }
        }
    }
}
