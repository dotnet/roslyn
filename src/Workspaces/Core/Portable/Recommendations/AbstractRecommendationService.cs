// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Recommendations;

internal abstract partial class AbstractRecommendationService<
    TSyntaxContext,
    TAnonymousFunctionSyntax> : IRecommendationService
    where TSyntaxContext : SyntaxContext
    where TAnonymousFunctionSyntax : SyntaxNode
{
    protected abstract AbstractRecommendationServiceRunner CreateRunner(
        TSyntaxContext context, bool filterOutOfScopeLocals, CancellationToken cancellationToken);

    public RecommendedSymbols GetRecommendedSymbolsInContext(SyntaxContext syntaxContext, RecommendationServiceOptions options, CancellationToken cancellationToken)
    {
        var semanticModel = syntaxContext.SemanticModel;
        var result = CreateRunner((TSyntaxContext)syntaxContext, options.FilterOutOfScopeLocals, cancellationToken).GetRecommendedSymbols();

        var namedSymbols = result.NamedSymbols;
        var unnamedSymbols = result.UnnamedSymbols;

        namedSymbols = namedSymbols.FilterToVisibleAndBrowsableSymbols(options.HideAdvancedMembers, semanticModel.Compilation);
        unnamedSymbols = unnamedSymbols.FilterToVisibleAndBrowsableSymbols(options.HideAdvancedMembers, semanticModel.Compilation);

        var shouldIncludeSymbolContext = new ShouldIncludeSymbolContext(syntaxContext, cancellationToken);
        namedSymbols = namedSymbols.WhereAsArray(shouldIncludeSymbolContext.ShouldIncludeSymbol);
        unnamedSymbols = unnamedSymbols.WhereAsArray(shouldIncludeSymbolContext.ShouldIncludeSymbol);

        return new RecommendedSymbols(namedSymbols, unnamedSymbols);
    }

    protected static ISet<INamedTypeSymbol> ComputeOuterTypes(SyntaxContext context, CancellationToken cancellationToken)
    {
        var enclosingSymbol = context.SemanticModel.GetEnclosingSymbol(context.LeftToken.SpanStart, cancellationToken);
        if (enclosingSymbol != null)
        {
            var containingType = enclosingSymbol.GetContainingTypeOrThis();
            if (containingType != null)
            {
                return containingType.GetContainingTypes().ToSet();
            }
        }

        return SpecializedCollections.EmptySet<INamedTypeSymbol>();
    }

    private sealed class ShouldIncludeSymbolContext
    {
        private readonly SyntaxContext _context;
        private readonly CancellationToken _cancellationToken;
        private ImmutableArray<INamedTypeSymbol> _lazyOuterTypesAndBases;
        private ImmutableArray<INamedTypeSymbol> _lazyEnclosingTypeBases;

        internal ShouldIncludeSymbolContext(SyntaxContext context, CancellationToken cancellationToken)
        {
            _context = context;
            _cancellationToken = cancellationToken;
        }

        internal bool ShouldIncludeSymbol(ISymbol symbol)
        {
            var isMember = false;
            var isConstructorParameter = false;
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

                case SymbolKind.Parameter:
                    isConstructorParameter = symbol.ContainingSymbol is IMethodSymbol
                    {
                        MethodKind: MethodKind.Constructor
                    };
                    break;
            }

            if (_context.IsAttributeNameContext)
            {
                return symbol.IsOrContainsAccessibleAttribute(
                    _context.SemanticModel.GetEnclosingNamedType(_context.LeftToken.SpanStart, _cancellationToken),
                    _context.SemanticModel.Compilation.Assembly,
                    _cancellationToken);
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

            // Primary constructor parameters should not appear in static context
            if (_context.IsStatementContext &&
                !_context.IsAnyExpressionContext &&
                !symbol.IsStatic &&
                isConstructorParameter)
            {
                // Only referrable when inside a nameof
                return _context.IsNameOfContext;
            }

            if (symbol is INamespaceSymbol namespaceSymbol)
            {
                return namespaceSymbol.ContainsAccessibleTypesOrNamespaces(_context.SemanticModel.Compilation.Assembly);
            }

            return true;
        }

        private ImmutableArray<INamedTypeSymbol> GetOuterTypesAndBases()
        {
            if (_lazyOuterTypesAndBases.IsDefault)
            {
                _lazyOuterTypesAndBases = ComputeOuterTypes(_context, _cancellationToken)
                    .SelectMany(o => o.GetBaseTypesAndThis())
                    .SelectAsArray(t => t.OriginalDefinition);
            }

            return _lazyOuterTypesAndBases;
        }

        private ImmutableArray<INamedTypeSymbol> GetEnclosingTypeBases()
        {
            if (_lazyEnclosingTypeBases.IsDefault)
            {
                var enclosingType = _context.SemanticModel.GetEnclosingNamedType(_context.LeftToken.SpanStart, _cancellationToken);
                _lazyEnclosingTypeBases = enclosingType == null
                    ? []
                    : enclosingType.GetBaseTypes().SelectAsArray(b => b.OriginalDefinition);
            }

            return _lazyEnclosingTypeBases;
        }
    }
}
