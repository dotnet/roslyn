// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Recommendations
{
    internal abstract class AbstractRecommendationService<TSyntaxContext> : IRecommendationService
        where TSyntaxContext : SyntaxContext
    {
        protected abstract Task<TSyntaxContext> CreateContext(
            Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken);

        protected abstract AbstractRecommendationServiceRunner<TSyntaxContext> CreateRunner(
            TSyntaxContext context, bool filterOutOfScopeLocals, CancellationToken cancellationToken);

        public async Task<ImmutableArray<ISymbol>> GetRecommendedSymbolsAtPositionAsync(
            Workspace workspace, SemanticModel semanticModel, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var context = await CreateContext(workspace, semanticModel, position, cancellationToken).ConfigureAwait(false);
            var filterOutOfScopeLocals = options.GetOption(RecommendationOptions.FilterOutOfScopeLocals, semanticModel.Language);
            var symbols = CreateRunner(context, filterOutOfScopeLocals, cancellationToken).GetSymbols();

            var hideAdvancedMembers = options.GetOption(RecommendationOptions.HideAdvancedMembers, semanticModel.Language);
            symbols = symbols.FilterToVisibleAndBrowsableSymbols(hideAdvancedMembers, semanticModel.Compilation);

            var shouldIncludeSymbolContext = new ShouldIncludeSymbolContext(context, cancellationToken);
            symbols = symbols.WhereAsArray(shouldIncludeSymbolContext.ShouldIncludeSymbol);
            return symbols;
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

                if (symbol is INamespaceSymbol namespaceSymbol)
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
