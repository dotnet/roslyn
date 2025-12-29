// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

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
