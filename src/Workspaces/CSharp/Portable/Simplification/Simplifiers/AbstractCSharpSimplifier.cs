// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    /// <summary>
    /// Contains helpers used by several simplifier subclasses.
    /// </summary>
    internal abstract class AbstractCSharpSimplifier<TSyntax, TSimplifiedSyntax>
        : AbstractSimplifier<TSyntax, TSimplifiedSyntax, CSharpSimplifierOptions>
        where TSyntax : SyntaxNode
        where TSimplifiedSyntax : SyntaxNode
    {
        private static readonly ConditionalWeakTable<SemanticModel, StrongBox<bool>> s_modelToHasUsingAliasesMap = new();

        /// <summary>
        /// Returns the predefined keyword kind for a given <see cref="SpecialType"/>.
        /// </summary>
        /// <param name="specialType">The <see cref="SpecialType"/> of this type.</param>
        /// <returns>The keyword kind for a given special type, or SyntaxKind.None if the type name is not a predefined type.</returns>
        protected static SyntaxToken? TryGetPredefinedKeywordToken(SemanticModel semanticModel, SpecialType specialType)
        {
            var kind = specialType switch
            {
                SpecialType.System_Boolean => SyntaxKind.BoolKeyword,
                SpecialType.System_Byte => SyntaxKind.ByteKeyword,
                SpecialType.System_SByte => SyntaxKind.SByteKeyword,
                SpecialType.System_Int32 => SyntaxKind.IntKeyword,
                SpecialType.System_UInt32 => SyntaxKind.UIntKeyword,
                SpecialType.System_Int16 => SyntaxKind.ShortKeyword,
                SpecialType.System_UInt16 => SyntaxKind.UShortKeyword,
                SpecialType.System_Int64 => SyntaxKind.LongKeyword,
                SpecialType.System_UInt64 => SyntaxKind.ULongKeyword,
                SpecialType.System_Single => SyntaxKind.FloatKeyword,
                SpecialType.System_Double => SyntaxKind.DoubleKeyword,
                SpecialType.System_Decimal => SyntaxKind.DecimalKeyword,
                SpecialType.System_String => SyntaxKind.StringKeyword,
                SpecialType.System_Char => SyntaxKind.CharKeyword,
                SpecialType.System_Object => SyntaxKind.ObjectKeyword,
                SpecialType.System_Void => SyntaxKind.VoidKeyword,
                _ => SyntaxKind.None,
            };

            if (kind != SyntaxKind.None)
                return SyntaxFactory.Token(kind);

            if (specialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr &&
                semanticModel.SyntaxTree.Options.LanguageVersion() >= LanguageVersion.CSharp9 &&
                    semanticModel.Compilation.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr))
            {
                return SyntaxFactory.Identifier(specialType == SpecialType.System_IntPtr ? "nint" : "nuint");
            }

            return null;
        }

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/23582",
            Constraint = "Most trees do not have using alias directives, so avoid the expensive " + nameof(CSharpExtensions.GetSymbolInfo) + " call for this case.")]
        protected static bool TryReplaceExpressionWithAlias(
            ExpressionSyntax node, SemanticModel semanticModel,
            ISymbol symbol, CancellationToken cancellationToken, out IAliasSymbol aliasReplacement)
        {
            aliasReplacement = null;

            if (!IsAliasReplaceableExpression(node))
                return false;

            // Avoid the TryReplaceWithAlias algorithm if the tree has no using alias directives. Since the input node
            // might be a speculative node (not fully rooted in a tree), we use the original semantic model to find the
            // equivalent node in the original tree, and from there determine if the tree has any using alias
            // directives.
            var originalModel = semanticModel.GetOriginalSemanticModel();
            var hasUsingAliases = HasUsingAliases(originalModel, cancellationToken);
            if (!hasUsingAliases)
                return false;

            // If the Symbol is a constructor get its containing type
            if (symbol.IsConstructor())
            {
                symbol = symbol.ContainingType;
            }

            if (node is QualifiedNameSyntax or AliasQualifiedNameSyntax)
            {
                SyntaxAnnotation aliasAnnotationInfo = null;

                // The following condition checks if the user has used alias in the original code and
                // if so the expression is replaced with the Alias
                if (node is QualifiedNameSyntax qualifiedNameNode)
                {
                    if (qualifiedNameNode.Right.Identifier.HasAnnotations(AliasAnnotation.Kind))
                    {
                        aliasAnnotationInfo = qualifiedNameNode.Right.Identifier.GetAnnotations(AliasAnnotation.Kind).Single();
                    }
                }

                if (node is AliasQualifiedNameSyntax aliasQualifiedNameNode)
                {
                    if (aliasQualifiedNameNode.Name.Identifier.HasAnnotations(AliasAnnotation.Kind))
                    {
                        aliasAnnotationInfo = aliasQualifiedNameNode.Name.Identifier.GetAnnotations(AliasAnnotation.Kind).Single();
                    }
                }

                if (aliasAnnotationInfo != null)
                {
                    var aliasName = AliasAnnotation.GetAliasName(aliasAnnotationInfo);
                    var aliasIdentifier = SyntaxFactory.IdentifierName(aliasName);

                    var aliasTypeInfo = semanticModel.GetSpeculativeAliasInfo(node.SpanStart, aliasIdentifier, SpeculativeBindingOption.BindAsTypeOrNamespace);

                    if (aliasTypeInfo != null)
                    {
                        aliasReplacement = aliasTypeInfo;
                        return ValidateAliasForTarget(aliasReplacement, semanticModel, node, symbol);
                    }
                }
            }

            if (node.Kind() == SyntaxKind.IdentifierName &&
                semanticModel.GetAliasInfo((IdentifierNameSyntax)node, cancellationToken) != null)
            {
                return false;
            }

            // an alias can only replace a type or namespace
            if (symbol == null ||
                (symbol.Kind != SymbolKind.Namespace && symbol.Kind != SymbolKind.NamedType))
            {
                return false;
            }

            var preferAliasToQualifiedName = true;
            if (node is QualifiedNameSyntax qualifiedName)
            {
                if (!qualifiedName.Right.HasAnnotation(Simplifier.SpecialTypeAnnotation))
                {
                    var type = semanticModel.GetTypeInfo(node, cancellationToken).Type;
                    if (type != null)
                    {
                        var keywordToken = TryGetPredefinedKeywordToken(semanticModel, type.SpecialType);
                        if (keywordToken != null)
                            preferAliasToQualifiedName = false;
                    }
                }
            }

            if (node is AliasQualifiedNameSyntax aliasQualifiedNameSyntax)
            {
                if (!aliasQualifiedNameSyntax.Name.HasAnnotation(Simplifier.SpecialTypeAnnotation))
                {
                    var type = semanticModel.GetTypeInfo(node, cancellationToken).Type;
                    if (type != null)
                    {
                        var keywordToken = TryGetPredefinedKeywordToken(semanticModel, type.SpecialType);
                        if (keywordToken != null)
                            preferAliasToQualifiedName = false;
                    }
                }
            }

            aliasReplacement = GetAliasForSymbol((INamespaceOrTypeSymbol)symbol, node.GetFirstToken(), semanticModel, cancellationToken);
            if (aliasReplacement != null && preferAliasToQualifiedName)
            {
                return ValidateAliasForTarget(aliasReplacement, semanticModel, node, symbol);
            }

            return false;

            static bool IsAliasReplaceableExpression(ExpressionSyntax expression)
            {
                var current = expression;
                while (current is MemberAccessExpressionSyntax(SyntaxKind.SimpleMemberAccessExpression) currentMember)
                {
                    current = currentMember.Expression;
                    continue;
                }

                return current.Kind() is SyntaxKind.AliasQualifiedName or SyntaxKind.IdentifierName or SyntaxKind.GenericName or SyntaxKind.QualifiedName;
            }
        }

        private static bool HasUsingAliases(SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!s_modelToHasUsingAliasesMap.TryGetValue(semanticModel, out var hasAliases))
            {
                hasAliases = new StrongBox<bool>(ComputeHasUsingAliases(semanticModel, cancellationToken));
                lock (s_modelToHasUsingAliasesMap)
                {
                    s_modelToHasUsingAliasesMap.Remove(semanticModel);
                    s_modelToHasUsingAliasesMap.Add(semanticModel, hasAliases);
                }
            }

            return hasAliases.Value;
        }

        private static bool ComputeHasUsingAliases(SemanticModel model, CancellationToken cancellationToken)
        {
            if (!model.SyntaxTree.HasCompilationUnitRoot)
                return false;

            var root = (CompilationUnitSyntax)model.SyntaxTree.GetRoot(cancellationToken);
            if (HasUsingAliasDirective(root))
                return true;

            var firstMember =
                root.Members.Count > 0 ? root.Members[0] :
                root.AttributeLists.Count > 0 ? root.AttributeLists[0] : (SyntaxNode)null;
            if (firstMember == null)
                return false;

            var scopes = model.GetImportScopes(firstMember.SpanStart, cancellationToken);
            return scopes.Any(static s => s.Aliases.Length > 0);

            static bool HasUsingAliasDirective(SyntaxNode syntax)
            {
                var (usings, members) = syntax switch
                {
                    BaseNamespaceDeclarationSyntax ns => (ns.Usings, ns.Members),
                    CompilationUnitSyntax compilationUnit => (compilationUnit.Usings, compilationUnit.Members),
                    _ => default,
                };

                foreach (var usingDirective in usings)
                {
                    if (usingDirective.Alias != null)
                        return true;
                }

                foreach (var member in members)
                {
                    if (HasUsingAliasDirective(member))
                        return true;
                }

                return false;
            }
        }

        // We must verify that the alias actually binds back to the thing it's aliasing.
        // It's possible there's another symbol with the same name as the alias that binds
        // first
        private static bool ValidateAliasForTarget(IAliasSymbol aliasReplacement, SemanticModel semanticModel, ExpressionSyntax node, ISymbol symbol)
        {
            var aliasName = aliasReplacement.Name;

            // If we're the argument of a nameof(X.Y) call, then we can't simplify to an
            // alias unless the alias has the same name as us (i.e. 'Y').
            if (node.IsNameOfArgumentExpression())
            {
                var nameofValueOpt = semanticModel.GetConstantValue(node.Parent.Parent.Parent);
                if (!nameofValueOpt.HasValue)
                {
                    return false;
                }

                if (nameofValueOpt.Value is string existingVal &&
                    existingVal != aliasName)
                {
                    return false;
                }
            }

            // If something is dotting off the node we need to make sure the name couldn't
            // be a different symbol that has a different type to the alias.
            if (node.IsLeftSideOfDot())
            {
                var aliasIdentifier = SyntaxFactory.IdentifierName(aliasName);

                var symbolInfo = semanticModel.GetSpeculativeSymbolInfo(node.SpanStart, aliasIdentifier, SpeculativeBindingOption.BindAsExpression);
                if (symbolInfo.Symbol is not INamespaceOrTypeSymbol)
                {
                    // We bound the alias to something other than a namespace or a type, which is normally not good, but if the
                    // types are the same then it is okay.
                    var typeInfo = semanticModel.GetSpeculativeTypeInfo(node.SpanStart, aliasIdentifier, SpeculativeBindingOption.BindAsExpression);
                    if (!symbol.Equals(typeInfo.Type))
                    {
                        return false;
                    }
                }
            }

            var boundSymbols = semanticModel.LookupNamespacesAndTypes(node.SpanStart, name: aliasName);

            if (boundSymbols.Length == 1)
            {
                if (boundSymbols[0] is IAliasSymbol && aliasReplacement.Target.Equals(symbol))
                {
                    return true;
                }
            }

            return false;
        }

        private static IAliasSymbol GetAliasForSymbol(INamespaceOrTypeSymbol symbol, SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var originalSemanticModel = semanticModel.GetOriginalSemanticModel();
            if (!originalSemanticModel.SyntaxTree.HasCompilationUnitRoot)
                return null;

            var namespaceId = GetNamespaceIdForAliasSearch(semanticModel, token, cancellationToken);
            if (namespaceId == null)
                return null;

            if (!AliasSymbolCache.TryGetAliasSymbol(originalSemanticModel, namespaceId.Value, symbol, out var aliasSymbol))
            {
                // add cache
                AliasSymbolCache.AddAliasSymbols(
                    originalSemanticModel, namespaceId.Value, semanticModel.LookupNamespacesAndTypes(token.SpanStart).OfType<IAliasSymbol>());

                // retry
                AliasSymbolCache.TryGetAliasSymbol(originalSemanticModel, namespaceId.Value, symbol, out aliasSymbol);
            }

            return aliasSymbol;
        }

        private static int? GetNamespaceIdForAliasSearch(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        {
            var startNode = GetStartNodeForNamespaceId(semanticModel, token, cancellationToken);
            if (!startNode.SyntaxTree.HasCompilationUnitRoot)
                return null;

            // NOTE: If we're currently in a block of usings, then we want to collect the
            // aliases that are higher up than this block.  Using aliases declared in a block of
            // usings are not usable from within that same block.
            var usingDirective = startNode.GetAncestorOrThis<UsingDirectiveSyntax>();
            if (usingDirective != null)
            {
                startNode = usingDirective.Parent.Parent;
                if (startNode == null)
                    return null;
            }

            // check whether I am under a namespace
            var @namespace = startNode.GetAncestorOrThis<BaseNamespaceDeclarationSyntax>();
            if (@namespace != null)
                return @namespace.SpanStart;

            // no namespace, under compilation unit directly.  Pass -1 so there is no ambiguity with a namespace decl
            // that starts at position 0.
            return -1;
        }

        private static SyntaxNode GetStartNodeForNamespaceId(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        {
            if (!semanticModel.IsSpeculativeSemanticModel)
                return token.Parent;

            var originalSemanticMode = semanticModel.GetOriginalSemanticModel();
            token = originalSemanticMode.SyntaxTree.GetRoot(cancellationToken).FindToken(semanticModel.OriginalPositionForSpeculation);

            return token.Parent;
        }

        protected static TypeSyntax CreatePredefinedTypeSyntax(SyntaxNode nodeToReplace, SyntaxToken token)
        {
            TypeSyntax node = token.Kind() == SyntaxKind.IdentifierToken
                ? SyntaxFactory.IdentifierName(token)
                : SyntaxFactory.PredefinedType(token);
            return node.WithTriviaFrom(nodeToReplace);
        }

        protected static bool InsideNameOfExpression(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var nameOfInvocationExpr = expression.FirstAncestorOrSelf<InvocationExpressionSyntax>(
                invocationExpr =>
                {
                    return invocationExpr.Expression is IdentifierNameSyntax identifierName &&
                        identifierName.Identifier.Text == "nameof" &&
                        semanticModel.GetConstantValue(invocationExpr).HasValue &&
                        semanticModel.GetTypeInfo(invocationExpr).Type.SpecialType == SpecialType.System_String;
                });

            return nameOfInvocationExpr != null;
        }

        protected static bool PreferPredefinedTypeKeywordInMemberAccess(ExpressionSyntax expression, CSharpSimplifierOptions options, SemanticModel semanticModel)
        {
            if (!options.PreferPredefinedTypeKeywordInMemberAccess.Value)
                return false;

            return (expression.IsDirectChildOfMemberAccessExpression() || expression.InsideCrefReference()) &&
                   !InsideNameOfExpression(expression, semanticModel);
        }

        protected static bool WillConflictWithExistingLocal(
            ExpressionSyntax expression, ExpressionSyntax simplifiedNode, SemanticModel semanticModel)
        {
            if (simplifiedNode is IdentifierNameSyntax identifierName &&
                !SyntaxFacts.IsInNamespaceOrTypeContext(expression))
            {
                var symbols = semanticModel.LookupSymbols(expression.SpanStart, name: identifierName.Identifier.ValueText);
                return symbols.Any(static s => s is ILocalSymbol);
            }

            return false;
        }
    }
}
