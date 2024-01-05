// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LegacySolutionEvents;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    [ExportWorkspaceService(typeof(IUnitTestingSolutionCrawlerRegistrationService), ServiceLayer.Host), Shared]
    internal partial class UnitTestingSolutionCrawlerRegistrationService : IUnitTestingSolutionCrawlerRegistrationService
    {
        private const string Default = "*";

        private readonly object _gate = new();
        private readonly UnitTestingSolutionCrawlerProgressReporter _progressReporter = new();

        private readonly IAsynchronousOperationListener _listener;
        private readonly Dictionary<(string workspaceKind, SolutionServices services), UnitTestingWorkCoordinator> _documentWorkCoordinatorMap = new();

        private ImmutableDictionary<string, ImmutableArray<Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>>> _analyzerProviders;

        /// <summary>
        /// The last solution we've heard about from the <see cref="ILegacySolutionEventsListener"/>.  This is used by
        /// our <see cref="UnitTestingWorkCoordinator"/> to represent the equivalent entity that <see
        /// cref="Workspace.CurrentSolution"/> normally represents.
        /// </summary>
        private Solution _lastReportedSolution = null!;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnitTestingSolutionCrawlerRegistrationService(
            [ImportMany] IEnumerable<Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>> analyzerProviders,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _analyzerProviders = analyzerProviders.GroupBy(kv => kv.Metadata.Name).ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray());
            AssertAnalyzerProviders(_analyzerProviders);

            _listener = listenerProvider.GetListener(FeatureAttribute.SolutionCrawlerUnitTesting);
        }

        /// <summary>
        /// make sure solution cralwer is registered for the given workspace.
        /// </summary>
        public IUnitTestingWorkCoordinator Register(Solution solution)
        {
            var workspaceKind = solution.WorkspaceKind;
            var solutionServices = solution.Services;
            Contract.ThrowIfNull(workspaceKind);

            var correlationId = CorrelationIdFactory.GetNextId();

            UnitTestingWorkCoordinator? coordinator;
            lock (_gate)
            {
                _lastReportedSolution = solution;
                if (!_documentWorkCoordinatorMap.TryGetValue((workspaceKind, solutionServices), out coordinator))
                {
                    coordinator = new UnitTestingWorkCoordinator(
                        _listener,
                        GetAnalyzerProviders(workspaceKind),
                        new UnitTestingRegistration(this, correlationId, workspaceKind, solutionServices, _progressReporter));

                    _documentWorkCoordinatorMap.Add((workspaceKind, solutionServices), coordinator);
                }
            }

            UnitTestingSolutionCrawlerLogger.LogRegistration(correlationId, workspaceKind);
            return coordinator;
        }

        public bool HasRegisteredAnalyzerProviders
        {
            get
            {
                lock (_gate)
                {
                    return _analyzerProviders.Count > 0;
                }
            }
        }

        public void AddAnalyzerProvider(IUnitTestingIncrementalAnalyzerProvider provider, UnitTestingIncrementalAnalyzerProviderMetadata metadata)
        {
            // now update all existing work coordinator
            lock (_gate)
            {
                var lazyProvider = new Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>(() => provider, metadata);

                // update existing map for future solution crawler registration - no need for interlock but this makes add or update easier
                ImmutableInterlocked.AddOrUpdate(ref _analyzerProviders, metadata.Name, n => ImmutableArray.Create(lazyProvider), (n, v) => v.Add(lazyProvider));

                // assert map integrity
                AssertAnalyzerProviders(_analyzerProviders);

                // find existing coordinator to update
                var lazyProviders = _analyzerProviders[metadata.Name];
                foreach (var ((workspaceKind, solutionServices), coordinator) in _documentWorkCoordinatorMap)
                {
                    Contract.ThrowIfNull(workspaceKind);

                    if (!TryGetProvider(workspaceKind, lazyProviders, out var picked) || picked != lazyProvider)
                    {
                        // check whether new provider belong to current workspace
                        continue;
                    }

                    var analyzer = lazyProvider.Value.CreateIncrementalAnalyzer();
                    if (analyzer != null)
                    {
                        coordinator.AddAnalyzer(analyzer);
                    }
                }
            }
        }

        public void Reanalyze(string? workspaceKind, SolutionServices services, IUnitTestingIncrementalAnalyzer analyzer, IEnumerable<ProjectId>? projectIds, IEnumerable<DocumentId>? documentIds)
        {
            Contract.ThrowIfNull(workspaceKind);

            lock (_gate)
            {
                if (!_documentWorkCoordinatorMap.TryGetValue((workspaceKind, services), out var coordinator))
                {
                    // this can happen if solution crawler is already unregistered from workspace.
                    // one of those example will be VS shutting down so roslyn package is disposed but there is a pending
                    // async operation.
                    return;
                }

                // no specific projects or documents provided
                if (projectIds == null && documentIds == null)
                {
                    var solution = coordinator.Registration.GetSolutionToAnalyze();
                    coordinator.Reanalyze(analyzer, new UnitTestingReanalyzeScope(solution.Id));
                    return;
                }

                coordinator.Reanalyze(analyzer, new UnitTestingReanalyzeScope(projectIds, documentIds));
            }
        }

        private IEnumerable<Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>> GetAnalyzerProviders(string workspaceKind)
        {
            foreach (var (_, lazyProviders) in _analyzerProviders)
            {
                // try get provider for the specific workspace kind
                if (TryGetProvider(workspaceKind, lazyProviders, out var lazyProvider))
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

        private static bool TryGetProvider(
            string kind,
            ImmutableArray<Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>> lazyProviders,
            [NotNullWhen(true)] out Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>? lazyProvider)
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
            ImmutableDictionary<string, ImmutableArray<Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>>> analyzerProviders)
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

                    foreach (var kind in lazyProvider.Metadata.WorkspaceKinds!)
                    {
                        Debug.Assert(set.Add(kind));
                    }
                }

                set.Clear();
            }
#endif
        }

        private static bool IsDefaultProvider(UnitTestingIncrementalAnalyzerProviderMetadata providerMetadata)
            => providerMetadata.WorkspaceKinds == null || providerMetadata.WorkspaceKinds.Count == 0;

        internal TestAccessor GetTestAccessor()
        {
            return new TestAccessor(this);
        }

        internal readonly struct TestAccessor
        {
            private readonly UnitTestingSolutionCrawlerRegistrationService _solutionCrawlerRegistrationService;

            internal TestAccessor(UnitTestingSolutionCrawlerRegistrationService solutionCrawlerRegistrationService)
            {
                _solutionCrawlerRegistrationService = solutionCrawlerRegistrationService;
            }

            internal ref ImmutableDictionary<string, ImmutableArray<Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>>> AnalyzerProviders
                => ref _solutionCrawlerRegistrationService._analyzerProviders;
        }

        internal sealed class UnitTestingRegistration(
            UnitTestingSolutionCrawlerRegistrationService owner,
            int correlationId,
            string workspaceKind,
            SolutionServices solutionServices,
            UnitTestingSolutionCrawlerProgressReporter progressReporter)
        {
            private readonly UnitTestingSolutionCrawlerRegistrationService _owner = owner;

            public readonly int CorrelationId = correlationId;
            public readonly string WorkspaceKind = workspaceKind;
            public readonly SolutionServices Services = solutionServices;
            public readonly UnitTestingSolutionCrawlerProgressReporter ProgressReporter = progressReporter;

            public Solution GetSolutionToAnalyze()
            {
                lock (_owner._gate)
                    return _owner._lastReportedSolution;
            }
        }
    }
}
