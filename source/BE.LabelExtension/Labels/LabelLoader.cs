using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Settings;
using BE.LabelExtension.Threading;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Shell;

namespace BE.LabelExtension.Labels
{
    /// <summary>
    /// Starts loading the labels in the background as soon as they are needed, with the
    /// settings of the user (FA11). Loads again when the settings change what is loaded (F4),
    /// and offers to reload when label files change from outside (FA15).
    /// </summary>
    internal sealed class LabelLoader
    {
#if DEBUG
        // Development without Dynamics 365: points the Debug build at a package directory,
        // for example the synthetic one of the unit tests.
        private const string DevelopmentDirectoryVariable = "BELABELEXTENSION_PACKAGES_DIRECTORY";
#endif

        private readonly LabelStore store;
        private readonly ModelDiscovery discovery;
        private readonly ExtensionTasks tasks;
        private readonly ErrorBoundary errorBoundary;
        private readonly IMessageSink messages;
        private readonly VisualStudioExtensibility extensibility;
        private int started;
        private int offering;

        public LabelLoader(LabelStore store, ModelDiscovery discovery, ExtensionTasks tasks, ErrorBoundary errorBoundary, IMessageSink messages, VisualStudioExtensibility extensibility)
        {
            this.store = store;
            this.discovery = discovery;
            this.tasks = tasks;
            this.errorBoundary = errorBoundary;
            this.messages = messages;
            this.extensibility = extensibility;
            this.store.ExternalChange += (_, e) => { _ = this.tasks.Factory.RunAsync(() => this.OfferReloadAsync(e)); };
            LabelSettingsFile.Saved += (_, _) => this.OnSettingsSaved();
            SharedServices.Loader = this;
        }

        /// <summary>Raised when the options page saved settings, after <see cref="Settings"/> holds them.</summary>
        public event EventHandler? SettingsChanged;

        /// <summary>The settings of the last load, with defaults for what is not set.</summary>
        public LabelSettings Settings { get; private set; } = LabelSettingsDefaults.Complete(null);

        /// <summary>The label store it loads, for the tooltip and the inline display.</summary>
        public LabelStore Store => this.store;

        /// <summary>Runs work nobody waits for, also that of the editor parts.</summary>
        public ExtensionTasks Tasks => this.tasks;

        /// <summary>The package directories of the current settings, for the usage search.</summary>
        /// <returns>The directories, those of the metadata configuration first.</returns>
        public IReadOnlyList<PackageDirectory> PackageDirectories() => this.store.GetPackageDirectories(this.Settings);

        /// <summary>
        /// The DebugSourceFolder of the metadata configuration, where the X++ editor of the
        /// Unified Developer Experience keeps its <c>.xpp</c> files; <c>null</c> without a
        /// configuration, as on a classic VM.
        /// </summary>
        /// <returns>The folder, or <c>null</c>.</returns>
        public string? DebugSourceFolder()
        {
            string? name = this.Settings.MetadataConfiguration;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            try
            {
                return this.discovery.ReadConfiguration(name!).DebugSourceFolder;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is System.Runtime.Serialization.SerializationException)
            {
                return null;
            }
        }

        /// <summary>Starts the first load unless it has started already. Returns at once.</summary>
        public void EnsureLoaded()
        {
            if (Interlocked.Exchange(ref this.started, 1) == 0)
            {
                this.Settings = this.ReadSettings();
                this.ReportUnknownLanguages(this.Settings);
                this.StartLoad();
            }
        }

        /// <summary>Loads again with the current settings. Returns at once.</summary>
        public void Reload() => this.StartLoad();

        private void StartLoad()
        {
            LabelSettings settings = this.Settings;
            _ = this.tasks.Factory.RunAsync(() => this.errorBoundary.RunAsync("Load labels", token => this.store.LoadAsync(settings, token), this.tasks.ShutdownToken));
        }

        private async Task OfferReloadAsync(LabelFilesChangedEventArgs change)
        {
            // One question at a time; further changes are covered by the same reload.
            if (Interlocked.Exchange(ref this.offering, 1) == 1)
            {
                return;
            }

            try
            {
                await this.errorBoundary.RunAsync(
                    "Offer reload",
                    async token =>
                    {
                        string files = string.Join(Environment.NewLine, change.Paths.Take(5).Select(Path.GetFileName));
                        this.messages.Report(MessageSeverity.Message, $"Label files changed outside Visual Studio:\r\n{string.Join("\r\n", change.Paths)}");
                        bool reload = await this.extensibility.Shell().ShowPromptAsync(
                            $"Label files were changed outside Visual Studio:\n{files}\n\nReload the labels? Changes not yet saved are kept.",
                            PromptOptions.OKCancel,
                            token);
                        if (reload)
                        {
                            this.Reload();
                        }
                    },
                    this.tasks.ShutdownToken);
            }
            finally
            {
                Interlocked.Exchange(ref this.offering, 0);
            }
        }

