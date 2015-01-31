// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [ExportWorkspaceService(typeof(IWorkCoordinatorRegistrationService), ServiceLayer.Host), Shared]
    internal partial class WorkCoordinatorRegistrationService : IWorkCoordinatorRegistrationService
    {
        private readonly object gate;
        private readonly IAsynchronousOperationListener listener;
        private readonly ImmutableArray<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders;
        private readonly Dictionary<Workspace, WorkCoordinator> documentWorkCoordinatorMap;

        [ImportingConstructor]
        public WorkCoordinatorRegistrationService(
            [ImportMany] IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            this.gate = new object();

            this.analyzerProviders = analyzerProviders.ToImmutableArray();
            this.documentWorkCoordinatorMap = new Dictionary<Workspace, WorkCoordinator>(ReferenceEqualityComparer.Instance);
            this.listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.SolutionCrawler);
        }

        public void Register(Workspace workspace)
        {
            var correlationId = LogAggregator.GetNextId();

            lock (this.gate)
            {
                if (this.documentWorkCoordinatorMap.ContainsKey(workspace))
                {
                    // already registered.
                    return;
                }

                var coordinator = new WorkCoordinator(
                    this.listener, this.analyzerProviders.Where(l => l.Metadata.WorkspaceKinds.Any(wk => wk == workspace.Kind)), correlationId, workspace);

                this.documentWorkCoordinatorMap.Add(workspace, coordinator);
            }

            SolutionCrawlerLogger.LogRegistration(correlationId, workspace);
        }

        public void Unregister(Workspace workspace, bool blockingShutdown = false)
        {
            var coordinator = default(WorkCoordinator);

            lock (this.gate)
            {
                if (!this.documentWorkCoordinatorMap.TryGetValue(workspace, out coordinator))
                {
                    // already unregistered
                    return;
                }

                this.documentWorkCoordinatorMap.Remove(workspace);
                coordinator.Shutdown(blockingShutdown);
            }

            SolutionCrawlerLogger.LogUnregistration(coordinator.CorrelationId);
        }

        public void Reanalyze(Workspace workspace, IIncrementalAnalyzer analyzer, IEnumerable<ProjectId> projectIds, IEnumerable<DocumentId> documentIds)
        {
            lock (this.gate)
            {
                var coordinator = default(WorkCoordinator);
                if (!this.documentWorkCoordinatorMap.TryGetValue(workspace, out coordinator))
                {
                    throw new ArgumentException("workspace");
                }

                // no specific projects or documents provided
                if (projectIds == null && documentIds == null)
                {
                    coordinator.Reanalyze(analyzer, workspace.CurrentSolution.Projects.SelectMany(p => p.DocumentIds).ToSet());
                    return;
                }

                // specific documents provided
                if (projectIds == null)
                {
                    coordinator.Reanalyze(analyzer, documentIds.ToSet());
                    return;
                }

                var solution = workspace.CurrentSolution;
                var set = new HashSet<DocumentId>(documentIds ?? SpecializedCollections.EmptyEnumerable<DocumentId>());
                set.Union(projectIds.Select(id => solution.GetProject(id)).SelectMany(p => p.DocumentIds));

                coordinator.Reanalyze(analyzer, set);
            }
        }

        internal void WaitUntilCompletion_ForTestingPurposesOnly(Workspace workspace, ImmutableArray<IIncrementalAnalyzer> workers)
        {
            if (this.documentWorkCoordinatorMap.ContainsKey(workspace))
            {
                this.documentWorkCoordinatorMap[workspace].WaitUntilCompletion_ForTestingPurposesOnly(workers);
            }
        }

        internal void WaitUntilCompletion_ForTestingPurposesOnly(Workspace workspace)
        {
            if (this.documentWorkCoordinatorMap.ContainsKey(workspace))
            {
                this.documentWorkCoordinatorMap[workspace].WaitUntilCompletion_ForTestingPurposesOnly();
            }
        }

        // a workaround since we can't export two workspace services from one class
        [ExportWorkspaceService(typeof(ISolutionCrawlerService), ServiceLayer.Host), Shared]
        private class NestedService : ISolutionCrawlerService
        {
            public void Reanalyze(Workspace workspace, IIncrementalAnalyzer analyzer, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null)
            {
                var registration = workspace.Services.GetService<IWorkCoordinatorRegistrationService>() as WorkCoordinatorRegistrationService;
                if (registration != null)
                {
                    registration.Reanalyze(workspace, analyzer, projectIds, documentIds);
                }
            }
        }
    }
}
