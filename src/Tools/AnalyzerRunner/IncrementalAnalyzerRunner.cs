// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.IncrementalCaches;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.Storage;

namespace AnalyzerRunner
{
    internal class IncrementalAnalyzerRunner
    {
        private readonly Options _options;

        public IncrementalAnalyzerRunner(Options options)
        {
            _options = options;
        }

        public bool HasAnalyzers => _options.IncrementalAnalyzerNames.Any();

        internal async Task RunAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            if (!HasAnalyzers)
            {
                return;
            }

            workspace.Options = workspace.Options
                .WithChangedOption(StorageOptions.SolutionSizeThreshold, _options.UsePersistentStorage ? 0 : int.MaxValue)
                .WithChangedOption(RuntimeOptions.FullSolutionAnalysis, _options.FullSolutionAnalysis)
                .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, _options.FullSolutionAnalysis)
                .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.VisualBasic, _options.FullSolutionAnalysis);

            if (!string.IsNullOrEmpty(_options.ProfileRoot))
            {
                ProfileOptimization.StartProfile(nameof(IIncrementalAnalyzer));
            }

            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;

            var solutionSizeTracker = exportProvider.GetExports<ISolutionSizeTracker>().Single().Value;

            // This will return the tracker, since it's a singleton.
            var analyzer = ((IIncrementalAnalyzerProvider)solutionSizeTracker).CreateIncrementalAnalyzer(workspace);
            await analyzer.NewSolutionSnapshotAsync(workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);

            var solutionCrawlerRegistrationService = (SolutionCrawlerRegistrationService)workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            solutionCrawlerRegistrationService.Register(workspace);
            solutionCrawlerRegistrationService.WaitUntilCompletion_ForTestingPurposesOnly(workspace, ImmutableArray.Create(analyzer));

            var size = solutionSizeTracker.GetSolutionSize(workspace, workspace.CurrentSolution.Id);
            Console.WriteLine("Current solution size:\t" + size);

            if (_options.UsePersistentStorage)
            {
                if (size <= 0)
                {
                    throw new InvalidOperationException("Solution size is too small; persistent storage is disabled.");
                }

                var persistentStorageService = workspace.Services.GetRequiredService<IPersistentStorageService>();
                var persistentStorage = persistentStorageService.GetStorage(workspace.CurrentSolution);
                if (persistentStorage is NoOpPersistentStorage)
                {
                    throw new InvalidOperationException("Benchmark is not configured to use persistent storage.");
                }
            }

            Console.WriteLine("Pausing 5 seconds before continuing analysis...");
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            var incrementalAnalyzerProviders = exportProvider.GetExports<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>();

            foreach (var incrementalAnalyzerName in _options.IncrementalAnalyzerNames)
            {
                var incrementalAnalyzerProvider = incrementalAnalyzerProviders.Where(x => x.Metadata.Name == incrementalAnalyzerName).SingleOrDefault(provider => provider.Metadata.WorkspaceKinds?.Contains(workspace.Kind) ?? false)?.Value;
                incrementalAnalyzerProvider ??= incrementalAnalyzerProviders.Where(x => x.Metadata.Name == incrementalAnalyzerName).SingleOrDefault(provider => provider.Metadata.WorkspaceKinds?.Contains(WorkspaceKind.Host) ?? false)?.Value;
                incrementalAnalyzerProvider ??= incrementalAnalyzerProviders.Where(x => x.Metadata.Name == incrementalAnalyzerName).Single(provider => provider.Metadata.WorkspaceKinds is null).Value;
                var incrementalAnalyzer = incrementalAnalyzerProvider.CreateIncrementalAnalyzer(workspace);
                solutionCrawlerRegistrationService.WaitUntilCompletion_ForTestingPurposesOnly(workspace, ImmutableArray.Create(incrementalAnalyzer));

                switch (incrementalAnalyzerName)
                {
                    case nameof(SymbolTreeInfoIncrementalAnalyzerProvider):
                        var symbolTreeInfoCacheService = workspace.Services.GetRequiredService<ISymbolTreeInfoCacheService>();
                        var symbolTreeInfo = await symbolTreeInfoCacheService.TryGetSourceSymbolTreeInfoAsync(workspace.CurrentSolution.Projects.First(), cancellationToken).ConfigureAwait(false);
                        if (symbolTreeInfo is null)
                        {
                            throw new InvalidOperationException("Benchmark failed to calculate symbol tree info.");
                        }

                        break;

                    default:
                        // No additional actions required
                        break;
                }
            }
        }
    }
}
