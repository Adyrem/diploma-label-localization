#if DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BE.LabelExtension.Core.Diagnostics;
using BE.LabelExtension.Core.Search;
using BE.LabelExtension.Core.Store;
using BE.LabelExtension.Labels;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;

namespace BE.LabelExtension.Probes
{
    /// <summary>
    /// Probe for D1: measures one search per search mode over the loaded labels, three times
    /// each, before the tool window has a search (NFA02). The modes carry the names of the
    /// existing tool, so the user can repeat the same searches there. Reports counts and
    /// times only, no label IDs or texts.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class ProbeSearchTimesCommand : Command
    {
        private const string Title = "Probe: Measure search times";
        private const int Runs = 3;
        private const long MegaByte = 1024 * 1024;

        private readonly ErrorBoundary errorBoundary;
        private readonly IMessageSink sink;
        private readonly LabelLoader loader;
        private readonly LabelStore store;

        public ProbeSearchTimesCommand(ErrorBoundary errorBoundary, IMessageSink sink, LabelLoader loader, LabelStore store)
        {
            this.errorBoundary = errorBoundary;
            this.sink = sink;
            this.loader = loader;
            this.store = store;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%BE.LabelExtension.ProbeSearchTimesCommand.DisplayName%")
        {
            Icon = new(ImageMoniker.KnownValues.Search, IconSettings.IconAndText),
        };

        /// <inheritdoc />
        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
            => this.errorBoundary.RunAsync(Title, this.RunAsync, cancellationToken);

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            this.loader.EnsureLoaded();
            if (!this.store.IsLoaded || this.store.IsLoading)
            {
                this.sink.Report(MessageSeverity.Warning, $"{Title}: the labels are still being loaded. Run the probe again once the Output Window reports the load.");
                return;
            }

            ShellExtensibility shell = this.Extensibility.Shell();
            string? term = await shell.ShowPromptAsync(
                "Probe: measure search times.\nSearch term for Exact match, Substring and MatchWord:",
                new InputPromptOptions { DefaultText = "Customer" },
                cancellationToken);
            if (string.IsNullOrWhiteSpace(term))
            {
                return;
            }

            string? words = await shell.ShowPromptAsync(
                "Words for Anything like that:",
                new InputPromptOptions { DefaultText = "customer account" },
                cancellationToken);
            if (string.IsNullOrWhiteSpace(words))
            {
                return;
            }

            ProbeReport report = await Task.Run(() => this.Measure(term!.Trim(), words!.Trim(), cancellationToken), cancellationToken);
            report.Send(this.sink);
        }

        private ProbeReport Measure(string term, string words, CancellationToken cancellationToken)
        {
            var search = new LabelSearch(this.store);
            var report = new ProbeReport(Title);
            report.Line($"{this.store.Count} labels in the languages {string.Join(", ", this.loader.Settings.LoadLanguages)}, from {this.store.LabelFiles.Count} label files; {Environment.ProcessorCount} logical processors");
            report.Line($"Times in ms for run 1 / 2 / 3. The terms are not repeated here.");

            var queries = new List<(string Name, SearchQuery Query)>
            {
                ("ExactMatch", new SearchQuery(term, SearchMode.ExactMatch, caseSensitive: true)),
                ("ExactMatchIgnoreCase", new SearchQuery(term, SearchMode.ExactMatch, caseSensitive: false)),
                ("SubString", new SearchQuery(term, SearchMode.Substring, caseSensitive: true)),
                ("SubStringIgnoreCase", new SearchQuery(term, SearchMode.Substring, caseSensitive: false)),
                ("AnythingLikeThat", new SearchQuery(words, SearchMode.AnythingLike, caseSensitive: true)),
                ("AnythingLikeThatIgnoreCase", new SearchQuery(words, SearchMode.AnythingLike, caseSensitive: false)),
                ("MatchWord", new SearchQuery(term, SearchMode.MatchWord, caseSensitive: false)),
            };

            string? firstId = null;
            foreach (var (name, query) in queries)
            {
                IReadOnlyList<SearchHit> hits = this.Run(search, query, name, report, cancellationToken);
                if (name == "SubStringIgnoreCase" && hits.Count > 0)
                {
                    firstId = hits[0].Label.Id.FullId;
                }
            }

            // Label id needs an ID that exists: the first hit of the substring search.
            if (firstId != null)
            {
                this.Run(search, new SearchQuery(firstId, SearchMode.Id, caseSensitive: true), "LabelId (first hit of SubStringIgnoreCase)", report, cancellationToken);
            }
            else
            {
                report.Line("LabelId: not measured, the substring search found nothing");
            }

            using Process process = Process.GetCurrentProcess();
            report.Line($"Memory now: {GC.GetTotalMemory(forceFullCollection: false) / MegaByte} MB managed, {process.PrivateMemorySize64 / MegaByte} MB private bytes of the process");
            return report;
        }

        private IReadOnlyList<SearchHit> Run(LabelSearch search, SearchQuery query, string name, ProbeReport report, CancellationToken cancellationToken)
        {
            IReadOnlyList<SearchHit> hits = Array.Empty<SearchHit>();
            var times = new long[Runs];
            for (int run = 0; run < Runs; run++)
            {
                var stopwatch = Stopwatch.StartNew();
                hits = search.Search(query, cancellationToken);
                times[run] = stopwatch.ElapsedMilliseconds;
            }

            report.Line($"  {name,-42} {hits.Count,8} hits   {string.Join(" / ", times)} ms");
            return hits;
        }
    }
}
#endif
