// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class MethodTypeParameterSymbolReferenceFinder : AbstractTypeParameterSymbolReferenceFinder
{
    public static readonly MethodTypeParameterSymbolReferenceFinder Instance = new();

    private MethodTypeParameterSymbolReferenceFinder()
    {
    }

    protected override bool CanFind(ITypeParameterSymbol symbol)
        => symbol.TypeParameterKind == TypeParameterKind.Method;

    protected override async ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        ITypeParameterSymbol symbol,
        Solution solution,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var method = (IMethodSymbol)symbol.ContainingSymbol;
        var ordinal = method.TypeParameters.IndexOf(symbol);

        if (ordinal >= 0)
        {
            if (method.PartialDefinitionPart != null && ordinal < method.PartialDefinitionPart.TypeParameters.Length)
                return [method.PartialDefinitionPart.TypeParameters[ordinal]];

            if (method.PartialImplementationPart != null && ordinal < method.PartialImplementationPart.TypeParameters.Length)
                return [method.PartialImplementationPart.TypeParameters[ordinal]];
        }

        return [];
    }

    protected sealed override Task DetermineDocumentsToSearchAsync<TData>(
        ITypeParameterSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // Type parameters are only found in documents that have both their name, and the name of its owning method.
        // NOTE(cyrusn): We have to check in multiple files because of partial types.  A type parameter can be
        // referenced across all the parts. NOTE(cyrusn): We look for type parameters by name.  This means if the same
        // type parameter has a different name in different parts that we won't find it. However, this only happens in
        // error situations.  It is not legal in C# to use a different name for a type parameter in different parts.
        //
        // Also, we only look for files that have the name of the owning type.  This helps filter down the set
        // considerably.  Note: we don't do this for top level local functions as they obviously appear only in one
        // document, and their containing type name ("Program") doesn't have to appear there at all.

        Contract.ThrowIfNull(symbol.DeclaringMethod);

        using var names = TemporaryArray<string>.Empty;
        names.Add(symbol.Name);
        names.Add(GetMemberNameWithoutInterfaceName(symbol.DeclaringMethod.Name));

        if (symbol is not
            {
                ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.LocalFunction },
                ContainingType: INamedTypeSymbol { Name: "Program", ContainingNamespace.IsGlobalNamespace: true }
            })
        {
            names.Add(symbol.ContainingType.Name);
        }

        return FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, names.ToImmutableAndClear());
    }

    private static string GetMemberNameWithoutInterfaceName(string fullName)
    {
        var index = fullName.LastIndexOf('.');
        return index > 0 ? fullName[(index + 1)..] : fullName;
    }
}
