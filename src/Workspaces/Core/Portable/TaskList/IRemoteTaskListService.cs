// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.TaskList;

/// <summary>
/// Interface to allow host (VS) to inform the OOP service to start incrementally analyzing and
/// reporting results back to the host.
/// </summary>
internal interface IRemoteTaskListService
{
    ValueTask<ImmutableArray<TaskListItem>> GetTaskListItemsAsync(Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TaskListItemDescriptor> descriptors, CancellationToken cancellationToken);
}
