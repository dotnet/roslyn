// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal abstract class AbstractTypeParameterSymbolReferenceFinder : AbstractReferenceFinder<ITypeParameterSymbol>
    {
        protected sealed override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            ITypeParameterSymbol symbol,
            FindReferencesDocumentState state,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // TODO(cyrusn): Method type parameters are like locals.  They are only in scope in the bounds of the method
            // they're declared within.  We could improve perf by limiting our search by only looking within the method
            // body's span. 

            // Type parameters can be found both in normal type locations, and in object creation expression (e.g. `new
            // T()`). In the former case GetSymbolInfo can be used to bind the symbol and check if it matches this symbol.
            // in the latter though GetSymbolInfo will fail and we have to directly check if we have the right type info.

            var tokens = await FindMatchingIdentifierTokensAsync(state, symbol.Name, cancellationToken).ConfigureAwait(false);

            var normalReferences = await FindReferencesInTokensAsync(
                symbol, state,
                tokens.WhereAsArray(static (token, state) => !IsObjectCreationToken(token, state), state),
                cancellationToken).ConfigureAwait(false);

            var objectCreationReferences = GetObjectCreationReferences(
                tokens.WhereAsArray(static (token, state) => IsObjectCreationToken(token, state), state));

            return normalReferences.Concat(objectCreationReferences);

            static bool IsObjectCreationToken(SyntaxToken token, FindReferencesDocumentState state)
            {
                var syntaxFacts = state.SyntaxFacts;
                return syntaxFacts.IsIdentifierName(token.Parent) &&
                       syntaxFacts.IsObjectCreationExpression(token.Parent.Parent);
            }

            ImmutableArray<FinderLocation> GetObjectCreationReferences(ImmutableArray<SyntaxToken> objectCreationTokens)
            {
                using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var result);

                foreach (var token in objectCreationTokens)
                {
                    Contract.ThrowIfNull(token.Parent?.Parent);
                    var typeInfo = state.SemanticModel.GetTypeInfo(token.Parent.Parent, cancellationToken);
                    if (symbol.Equals(typeInfo.Type, SymbolEqualityComparer.Default))
                        result.Add(CreateFinderLocation(state, token, CandidateReason.None, cancellationToken));
                }

                return result.ToImmutable();
            }
        }
    }
}
