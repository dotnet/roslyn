// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LegacySolutionEvents;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteLegacySolutionEventsAggregationService : BrokeredServiceBase, IRemoteLegacySolutionEventsAggregationService
    {
        internal sealed class Factory : FactoryBase<IRemoteLegacySolutionEventsAggregationService>
        {
            protected override IRemoteLegacySolutionEventsAggregationService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteLegacySolutionEventsAggregationService(arguments);
        }

        public RemoteLegacySolutionEventsAggregationService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        private ILegacyWorkspaceDescriptor GetDescriptor()
            => RemoteLegacyWorkspaceDescriptor.Create(this.GetWorkspace());

        public ValueTask OnTextDocumentOpenedAsync(Checksum solutionChecksum, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var aggregationService = solution.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();
                await aggregationService.OnTextDocumentOpenedAsync(
                    GetDescriptor(), new TextDocumentEventArgs(solution.GetRequiredDocument(documentId)), cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask OnTextDocumentClosedAsync(Checksum solutionChecksum, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var aggregationService = solution.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();
                await aggregationService.OnTextDocumentClosedAsync(
                    GetDescriptor(), new TextDocumentEventArgs(solution.GetRequiredDocument(documentId)), cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask OnWorkspaceChangedEventAsync(
            Checksum oldSolutionChecksum,
            Checksum newSolutionChecksum,
            WorkspaceChangeKind kind,
            ProjectId? projectId,
            DocumentId? documentId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(oldSolutionChecksum, newSolutionChecksum,
                async (oldSolution, newSolution) =>
                {
                    var aggregationService = oldSolution.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();
                    await aggregationService.OnWorkspaceChangedEventAsync(
                        GetDescriptor(), new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId), cancellationToken).ConfigureAwait(false);
                }, cancellationToken);
        }

        /// <summary>
        /// NOTE: When we remove the RemoteWorkspace from OOP this will be impacted.  The approach we should take then
        /// is that this descriptor works by trying to return the latest primary-solution snapshot that VS has sync'ed
        /// to OOP.  This is invariably an unpleasant concept in OOP (which we want to be entirely checksum/snapshot
        /// based).  However, this unpleasantness is necessary as this code exists *solely* to allow UnitTesting to
        /// continue running with their own form of SolutionCrawler, and SolutionCrawler's design itself is deeply
        /// connected to the concept of the latest "CurrentSolution".
        /// 
        /// Generally speaking, this should still continue to work though as we will still very likely have it as part
        /// of our design that OOP holds onto the latest-primary-solution-snapshot as that's still very helpful for perf
        /// reasons, even if the individual OOP requests come in are referencing a specific checksum/snapshot pair.  The
        /// caching of the latest-primary-solution-snapshot helps keep appropriate assets pinned in OOP and makes
        /// producing the requested snapshot cheaper is it can be a fork of that version we are holding onto.
        /// 
        /// In other words, we are not keeping around the latest-primary-solution-snapshot to power UnitTesting
        /// solution-crawling.  We are keeping it around for perf around standard OOP snapshot requests, and this
        /// component can appropriately leverage that.
        /// </summary>
        private sealed class RemoteLegacyWorkspaceDescriptor : ILegacyWorkspaceDescriptor
        {
            private static readonly ConditionalWeakTable<RemoteWorkspace, ILegacyWorkspaceDescriptor> s_workspaceToDescriptor = new();

            private readonly RemoteWorkspace _remoteWorkspace;

            private RemoteLegacyWorkspaceDescriptor(RemoteWorkspace remoteWorkspace)
            {
                _remoteWorkspace = remoteWorkspace;
            }

            public static ILegacyWorkspaceDescriptor Create(RemoteWorkspace workspace)
                => s_workspaceToDescriptor.GetValue(workspace, static workspace => new RemoteLegacyWorkspaceDescriptor(workspace));

            public string? WorkspaceKind => _remoteWorkspace.Kind;

            public SolutionServices SolutionServices => _remoteWorkspace.Services.SolutionServices;

            public Solution CurrentSolution => _remoteWorkspace.CurrentSolution;
        }
    }
}
