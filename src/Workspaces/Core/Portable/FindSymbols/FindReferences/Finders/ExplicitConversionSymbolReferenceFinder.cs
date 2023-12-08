// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal sealed partial class ExplicitConversionSymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
            => symbol is { MethodKind: MethodKind.Conversion, Name: WellKnownMemberNames.ExplicitConversionName or WellKnownMemberNames.ImplicitConversionName } &&
               GetUnderlyingNamedType(symbol.ReturnType) is not null;

        private static INamedTypeSymbol? GetUnderlyingNamedType(ITypeSymbol symbol)
            => UnderlyingNamedTypeVisitor.Instance.Visit(symbol);

        protected sealed override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // Look for documents that both contain an explicit cast in them as well as a reference to the type in the
            // explicit conversion.  i.e. if we have `public static explicit operator Goo(Bar b);` we want to find files
            // both with `Goo` `and `(...)` in them as we're looking for cases of `(Goo)...`.
            //
            // Note that explicit conversions may be to complex types (like arrays).  For example:
            //
            //      public static explicit operator Goo[](Bar b);
            //
            // So we need to find the underlying named type `Goo` (if there is one) to find references.

            var underlyingNamedType = GetUnderlyingNamedType(symbol.ReturnType);
            Contract.ThrowIfNull(underlyingNamedType);
            var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, underlyingNamedType.Name).ConfigureAwait(false);
            var documentsWithType = await FindDocumentsAsync(project, documents, underlyingNamedType.SpecialType.ToPredefinedType(), cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            // Ignore any documents that don't also have an explicit cast in them.
            foreach (var document in documentsWithName.Concat(documentsWithType).Distinct())
            {
                var index = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                if (index.ContainsConversion)
                    result.Add(document);
            }

            return result.ToImmutable();
        }

        protected sealed override ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol,
            FindReferencesDocumentState state,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var tokens = state.Root
                .DescendantTokens(descendIntoTrivia: true)
                .WhereAsArray(
                    static (token, state) => IsPotentialReference(state.SyntaxFacts, token),
                    state);

            return FindReferencesInTokensAsync(symbol, state, tokens, cancellationToken);
        }

        private static bool IsPotentialReference(
            ISyntaxFactsService syntaxFacts, SyntaxToken token)
        {
            var node = token.GetRequiredParent();
            return node.GetFirstToken() == token && syntaxFacts.IsConversionExpression(node);
        }
    }
}
