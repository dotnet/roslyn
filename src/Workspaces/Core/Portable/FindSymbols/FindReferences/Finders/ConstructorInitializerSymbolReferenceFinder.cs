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

internal sealed class ConstructorInitializerSymbolReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
{
    protected override bool CanFind(IMethodSymbol symbol)
        => symbol.MethodKind == MethodKind.Constructor;

    protected override Task DetermineDocumentsToSearchAsync<TData>(
        IMethodSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        return FindDocumentsAsync(project, documents, static async (document, name, cancellationToken) =>
        {
            var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
            if (index.ContainsBaseConstructorInitializer)
                return true;

            if (index.ProbablyContainsIdentifier(name))
            {
                if (index.ContainsThisConstructorInitializer)
                {
                    return true;
                }
                else if (document.Project.Language == LanguageNames.VisualBasic && index.ProbablyContainsIdentifier("New"))
                {
                    // "New" can be explicitly accessed in xml doc comments to reference a constructor.
                    return true;
                }
            }

            return false;
        }, symbol.ContainingType.Name, processResult, processResultData, cancellationToken);
    }

    protected sealed override void FindReferencesInDocument<TData>(
        IMethodSymbol methodSymbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var tokens = state.Cache.GetConstructorInitializerTokens(cancellationToken);
        if (state.SemanticModel.Language == LanguageNames.VisualBasic)
            tokens = tokens.Concat(FindMatchingIdentifierTokens(state, "New", cancellationToken)).Distinct();

        var totalTokens = tokens.WhereAsArray(
            static (token, tuple) => TokensMatch(tuple.state, token, tuple.methodSymbol.ContainingType.Name, tuple.cancellationToken),
            (state, methodSymbol, cancellationToken));

        FindReferencesInTokens(methodSymbol, state, totalTokens, processResult, processResultData, cancellationToken);
        return;

        // local functions
        static bool TokensMatch(
            FindReferencesDocumentState state,
            SyntaxToken token,
            string typeName,
            CancellationToken cancellationToken)
        {
            var semanticModel = state.SemanticModel;
            var syntaxFacts = state.SyntaxFacts;

            if (syntaxFacts.IsBaseConstructorInitializer(token))
            {
                var containingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);
                return containingType != null && containingType.BaseType != null && containingType.BaseType.Name == typeName;
            }
            else if (syntaxFacts.IsThisConstructorInitializer(token))
            {
                var containingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);
                return containingType != null && containingType.Name == typeName;
            }
            else if (semanticModel.Language == LanguageNames.VisualBasic && token.IsPartOfStructuredTrivia())
            {
                return true;
            }

            return false;
        }
    }
}
