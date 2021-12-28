﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal abstract class AbstractMethodOrPropertyOrEventSymbolReferenceFinder<TSymbol> : AbstractReferenceFinder<TSymbol>
        where TSymbol : ISymbol
    {
        protected AbstractMethodOrPropertyOrEventSymbolReferenceFinder()
        {
        }

        protected static ImmutableArray<IMethodSymbol> GetReferencedAccessorSymbols(
            ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts,
            SemanticModel model, IPropertySymbol property, SyntaxNode node, CancellationToken cancellationToken)
        {
            if (syntaxFacts.IsForEachStatement(node))
            {
                var symbols = semanticFacts.GetForEachSymbols(model, node);

                // the only accessor method referenced in a foreach-statement is the .Current's
                // get-accessor
                return symbols.CurrentProperty.GetMethod == null
                    ? ImmutableArray<IMethodSymbol>.Empty
                    : ImmutableArray.Create(symbols.CurrentProperty.GetMethod);
            }

            if (semanticFacts.IsWrittenTo(model, node, cancellationToken))
            {
                // if it was only written to, then only the setter was referenced.
                // if it was written *and* read, then both accessors were referenced.
                using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var result);
                result.AddIfNotNull(property.SetMethod);

                if (!semanticFacts.IsOnlyWrittenTo(model, node, cancellationToken))
                    result.AddIfNotNull(property.GetMethod);

                return result.ToImmutable();
            }
            else
            {
                // Wasn't written. This could be a normal read, or it could be neither a read nor
                // write. Example of this include:
                //
                // 1) referencing through something like nameof().
                // 2) referencing in a cref in a doc-comment.
                //
                // This list is thought to be complete.  However, if new examples are found, they
                // can be added here.
                var inNameOf = semanticFacts.IsInsideNameOfExpression(model, node, cancellationToken);
                var inStructuredTrivia = node.IsPartOfStructuredTrivia();

                return inNameOf || inStructuredTrivia || property.GetMethod == null
                    ? ImmutableArray<IMethodSymbol>.Empty
                    : ImmutableArray.Create(property.GetMethod);
            }
        }
    }
}
