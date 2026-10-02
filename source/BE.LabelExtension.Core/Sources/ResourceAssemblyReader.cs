using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Resources;
using BE.LabelExtension.Core.Labels;

namespace BE.LabelExtension.Core.Sources
{
    /// <summary>
    /// Reads the first manifest resource of a resource assembly directly from the file,
    /// without loading the assembly. Inside Visual Studio a loaded assembly would stay loaded
    /// until Visual Studio closes, and loading it again would return the old version.
    /// </summary>
    internal static class ResourceAssemblyReader
    {
        /// <summary>Reads the pairs of ID and text of the first manifest resource.</summary>
        /// <param name="path">Path of the resource assembly.</param>
        /// <returns>The entries, without comments; empty if the assembly has no resource.</returns>
        /// <exception cref="BadImageFormatException">The file is no .NET assembly or the resource cannot be read.</exception>
        public static IReadOnlyList<LabelEntry> ReadFirstResource(string path)
        {
            // Reading the bytes first leaves the file free; it can be replaced right afterwards.
            byte[] bytes = File.ReadAllBytes(path);
            using var peReader = new PEReader(ImmutableArray.Create(bytes));
            if (!peReader.HasMetadata || peReader.PEHeaders.CorHeader == null)
            {
                throw new BadImageFormatException("The file is no .NET assembly.", path);
            }

            MetadataReader metadata = peReader.GetMetadataReader();
            ManifestResourceHandle? first = null;
            foreach (ManifestResourceHandle handle in metadata.ManifestResources)
            {
                first = handle;
                break;
            }

            var entries = new List<LabelEntry>();
            if (first == null)
            {
                return entries;
            }

            ManifestResource resource = metadata.GetManifestResource(first.Value);
            if (!resource.Implementation.IsNil)
            {
                throw new BadImageFormatException("The first resource is not embedded in the assembly.", path);
            }

            // Embedded resources lie in the resources directory: a 32-bit length, then the data.
            DirectoryEntry directory = peReader.PEHeaders.CorHeader.ResourcesDirectory;
            PEMemoryBlock block = peReader.GetSectionData(directory.RelativeVirtualAddress);
            if (resource.Offset < 0 || resource.Offset + sizeof(int) > Math.Min(block.Length, directory.Size))
            {
                throw new BadImageFormatException("The resource lies outside the resources directory.", path);
            }

            BlobReader reader = block.GetReader((int)resource.Offset, block.Length - (int)resource.Offset);
            int length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(length);

            using var resources = new ResourceReader(new MemoryStream(data, writable: false));
            foreach (DictionaryEntry entry in resources)
            {
                if (entry.Key is string key && entry.Value is string text)
                {
                    entries.Add(new LabelEntry(key, text, comment: null));
                }
            }

            return entries;
        }
    }
}
