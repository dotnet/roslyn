// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CallHierarchy;

/// <summary>
/// Service for finding call hierarchy relationships for symbols.
/// </summary>
internal interface ICallHierarchyService : ILanguageService
{
    /// <summary>
    /// Gets the call hierarchy item for a symbol at the specified position.
    /// </summary>
    Task<CallHierarchyItem?> GetCallHierarchyItemAsync(Document document, int position, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the incoming calls to the specified symbol.
    /// </summary>
    Task<ImmutableArray<CallHierarchyIncomingCall>> FindIncomingCallsAsync(Document document, ISymbol symbol, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the outgoing calls from the specified symbol.
    /// </summary>
    Task<ImmutableArray<CallHierarchyOutgoingCall>> FindOutgoingCallsAsync(Document document, ISymbol symbol, CancellationToken cancellationToken);
}
