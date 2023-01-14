﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Storage;

namespace AnalyzerRunner
{
    public sealed class IncrementalAnalyzerRunner
    {
        private readonly Workspace _workspace;
        private readonly Options _options;

        public IncrementalAnalyzerRunner(Workspace workspace, Options options)
        {
            _workspace = workspace;
            _options = options;
        }

        public bool HasAnalyzers => _options.IncrementalAnalyzerNames.Any();

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (!HasAnalyzers)
            {
                return;
            }

            var usePersistentStorage = _options.UsePersistentStorage;

            var exportProvider = _workspace.Services.SolutionServices.ExportProvider;

            var globalOptions = exportProvider.GetExports<IGlobalOptionService>().Single().Value;
            globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, _options.AnalysisScope);
            globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, _options.AnalysisScope);

            var workspaceConfigurationService = (AnalyzerRunnerWorkspaceConfigurationService)_workspace.Services.GetRequiredService<IWorkspaceConfigurationService>();
            workspaceConfigurationService.Options = new(CacheStorage: usePersistentStorage ? StorageDatabase.SQLite : StorageDatabase.None);

            var solutionCrawlerRegistrationService = (SolutionCrawlerRegistrationService)_workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            solutionCrawlerRegistrationService.Register(_workspace);

            if (usePersistentStorage)
            {
                var persistentStorageService = _workspace.Services.SolutionServices.GetPersistentStorageService();
                await using var persistentStorage = await persistentStorageService.GetStorageAsync(SolutionKey.ToSolutionKey(_workspace.CurrentSolution), cancellationToken).ConfigureAwait(false);
                if (persistentStorage is NoOpPersistentStorage)
                {
                    throw new InvalidOperationException("Benchmark is not configured to use persistent storage.");
                }
            }

            var incrementalAnalyzerProviders = exportProvider.GetExports<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>();
            foreach (var incrementalAnalyzerName in _options.IncrementalAnalyzerNames)
            {
                var incrementalAnalyzerProvider = incrementalAnalyzerProviders.Where(x => x.Metadata.Name == incrementalAnalyzerName).SingleOrDefault(provider => provider.Metadata.WorkspaceKinds?.Contains(_workspace.Kind) ?? false)?.Value;
                incrementalAnalyzerProvider ??= incrementalAnalyzerProviders.Where(x => x.Metadata.Name == incrementalAnalyzerName).SingleOrDefault(provider => provider.Metadata.WorkspaceKinds?.Contains(WorkspaceKind.Host) ?? false)?.Value;
                incrementalAnalyzerProvider ??= incrementalAnalyzerProviders.Where(x => x.Metadata.Name == incrementalAnalyzerName).SingleOrDefault(provider => provider.Metadata.WorkspaceKinds?.Contains(WorkspaceKind.RemoteWorkspace) ?? false)?.Value;
                incrementalAnalyzerProvider ??= incrementalAnalyzerProviders.Where(x => x.Metadata.Name == incrementalAnalyzerName).Single(provider => provider.Metadata.WorkspaceKinds is null).Value;
                var incrementalAnalyzer = incrementalAnalyzerProvider.CreateIncrementalAnalyzer(_workspace);
                solutionCrawlerRegistrationService.GetTestAccessor().WaitUntilCompletion(_workspace, ImmutableArray.Create(incrementalAnalyzer));
            }
        }
    }
}
