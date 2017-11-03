// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal struct TokenSemanticInfo
    {
        public static readonly TokenSemanticInfo Empty = new TokenSemanticInfo(
            null, null, ImmutableArray<ISymbol>.Empty, null, default(TextSpan));

        public readonly ISymbol DeclaredSymbol;
        public readonly IAliasSymbol AliasSymbol;
        public readonly ImmutableArray<ISymbol> ReferencedSymbols;
        public readonly ITypeSymbol Type;
        public readonly TextSpan Span;

        public TokenSemanticInfo(
            ISymbol declaredSymbol, 
            IAliasSymbol aliasSymbol,
            ImmutableArray<ISymbol> referencedSymbols,
            ITypeSymbol type,
            TextSpan span)
        {
            DeclaredSymbol = declaredSymbol;
            AliasSymbol = aliasSymbol;
            ReferencedSymbols = referencedSymbols;
            Type = type;
            Span = span;
        }

        public ImmutableArray<ISymbol> GetSymbols(bool includeType)
        {
            var result = ArrayBuilder<ISymbol>.GetInstance();
            result.AddIfNotNull(DeclaredSymbol);
            result.AddIfNotNull(AliasSymbol);
            result.AddRange(ReferencedSymbols);

            if (includeType)
            {
                result.AddIfNotNull(Type);
            }

            return result.ToImmutableAndFree();
        }

        public ISymbol GetAnySymbol(bool includeType)
        {
            return GetSymbols(includeType).FirstOrDefault();
        }
    }

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

        public static TSymbol GetEnclosingSymbol<TSymbol>(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            where TSymbol : ISymbol
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

        public static INamedTypeSymbol GetEnclosingNamedType(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(position, cancellationToken);
        }

        public static INamespaceSymbol GetEnclosingNamespace(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.GetEnclosingSymbol<INamespaceSymbol>(position, cancellationToken);
        }

        public static ITypeSymbol GetType(
            this SemanticModel semanticModel,
            SyntaxNode expression,
            CancellationToken cancellationToken)
        {
            var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);
            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            return typeInfo.Type ?? symbolInfo.GetAnySymbol().ConvertToType(semanticModel.Compilation);
        }

        public static TokenSemanticInfo GetSemanticInfo(
            this SemanticModel semanticModel,
            SyntaxToken token,
            Workspace workspace,
            CancellationToken cancellationToken)
        {
            var languageServices = workspace.Services.GetLanguageServices(token.Language);
            var syntaxFacts = languageServices.GetService<ISyntaxFactsService>();
            if (!syntaxFacts.IsBindableToken(token))
            {
                return TokenSemanticInfo.Empty;
            }

            var semanticFacts = languageServices.GetService<ISemanticFactsService>();

            return GetSemanticInfo(
                semanticModel, semanticFacts, syntaxFacts,
                token, cancellationToken);
        }

        private static ISymbol MapSymbol(ISymbol symbol, ITypeSymbol type)
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

            return symbol;
        }

        private static TokenSemanticInfo GetSemanticInfo(
            SemanticModel semanticModel,
            ISemanticFactsService semanticFacts,
            ISyntaxFactsService syntaxFacts,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var aliasSymbol = semanticModel.GetAliasInfo(token.Parent, cancellationToken);

            var bindableParent = syntaxFacts.GetBindableParent(token);
            var type = semanticModel.GetTypeInfo(bindableParent, cancellationToken).Type;

            var declaredSymbol = MapSymbol(semanticFacts.GetDeclaredSymbol(semanticModel, token, cancellationToken), type);
            var allSymbols = semanticModel.GetSymbolInfo(bindableParent, cancellationToken)
                                          .GetBestOrAllSymbols()
                                          .WhereAsArray(s => !s.Equals(declaredSymbol))
                                          .SelectAsArray(s => MapSymbol(s, type));

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
                        allSymbols = ImmutableArray.Create<ISymbol>(type);
                        type = null;
                    }
                }
            }

            return new TokenSemanticInfo(declaredSymbol, aliasSymbol, allSymbols, type, token.Span);
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
            this SemanticModel semanticModel, SyntaxNode container, CancellationToken cancellationToken)
        {
            var symbols = new HashSet<ISymbol>();
            if (container != null)
            {
                GetAllDeclaredSymbols(semanticModel, container, symbols, cancellationToken);
            }

            return symbols;
        }

        public static IEnumerable<ISymbol> GetExistingSymbols(
            this SemanticModel semanticModel, SyntaxNode container, CancellationToken cancellationToken)
        {
            // Ignore an annonymous type property or tuple field.  It's ok if they have a name that 
            // matches the name of the local we're introducing.
            return semanticModel.GetAllDeclaredSymbols(container, cancellationToken)
                                .Where(s => !s.IsAnonymousTypeProperty() && !s.IsTupleField());
        }

        private static void GetAllDeclaredSymbols(
            SemanticModel semanticModel, SyntaxNode node,
            HashSet<ISymbol> symbols, CancellationToken cancellationToken)
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
                    GetAllDeclaredSymbols(semanticModel, child.AsNode(), symbols, cancellationToken);
                }
            }
        }
    }
}
