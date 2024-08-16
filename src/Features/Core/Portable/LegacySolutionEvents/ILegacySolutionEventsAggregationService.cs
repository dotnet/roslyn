// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LegacySolutionEvents;

/// <summary>
/// This is a legacy api intended only for existing SolutionCrawler partners to continue to function (albeit with
/// ownership of that crawling task now belonging to the partner team, not roslyn).  It should not be used for any
/// new services.
/// </summary>
internal interface ILegacySolutionEventsAggregationService : IWorkspaceService
{
    bool ShouldReportChanges(SolutionServices services);

    ValueTask OnWorkspaceChangedAsync(WorkspaceChangeEventArgs args, CancellationToken cancellationToken);
}

[ExportWorkspaceService(typeof(ILegacySolutionEventsAggregationService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class DefaultLegacySolutionEventsAggregationService(
    [ImportMany] IEnumerable<Lazy<ILegacySolutionEventsListener>> eventsServices) : ILegacySolutionEventsAggregationService
{
    private readonly ImmutableArray<Lazy<ILegacySolutionEventsListener>> _eventsServices = eventsServices.ToImmutableArray();

    public bool ShouldReportChanges(SolutionServices services)
    {
        foreach (var service in _eventsServices)
        {
            if (service.Value.ShouldReportChanges(services))
                return true;
        }

        return false;
    }

    public async ValueTask OnWorkspaceChangedAsync(WorkspaceChangeEventArgs args, CancellationToken cancellationToken)
    {
        foreach (var service in _eventsServices)
            await service.Value.OnWorkspaceChangedAsync(args, cancellationToken).ConfigureAwait(false);
    }
}
