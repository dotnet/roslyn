// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class ConstructorInitializerSymbolReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
        {
            return symbol.MethodKind == MethodKind.Constructor;
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var contextInfo = await SyntaxTreeInfo.GetContextInfoAsync(d, c).ConfigureAwait(false);
                if (contextInfo.ContainsBaseConstructorInitializer)
                {
                    return true;
                }

                var identifierInfo = await SyntaxTreeInfo.GetIdentifierInfoAsync(d, c).ConfigureAwait(false);
                if (identifierInfo.ProbablyContainsIdentifier(symbol.ContainingType.Name))
                {
                    if (contextInfo.ContainsThisConstructorInitializer)
                    {
                        return true;
                    }
                    else if (project.Language == LanguageNames.VisualBasic && identifierInfo.ProbablyContainsIdentifier("New"))
                    {
                        // "New" can be explicitly accessed in xml doc comments to reference a constructor.
                        return true;
                    }
                }

                return false;
            }, cancellationToken);
        }

        protected override async Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var typeName = methodSymbol.ContainingType.Name;

            Func<SyntaxToken, bool> tokensMatch = t =>
            {
                if (syntaxFactsService.IsBaseConstructorInitializer(t))
                {
                    var containingType = semanticModel.GetEnclosingNamedType(t.SpanStart, cancellationToken);
                    return containingType != null && containingType.BaseType != null && containingType.BaseType.Name == typeName;
                }
                else if (syntaxFactsService.IsThisConstructorInitializer(t))
                {
                    var containingType = semanticModel.GetEnclosingNamedType(t.SpanStart, cancellationToken);
                    return containingType != null && containingType.Name == typeName;
                }
                else if (semanticModel.Language == LanguageNames.VisualBasic && t.IsPartOfStructuredTrivia())
                {
                    return true;
                }

                return false;
            };

            var tokens = await document.GetConstructorInitializerTokensAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel.Language == LanguageNames.VisualBasic)
            {
                tokens = tokens.Concat(await document.GetIdentifierOrGlobalNamespaceTokensWithTextAsync("New", cancellationToken).ConfigureAwait(false)).Distinct();
            }

            return await FindReferencesInTokensAsync(
                 methodSymbol,
                 document,
                 tokens,
                 tokensMatch,
                 cancellationToken).ConfigureAwait(false);
        }
    }
}
