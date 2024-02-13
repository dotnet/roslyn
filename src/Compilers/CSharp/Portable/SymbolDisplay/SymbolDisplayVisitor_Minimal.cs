// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SymbolDisplayVisitor
    {
        private bool TryAddAlias(
            INamespaceOrTypeSymbol symbol,
            ArrayBuilder<SymbolDisplayPart> builder)
        {
            var alias = GetAliasSymbol(symbol);
            if (alias != null)
            {
                Debug.Assert(IsMinimizing);

                // We must verify that the alias actually binds back to the thing it's aliasing.
                // It's possible there's another symbol with the same name as the alias that binds
                // first
                var aliasName = alias.Name;

                var boundSymbols = SemanticModelOpt.LookupNamespacesAndTypes(PositionOpt, name: aliasName);

                if (boundSymbols.Length == 1)
                {
                    var boundAlias = boundSymbols[0] as IAliasSymbol;
                    if ((object?)boundAlias != null && alias.Target.Equals(symbol))
                    {
                        builder.Add(CreatePart(SymbolDisplayPartKind.AliasName, alias, aliasName));
                        return true;
                    }
                }
            }

            return false;
        }

        protected override bool ShouldRestrictMinimallyQualifyLookupToNamespacesAndTypes()
        {
            Debug.Assert(IsMinimizing);

            var token = SemanticModelOpt.SyntaxTree.GetRoot().FindToken(PositionOpt);
            var startNode = token.Parent;

            return SyntaxFacts.IsInNamespaceOrTypeContext(startNode as ExpressionSyntax) || token.IsKind(SyntaxKind.NewKeyword) || this.InNamespaceOrType;
        }

        private void MinimallyQualify(INamespaceSymbol symbol)
        {
            Debug.Assert(IsMinimizing);

            // only the global namespace does not have a containing namespace
            Debug.Assert(symbol.ContainingNamespace != null || symbol.IsGlobalNamespace);

            // NOTE(cyrusn): We only call this once we've already checked if there is an alias that
            // corresponds to this namespace. 

            if (symbol.IsGlobalNamespace)
            {
                // nothing to add for global namespace itself
                return;
            }

            // Check if the name of this namespace binds to the same namespace symbol.  If so,
            // then that's all we need to add.  Otherwise, we will add the minimally qualified
            // version of our parent, and then add ourselves to that.
            var symbols = ShouldRestrictMinimallyQualifyLookupToNamespacesAndTypes()
                ? SemanticModelOpt.LookupNamespacesAndTypes(PositionOpt, name: symbol.Name)
                : SemanticModelOpt.LookupSymbols(PositionOpt, name: symbol.Name);
            var firstSymbol = symbols.OfType<ISymbol>().FirstOrDefault();
            if (symbols.Length != 1 ||
                firstSymbol == null ||
                !firstSymbol.Equals(symbol))
            {
                // Just the name alone didn't bind properly.  Add our minimally qualified parent (if
                // we have one), a dot, and then our name.
                var containingNamespace = symbol.ContainingNamespace == null
                    ? null
                    : SemanticModelOpt.Compilation.GetCompilationNamespace(symbol.ContainingNamespace);
                if (containingNamespace != null)
                {
                    if (containingNamespace.IsGlobalNamespace)
                    {
                        Debug.Assert(Format.GlobalNamespaceStyle == SymbolDisplayGlobalNamespaceStyle.Included ||
                                          Format.GlobalNamespaceStyle == SymbolDisplayGlobalNamespaceStyle.Omitted ||
                                          Format.GlobalNamespaceStyle == SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining);

                        if (Format.GlobalNamespaceStyle == SymbolDisplayGlobalNamespaceStyle.Included)
                        {
                            AddGlobalNamespace(containingNamespace);
                            AddPunctuation(SyntaxKind.ColonColonToken);
                        }
                    }
                    else
                    {
                        containingNamespace.Accept(this.NotFirstVisitor);
                        AddPunctuation(SyntaxKind.DotToken);
                    }
                }
            }

            // If we bound properly, then we'll just add our name.
            Builder.Add(CreatePart(SymbolDisplayPartKind.NamespaceName, symbol, symbol.Name));
        }

        private void MinimallyQualify(INamedTypeSymbol symbol)
        {
            // NOTE(cyrusn): We only call this once we've already checked if there is an alias or
            // special type that corresponds to this type.
            //
            // We first start by trying to bind just our name and type arguments.  If they bind to
            // the symbol that we were constructed from, then we have our minimal name. Otherwise,
            // we get the minimal name of our parent, add a dot, and then add ourselves.

            // TODO(cyrusn): This code needs to see if type is an attribute and if it can be shown 
            // in simplified form here.

            if (!(symbol.IsAnonymousType || symbol.IsTupleType))
            {
                Debug.Assert(IsMinimizing);

                if (!NameBoundSuccessfullyToSameSymbol(symbol))
                {
                    // Just the name alone didn't bind properly.  Add our minimally qualified parent (if
                    // we have one), a dot, and then our name.
                    if (IncludeNamedType(symbol.ContainingType))
                    {
                        symbol.ContainingType.Accept(this.NotFirstVisitor);
                        AddPunctuation(SyntaxKind.DotToken);
                    }
                    else
                    {
                        var containingNamespace = symbol.ContainingNamespace == null
                            ? null
                            : SemanticModelOpt.Compilation.GetCompilationNamespace(symbol.ContainingNamespace);
                        if (containingNamespace != null)
                        {
                            if (containingNamespace.IsGlobalNamespace)
                            {
                                // Error symbols are put into the global namespace if the compiler has
                                // no better guess for it, so we shouldn't go spitting it everywhere.
                                if (symbol.TypeKind != TypeKind.Error)
                                {
                                    AddKeyword(SyntaxKind.GlobalKeyword);
                                    AddPunctuation(SyntaxKind.ColonColonToken);
                                }
                            }
                            else
                            {
                                containingNamespace.Accept(this.NotFirstVisitor);
                                AddPunctuation(SyntaxKind.DotToken);
                            }
                        }
                    }
                }
            }

            AddNameAndTypeArgumentsOrParameters(symbol);
        }

        private IDictionary<INamespaceOrTypeSymbol, IAliasSymbol> CreateAliasMap()
        {
            if (!this.IsMinimizing)
            {
                return SpecializedCollections.EmptyDictionary<INamespaceOrTypeSymbol, IAliasSymbol>();
            }

            // Walk up the ancestors from the current position. If this is a speculative
            // model, walk up the corresponding ancestors in the parent model.
            SemanticModel semanticModel;
            int position;
            if (SemanticModelOpt.IsSpeculativeSemanticModel)
            {
                semanticModel = SemanticModelOpt.ParentModel;
                position = SemanticModelOpt.OriginalPositionForSpeculation;
            }
            else
            {
                semanticModel = SemanticModelOpt;
                position = PositionOpt;
            }

            var token = semanticModel.SyntaxTree.GetRoot().FindToken(position);
            var startNode = token.Parent!;

            // NOTE(cyrusn): If we're currently in a block of usings, then we want to collect the
            // aliases that are higher up than this block.  Using aliases declared in a block of
            // usings are not usable from within that same block.
            var usingDirective = GetAncestorOrThis<UsingDirectiveSyntax>(startNode);
            if (usingDirective != null)
            {
                startNode = usingDirective.Parent!.Parent!;
            }

            var usingAliases = GetAncestorsOrThis<BaseNamespaceDeclarationSyntax>(startNode)
                .SelectMany(n => n.Usings)
                .Concat(GetAncestorsOrThis<CompilationUnitSyntax>(startNode).SelectMany(c => c.Usings))
                .Where(u => u.Alias != null)
                .Select(u => semanticModel.GetDeclaredSymbol(u) as IAliasSymbol)
                .WhereNotNull();

            var builder = ImmutableDictionary.CreateBuilder<INamespaceOrTypeSymbol, IAliasSymbol>();
            foreach (var alias in usingAliases)
            {
                if (!builder.ContainsKey(alias.Target))
                {
                    builder.Add(alias.Target, alias);
                }
            }

            return builder.ToImmutable();
        }

        private ITypeSymbol? GetRangeVariableType(IRangeVariableSymbol symbol)
        {
            ITypeSymbol? type = null;

            if (this.IsMinimizing && !symbol.Locations.IsEmpty)
            {
                var location = symbol.Locations.First();
                if (location.IsInSource && location.SourceTree == SemanticModelOpt.SyntaxTree)
                {
                    var token = location.SourceTree.GetRoot().FindToken(PositionOpt);
                    var queryBody = GetQueryBody(token);
                    if (queryBody != null)
                    {
                        // To heuristically determine the type of the range variable in a query
                        // clause, we speculatively bind the name of the variable in the select
                        // or group clause of the query body.
                        var identifierName = SyntaxFactory.IdentifierName(symbol.Name);
                        type = SemanticModelOpt.GetSpeculativeTypeInfo(
                            queryBody.SelectOrGroup.Span.End - 1, identifierName, SpeculativeBindingOption.BindAsExpression).Type;
                    }

                    var identifier = token.Parent as IdentifierNameSyntax;
                    if (identifier != null)
                    {
                        type = SemanticModelOpt.GetTypeInfo(identifier).Type;
                    }
                }
            }

            return type;
        }

        private static QueryBodySyntax? GetQueryBody(SyntaxToken token) =>
            token.Parent switch
            {
                FromClauseSyntax fromClause when fromClause.Identifier == token =>
                    fromClause.Parent as QueryBodySyntax ?? ((QueryExpressionSyntax)fromClause.Parent!).Body,
                LetClauseSyntax letClause when letClause.Identifier == token =>
                    letClause.Parent as QueryBodySyntax,
                JoinClauseSyntax joinClause when joinClause.Identifier == token =>
                    joinClause.Parent as QueryBodySyntax,
                QueryContinuationSyntax continuation when continuation.Identifier == token =>
                    continuation.Body,
                _ => null
            };

        private string RemoveAttributeSuffixIfNecessary(INamedTypeSymbol symbol, string symbolName)
        {
            if (this.IsMinimizing &&
                Format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix) &&
                SemanticModelOpt.Compilation.IsAttributeType(symbol))
            {
                string? nameWithoutAttributeSuffix;
                if (symbolName.TryGetWithoutAttributeSuffix(out nameWithoutAttributeSuffix))
                {
                    var token = SyntaxFactory.ParseToken(nameWithoutAttributeSuffix);
                    if (token.IsKind(SyntaxKind.IdentifierToken))
                    {
                        symbolName = nameWithoutAttributeSuffix;
                    }
                }
            }

            return symbolName;
        }

        private static T? GetAncestorOrThis<T>(SyntaxNode node) where T : SyntaxNode
        {
            return GetAncestorsOrThis<T>(node).FirstOrDefault();
        }

        private static IEnumerable<T> GetAncestorsOrThis<T>(SyntaxNode node) where T : SyntaxNode
        {
            return node == null
                ? SpecializedCollections.EmptyEnumerable<T>()
                : node.AncestorsAndSelf().OfType<T>();
        }

        private IDictionary<INamespaceOrTypeSymbol, IAliasSymbol> AliasMap
        {
            get
            {
                var map = _lazyAliasMap;
                if (map != null)
                {
                    return map;
                }

                map = CreateAliasMap();
                return Interlocked.CompareExchange(ref _lazyAliasMap, map, null) ?? map;
            }
        }

        private IAliasSymbol? GetAliasSymbol(INamespaceOrTypeSymbol symbol)
        {
            IAliasSymbol? result;
            return AliasMap.TryGetValue(symbol, out result) ? result : null;
        }
    }
}
