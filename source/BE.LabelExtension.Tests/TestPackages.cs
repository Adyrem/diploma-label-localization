using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace BE.LabelExtension.Tests
{
    /// <summary>
    /// A copy of the synthetic package directory in a temporary folder, which a test may
    /// change. Also writes metadata configurations and compiled label resources.
    /// </summary>
    internal sealed class TestPackages : IDisposable
    {
        public TestPackages()
        {
            this.Root = Path.Combine(Path.GetTempPath(), "BE.LabelExtension.Tests", Guid.NewGuid().ToString("N"));
            CopyDirectory(Path.Combine(AppContext.BaseDirectory, "TestData", "PackagesLocalDirectory"), this.PackagesDirectory);
        }

        /// <summary>The temporary folder.</summary>
        public string Root { get; }

        /// <summary>The copy of the synthetic PackagesLocalDirectory.</summary>
        public string PackagesDirectory => Path.Combine(this.Root, "PackagesLocalDirectory");

        /// <summary>Folder for metadata configurations.</summary>
        public string ConfigurationFolder => Path.Combine(this.Root, "XPPConfig");

        public string ModelDirectory(string package, string model) => Path.Combine(this.PackagesDirectory, package, model);

        public string LabelFilePath(string package, string model, string labelFile, string language)
            => Path.Combine(this.ModelDirectory(package, model), "AxLabelFile", "LabelResources", language, $"{labelFile}.{language}.label.txt");

        /// <summary>Writes a metadata configuration as the Developer Tools do.</summary>
        public void WriteConfiguration(string name, string? modelStoreFolder, string? frameworkDirectory, params string[] referencePackagesPaths)
        {
            static string Json(string? value) => value == null ? "null" : "\"" + value.Replace("\\", "\\\\") + "\"";

            var references = new List<string>();
            foreach (string path in referencePackagesPaths)
            {
                references.Add(Json(path));
            }

            string json = "{\r\n"
                + $"  \"ModelStoreFolder\": {Json(modelStoreFolder)},\r\n"
                + $"  \"DebugSourceFolder\": {Json(modelStoreFolder == null ? null : Path.Combine(modelStoreFolder, "XppSource"))},\r\n"
                + $"  \"FrameworkDirectory\": {Json(frameworkDirectory)},\r\n"
                + $"  \"ReferencePackagesPaths\": [ {string.Join(", ", references)} ],\r\n"
                + "  \"SomeOtherSetting\": true\r\n"
                + "}\r\n";
            Directory.CreateDirectory(this.ConfigurationFolder);
            File.WriteAllText(Path.Combine(this.ConfigurationFolder, name + ".json"), json, new UTF8Encoding(true));
        }

        /// <summary>
        /// Writes a resource assembly <c>&lt;labelFile&gt;.Resources.dll</c> whose first manifest
        /// resource holds the given pairs of ID and text.
        /// </summary>
        public string WriteCompiledLabels(string folder, string labelFile, string language, IReadOnlyDictionary<string, string> labels)
        {
            byte[] resources;
            using (var stream = new MemoryStream())
            {
                using (var writer = new ResourceWriter(stream))
                {
                    foreach (KeyValuePair<string, string> label in labels)
                    {
                        writer.AddResource(label.Key, label.Value);
                    }

                    writer.Generate();
                    resources = stream.ToArray();
                }
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                labelFile + ".Resources",
                syntaxTrees: null,
                references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            string target = Path.Combine(folder, language);
            Directory.CreateDirectory(target);
            string path = Path.Combine(target, labelFile + ".Resources.dll");
            using (FileStream file = File.Create(path))
            {
                var resource = new ResourceDescription($"{labelFile}.{language}.resources", () => new MemoryStream(resources), isPublic: true);
                EmitResult result = compilation.Emit(file, manifestResources: new[] { resource });
                if (!result.Success)
                {
                    throw new InvalidOperationException("The resource assembly could not be built.");
                }
            }

            return path;
        }

        public void Dispose()
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (Directory.Exists(this.Root))
                    {
                        Directory.Delete(this.Root, recursive: true);
                    }

                    return;
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    Thread.Sleep(200);
                }
            }
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
            }

            foreach (string folder in Directory.GetDirectories(source))
            {
                CopyDirectory(folder, Path.Combine(target, Path.GetFileName(folder)));
            }
        }
    }
}
