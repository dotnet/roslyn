// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class TypeParameterSymbolReferenceFinder : AbstractTypeParameterSymbolReferenceFinder
{
    public static readonly TypeParameterSymbolReferenceFinder Instance = new();

    private TypeParameterSymbolReferenceFinder()
    {
    }

    protected override bool CanFind(ITypeParameterSymbol symbol)
        => symbol.TypeParameterKind == TypeParameterKind.Type;

    protected override Task DetermineDocumentsToSearchAsync<TData>(
        ITypeParameterSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // Type parameters are only found in documents that have both their name, and the
        // name of its owning type.  NOTE(cyrusn): We have to check in multiple files because
        // of partial types.  A type parameter can be referenced across all the parts.
        // NOTE(cyrusn): We look for type parameters by name.  This means if the same type
        // parameter has a different name in different parts that we won't find it.  However,
        // this only happens in error situations.  It is not legal in C# to use a different
        // name for a type parameter in different parts.
        return symbol.ContainingType is { IsExtension: true, ContainingType.Name: var staticClassName }
            ? FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, symbol.Name, staticClassName)
            : FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, symbol.Name, symbol.ContainingType.Name);
    }
}
