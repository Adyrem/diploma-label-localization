using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Models;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Threading;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Shell;

namespace BE.LabelExtension.Labels
{
    /// <summary>
    /// Starts loading the labels in the background as soon as they are needed, and offers to
    /// reload when label files change from outside (FA15).
    /// </summary>
    internal sealed class LabelLoader
    {
        // Until the settings page exists (AP3.5), these languages are loaded.
        private static readonly string[] DefaultLanguages = { "en-US", "de", "de-CH", "fr-CH", "it-CH" };

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
        }

        /// <summary>The settings of the last load.</summary>
        public LabelSettings Settings { get; private set; } = new();

        /// <summary>Starts the first load unless it has started already. Returns at once.</summary>
        public void EnsureLoaded()
        {
            if (Interlocked.Exchange(ref this.started, 1) == 0)
            {
                this.Settings = this.CreateDefaultSettings();
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

        /// <summary>
        /// The simple default until the settings page exists (AP3.5): the newest metadata
        /// configuration, otherwise the PackagesLocalDirectory of a classic development VM.
        /// </summary>
        private LabelSettings CreateDefaultSettings()
        {
            var settings = new LabelSettings();
            foreach (string language in DefaultLanguages)
            {
                settings.LoadLanguages.Add(language);
            }

            string? configuration = this.discovery.ListConfigurations()
                .OrderByDescending(name => File.GetLastWriteTimeUtc(Path.Combine(this.discovery.ConfigurationFolder, name + ".json")))
                .FirstOrDefault();
            if (configuration != null)
            {
                settings.MetadataConfiguration = configuration;
                this.messages.Report(MessageSeverity.Message, $"Using the metadata configuration {configuration}, the most recently changed one.");
            }
            else if (ModelDiscovery.FindClassicDirectory() is string classic)
            {
                settings.PackageDirectories.Add(classic);
                this.messages.Report(MessageSeverity.Message, $"No metadata configuration found, using {classic}.");
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
