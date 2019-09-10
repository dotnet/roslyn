// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [ExportWorkspaceService(typeof(ISolutionCrawlerRegistrationService), ServiceLayer.Host), Shared]
    internal partial class SolutionCrawlerRegistrationService : ISolutionCrawlerRegistrationService
    {
        private const string Default = "*";

        private readonly object _gate;
        private readonly SolutionCrawlerProgressReporter _progressReporter;

        private readonly IAsynchronousOperationListener _listener;
        private readonly Dictionary<Workspace, WorkCoordinator> _documentWorkCoordinatorMap;

        private ImmutableDictionary<string, ImmutableArray<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>>> _analyzerProviders;

        [ImportingConstructor]
        public SolutionCrawlerRegistrationService(
            [ImportMany] IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _gate = new object();

            _analyzerProviders = analyzerProviders.GroupBy(kv => kv.Metadata.Name).ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray());
            AssertAnalyzerProviders(_analyzerProviders);

            _documentWorkCoordinatorMap = new Dictionary<Workspace, WorkCoordinator>(ReferenceEqualityComparer.Instance);
            _listener = listenerProvider.GetListener(FeatureAttribute.SolutionCrawler);

            _progressReporter = new SolutionCrawlerProgressReporter();
        }

        public void Register(Workspace workspace)
        {
            EnsureRegistration(workspace, initializeLazily: true);
        }

        /// <summary>
        /// make sure solution cralwer is registered for the given workspace.
        /// </summary>
        /// <param name="workspace"><see cref="Workspace"/> this solution crawler runs for</param>
        /// <param name="initializeLazily">
        /// when true, solution crawler will be initialized when there is the first workspace event fired. 
        /// otherwise, it will be initialized when workspace is registered right away. 
        /// something like "Build" will use initializeLazily:false to make sure diagnostic analyzer engine (incremental analyzer)
        /// is initialized. otherwise, if build is called before workspace is fully populated, we will think some errors from build
        /// doesn't belong to us since diagnostic analyzer engine is not there yet and 
        /// let project system to take care of these unknown errors.
        /// </param>
        public void EnsureRegistration(Workspace workspace, bool initializeLazily)
        {
            var correlationId = LogAggregator.GetNextId();

            lock (_gate)
            {
                if (_documentWorkCoordinatorMap.ContainsKey(workspace))
                {
                    // already registered.
                    return;
                }

                var coordinator = new WorkCoordinator(
                    _listener,
                    GetAnalyzerProviders(workspace),
                    initializeLazily,
                    new Registration(correlationId, workspace, _progressReporter));

                _documentWorkCoordinatorMap.Add(workspace, coordinator);
            }

            SolutionCrawlerLogger.LogRegistration(correlationId, workspace);
        }

        public void Unregister(Workspace workspace, bool blockingShutdown = false)
        {
            var coordinator = default(WorkCoordinator);

            lock (_gate)
            {
                if (!_documentWorkCoordinatorMap.TryGetValue(workspace, out coordinator))
                {
                    // already unregistered
                    return;
                }

                _documentWorkCoordinatorMap.Remove(workspace);
                coordinator.Shutdown(blockingShutdown);
            }

            SolutionCrawlerLogger.LogUnregistration(coordinator.CorrelationId);
        }

        public void AddAnalyzerProvider(IIncrementalAnalyzerProvider provider, IncrementalAnalyzerProviderMetadata metadata)
        {
            // now update all existing work coordinator
            lock (_gate)
            {
                var lazyProvider = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => provider, metadata);

                // update existing map for future solution crawler registration - no need for interlock but this makes add or update easier
                ImmutableInterlocked.AddOrUpdate(ref _analyzerProviders, metadata.Name, n => ImmutableArray.Create(lazyProvider), (n, v) => v.Add(lazyProvider));

                // assert map integrity
                AssertAnalyzerProviders(_analyzerProviders);

                // find existing coordinator to update
                var lazyProviders = _analyzerProviders[metadata.Name];
                foreach (var kv in _documentWorkCoordinatorMap)
                {
                    var workspace = kv.Key;
                    var coordinator = kv.Value;
                    if (!TryGetProvider(workspace.Kind, lazyProviders, out var picked) || picked != lazyProvider)
                    {
                        // check whether new provider belong to current workspace
                        continue;
                    }

                    var analyzer = lazyProvider.Value.CreateIncrementalAnalyzer(workspace);
                    if (analyzer != null)
                    {
                        coordinator.AddAnalyzer(analyzer, metadata.HighPriorityForActiveFile);
                    }
                }
            }
        }

        public void Reanalyze(Workspace workspace, IIncrementalAnalyzer analyzer, IEnumerable<ProjectId> projectIds, IEnumerable<DocumentId> documentIds, bool highPriority)
        {
            lock (_gate)
            {
                if (!_documentWorkCoordinatorMap.TryGetValue(workspace, out var coordinator))
                {
                    // this can happen if solution crawler is already unregistered from workspace.
                    // one of those example will be VS shutting down so roslyn package is disposed but there is a pending
                    // async operation.
                    return;
                }

                // no specific projects or documents provided
                if (projectIds == null && documentIds == null)
                {
                    coordinator.Reanalyze(analyzer, new ReanalyzeScope(workspace.CurrentSolution.Id), highPriority);
                    return;
                }

                coordinator.Reanalyze(analyzer, new ReanalyzeScope(projectIds, documentIds), highPriority);
            }
        }

        internal void WaitUntilCompletion_ForTestingPurposesOnly(Workspace workspace, ImmutableArray<IIncrementalAnalyzer> workers)
        {
            if (_documentWorkCoordinatorMap.TryGetValue(workspace, out var coordinator))
            {
                coordinator.WaitUntilCompletion_ForTestingPurposesOnly(workers);
            }
        }

        internal void WaitUntilCompletion_ForTestingPurposesOnly(Workspace workspace)
        {
            if (_documentWorkCoordinatorMap.TryGetValue(workspace, out var coordinator))
            {
                coordinator.WaitUntilCompletion_ForTestingPurposesOnly();
            }
        }

        private IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> GetAnalyzerProviders(Workspace workspace)
        {
            foreach (var kv in _analyzerProviders)
            {
                var lazyProviders = kv.Value;

                // try get provider for the specific workspace kind
                if (TryGetProvider(workspace.Kind, lazyProviders, out var lazyProvider))
                {
                    yield return lazyProvider;
                    continue;
                }

                // try get default provider
                if (TryGetProvider(Default, lazyProviders, out lazyProvider))
                {
                    yield return lazyProvider;
                }
            }
        }

        private bool TryGetProvider(
            string kind,
            ImmutableArray<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> lazyProviders,
            out Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata> lazyProvider)
        {
            // set out param
            lazyProvider = null;

            // try find provider for specific workspace kind
            if (kind != Default)
            {
                foreach (var provider in lazyProviders)
                {
                    if (provider.Metadata.WorkspaceKinds?.Any(wk => wk == kind) == true)
                    {
                        lazyProvider = provider;
                        return true;
                    }
                }

                return false;
            }

            // try find default provider
            foreach (var provider in lazyProviders)
            {
                if (IsDefaultProvider(provider.Metadata))
                {
                    lazyProvider = provider;
                    return true;
                }

                return false;
            }

            return false;
        }

        [Conditional("DEBUG")]
        private static void AssertAnalyzerProviders(
            ImmutableDictionary<string, ImmutableArray<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>>> analyzerProviders)
        {
#if DEBUG
            // make sure there is duplicated provider defined for same workspace.
            var set = new HashSet<string>();
            foreach (var kv in analyzerProviders)
            {
                foreach (var lazyProvider in kv.Value)
                {
                    if (IsDefaultProvider(lazyProvider.Metadata))
                    {
                        Debug.Assert(set.Add(Default));
                        continue;
                    }

                    foreach (var kind in lazyProvider.Metadata.WorkspaceKinds)
                    {
                        Debug.Assert(set.Add(kind));
                    }
                }

                set.Clear();
            }
#endif
        }

        private static bool IsDefaultProvider(IncrementalAnalyzerProviderMetadata providerMetadata)
        {
            return providerMetadata.WorkspaceKinds == null || providerMetadata.WorkspaceKinds.Count == 0;
        }

        private class Registration
        {
            public readonly int CorrelationId;
            public readonly Workspace Workspace;
            public readonly SolutionCrawlerProgressReporter ProgressReporter;

            public Registration(int correlationId, Workspace workspace, SolutionCrawlerProgressReporter progressReporter)
            {
                CorrelationId = correlationId;
                Workspace = workspace;
                ProgressReporter = progressReporter;
            }

            public Solution CurrentSolution => Workspace.CurrentSolution;

            public TService GetService<TService>() where TService : IWorkspaceService
            {
                return Workspace.Services.GetService<TService>();
            }
        }
    }
}
