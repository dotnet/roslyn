// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
            {
                // if we have `partial class C { ... : base(...) }` we have to assume it might be a match, as the base
                // type reference might be in a another part of the partial in another file.
                if (index.ContainsPartialClass)
                    return true;

                // Otherwise, if it doesn't have any partial types, ensure that the base type name is referenced in the
                // same file.  e.g. `partial class C : B { ... base(...) }`.   This allows us to greatly filter down the
                // number of matches, presuming that most inheriting types in a project are not themselves partial.
                if (index.ProbablyContainsIdentifier(name))
                    return true;
            }

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
