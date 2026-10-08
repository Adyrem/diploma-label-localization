using System;
using System.Collections.Generic;
using System.Linq;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;

namespace BE.LabelExtension.Core.Sources
{
    /// <summary>
    /// What a <see cref="ILabelSource"/> loads.
    /// </summary>
    public sealed class LabelLoadRequest
    {
        private readonly HashSet<string> languages;
        private readonly HashSet<string> textLabelFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

        /// <summary>
        /// Records the label files a source read from <c>.label.txt</c> files. A later source
        /// can then skip compiled resources that the label store would drop anyway.
        /// </summary>
        /// <param name="labelFiles">The label files of a source; compiled ones are ignored.</param>
        public void AddTextLabelFiles(IEnumerable<LabelFile> labelFiles)
        {
            foreach (LabelFile labelFile in labelFiles.Where(f => !f.IsCompiled))
            {
                foreach (string language in labelFile.Languages)
                {
                    this.textLabelFiles.Add(TextKey(labelFile.Name, language));
                }
            }
        }

        /// <summary>
        /// Whether a <c>.label.txt</c> exists for this label file and language, ignoring case.
        /// It wins over the compiled resources of the same label file and language.
        /// </summary>
        /// <param name="labelFile">Name of the label file.</param>
        /// <param name="language">The language.</param>
        /// <returns>Whether a source recorded such a file.</returns>
        public bool HasTextLabelFile(string labelFile, string language) => this.textLabelFiles.Contains(TextKey(labelFile, language));

        /// <summary>Returns the model whose package folder contains the path, the deepest one.</summary>
        /// <param name="path">Path of a file.</param>
        /// <returns>The model, or <c>null</c>.</returns>
        public ModelInfo? FindModelInPackage(string path)
            => this.Models
                .Where(m => path.StartsWith(m.PackageDirectory.TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => string.Equals(m.Name, m.Package, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .FirstOrDefault();

        private static string TextKey(string labelFile, string language) => labelFile + "|" + language;
    }
}
