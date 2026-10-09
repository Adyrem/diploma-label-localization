using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Files;
using BE.LabelExtension.Core.Labels;
using BE.LabelExtension.Core.Models;

namespace BE.LabelExtension.Core.Sources
{
    /// <summary>
    /// Reads the label files of the models,
    /// <c>&lt;Model&gt;\AxLabelFile\LabelResources\&lt;Language&gt;\&lt;Label file&gt;.&lt;Language&gt;.label.txt</c>.
    /// </summary>
    /// <remarks>
    /// The language follows from the folder and the file name, not from the XML description
    /// of the label file, which lacks the element Language for en-US.
    /// </remarks>
    public sealed class LabelFileSource : ILabelSource
    {
        private const string Suffix = ".label.txt";

        /// <inheritdoc />
        public LabelSourceResult Load(LabelLoadRequest request, CancellationToken cancellationToken)
        {
            var labelFiles = new List<LabelFile>();
            var documents = new List<LabelDocument>();
            foreach (ModelInfo model in request.Models)
            {
                string resources = Path.Combine(model.Directory, "AxLabelFile", "LabelResources");
                if (!LongPath.DirectoryExists(resources))
                {
                    continue;
                }

                try
                {
                    LoadModel(model, resources, request, labelFiles, documents, cancellationToken);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    request.Messages.Report(MessageSeverity.Warning, $"Label files of model {model.Name} could not be read: {resources} ({exception.Message})");
                }
            }

            return new LabelSourceResult(labelFiles, documents);
        }

        private static void LoadModel(ModelInfo model, string resources, LabelLoadRequest request, List<LabelFile> found, List<LabelDocument> documents, CancellationToken cancellationToken)
        {
            var labelFiles = new Dictionary<string, LabelFile>(StringComparer.OrdinalIgnoreCase);
            foreach (string languageFolder in LongPath.GetDirectories(resources))
            {
                string language = Path.GetFileName(languageFolder);
                foreach (string path in LongPath.GetFiles(languageFolder, "*" + Suffix))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string fileName = Path.GetFileName(path);
                    string withoutSuffix = fileName.Substring(0, fileName.Length - Suffix.Length);
                    int dot = withoutSuffix.LastIndexOf('.');
                    if (dot <= 0 || !string.Equals(withoutSuffix.Substring(dot + 1), language, StringComparison.OrdinalIgnoreCase))
                    {
                        request.Messages.Report(MessageSeverity.Warning, $"Label file name does not match its language folder {language} and is skipped: {path}");
                        continue;
                    }

                    string name = withoutSuffix.Substring(0, dot);
                    if (!labelFiles.TryGetValue(name, out LabelFile? labelFile))
                    {
                        labelFile = new LabelFile(name, model, isCompiled: false);
                        labelFiles.Add(name, labelFile);
                        found.Add(labelFile);
                    }

                    labelFile.AddLanguage(language, path);
                    if (request.IsRequested(language))
                    {
                        LabelDocument? document = Read(labelFile, language, path, request.Messages);
                        if (document != null)
                        {
                            documents.Add(document);
                        }
                    }
                }
            }
        }

        private static LabelDocument? Read(LabelFile labelFile, string language, string path, IMessageSink messages)
        {
            LabelFileContent content;
            try
            {
                content = LabelFileFormat.Read(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                messages.Report(MessageSeverity.Warning, $"Label file could not be read and is skipped: {path} ({exception.Message})");
                return null;
            }

            if (content.IsDamaged)
            {
                IEnumerable<LabelFileIssue> invalid = content.Issues.Where(i => i.Kind == LabelFileIssueKind.InvalidLine).Take(3);
                messages.Report(MessageSeverity.Warning, $"Label file is damaged and is skipped: {path}\r\n{string.Join("\r\n", invalid)}");
                return null;
            }

            if (content.Issues.Count > 0)
            {
                messages.Report(MessageSeverity.Warning, $"Label file {path}:\r\n{string.Join("\r\n", content.Issues)}");
                if (!labelFile.IsReadOnly)
                {
                    labelFile.MarkForCleanup(language);
                }
            }

            return new LabelDocument(labelFile, language, path, content.Entries);
        }
    }
}
