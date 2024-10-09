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

namespace Microsoft.CodeAnalysis.Remote;

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

    public ValueTask<bool> ShouldReportChangesAsync(CancellationToken cancellationToken)
    {
        return RunServiceImplAsync(
            cancellationToken =>
            {
                var services = this.GetWorkspaceServices();
                var aggregationService = services.GetRequiredService<ILegacySolutionEventsAggregationService>();
                return new ValueTask<bool>(aggregationService.ShouldReportChanges(services));
            },
            cancellationToken);
    }

    public ValueTask OnWorkspaceChangedAsync(
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
                await aggregationService.OnWorkspaceChangedAsync(
                    new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId), cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
    }
}
