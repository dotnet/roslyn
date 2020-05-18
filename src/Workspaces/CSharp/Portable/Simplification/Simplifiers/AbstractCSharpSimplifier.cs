// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Simplification.Simplifiers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    /// <summary>
    /// Contains helpers used by several simplifier subclasses.
    /// </summary>
    internal abstract class AbstractCSharpSimplifier<TSyntax, TSimplifiedSyntax>
        : AbstractSimplifier<TSyntax, TSimplifiedSyntax>
        where TSyntax : SyntaxNode
        where TSimplifiedSyntax : SyntaxNode
    {
        /// <summary>
        /// Returns the predefined keyword kind for a given <see cref="SpecialType"/>.
        /// </summary>
        /// <param name="specialType">The <see cref="SpecialType"/> of this type.</param>
        /// <returns>The keyword kind for a given special type, or SyntaxKind.None if the type name is not a predefined type.</returns>
        protected static SyntaxKind GetPredefinedKeywordKind(SpecialType specialType)
            => specialType switch
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

            // Perf: We are only using the syntax tree root in a fast-path syntax check. If the root is not readily
            // available, it is fine to continue through the normal algorithm.
            if (originalModel.SyntaxTree.TryGetRoot(out var root))
            {
                if (!HasUsingAliasDirective(root))
                {
                    return false;
                }
            }

            // If the Symbol is a constructor get its containing type
            if (symbol.IsConstructor())
            {
                symbol = symbol.ContainingType;
            }

            if (node is QualifiedNameSyntax || node is AliasQualifiedNameSyntax)
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
                        var keywordKind = GetPredefinedKeywordKind(type.SpecialType);
                        if (keywordKind != SyntaxKind.None)
                        {
                            preferAliasToQualifiedName = false;
                        }
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
                        var keywordKind = GetPredefinedKeywordKind(type.SpecialType);
                        if (keywordKind != SyntaxKind.None)
                        {
                            preferAliasToQualifiedName = false;
                        }
                    }
                }
            }

            aliasReplacement = GetAliasForSymbol((INamespaceOrTypeSymbol)symbol, node.GetFirstToken(), semanticModel, cancellationToken);
            if (aliasReplacement != null && preferAliasToQualifiedName)
            {
                return ValidateAliasForTarget(aliasReplacement, semanticModel, node, symbol);
            }

            return false;
        }

        private static bool IsAliasReplaceableExpression(ExpressionSyntax expression)
        {
            var current = expression;
            while (current.IsKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax currentMember))
            {
                current = currentMember.Expression;
                continue;
            }

            return current.IsKind(SyntaxKind.AliasQualifiedName,
                                  SyntaxKind.IdentifierName,
                                  SyntaxKind.GenericName,
                                  SyntaxKind.QualifiedName);
        }

        private static bool HasUsingAliasDirective(SyntaxNode syntax)
        {
            SyntaxList<UsingDirectiveSyntax> usings;
            SyntaxList<MemberDeclarationSyntax> members;
            if (syntax.IsKind(SyntaxKind.NamespaceDeclaration, out NamespaceDeclarationSyntax namespaceDeclaration))
            {
                usings = namespaceDeclaration.Usings;
                members = namespaceDeclaration.Members;
            }
            else if (syntax.IsKind(SyntaxKind.CompilationUnit, out CompilationUnitSyntax compilationUnit))
            {
                usings = compilationUnit.Usings;
                members = compilationUnit.Members;
            }
            else
            {
                return false;
            }

            foreach (var usingDirective in usings)
            {
                if (usingDirective.Alias != null)
                {
                    return true;
                }
            }

            foreach (var member in members)
            {
                if (HasUsingAliasDirective(member))
                {
                    return true;
                }
            }

            return false;
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
            {
                return null;
            }

            var namespaceId = GetNamespaceIdForAliasSearch(semanticModel, token, cancellationToken);
            if (namespaceId < 0)
            {
                return null;
            }

            if (!AliasSymbolCache.TryGetAliasSymbol(originalSemanticModel, namespaceId, symbol, out var aliasSymbol))
            {
                // add cache
                AliasSymbolCache.AddAliasSymbols(originalSemanticModel, namespaceId, semanticModel.LookupNamespacesAndTypes(token.SpanStart).OfType<IAliasSymbol>());

                // retry
                AliasSymbolCache.TryGetAliasSymbol(originalSemanticModel, namespaceId, symbol, out aliasSymbol);
            }

            return aliasSymbol;
        }

        private static int GetNamespaceIdForAliasSearch(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        {
            var startNode = GetStartNodeForNamespaceId(semanticModel, token, cancellationToken);
            if (!startNode.SyntaxTree.HasCompilationUnitRoot)
            {
                return -1;
            }

            // NOTE: If we're currently in a block of usings, then we want to collect the
            // aliases that are higher up than this block.  Using aliases declared in a block of
            // usings are not usable from within that same block.
            var usingDirective = startNode.GetAncestorOrThis<UsingDirectiveSyntax>();
            if (usingDirective != null)
            {
                startNode = usingDirective.Parent.Parent;
                if (startNode == null)
                {
                    return -1;
                }
            }

            // check whether I am under a namespace
            var @namespace = startNode.GetAncestorOrThis<NamespaceDeclarationSyntax>();
            if (@namespace != null)
            {
                // since we have node inside of the root, root should be already there
                // search for namespace id should be quite cheap since normally there should be
                // only a few namespace defined in a source file if it is not 1. that is why it is
                // not cached.
                var startIndex = 1;
                return GetNamespaceId(startNode.SyntaxTree.GetRoot(cancellationToken), @namespace, ref startIndex);
            }

            // no namespace, under compilation unit directly
            return 0;
        }

        private static SyntaxNode GetStartNodeForNamespaceId(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        {
            if (!semanticModel.IsSpeculativeSemanticModel)
            {
                return token.Parent;
            }

            var originalSemanticMode = semanticModel.GetOriginalSemanticModel();
            token = originalSemanticMode.SyntaxTree.GetRoot(cancellationToken).FindToken(semanticModel.OriginalPositionForSpeculation);

            return token.Parent;
        }

        private static int GetNamespaceId(SyntaxNode container, NamespaceDeclarationSyntax target, ref int index)
            => container switch
            {
                CompilationUnitSyntax compilation => GetNamespaceId(compilation.Members, target, ref index),
                NamespaceDeclarationSyntax @namespace => GetNamespaceId(@namespace.Members, target, ref index),
                _ => throw ExceptionUtilities.UnexpectedValue(container)
            };

        private static int GetNamespaceId(SyntaxList<MemberDeclarationSyntax> members, NamespaceDeclarationSyntax target, ref int index)
        {
            foreach (var member in members)
            {
                if (!(member is NamespaceDeclarationSyntax childNamespace))
                {
                    continue;
                }

                if (childNamespace == target)
                {
                    return index;
                }

                index++;
                var result = GetNamespaceId(childNamespace, target, ref index);
                if (result > 0)
                {
                    return result;
                }
            }

            return -1;
        }

        protected static TypeSyntax CreatePredefinedTypeSyntax(ExpressionSyntax expression, SyntaxKind keywordKind)
            => SyntaxFactory.PredefinedType(SyntaxFactory.Token(expression.GetLeadingTrivia(), keywordKind, expression.GetTrailingTrivia()));

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

        protected static bool PreferPredefinedTypeKeywordInMemberAccess(ExpressionSyntax expression, OptionSet optionSet, SemanticModel semanticModel)
        {
            if (!SimplificationHelpers.PreferPredefinedTypeKeywordInMemberAccess(optionSet, semanticModel.Language))
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
                return symbols.Any(s => s is ILocalSymbol);
            }

            return false;
        }
    }
}
