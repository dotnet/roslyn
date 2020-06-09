// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class SemanticModelExtensions
    {
        public static SemanticMap GetSemanticMap(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => SemanticMap.From(semanticModel, node, cancellationToken);

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
                return typeInfo.Type;
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
                aliasSymbol = semanticModel.GetAliasInfo(token.Parent!, cancellationToken);
                var bindableParent = syntaxFacts.TryGetBindableParent(token);
                var typeInfo = bindableParent != null ? semanticModel.GetTypeInfo(bindableParent, cancellationToken) : default;
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
    }
}
