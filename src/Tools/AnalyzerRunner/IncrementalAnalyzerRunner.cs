// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            var usePersistentStorage = _options.UsePersistentStorage;

            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options
                .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.CSharp, _options.AnalysisScope)
                .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, _options.AnalysisScope)
                .WithChangedOption(StorageOptions.Database, usePersistentStorage ? StorageDatabase.SQLite : StorageDatabase.None)));

            if (!string.IsNullOrEmpty(_options.ProfileRoot))
            {
                ProfileOptimization.StartProfile(nameof(IIncrementalAnalyzer));
            }

            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;

            var solutionCrawlerRegistrationService = (SolutionCrawlerRegistrationService)workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            solutionCrawlerRegistrationService.Register(workspace);

            if (usePersistentStorage)
            {
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
