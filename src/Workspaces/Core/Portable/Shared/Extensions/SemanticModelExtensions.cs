// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SemanticModelExtensions
    {
        public static SemanticMap GetSemanticMap(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            return SemanticMap.From(semanticModel, node, cancellationToken);
        }

        /// <summary>
        /// Gets semantic information, such as type, symbols, and diagnostics, about the parent of a token.
        /// </summary>
        /// <param name="semanticModel">The SemanticModel object to get semantic information
        /// from.</param>
        /// <param name="token">The token to get semantic information from. This must be part of the
        /// syntax tree associated with the binding.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        {
            return semanticModel.GetSymbolInfo(token.Parent, cancellationToken);
        }

        public static TSymbol? GetEnclosingSymbol<TSymbol>(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            where TSymbol : class, ISymbol
        {
            for (var symbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
                 symbol != null;
                 symbol = symbol.ContainingSymbol)
            {
                if (symbol is TSymbol tSymbol)
                {
                    return tSymbol;
                }
            }

            return default;
        }

        public static ISymbol GetEnclosingNamedTypeOrAssembly(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(position, cancellationToken) ??
                (ISymbol)semanticModel.Compilation.Assembly;
        }

        public static INamedTypeSymbol? GetEnclosingNamedType(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(position, cancellationToken);
        }

        public static INamespaceSymbol? GetEnclosingNamespace(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.GetEnclosingSymbol<INamespaceSymbol>(position, cancellationToken);
        }

        /// <summary>
        /// Fetches the ITypeSymbol that should be used if we were generating a parameter or local that would accept <paramref name="expression"/>. If
        /// expression is a type, that's returned; otherwise this will see if it's something like a method group and then choose an appropriate delegate.
        /// </summary>
        public static ITypeSymbol GetType(
            this SemanticModel semanticModel,
            SyntaxNode expression,
            CancellationToken cancellationToken)
        {
            var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

            if (typeInfo.Type != null)
            {
                return typeInfo.GetTypeWithFlowNullability();
            }

            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            return symbolInfo.GetAnySymbol().ConvertToType(semanticModel.Compilation);
        }

        private static ISymbol? MapSymbol(ISymbol symbol, ITypeSymbol? type)
        {
            if (symbol.IsConstructor() && symbol.ContainingType.IsAnonymousType)
            {
                return symbol.ContainingType;
            }

            if (symbol.IsThisParameter())
            {
                // Map references to this/base to the actual type that those correspond to.
                return type;
            }

            if (symbol.IsFunctionValue() &&
                symbol.ContainingSymbol is IMethodSymbol method)
            {
                if (method?.AssociatedSymbol != null)
                {
                    return method.AssociatedSymbol;
                }
                else
                {
                    return method;
                }
            }

            // see if we can map the built-in language operator to a real method on the containing
            // type of the symbol.  built-in operators can happen when querying the semantic model
            // for operators.  However, we would prefer to just use the real operator on the type
            // if it has one.
            if (symbol is IMethodSymbol methodSymbol &&
                methodSymbol.MethodKind == MethodKind.BuiltinOperator &&
                methodSymbol.ContainingType is ITypeSymbol containingType)
            {
                var comparer = SymbolEquivalenceComparer.Instance.ParameterEquivalenceComparer;

                // Note: this will find the real method vs the built-in.  That's because the
                // built-in is synthesized operator that isn't actually in the list of members of
                // its 'ContainingType'.
                var mapped = containingType.GetMembers(methodSymbol.Name)
                                           .OfType<IMethodSymbol>()
                                           .FirstOrDefault(s => s.Parameters.SequenceEqual(methodSymbol.Parameters, comparer));
                symbol = mapped ?? symbol;
            }

            return symbol;
        }

        public static TokenSemanticInfo GetSemanticInfo(
            this SemanticModel semanticModel,
            SyntaxToken token,
            Workspace workspace,
            CancellationToken cancellationToken)
        {
            var languageServices = workspace.Services.GetLanguageServices(token.Language);
            var syntaxFacts = languageServices.GetRequiredService<ISyntaxFactsService>();
            if (!syntaxFacts.IsBindableToken(token))
            {
                return TokenSemanticInfo.Empty;
            }

            var semanticFacts = languageServices.GetRequiredService<ISemanticFactsService>();

            IAliasSymbol? aliasSymbol;
            ITypeSymbol? type;
            ITypeSymbol? convertedType;
            ISymbol? declaredSymbol;
            ImmutableArray<ISymbol?> allSymbols;

            var overriddingIdentifier = syntaxFacts.GetDeclarationIdentifierIfOverride(token);
            if (overriddingIdentifier.HasValue)
            {
                // on an "override" token, we'll find the overridden symbol
                aliasSymbol = null;
                var overriddingSymbol = semanticFacts.GetDeclaredSymbol(semanticModel, overriddingIdentifier.Value, cancellationToken);
                var overriddenSymbol = overriddingSymbol.GetOverriddenMember();

                // on an "override" token, the overridden symbol is the only part of TokenSemanticInfo used by callers, so type doesn't matter
                type = null;
                convertedType = null;
                declaredSymbol = null;
                allSymbols = overriddenSymbol is null ? ImmutableArray<ISymbol?>.Empty : ImmutableArray.Create<ISymbol?>(overriddenSymbol);
            }
            else
            {
                aliasSymbol = semanticModel.GetAliasInfo(token.Parent, cancellationToken);
                var bindableParent = syntaxFacts.GetBindableParent(token);
                var typeInfo = semanticModel.GetTypeInfo(bindableParent, cancellationToken);
                type = typeInfo.Type;
                convertedType = typeInfo.ConvertedType;
                declaredSymbol = MapSymbol(semanticFacts.GetDeclaredSymbol(semanticModel, token, cancellationToken), type);

                var skipSymbolInfoLookup = declaredSymbol.IsKind(SymbolKind.RangeVariable);
                allSymbols = skipSymbolInfoLookup
                    ? ImmutableArray<ISymbol?>.Empty
                    : semanticFacts
                        .GetBestOrAllSymbols(semanticModel, bindableParent, token, cancellationToken)
                        .WhereAsArray(s => !s.Equals(declaredSymbol))
                        .SelectAsArray(s => MapSymbol(s, type));
            }

            // NOTE(cyrusn): This is a workaround to how the semantic model binds and returns
            // information for VB event handlers.  Namely, if you have:
            //
            // Event X]()
            // Sub Goo()
            //      Dim y = New $$XEventHandler(AddressOf bar)
            // End Sub
            //
            // Only GetTypeInfo will return any information for XEventHandler.  So, in this
            // case, we upgrade the type to be the symbol we return.
            if (type != null && allSymbols.Length == 0)
            {
                if (type.Kind == SymbolKind.NamedType)
                {
                    var namedType = (INamedTypeSymbol)type;
                    if (namedType.TypeKind == TypeKind.Delegate ||
                        namedType.AssociatedSymbol != null)
                    {
                        allSymbols = ImmutableArray.Create<ISymbol?>(type);
                        type = null;
                    }
                }
            }

            if (allSymbols.Length == 0 && syntaxFacts.IsQueryKeyword(token))
            {
                type = null;
                convertedType = null;
            }

            return new TokenSemanticInfo(declaredSymbol, aliasSymbol, allSymbols, type, convertedType, token.Span);
        }

        public static SemanticModel GetOriginalSemanticModel(this SemanticModel semanticModel)
        {
            if (!semanticModel.IsSpeculativeSemanticModel)
            {
                return semanticModel;
            }

            Contract.ThrowIfNull(semanticModel.ParentModel);
            Contract.ThrowIfTrue(semanticModel.ParentModel.IsSpeculativeSemanticModel);
            Contract.ThrowIfTrue(semanticModel.ParentModel.ParentModel != null);
            return semanticModel.ParentModel;
        }

        public static HashSet<ISymbol> GetAllDeclaredSymbols(
            this SemanticModel semanticModel, SyntaxNode? container, CancellationToken cancellationToken, Func<SyntaxNode, bool>? filter = null)
        {
            var symbols = new HashSet<ISymbol>();
            if (container != null)
            {
                GetAllDeclaredSymbols(semanticModel, container, symbols, cancellationToken, filter);
            }

            return symbols;
        }

        public static IEnumerable<ISymbol> GetExistingSymbols(
            this SemanticModel semanticModel, SyntaxNode? container, CancellationToken cancellationToken, Func<SyntaxNode, bool>? descendInto = null)
        {
            // Ignore an anonymous type property or tuple field.  It's ok if they have a name that
            // matches the name of the local we're introducing.
            return semanticModel.GetAllDeclaredSymbols(container, cancellationToken, descendInto)
                .Where(s => !s.IsAnonymousTypeProperty() && !s.IsTupleField());
        }

        private static void GetAllDeclaredSymbols(
            SemanticModel semanticModel, SyntaxNode node,
            HashSet<ISymbol> symbols, CancellationToken cancellationToken, Func<SyntaxNode, bool>? descendInto = null)
        {
            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

            if (symbol != null)
            {
                symbols.Add(symbol);
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    var childNode = child.AsNode();
                    if (ShouldDescendInto(childNode, descendInto))
                    {
                        GetAllDeclaredSymbols(semanticModel, child.AsNode(), symbols, cancellationToken, descendInto);
                    }
                }
            }

            static bool ShouldDescendInto(SyntaxNode node, Func<SyntaxNode, bool>? filter)
                => filter != null ? filter(node) : true;
        }
    }
}
