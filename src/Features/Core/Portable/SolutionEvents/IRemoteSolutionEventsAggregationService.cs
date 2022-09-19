// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.SolutionEvents
{
    internal interface IRemoteSolutionEventsAggregationService
    {
        ValueTask OnSolutionEventAsync(Checksum solutionChecksum, InvocationReasons reasons, CancellationToken cancellationToken);
        ValueTask OnDocumentEventAsync(Checksum solutionChecksum, DocumentId documentId, InvocationReasons reasons, CancellationToken cancellationToken);
        ValueTask OnProjectEventAsync(Checksum solutionChecksum, ProjectId projectId, InvocationReasons reasons, CancellationToken cancellationToken);

        ValueTask OnSolutionChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, CancellationToken cancellationToken);
        ValueTask OnProjectChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, ProjectId projectId, CancellationToken cancellationToken);
        ValueTask OnDocumentChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, DocumentId documentId, CancellationToken cancellationToken);
    }
}
