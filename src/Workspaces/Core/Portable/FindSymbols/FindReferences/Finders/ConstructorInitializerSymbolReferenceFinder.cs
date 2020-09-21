// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => symbol.MethodKind == MethodKind.Constructor;

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return FindDocumentsAsync(project, documents, async (d, c) =>
            {
                var index = await SyntaxTreeIndex.GetIndexAsync(d, c).ConfigureAwait(false);
                if (index.ContainsBaseConstructorInitializer)
                {
                    return true;
                }

                if (index.ProbablyContainsIdentifier(symbol.ContainingType.Name))
                {
                    if (index.ContainsThisConstructorInitializer)
                    {
                        return true;
                    }
                    else if (project.Language == LanguageNames.VisualBasic && index.ProbablyContainsIdentifier("New"))
                    {
                        // "New" can be explicitly accessed in xml doc comments to reference a constructor.
                        return true;
                    }
                }

                return false;
            }, cancellationToken);
        }

        protected override async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var typeName = methodSymbol.ContainingType.Name;

            var tokens = await document.GetConstructorInitializerTokensAsync(semanticModel, cancellationToken).ConfigureAwait(false);
            if (semanticModel.Language == LanguageNames.VisualBasic)
            {
                tokens = tokens.Concat(await GetIdentifierOrGlobalNamespaceTokensWithTextAsync(
                    document, semanticModel, "New", cancellationToken).ConfigureAwait(false)).Distinct();
            }

            return await FindReferencesInTokensAsync(
                 methodSymbol,
                 document,
                 semanticModel,
                 tokens,
                 TokensMatch,
                 cancellationToken).ConfigureAwait(false);

            // local functions
            bool TokensMatch(SyntaxToken t)
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
            }
        }
    }
}
