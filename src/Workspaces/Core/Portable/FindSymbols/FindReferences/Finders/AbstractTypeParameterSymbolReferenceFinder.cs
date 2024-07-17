// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal abstract class AbstractTypeParameterSymbolReferenceFinder : AbstractReferenceFinder<ITypeParameterSymbol>
{
    protected sealed override void FindReferencesInDocument<TData>(
        ITypeParameterSymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // TODO(cyrusn): Method type parameters are like locals.  They are only in scope in the bounds of the method
        // they're declared within.  We could improve perf by limiting our search by only looking within the method
        // body's span. 

        // Type parameters can be found both in normal type locations, and in object creation expression (e.g. `new
        // T()`). In the former case GetSymbolInfo can be used to bind the symbol and check if it matches this symbol.
        // in the latter though GetSymbolInfo will fail and we have to directly check if we have the right type info.

        var tokens = FindMatchingIdentifierTokens(state, symbol.Name, cancellationToken);

        FindReferencesInTokens(
            symbol, state,
            tokens.WhereAsArray(predicate: static (token, state) => !IsObjectCreationToken(token, state), arg: state),
            processResult,
            processResultData,
            cancellationToken);

        GetObjectCreationReferences(
            tokens.WhereAsArray(predicate: static (token, state) => IsObjectCreationToken(token, state), arg: state),
            processResult,
            processResultData);

        return;

        static bool IsObjectCreationToken(SyntaxToken token, FindReferencesDocumentState state)
        {
            var syntaxFacts = state.SyntaxFacts;
            return syntaxFacts.IsIdentifierName(token.Parent) &&
                   syntaxFacts.IsObjectCreationExpression(token.Parent.Parent);
        }

        void GetObjectCreationReferences(
            ImmutableArray<SyntaxToken> objectCreationTokens,
            Action<FinderLocation, TData> processResult,
            TData processResultData)
        {
            foreach (var token in objectCreationTokens)
            {
                Contract.ThrowIfNull(token.Parent?.Parent);
                var typeInfo = state.SemanticModel.GetTypeInfo(token.Parent.Parent, cancellationToken);
                if (symbol.Equals(typeInfo.Type, SymbolEqualityComparer.Default))
                    processResult(CreateFinderLocation(state, token, CandidateReason.None, cancellationToken), processResultData);
            }
        }
    }
}
