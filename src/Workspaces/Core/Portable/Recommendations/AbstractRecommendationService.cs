// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Recommendations
{
    internal abstract class AbstractRecommendationService : IRecommendationService
    {
        protected abstract Task<Tuple<ImmutableArray<ISymbol>, SyntaxContext>> GetRecommendedSymbolsAtPositionWorkerAsync(
            Workspace workspace, SemanticModel semanticModel, int position, OptionSet options, CancellationToken cancellationToken);

        public async Task<ImmutableArray<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
            Workspace workspace, SemanticModel semanticModel, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var result = await GetRecommendedSymbolsAtPositionWorkerAsync(workspace, semanticModel, position, options, cancellationToken).ConfigureAwait(false);

            var symbols = result.Item1;
            var context = new ShouldIncludeSymbolContext(result.Item2, cancellationToken);

            symbols = symbols.WhereAsArray(context.ShouldIncludeSymbol);
            return symbols;
        }

        protected static ImmutableArray<ISymbol> GetRecommendedNamespaceNameSymbols(
            SemanticModel semanticModel, SyntaxNode declarationSyntax, CancellationToken cancellationToken)
        {
            if (declarationSyntax == null)
            {
                throw new ArgumentNullException(nameof(declarationSyntax));
            }

            var containingNamespaceSymbol = semanticModel.Compilation.GetCompilationNamespace(
                semanticModel.GetEnclosingNamespace(declarationSyntax.SpanStart, cancellationToken));

            var symbols = semanticModel.LookupNamespacesAndTypes(declarationSyntax.SpanStart, containingNamespaceSymbol)
                                       .WhereAsArray(recommendationSymbol => IsNonIntersectingNamespace(recommendationSymbol, declarationSyntax));

            return symbols;
        }

        protected static bool IsNonIntersectingNamespace(
            ISymbol recommendationSymbol, SyntaxNode declarationSyntax)
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
            //     Namespace Foo.$$
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

        /// <summary>
        /// If container is a tuple type, any of its tuple element which has a friendly name will cause
        /// the suppression of the corresponding default name (ItemN).
        /// In that case, Rest is also removed.
        /// </summary>
        protected static ImmutableArray<ISymbol> SuppressDefaultTupleElements(
            INamespaceOrTypeSymbol container, ImmutableArray<ISymbol> symbols)
        {
            if (!container.IsType)
            {
                return symbols;
            }

            var type = (ITypeSymbol)container;
            if (!type.IsTupleType)
            {
                return symbols;
            }

            var tuple = (INamedTypeSymbol)type;
            var elementNames = tuple.TupleElementNames;
            if (elementNames.IsDefault)
            {
                return symbols;
            }

            // TODO This should be revised once we have a good public API for tuple fields
            // See https://github.com/dotnet/roslyn/issues/13229
            var fieldsToRemove = elementNames.Select((n, i) => IsFriendlyName(i, n) ? "Item" + (i + 1) : null)
                .Where(n => n != null).Concat("Rest").ToSet();

            return symbols.WhereAsArray(
                s => s.Kind != SymbolKind.Field ||
                     elementNames.Contains(s.Name) || 
                     !fieldsToRemove.Contains(s.Name));
        }

        private static bool IsFriendlyName(int i, string elementName)
        {
            return elementName != null && string.Compare(elementName, "Item" + (i + 1), StringComparison.OrdinalIgnoreCase) != 0;
        }

        private sealed class ShouldIncludeSymbolContext
        {
            private readonly SyntaxContext _context;
            private readonly CancellationToken _cancellationToken;
            private IEnumerable<INamedTypeSymbol> _lazyOuterTypesAndBases;
            private IEnumerable<INamedTypeSymbol> _lazyEnclosingTypeBases;

            internal ShouldIncludeSymbolContext(SyntaxContext context, CancellationToken cancellationToken)
            {
                _context = context;
                _cancellationToken = cancellationToken;
            }

            internal bool ShouldIncludeSymbol(ISymbol symbol)
            {
                var isMember = false;
                switch (symbol.Kind)
                {
                    case SymbolKind.NamedType:
                        var namedType = (INamedTypeSymbol)symbol;
                        if (namedType.SpecialType == SpecialType.System_Void)
                        {
                            return false;
                        }

                        break;

                    case SymbolKind.Method:
                        switch (((IMethodSymbol)symbol).MethodKind)
                        {
                            case MethodKind.EventAdd:
                            case MethodKind.EventRemove:
                            case MethodKind.EventRaise:
                            case MethodKind.PropertyGet:
                            case MethodKind.PropertySet:
                                return false;
                        }

                        isMember = true;
                        break;

                    case SymbolKind.Event:
                    case SymbolKind.Field:
                    case SymbolKind.Property:
                        isMember = true;
                        break;

                    case SymbolKind.TypeParameter:
                        return ((ITypeParameterSymbol)symbol).TypeParameterKind != TypeParameterKind.Cref;
                }

                if (_context.IsAttributeNameContext)
                {
                    var enclosingSymbol = _context.SemanticModel.GetEnclosingNamedType(_context.LeftToken.SpanStart, _cancellationToken);
                    return symbol.IsOrContainsAccessibleAttribute(enclosingSymbol, _context.SemanticModel.Compilation.Assembly);
                }

                if (_context.IsEnumTypeMemberAccessContext)
                {
                    return symbol.Kind == SymbolKind.Field;
                }

                // In an expression or statement context, we don't want to display instance members declared in outer containing types.
                if ((_context.IsStatementContext || _context.IsAnyExpressionContext) &&
                    !symbol.IsStatic &&
                    isMember)
                {
                    var containingTypeOriginalDefinition = symbol.ContainingType.OriginalDefinition;
                    if (this.GetOuterTypesAndBases().Contains(containingTypeOriginalDefinition))
                    {
                        return this.GetEnclosingTypeBases().Contains(containingTypeOriginalDefinition);
                    }
                }

                var namespaceSymbol = symbol as INamespaceSymbol;
                if (namespaceSymbol != null)
                {
                    return namespaceSymbol.ContainsAccessibleTypesOrNamespaces(_context.SemanticModel.Compilation.Assembly);
                }

                return true;
            }

            private IEnumerable<INamedTypeSymbol> GetOuterTypesAndBases()
            {
                if (_lazyOuterTypesAndBases == null)
                {
                    _lazyOuterTypesAndBases = _context.GetOuterTypes(_cancellationToken).SelectMany(o => o.GetBaseTypesAndThis()).Select(t => t.OriginalDefinition);
                }

                return _lazyOuterTypesAndBases;
            }

            private IEnumerable<INamedTypeSymbol> GetEnclosingTypeBases()
            {
                if (_lazyEnclosingTypeBases == null)
                {
                    var enclosingType = _context.SemanticModel.GetEnclosingNamedType(_context.LeftToken.SpanStart, _cancellationToken);
                    _lazyEnclosingTypeBases = (enclosingType == null) ?
                        SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>() :
                        enclosingType.GetBaseTypes().Select(b => b.OriginalDefinition);
                }

                return _lazyEnclosingTypeBases;
            }
        }
    }
}
