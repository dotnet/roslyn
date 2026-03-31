// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CallHierarchy;

internal interface ICallHierarchyService : ILanguageService
{
    Task<CallHierarchyItemDescriptor?> CreateItemAsync(ISymbol symbol, Project project, CancellationToken cancellationToken);

    Task<ImmutableArray<CallHierarchySearchResult>> SearchIncomingCallsAsync(
        Solution solution,
        CallHierarchySearchDescriptor searchDescriptor,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken);

    Task<ImmutableArray<CallHierarchySearchResult>> SearchOutgoingCallsAsync(
        Solution solution,
        CallHierarchyItemId itemId,
        IImmutableSet<Document>? documents,
        CancellationToken cancellationToken);
}
