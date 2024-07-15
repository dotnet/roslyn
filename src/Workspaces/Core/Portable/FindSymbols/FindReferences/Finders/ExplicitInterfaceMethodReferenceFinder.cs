// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class ExplicitInterfaceMethodReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
{
    protected override bool CanFind(IMethodSymbol symbol)
        => symbol.MethodKind == MethodKind.ExplicitInterfaceImplementation;

    protected sealed override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
        IMethodSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // An explicit method can't be referenced anywhere.
        return SpecializedTasks.EmptyImmutableArray<Document>();
    }

    protected sealed override ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
        IMethodSymbol symbol,
        FindReferencesDocumentState state,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // An explicit method can't be referenced anywhere.
        return new([]);
    }
}
