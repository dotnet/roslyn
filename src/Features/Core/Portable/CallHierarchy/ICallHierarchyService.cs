// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Language service for Call Hierarchy operations.
/// </summary>
internal interface ICallHierarchyService : ILanguageService
{
    /// <summary>
    /// Prepares the call hierarchy items for the symbol at the specified position in the document.
    /// This is the entry point for Call Hierarchy - it identifies the symbol and creates the initial item(s).
    /// </summary>
    /// <param name="document">The document containing the symbol</param>
    /// <param name="position">The position in the document</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Call hierarchy items for the symbol at the position, or empty if no valid symbol found</returns>
    Task<ImmutableArray<CallHierarchyItem>> PrepareCallHierarchyAsync(
        Document document,
        int position,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the incoming calls (callers) for the specified call hierarchy item.
    /// </summary>
    /// <param name="item">The call hierarchy item to find callers for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Incoming calls to the item</returns>
    Task<ImmutableArray<CallHierarchyIncomingCall>> GetIncomingCallsAsync(
        CallHierarchyItem item,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the outgoing calls (callees) for the specified call hierarchy item.
    /// </summary>
    /// <param name="item">The call hierarchy item to find callees for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Outgoing calls from the item</returns>
    Task<ImmutableArray<CallHierarchyOutgoingCall>> GetOutgoingCallsAsync(
        CallHierarchyItem item,
        CancellationToken cancellationToken);
}
