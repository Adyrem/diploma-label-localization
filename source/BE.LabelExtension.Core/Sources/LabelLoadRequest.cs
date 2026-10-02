using System;
using System.Collections.Generic;
using System.Linq;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Models;

namespace BE.LabelExtension.Core.Sources
{
    /// <summary>
    /// What a <see cref="ILabelSource"/> loads.
    /// </summary>
    public sealed class LabelLoadRequest
    {
        private readonly HashSet<string> languages;

        /// <summary>Creates a request.</summary>
        /// <param name="models">The models found in the package directories.</param>
        /// <param name="directories">The package directories.</param>
        /// <param name="languages">The languages to load.</param>
        /// <param name="messages">Receives warnings about single files.</param>
        public LabelLoadRequest(IReadOnlyList<ModelInfo> models, IReadOnlyList<PackageDirectory> directories, IEnumerable<string> languages, IMessageSink messages)
        {
            this.Models = models;
            this.Directories = directories;
            this.languages = new HashSet<string>(languages, StringComparer.OrdinalIgnoreCase);
            this.Messages = messages;
        }

        /// <summary>The models found in the package directories.</summary>
        public IReadOnlyList<ModelInfo> Models { get; }

        /// <summary>The package directories.</summary>
        public IReadOnlyList<PackageDirectory> Directories { get; }

        /// <summary>The languages to load.</summary>
        public IReadOnlyCollection<string> Languages => this.languages;

        /// <summary>Receives warnings about single files.</summary>
        public IMessageSink Messages { get; }

        /// <summary>Whether a language is to be loaded, ignoring case.</summary>
        /// <param name="language">The language.</param>
        /// <returns>Whether it is requested.</returns>
        public bool IsRequested(string language) => this.languages.Contains(language);

        /// <summary>Returns the model whose package folder contains the path, the deepest one.</summary>
        /// <param name="path">Path of a file.</param>
        /// <returns>The model, or <c>null</c>.</returns>
        public ModelInfo? FindModelInPackage(string path)
            => this.Models
                .Where(m => path.StartsWith(m.PackageDirectory.TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => string.Equals(m.Name, m.Package, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .FirstOrDefault();
    }
}
