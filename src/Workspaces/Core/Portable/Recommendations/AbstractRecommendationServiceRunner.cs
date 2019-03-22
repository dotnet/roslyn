// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Recommendations
{
    internal abstract class AbstractRecommendationServiceRunner<TSyntaxContext>
        where TSyntaxContext : SyntaxContext
    {
        protected readonly TSyntaxContext _context;
        protected readonly bool _filterOutOfScopeLocals;
        protected readonly CancellationToken _cancellationToken;

        public AbstractRecommendationServiceRunner(
            TSyntaxContext context, 
            bool filterOutOfScopeLocals, 
            CancellationToken cancellationToken)
        {
            _context = context;
            _filterOutOfScopeLocals = filterOutOfScopeLocals;
            _cancellationToken = cancellationToken;
        }

        public abstract ImmutableArray<ISymbol> GetSymbols();

        protected ImmutableArray<ISymbol> GetSymbolsForNamespaceDeclarationNameContext<TNamespaceDeclarationSyntax>()
            where TNamespaceDeclarationSyntax : SyntaxNode
        {
            var declarationSyntax = _context.TargetToken.GetAncestor<TNamespaceDeclarationSyntax>();

            if (declarationSyntax == null)
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            var semanticModel = _context.SemanticModel;
            var containingNamespaceSymbol = semanticModel.Compilation.GetCompilationNamespace(
                semanticModel.GetEnclosingNamespace(declarationSyntax.SpanStart, _cancellationToken));

            var symbols = semanticModel.LookupNamespacesAndTypes(declarationSyntax.SpanStart, containingNamespaceSymbol)
                                       .WhereAsArray(recommendationSymbol => IsNonIntersectingNamespace(recommendationSymbol, declarationSyntax));

            return symbols;
        }

        protected static bool IsNonIntersectingNamespace(ISymbol recommendationSymbol, SyntaxNode declarationSyntax)
        {
            //
            // Apart from filtering out non-namespace symbols, this also filters out the symbol
            // currently being declared. For example...
            //
            //     namespace X$$
            //
            // ...X won't show in the completion list (unless it is also declared elsewhere).
            //
            // In addition, in VB, it will filter out Bar from the sample below...
            //
            //     Namespace Goo.$$
            //         Namespace Bar
            //         End Namespace
            //     End Namespace
            //
            // ...unless, again, it's also declared elsewhere.
            //
            return recommendationSymbol.IsNamespace() &&
                   recommendationSymbol.Locations.Any(
                       candidateLocation => !(declarationSyntax.SyntaxTree == candidateLocation.SourceTree &&
                                              declarationSyntax.Span.IntersectsWith(candidateLocation.SourceSpan)));
        }

        protected ImmutableArray<ISymbol> GetSymbols(
            INamespaceOrTypeSymbol container,
            int position,
            bool excludeInstance,
            bool useBaseReferenceAccessibility)
        {
            return useBaseReferenceAccessibility
                ? _context.SemanticModel.LookupBaseMembers(position)
                : LookupSymbolsInContainer(container, position, excludeInstance);
        }

        protected ImmutableArray<ISymbol> LookupSymbolsInContainer(
            INamespaceOrTypeSymbol container, int position, bool excludeInstance)
        {
            return excludeInstance
                ? _context.SemanticModel.LookupStaticMembers(position, container)
                : SuppressDefaultTupleElements(
                    container,
                    _context.SemanticModel.LookupSymbols(position, container, includeReducedExtensionMethods: true));
        }

        /// <summary>
        /// If container is a tuple type, any of its tuple element which has a friendly name will cause
        /// the suppression of the corresponding default name (ItemN).
        /// In that case, Rest is also removed.
        /// </summary>
        protected static ImmutableArray<ISymbol> SuppressDefaultTupleElements(
            INamespaceOrTypeSymbol container, ImmutableArray<ISymbol> symbols)
        {
            var namedType = container as INamedTypeSymbol;
            if (namedType?.IsTupleType != true)
            {
                // container is not a tuple
                return symbols;
            }

            //return tuple elements followed by other members that are not fields
            return ImmutableArray<ISymbol>.CastUp(namedType.TupleElements).
                Concat(symbols.WhereAsArray(s => s.Kind != SymbolKind.Field));
        }
    }
}
