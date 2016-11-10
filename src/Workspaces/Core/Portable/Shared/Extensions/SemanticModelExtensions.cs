// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal struct TokenSemanticInfo
    {
        public static readonly TokenSemanticInfo Empty = new TokenSemanticInfo(
            null, null, ImmutableArray<ISymbol>.Empty, null);

        public readonly ISymbol DeclaredSymbol;
        public readonly IAliasSymbol AliasSymbol;
        public readonly ImmutableArray<ISymbol> ReferencedSymbols;
        public readonly ITypeSymbol Type;

        public TokenSemanticInfo(
            ISymbol declaredSymbol, 
            IAliasSymbol aliasSymbol,
            ImmutableArray<ISymbol> referencedSymbols,
            ITypeSymbol type)
        {
            DeclaredSymbol = declaredSymbol;
            AliasSymbol = aliasSymbol;
            ReferencedSymbols = referencedSymbols;
            Type = type;
        }

        public ImmutableArray<ISymbol> GetSymbols(bool includeType)
        {
            var result = ArrayBuilder<ISymbol>.GetInstance();
            result.AddIfNotNull(DeclaredSymbol);
            result.AddIfNotNull(AliasSymbol);
            result.AddRange(ReferencedSymbols);

            if (includeType)
            {
                result.Add(Type);
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
                if (symbol is TSymbol)
                {
                    return (TSymbol)symbol;
                }
            }

            return default(TSymbol);
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

        private static ISymbol MapSymbol(ISymbol symbol)
        {
            if (symbol.IsConstructor() && symbol.ContainingType.IsAnonymousType)
            {
                return symbol.ContainingType;
            }

            if (symbol.IsFunctionValue())
            {
                var method = symbol.ContainingSymbol as IMethodSymbol;

                if (method != null)
                {
                    if (method.AssociatedSymbol != null)
                    {
                        return method.AssociatedSymbol;
                    }
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
            var declaredSymbol = MapSymbol(semanticFacts.GetDeclaredSymbol(semanticModel, token, cancellationToken));
            var aliasSymbol = semanticModel.GetAliasInfo(token.Parent, cancellationToken);

            var bindableParent = syntaxFacts.GetBindableParent(token);
            var allSymbols = semanticModel.GetSymbolInfo(bindableParent, cancellationToken)
                                          .GetBestOrAllSymbols()
                                          .SelectAsArray(MapSymbol);
            var type = semanticModel.GetTypeInfo(bindableParent, cancellationToken).Type;

            return new TokenSemanticInfo(declaredSymbol, aliasSymbol, allSymbols, type);
#if false
            if ((bindLiteralsToUnderlyingType && syntaxFacts.IsLiteral(token)) ||
                syntaxFacts.IsAwaitKeyword(token))
            {
                yield return type;
            }

            if (type.Kind == SymbolKind.NamedType)
            {
                var namedType = (INamedTypeSymbol)type;
                if (namedType.TypeKind == TypeKind.Delegate ||
                    namedType.AssociatedSymbol != null)
                {
                    yield return type;
                }
            }

            foreach (var symbol in allSymbols)
            {
                if (symbol.IsThisParameter() && type != null)
                {
                    yield return type;
                }
                else if (symbol.IsFunctionValue())
                {
                    var method = symbol.ContainingSymbol as IMethodSymbol;

                    if (method != null)
                    {
                        if (method.AssociatedSymbol != null)
                        {
                            yield return method.AssociatedSymbol;
                        }
                        else
                        {
                            yield return method;
                        }
                    }
                    else
                    {
                        yield return symbol;
                    }
                }
                else
                {
                    yield return symbol;
                }
            }

            if (type != null && allSymbols.Length == 0)
            {
                if ((bindLiteralsToUnderlyingType && syntaxFacts.IsLiteral(token)) ||
                    syntaxFacts.IsAwaitKeyword(token))
                {
                    yield return type;
                }

                if (type.Kind == SymbolKind.NamedType)
                {
                    var namedType = (INamedTypeSymbol)type;
                    if (namedType.TypeKind == TypeKind.Delegate ||
                        namedType.AssociatedSymbol != null)
                    {
                        yield return type;
                    }
                }
            }
#endif
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
    }
}
