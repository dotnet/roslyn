// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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

internal sealed class CrefTypeParameterSymbolReferenceFinder : AbstractReferenceFinder<ITypeParameterSymbol>
{
    public static readonly CrefTypeParameterSymbolReferenceFinder Instance = new();

    private CrefTypeParameterSymbolReferenceFinder()
    {
    }

    protected override bool CanFind(ITypeParameterSymbol symbol)
        => symbol.TypeParameterKind == TypeParameterKind.Cref;

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
        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            var document = project.Solution.GetDocument(reference.SyntaxTree);
            if (document != null)
                processResult(document, processResultData);
        }

        return Task.CompletedTask;
    }

    protected override void FindReferencesInDocument<TData>(
        ITypeParameterSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var root = state.Root;
        var syntaxFacts = state.SyntaxFacts;
        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            if (reference.SyntaxTree == state.Root.SyntaxTree)
            {
                var token = root.FindToken(reference.Span.Start, findInsideTrivia: true);
                var attribute = token.GetAncestors<SyntaxNode>().FirstOrDefault(n => syntaxFacts.SyntaxKinds.XmlCrefAttribute == n.RawKind);

                if (attribute == null)
                    continue;

                var tokens = attribute.DescendantTokens().WhereAsArray(static (t, token) => t != token, token);
                FindReferencesInTokens(
                    symbol, state, tokens, processResult, processResultData, cancellationToken);
            }
        }
    }
}
