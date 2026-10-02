namespace BE.LabelExtension.Core.Models
{
    /// <summary>
    /// A model found through its descriptor <c>&lt;Package&gt;\Descriptor\&lt;Model&gt;.xml</c>.
    /// </summary>
    public sealed class ModelInfo
    {
        /// <summary>Creates the description of a model.</summary>
        /// <param name="name">Name of the model, the name of its descriptor file.</param>
        /// <param name="displayName">Display name from the descriptor.</param>
        /// <param name="package">Name of the package folder.</param>
        /// <param name="layer">Layer from the descriptor.</param>
        /// <param name="isLocked">Whether the descriptor marks the model as locked.</param>
        /// <param name="isReadOnly">Result of <see cref="ModelAccessRule"/>.</param>
        /// <param name="directory">The model folder <c>&lt;Package&gt;\&lt;Model&gt;</c>.</param>
        /// <param name="packageDirectory">The package folder.</param>
        public ModelInfo(string name, string displayName, string package, ModelLayer layer, bool isLocked, bool isReadOnly, string directory, string packageDirectory)
        {
            this.Name = name;
            this.DisplayName = displayName;
            this.Package = package;
            this.Layer = layer;
            this.IsLocked = isLocked;
            this.IsReadOnly = isReadOnly;
            this.Directory = directory;
            this.PackageDirectory = packageDirectory;
        }

        /// <summary>Name of the model, the name of its descriptor file.</summary>
        public string Name { get; }

        /// <summary>Display name from the descriptor.</summary>
        public string DisplayName { get; }

        /// <summary>Name of the package folder.</summary>
        public string Package { get; }

        /// <summary>Layer from the descriptor.</summary>
        public ModelLayer Layer { get; }

        /// <summary>Whether the descriptor marks the model as locked.</summary>
        public bool IsLocked { get; }

        /// <summary>Whether labels of the model must not be changed, see <see cref="ModelAccessRule"/>.</summary>
        public bool IsReadOnly { get; }

        /// <summary>The model folder <c>&lt;Package&gt;\&lt;Model&gt;</c>.</summary>
        public string Directory { get; }

        /// <summary>The package folder.</summary>
        public string PackageDirectory { get; }

        /// <inheritdoc />
        public override string ToString() => this.Name;
    }
}
