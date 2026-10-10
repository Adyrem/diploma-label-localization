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

        /// <summary>The settings of the last load, with defaults for what is not set.</summary>
        public LabelSettings Settings { get; private set; } = LabelSettingsDefaults.Complete(null);

        /// <summary>Starts the first load unless it has started already. Returns at once.</summary>
        public void EnsureLoaded()
        {
            if (Interlocked.Exchange(ref this.started, 1) == 0)
            {
                this.Settings = this.ReadSettings();
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

        // After the options page saved: load again if what the load uses changed (F4).
        private void OnSettingsSaved()
            => this.errorBoundary.Run("Apply settings", () =>
            {
                if (Volatile.Read(ref this.started) == 0)
                {
                    return;
                }

                LabelSettings next = this.ReadSettings();
                bool reload = this.Settings.RequiresReload(next);
                this.Settings = next;
                if (reload)
                {
                    this.messages.Report(MessageSeverity.Message, "The settings changed what is loaded; the labels are loaded again.");
                    this.Reload();
                }
            });

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
