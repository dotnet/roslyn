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
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class ConstructorInitializerSymbolReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
        {
            return symbol.MethodKind == MethodKind.Constructor;
        }

        protected override Task<IEnumerable<Document>> DetermineDocumentsToSearchAsync(
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
                    var declaredInfo = await SyntaxTreeInfo.GetDeclarationInfoAsync(d, c).ConfigureAwait(false);
                    if (contextInfo.ContainsThisConstructorInitializer
                        || declaredInfo.DeclaredSymbolInfos.Any(i => i.Kind == DeclaredSymbolInfoKind.Constructor))
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

        protected override async Task<IEnumerable<ReferenceLocation>> FindReferencesInDocumentAsync(
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
                else if (methodSymbol.Parameters.IsDefaultOrEmpty
                    && syntaxFactsService.IsConstructorToken(t))
                {
                    var containingType = semanticModel.GetEnclosingNamedType(t.SpanStart, cancellationToken);
                    return containingType != null
                        && containingType.BaseType != null
                        && containingType.BaseType.Name == typeName;
                }

                return false;
            };

            var tokens = await document.GetConstructorInitializerTokensAsync(cancellationToken).ConfigureAwait(false);
            var ctorTokens = await document.GetConstructorTokensAsync(cancellationToken).ConfigureAwait(false);
            tokens = tokens.Concat(ctorTokens);
            if (semanticModel.Language == LanguageNames.VisualBasic)
            {
                tokens = tokens.Concat(await document.GetIdentifierOrGlobalNamespaceTokensWithTextAsync("New", cancellationToken).ConfigureAwait(false)).Distinct();
            }

            return await FindReferencesAndConstructorsInTokensAsync(
                 methodSymbol,
                 document,
                 syntaxFactsService,
                 tokens,
                 tokensMatch,
                 cancellationToken).ConfigureAwait(false);
        }

        private Task<IEnumerable<ReferenceLocation>> FindReferencesAndConstructorsInTokensAsync(
            IMethodSymbol symbol,
            Document document,
            ISyntaxFactsService syntaxFactsService,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, bool> tokensMatch,
            CancellationToken cancellationToken,
            Func<SyntaxToken, SyntaxNode> findParentNode = null)
        {
            var standardSymbolsMatch = GetStandardSymbolsMatchFunction(symbol, findParentNode, document.Project.Solution, cancellationToken);
            Func<SyntaxToken, SemanticModel, ValueTuple<bool, CandidateReason>> ctorMatch = (t, model) =>
            {
                // tokens are already filtered by base type 
                if (syntaxFactsService.IsConstructorToken(t))
                {
                    if (t.Parent.DescendantTokens(descendIntoTrivia: false).All(token =>
                        !syntaxFactsService.IsThisConstructorInitializer(token) && !syntaxFactsService.IsBaseConstructorInitializer(token)))
                    {
                        return ValueTuple.Create(true, CandidateReason.None);
                    }
                }
                return ValueTuple.Create(false, CandidateReason.None);
            };

            Func<SyntaxToken, SemanticModel, ValueTuple<bool, CandidateReason>> symbolAndCtorMatch = (t, model) =>
            {
                var symbolMatchResult = standardSymbolsMatch(t, model);
                if (symbolMatchResult.Item1)
                {
                    return symbolMatchResult;
                }
                else
                {
                    return ctorMatch(t, model);
                }
            };

            return FindReferencesInTokensAsync(
                document,
                tokens,
                tokensMatch,
                symbolAndCtorMatch,
                cancellationToken);
        }
    }
}
