// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal sealed class OperatorSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
            => symbol.MethodKind == MethodKind.UserDefinedOperator;

        protected sealed override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var op = symbol.GetPredefinedOperator();
            var documentsWithOp = await FindDocumentsAsync(project, documents, op, cancellationToken).ConfigureAwait(false);
            var documentsWithGlobalAttributes = await FindDocumentsWithGlobalSuppressMessageAttributeAsync(project, documents, cancellationToken).ConfigureAwait(false);
            return documentsWithOp.Concat(documentsWithGlobalAttributes);
        }

        protected sealed override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol,
            HashSet<string>? globalAliases,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var op = symbol.GetPredefinedOperator();

            var opReferences = await FindReferencesInDocumentAsync(symbol, document, semanticModel, t =>
                IsPotentialReference(syntaxFacts, op, t),
                cancellationToken).ConfigureAwait(false);
            var suppressionReferences = await FindReferencesInDocumentInsideGlobalSuppressionsAsync(document, semanticModel, symbol, cancellationToken).ConfigureAwait(false);

            return opReferences.Concat(suppressionReferences);
        }

        private static bool IsPotentialReference(
            ISyntaxFactsService syntaxFacts,
            PredefinedOperator op,
            SyntaxToken token)
        {
            return syntaxFacts.TryGetPredefinedOperator(token, out var actualOperator) && actualOperator == op;
        }
    }
}
