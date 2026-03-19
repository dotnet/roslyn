// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LegacySolutionEvents;

/// <summary>
/// This is a legacy api intended only for existing SolutionCrawler partners to continue to function (albeit with
/// ownership of that crawling task now belonging to the partner team, not roslyn).  It should not be used for any
/// new services.
/// </summary>
internal interface ILegacySolutionEventsListener
{
    bool ShouldReportChanges(SolutionServices services);
    ValueTask OnWorkspaceChangedAsync(WorkspaceChangeEventArgs args, bool processSourceGeneratedDocuments, CancellationToken cancellationToken);
}
