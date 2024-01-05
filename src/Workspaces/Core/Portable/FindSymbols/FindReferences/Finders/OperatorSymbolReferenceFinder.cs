// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal sealed class OperatorSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
            => symbol.MethodKind is MethodKind.UserDefinedOperator or MethodKind.BuiltinOperator;

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

        private static Task<ImmutableArray<Document>> FindDocumentsAsync(
            Project project,
            IImmutableSet<Document>? documents,
            PredefinedOperator op,
            CancellationToken cancellationToken)
        {
            if (op == PredefinedOperator.None)
                return SpecializedTasks.EmptyImmutableArray<Document>();

            return FindDocumentsWithPredicateAsync(
                project, documents, static (index, op) => index.ContainsPredefinedOperator(op), op, cancellationToken);
        }

        protected sealed override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol,
            FindReferencesDocumentState state,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var op = symbol.GetPredefinedOperator();
            var tokens = state.Root
                .DescendantTokens(descendIntoTrivia: true)
                .WhereAsArray(
                    static (token, tuple) => IsPotentialReference(tuple.state.SyntaxFacts, tuple.op, token),
                    (state, op));

            var opReferences = await FindReferencesInTokensAsync(
                symbol, state, tokens, cancellationToken).ConfigureAwait(false);
            var suppressionReferences = await FindReferencesInDocumentInsideGlobalSuppressionsAsync(
                symbol, state, cancellationToken).ConfigureAwait(false);

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
