using System;
using System.Collections.Generic;
using System.Linq;
using BE.LabelExtension.Core.Models;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// A label file such as <c>BDM1</c>, stored as one file per language. It knows the
    /// languages it exists in, also those that are not loaded.
    /// </summary>
    public sealed class LabelFile
    {
        private const string ExtensionSuffix = "_Extension";

        private readonly Dictionary<string, string> paths = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> needsCleanup = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Creates a label file.</summary>
        /// <param name="name">Name as on disk, for example <c>BDM1</c> or <c>BDM1_Extension</c>.</param>
        /// <param name="model">The model the label file belongs to.</param>
        /// <param name="isCompiled">Whether it is read from compiled resources only.</param>
        public LabelFile(string name, ModelInfo model, bool isCompiled)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Model = model ?? throw new ArgumentNullException(nameof(model));
            this.IsCompiled = isCompiled;
            this.IdPrefix = GetIdPrefix(name);
        }

        /// <summary>Name as on disk, for example <c>BDM1</c> or <c>BDM1_Extension</c>.</summary>
        public string Name { get; }

        /// <summary>
        /// Label file part of the IDs in the new form. For a name ending in <c>_Extension</c>
        /// it is the part before, so the file <c>ABC_Extension</c> holds IDs <c>@ABC:...</c>,
        /// as in the existing tool.
        /// </summary>
        public string IdPrefix { get; }

        /// <summary>The model the label file belongs to.</summary>
        public ModelInfo Model { get; }

        /// <summary>Whether it is read from compiled resources only.</summary>
        public bool IsCompiled { get; }

        /// <summary>
        /// Whether the labels of this file must not be changed: its model is read-only or the
        /// file exists only in compiled form.
        /// </summary>
        public bool IsReadOnly => this.IsCompiled || this.Model.IsReadOnly;

        /// <summary>The languages the label file exists in, sorted.</summary>
        public IReadOnlyList<string> Languages
        {
            get
            {
                lock (this.paths)
                {
                    return this.paths.Keys.OrderBy(l => l, StringComparer.OrdinalIgnoreCase).ToList();
                }
            }
        }

        /// <summary>
        /// Languages whose file the next save changes even without edits, because reading found
        /// duplicate IDs or further comment lines.
        /// </summary>
        public IReadOnlyList<string> LanguagesNeedingCleanup
        {
            get
            {
                lock (this.paths)
                {
                    return this.needsCleanup.ToList();
                }
            }
        }

        /// <summary>
        /// Returns the label file part of the IDs for a label file name, see <see cref="IdPrefix"/>.
        /// </summary>
        /// <param name="name">Name of the label file.</param>
        /// <returns>The name without a trailing <c>_Extension</c>.</returns>
        public static string GetIdPrefix(string name)
            => name.Length > ExtensionSuffix.Length && name.EndsWith(ExtensionSuffix, StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - ExtensionSuffix.Length)
                : name;

        /// <summary>Returns the path of the file of one language.</summary>
        /// <param name="language">The language.</param>
        /// <returns>The path, or <c>null</c> if the label file does not exist in this language.</returns>
        public string? GetPath(string language)
        {
            lock (this.paths)
            {
                return this.paths.TryGetValue(language, out string? path) ? path : null;
            }
        }

        /// <summary>Records the file of one language.</summary>
        /// <param name="language">The language.</param>
        /// <param name="path">Path of the file.</param>
        public void AddLanguage(string language, string path)
        {
            lock (this.paths)
            {
                this.paths[language] = path;
            }
        }

        /// <summary>Marks the file of one language for cleanup on the next save.</summary>
        /// <param name="language">The language.</param>
        public void MarkForCleanup(string language)
        {
            lock (this.paths)
            {
                this.needsCleanup.Add(language);
            }
        }

        /// <inheritdoc />
        public override string ToString() => this.Name;
    }
}
