// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Service for computing call hierarchy information.
/// </summary>
internal interface ICallHierarchyService
{
    /// <summary>
    /// Prepares a call hierarchy item for the symbol at the given position.
    /// Returns null if no valid symbol is found at the position.
    /// </summary>
    Task<CallHierarchyItem?> PrepareCallHierarchyAsync(
        Document document,
        int position,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the incoming calls (callers) for the given call hierarchy item.
    /// </summary>
    Task<ImmutableArray<CallHierarchyIncomingCall>> GetIncomingCallsAsync(
        Solution solution,
        CallHierarchyItem item,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the outgoing calls (callees) for the given call hierarchy item.
    /// </summary>
    Task<ImmutableArray<CallHierarchyOutgoingCall>> GetOutgoingCallsAsync(
        Solution solution,
        CallHierarchyItem item,
        CancellationToken cancellationToken);
}
