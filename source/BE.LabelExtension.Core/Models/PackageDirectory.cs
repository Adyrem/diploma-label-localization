namespace BE.LabelExtension.Core.Models
{
    /// <summary>
    /// A directory with packages, such as a PackagesLocalDirectory or the folder of the own
    /// models of the Unified Developer Experience.
    /// </summary>
    public sealed class PackageDirectory
    {
        /// <summary>Creates a package directory.</summary>
        /// <param name="path">Full path of the directory.</param>
        /// <param name="isReference">
        /// Whether it holds reference metadata, from FrameworkDirectory or ReferencePackagesPaths.
        /// Everything in it is read-only.
        /// </param>
        public PackageDirectory(string path, bool isReference)
        {
            this.Path = path;
            this.IsReference = isReference;
        }

        /// <summary>Full path of the directory.</summary>
        public string Path { get; }

        /// <summary>Whether it holds reference metadata. Everything in it is read-only.</summary>
        public bool IsReference { get; }

        /// <inheritdoc />
        public override string ToString() => this.Path;
    }
}
