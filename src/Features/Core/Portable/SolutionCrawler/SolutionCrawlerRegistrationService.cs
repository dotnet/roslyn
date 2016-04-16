// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
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
        private readonly object _gate;
        private readonly SolutionCrawlerProgressReporter _progressReporter;

        private readonly IAsynchronousOperationListener _listener;
        private readonly ImmutableArray<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> _analyzerProviders;
        private readonly Dictionary<Workspace, WorkCoordinator> _documentWorkCoordinatorMap;

        [ImportingConstructor]
        public SolutionCrawlerRegistrationService(
            [ImportMany] IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _gate = new object();

            _analyzerProviders = analyzerProviders.ToImmutableArray();
            _documentWorkCoordinatorMap = new Dictionary<Workspace, WorkCoordinator>(ReferenceEqualityComparer.Instance);
            _listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.SolutionCrawler);

            _progressReporter = new SolutionCrawlerProgressReporter(_listener);
        }

        public void Register(Workspace workspace)
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
                    _analyzerProviders.Where(l => l.Metadata.WorkspaceKinds.Any(wk => wk == workspace.Kind)),
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

        public void Reanalyze(Workspace workspace, IIncrementalAnalyzer analyzer, IEnumerable<ProjectId> projectIds, IEnumerable<DocumentId> documentIds, bool highPriority)
        {
            lock (_gate)
            {
                var coordinator = default(WorkCoordinator);
                if (!_documentWorkCoordinatorMap.TryGetValue(workspace, out coordinator))
                {
                    throw new ArgumentException("workspace");
                }

                // no specific projects or documents provided
                if (projectIds == null && documentIds == null)
                {
                    coordinator.Reanalyze(analyzer, workspace.CurrentSolution.Projects.SelectMany(p => p.DocumentIds).ToSet(), highPriority);
                    return;
                }

                // specific documents provided
                if (projectIds == null)
                {
                    coordinator.Reanalyze(analyzer, documentIds.ToSet(), highPriority);
                    return;
                }

                var solution = workspace.CurrentSolution;
                var set = new HashSet<DocumentId>(documentIds ?? SpecializedCollections.EmptyEnumerable<DocumentId>());
                set.UnionWith(projectIds.Select(id => solution.GetProject(id)).SelectMany(p => p.DocumentIds));

                coordinator.Reanalyze(analyzer, set, highPriority);
            }
        }

        internal void WaitUntilCompletion_ForTestingPurposesOnly(Workspace workspace, ImmutableArray<IIncrementalAnalyzer> workers)
        {
            if (_documentWorkCoordinatorMap.ContainsKey(workspace))
            {
                _documentWorkCoordinatorMap[workspace].WaitUntilCompletion_ForTestingPurposesOnly(workers);
            }
        }

        internal void WaitUntilCompletion_ForTestingPurposesOnly(Workspace workspace)
        {
            if (_documentWorkCoordinatorMap.ContainsKey(workspace))
            {
                _documentWorkCoordinatorMap[workspace].WaitUntilCompletion_ForTestingPurposesOnly();
            }
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

            public Solution CurrentSolution
            {
                get { return Workspace.CurrentSolution; }
            }

            public TService GetService<TService>() where TService : IWorkspaceService
            {
                return Workspace.Services.GetService<TService>();
            }
        }
    }
}
