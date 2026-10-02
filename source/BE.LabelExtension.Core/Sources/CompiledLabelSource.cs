using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;

namespace BE.LabelExtension.Core.Sources
{
    /// <summary>
    /// Reads labels that exist only in compiled form, from the files
    /// <c>&lt;Label file&gt;.Resources.dll</c>. As in the existing tool, the package directories
    /// are searched recursively. The language is the name of the folder holding the file, the
    /// label file is the file name up to the first dot. All such labels are read-only.
    /// </summary>
    /// <remarks>
    /// Where a <c>.label.txt</c> exists for the same label file and language, it wins; the
    /// label store leaves out the compiled document then.
    /// </remarks>
    public sealed class CompiledLabelSource : ILabelSource
    {
        private const string Pattern = "*.Resources.dll";

        /// <inheritdoc />
        public LabelSourceResult Load(LabelLoadRequest request, CancellationToken cancellationToken)
        {
            var labelFiles = new Dictionary<string, LabelFile>(StringComparer.OrdinalIgnoreCase);
            var documents = new List<LabelDocument>();
            foreach (PackageDirectory directory in request.Directories)
            {
                if (!Directory.Exists(directory.Path))
                {
                    continue;
                }

                foreach (string path in FindFiles(directory.Path, request.Messages, cancellationToken))
                {
                    string language = Path.GetFileName(Path.GetDirectoryName(path)) ?? string.Empty;
                    string name = Path.GetFileName(path).Split('.')[0];
                    ModelInfo model = request.FindModelInPackage(path) ?? PackageModel(directory, path);

                    string key = model.PackageDirectory + "|" + name;
                    if (!labelFiles.TryGetValue(key, out LabelFile? labelFile))
                    {
                        labelFile = new LabelFile(name, model, isCompiled: true);
                        labelFiles.Add(key, labelFile);
                    }

                    labelFile.AddLanguage(language, path);
                    if (!request.IsRequested(language))
                    {
                        continue;
                    }

                    try
                    {
                        documents.Add(new LabelDocument(labelFile, language, path, ResourceAssemblyReader.ReadFirstResource(path)));
                    }
                    catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is BadImageFormatException || exception is InvalidOperationException)
                    {
                        request.Messages.Report(MessageSeverity.Warning, $"Compiled labels could not be read and are skipped: {path} ({exception.Message})");
                    }
                }
            }

            return new LabelSourceResult(labelFiles.Values.ToList(), documents);
        }

        // A package without a descriptor, as with compiled packages from other vendors.
        private static ModelInfo PackageModel(PackageDirectory directory, string path)
        {
            string relative = path.Substring(directory.Path.TrimEnd('\\', '/').Length).TrimStart('\\', '/');
            string package = relative.Split('\\', '/')[0];
            string packageDirectory = Path.Combine(directory.Path, package);
            return new ModelInfo(package, package, package, ModelLayer.SYS, isLocked: false, isReadOnly: true, packageDirectory, packageDirectory);
        }

        // Recursive search that skips folders it may not read instead of failing as a whole.
        private static IEnumerable<string> FindFiles(string root, IMessageSink messages, CancellationToken cancellationToken)
        {
            var pending = new Stack<string>();
            pending.Push(root);
            int skipped = 0;
            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string folder = pending.Pop();
                string[] files;
                string[] folders;
                try
                {
                    files = Directory.GetFiles(folder, Pattern);
                    folders = Directory.GetDirectories(folder);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    skipped++;
                    continue;
                }

                foreach (string file in files)
                {
                    yield return file;
                }

                foreach (string child in folders)
                {
                    pending.Push(child);
                }
            }

            if (skipped > 0)
            {
                messages.Report(MessageSeverity.Warning, $"{skipped} folders below {root} could not be searched for compiled labels.");
            }
        }
    }
}
