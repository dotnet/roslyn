// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal abstract class AbstractMethodOrPropertyOrEventSymbolReferenceFinder<TSymbol> : AbstractReferenceFinder<TSymbol>
        where TSymbol : ISymbol
    {
        protected AbstractMethodOrPropertyOrEventSymbolReferenceFinder()
        {
        }

        protected override async Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            TSymbol symbol,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // Static methods can't cascade.
            if (!symbol.IsStatic)
            {
                if (symbol.IsImplementableMember())
                {
                    // We have an interface method.  Find all implementations of that method and
                    // cascade to them.
                    return await SymbolFinder.FindMemberImplementationsArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // We have a normal method.  Find any interface methods that it implicitly or
                    // explicitly implements and cascade down to those.
                    var interfaceMembersImplemented = await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(
                        symbol, solution, projects, cancellationToken).ConfigureAwait(false);

                    // Finally, methods can cascade through virtual/override inheritance.  NOTE(cyrusn):
                    // We only need to go up or down one level.  Then, when we're finding references on
                    // those members, we'll end up traversing the entire hierarchy.
                    var overrides = await SymbolFinder.FindOverridesArrayAsync(
                        symbol, solution, projects, cancellationToken).ConfigureAwait(false);

                    var overriddenMember = symbol.OverriddenMember();
                    if (overriddenMember == null)
                    {
                        return interfaceMembersImplemented.Concat(overrides);
                    }

                    return interfaceMembersImplemented.Concat(overrides).Concat(overriddenMember);
                }
            }

            return ImmutableArray<ISymbol>.Empty;
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
                return ImmutableArray.Create(symbols.CurrentProperty.GetMethod);
            }

            if (semanticFacts.IsWrittenTo(model, node, cancellationToken))
            {
                // if it was only written to, then only the setter was referenced.
                // if it was written *and* read, then both accessors were referenced.
                return semanticFacts.IsOnlyWrittenTo(model, node, cancellationToken)
                    ? ImmutableArray.Create(property.SetMethod)
                    : ImmutableArray.Create(property.GetMethod, property.SetMethod);
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

                return inNameOf || inStructuredTrivia
                    ? ImmutableArray<IMethodSymbol>.Empty
                    : ImmutableArray.Create(property.GetMethod);
            }
        }
    }
}
