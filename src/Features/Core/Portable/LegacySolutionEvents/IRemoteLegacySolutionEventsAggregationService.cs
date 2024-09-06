// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LegacySolutionEvents;

/// <summary>
/// This is a legacy api intended only for existing SolutionCrawler partners to continue to function (albeit with
/// ownership of that crawling task now belonging to the partner team, not roslyn).  It should not be used for any
/// new services.
/// </summary>
internal interface IRemoteLegacySolutionEventsAggregationService
{
    ValueTask<bool> ShouldReportChangesAsync(CancellationToken cancellationToken);

    /// <param name="oldSolutionChecksum"><inheritdoc cref="WorkspaceChangeEventArgs.OldSolution"/></param>
    /// <param name="newSolutionChecksum"><inheritdoc cref="WorkspaceChangeEventArgs.NewSolution"/></param>
    /// <param name="kind"><inheritdoc cref="WorkspaceChangeEventArgs.Kind"/></param>
    /// <param name="projectId"><inheritdoc cref="WorkspaceChangeEventArgs.ProjectId"/></param>
    /// <param name="documentId"><inheritdoc cref="WorkspaceChangeEventArgs.DocumentId"/></param>
    ValueTask OnWorkspaceChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, WorkspaceChangeKind kind, ProjectId? projectId, DocumentId? documentId, CancellationToken cancellationToken);
}