        // After the options page saved: check the language codes, and load again if what the
        // load uses changed (F4).
        private void OnSettingsSaved()
            => this.errorBoundary.Run("Apply settings", () =>
            {
                if (Volatile.Read(ref this.started) == 0)
                {
                    // Nothing is loaded yet, the first load reads the settings. The codes are
                    // checked now, while the user looks at them.
                    this.ReportUnknownLanguages(LabelSettingsDefaults.Complete(LabelSettingsFile.Load(LabelSettingsFile.DefaultPath)));
                    return;
                }

                LabelSettings next = this.ReadSettings();
                this.ReportUnknownLanguages(next);
                bool reload = this.Settings.RequiresReload(next);
                this.Settings = next;
                this.SettingsChanged?.Invoke(this, EventArgs.Empty);
                if (reload)
                {
                    this.messages.Report(MessageSeverity.Message, "The settings changed what is loaded; the labels are loaded again.");
                    this.Reload();
                }
            });

        /// <summary>
        /// Warns about language codes that Windows does not predefine, with the field of the
        /// options page they stand in (RE52). The settings stay as entered.
        /// </summary>
        private void ReportUnknownLanguages(LabelSettings settings)
        {
            var fields = new (string Field, IEnumerable<string?> Codes)[]
            {
                ("Languages to load", settings.LoadLanguages),
                ("Languages to create", settings.CreateLanguages),
                ("Source language", string.IsNullOrEmpty(settings.SourceLanguage) ? Array.Empty<string?>() : new[] { settings.SourceLanguage }),
            };
            foreach ((string field, IEnumerable<string?> codes) in fields)
            {
                foreach (string code in LanguageCodes.Unknown(codes))
                {
                    this.messages.Report(MessageSeverity.Warning, $"{field}: \"{code}\" is not a language Windows knows, such as de or de-CH. The setting is kept as entered.");
                }
            }
        }

        /// <summary>
        /// The settings of the settings file with defaults for what is not set (FA11). Without
        /// a metadata configuration set, the most recently changed one is used; without any
        /// configuration and directory, the PackagesLocalDirectory of a classic development VM
        /// (RE22).
        /// </summary>
        private LabelSettings ReadSettings()
        {
            LabelSettings? saved = null;
            try
            {
                saved = LabelSettingsFile.Load(LabelSettingsFile.DefaultPath);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is System.Runtime.Serialization.SerializationException)
            {
                this.messages.Report(MessageSeverity.Warning, $"The settings could not be read, the defaults apply: {LabelSettingsFile.DefaultPath} ({exception.Message})");
            }

            LabelSettings settings = LabelSettingsDefaults.Complete(saved);
            IReadOnlyList<string> configurations = this.discovery.ListConfigurations();
            if (settings.MetadataConfiguration != null && !configurations.Contains(settings.MetadataConfiguration, StringComparer.OrdinalIgnoreCase))
            {
                this.messages.Report(MessageSeverity.Warning, $"The metadata configuration {settings.MetadataConfiguration} of the settings does not exist any more; the most recently changed one is used.");
                settings.MetadataConfiguration = null;
            }

            if (settings.MetadataConfiguration == null)
            {
                string? newest = configurations
                    .OrderByDescending(name => File.GetLastWriteTimeUtc(Path.Combine(this.discovery.ConfigurationFolder, name + ".json")))
                    .FirstOrDefault();
                if (newest != null)
                {
                    settings.MetadataConfiguration = newest;
                    this.messages.Report(MessageSeverity.Message, $"Using the metadata configuration {newest}, the most recently changed one.");
                }
                else if (settings.PackageDirectories.Count == 0 && ModelDiscovery.FindClassicDirectory() is string classic)
                {
                    settings.PackageDirectories.Add(classic);
                    this.messages.Report(MessageSeverity.Message, $"No metadata configuration found, using {classic}.");
                }
            }

#if DEBUG
            if (Environment.GetEnvironmentVariable(DevelopmentDirectoryVariable) is string development && development.Length > 0)
            {
                settings.MetadataConfiguration = null;
                settings.PackageDirectories.Clear();
                settings.PackageDirectories.Add(development);
                this.messages.Report(MessageSeverity.Message, $"Debug build: using {development} from {DevelopmentDirectoryVariable}.");
            }
#endif

            return settings;
        }
    }
}
