// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal class ExpressionSimplifier : AbstractCSharpSimplifier<ExpressionSyntax, ExpressionSyntax>
    {
        public static readonly ExpressionSimplifier Instance = new ExpressionSimplifier();

        private ExpressionSimplifier()
        {
        }

        public override bool TrySimplify(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            OptionSet optionSet,
            out ExpressionSyntax replacementNode,
            out TextSpan issueSpan,
            CancellationToken cancellationToken)
        {
            if (TryReduceExplicitName(expression, semanticModel, out var replacementTypeNode, out issueSpan, optionSet, cancellationToken))
            {
                replacementNode = replacementTypeNode;
                return true;
            }

            return TrySimplify(expression, semanticModel, out replacementNode, out issueSpan, cancellationToken);
        }

        private static bool TryReduceExplicitName(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default;

            if (expression.ContainsInterleavedDirective(cancellationToken))
                return false;

            if (expression.IsKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax memberAccess))
                return TryReduceMemberAccessExpression(memberAccess, semanticModel, out replacementNode, out issueSpan, optionSet, cancellationToken);

            if (expression is NameSyntax name)
                return NameSimplifier.Instance.TrySimplify(name, semanticModel, optionSet, out replacementNode, out issueSpan, cancellationToken);

            return false;
        }

        private static bool TryReduceMemberAccessExpression(
            MemberAccessExpressionSyntax memberAccess,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default;

            if (memberAccess.Name == null || memberAccess.Expression == null)
                return false;

            // if this node is annotated as being a SpecialType, let's use this information.
            if (memberAccess.HasAnnotations(SpecialTypeAnnotation.Kind))
            {
                replacementNode = SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(
                        memberAccess.GetLeadingTrivia(),
                        GetPredefinedKeywordKind(SpecialTypeAnnotation.GetSpecialType(memberAccess.GetAnnotations(SpecialTypeAnnotation.Kind).First())),
                        memberAccess.GetTrailingTrivia()));

                issueSpan = memberAccess.Span;
                return true;
            }

            // See https://github.com/dotnet/roslyn/issues/40974
            //
            // To be very safe, we only support simplifying code that bound to a symbol without any
            // sort of problems.  We could potentially relax this in the future.  However, we would
            // need to be very careful about the implications of us offering to fixup 'broken' code 
            // in a manner that might end up making things worse or confusing the user.
            var symbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, memberAccess);
            if (symbol == null)
                return false;

            if (memberAccess.Expression.IsKind(SyntaxKind.ThisExpression) &&
                !SimplificationHelpers.ShouldSimplifyThisOrMeMemberAccessExpression(semanticModel, optionSet, symbol))
            {
                return false;
            }

            // if this node is on the left side, we could simplify to aliases
            if (!memberAccess.IsRightSideOfDot())
            {
                // Check if we need to replace this syntax with an alias identifier
                if (TryReplaceExpressionWithAlias(
                        memberAccess, semanticModel, symbol,
                        cancellationToken, out var aliasReplacement))
                {
                    // get the token text as it appears in source code to preserve e.g. unicode character escaping
                    var text = aliasReplacement.Name;
                    var syntaxRef = aliasReplacement.DeclaringSyntaxReferences.FirstOrDefault();

                    if (syntaxRef != null)
                    {
                        var declIdentifier = ((UsingDirectiveSyntax)syntaxRef.GetSyntax(cancellationToken)).Alias.Name.Identifier;
                        text = declIdentifier.IsVerbatimIdentifier() ? declIdentifier.ToString().Substring(1) : declIdentifier.ToString();
                    }

                    replacementNode = SyntaxFactory.IdentifierName(
                                        memberAccess.Name.Identifier.CopyAnnotationsTo(SyntaxFactory.Identifier(
                                            memberAccess.GetLeadingTrivia(),
                                            SyntaxKind.IdentifierToken,
                                            text,
                                            aliasReplacement.Name,
                                            memberAccess.GetTrailingTrivia())));

                    replacementNode = memberAccess.CopyAnnotationsTo(replacementNode);
                    replacementNode = memberAccess.Name.CopyAnnotationsTo(replacementNode);

                    issueSpan = memberAccess.Span;

                    // In case the alias name is the same as the last name of the alias target, we only include 
                    // the left part of the name in the unnecessary span to Not confuse uses.
                    if (memberAccess.Name.Identifier.ValueText == ((IdentifierNameSyntax)replacementNode).Identifier.ValueText)
                    {
                        issueSpan = memberAccess.Expression.Span;
                    }

                    return true;
                }

                // Check if the Expression can be replaced by Predefined Type keyword
                if (PreferPredefinedTypeKeywordInMemberAccess(memberAccess, optionSet, semanticModel))
                {
                    if (symbol != null && symbol.IsKind(SymbolKind.NamedType))
                    {
                        var keywordKind = GetPredefinedKeywordKind(((INamedTypeSymbol)symbol).SpecialType);
                        if (keywordKind != SyntaxKind.None)
                        {
                            replacementNode = CreatePredefinedTypeSyntax(memberAccess, keywordKind);

                            replacementNode = replacementNode
                                .WithAdditionalAnnotations<TypeSyntax>(new SyntaxAnnotation(
                                    nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)));

                            issueSpan = memberAccess.Span; // we want to show the whole expression as unnecessary

                            return true;
                        }
                    }
                }
            }

            // Try to eliminate cases without actually calling CanReplaceWithReducedName. For expressions of the form
            // 'this.Name' or 'base.Name', no additional check here is required.
            if (!memberAccess.Expression.IsKind(SyntaxKind.ThisExpression, SyntaxKind.BaseExpression))
            {
                GetReplacementCandidates(
                    semanticModel,
                    memberAccess,
                    symbol,
                    out var speculativeSymbols,
                    out var speculativeNamespacesAndTypes);

                if (!IsReplacementCandidate(symbol, speculativeSymbols, speculativeNamespacesAndTypes))
                {
                    return false;
                }
            }

            replacementNode = memberAccess.GetNameWithTriviaMoved();
            issueSpan = memberAccess.Expression.Span;

            return CanReplaceWithReducedName(
                memberAccess, replacementNode, semanticModel, symbol, cancellationToken);
        }

        private static void GetReplacementCandidates(
            SemanticModel semanticModel,
            MemberAccessExpressionSyntax memberAccess,
            ISymbol actualSymbol,
            out ImmutableArray<ISymbol> speculativeSymbols,
            out ImmutableArray<ISymbol> speculativeNamespacesAndTypes)
        {
            var containsNamespaceOrTypeSymbol = actualSymbol is INamespaceOrTypeSymbol;
            var containsOtherSymbol = !containsNamespaceOrTypeSymbol;

            speculativeSymbols = containsOtherSymbol
                ? semanticModel.LookupSymbols(memberAccess.SpanStart, name: memberAccess.Name.Identifier.ValueText)
                : ImmutableArray<ISymbol>.Empty;
            speculativeNamespacesAndTypes = containsNamespaceOrTypeSymbol
                ? semanticModel.LookupNamespacesAndTypes(memberAccess.SpanStart, name: memberAccess.Name.Identifier.ValueText)
                : ImmutableArray<ISymbol>.Empty;
        }

        /// <summary>
        /// Determines if <paramref name="speculativeSymbols"/> and <paramref name="speculativeNamespacesAndTypes"/>
        /// together contain a superset of the symbols in <paramref name="actualSymbol"/>.
        /// </summary>
        private static bool IsReplacementCandidate(ISymbol actualSymbol, ImmutableArray<ISymbol> speculativeSymbols, ImmutableArray<ISymbol> speculativeNamespacesAndTypes)
        {
            if (speculativeSymbols.IsEmpty && speculativeNamespacesAndTypes.IsEmpty)
            {
                return false;
            }

            if (actualSymbol is object)
            {
                return speculativeSymbols.Contains(actualSymbol, CandidateSymbolEqualityComparer.Instance)
                    || speculativeNamespacesAndTypes.Contains(actualSymbol, CandidateSymbolEqualityComparer.Instance);
            }

            return true;
        }

        /// <summary>
        /// Compares symbols by their original definition.
        /// </summary>
        private sealed class CandidateSymbolEqualityComparer : IEqualityComparer<ISymbol>
        {
            public static CandidateSymbolEqualityComparer Instance { get; } = new CandidateSymbolEqualityComparer();

            private CandidateSymbolEqualityComparer()
            {
            }

            public bool Equals(ISymbol x, ISymbol y)
            {
                if (x is null || y is null)
                {
                    return x == y;
                }

                return x.OriginalDefinition.Equals(y.OriginalDefinition);
            }

            public int GetHashCode(ISymbol obj)
                => obj?.OriginalDefinition.GetHashCode() ?? 0;
        }

        private static bool TrySimplify(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out ExpressionSyntax replacementNode,
            out TextSpan issueSpan,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default;

            switch (expression.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                    {
                        var memberAccess = (MemberAccessExpressionSyntax)expression;
                        if (IsNonRemovablePartOfDynamicMethodInvocation(semanticModel, memberAccess, cancellationToken))
                        {
                            return false;
                        }

                        if (TrySimplifyMemberAccessOrQualifiedName(memberAccess.Expression, memberAccess.Name, semanticModel, out var newLeft, out issueSpan))
                        {
                            // replacement node might not be in it's simplest form, so add simplify annotation to it.
                            replacementNode = memberAccess.Update(newLeft, memberAccess.OperatorToken, memberAccess.Name)
                                .WithAdditionalAnnotations(Simplifier.Annotation);

                            // Ensure that replacement doesn't change semantics.
                            return !ReplacementChangesSemantics(memberAccess, replacementNode, semanticModel);
                        }

                        return false;
                    }

                case SyntaxKind.QualifiedName:
                    {
                        var qualifiedName = (QualifiedNameSyntax)expression;
                        if (TrySimplifyMemberAccessOrQualifiedName(qualifiedName.Left, qualifiedName.Right, semanticModel, out var newLeft, out issueSpan))
                        {
                            // replacement node might not be in it's simplest form, so add simplify annotation to it.
                            replacementNode = qualifiedName.Update((NameSyntax)newLeft, qualifiedName.DotToken, qualifiedName.Right)
                                .WithAdditionalAnnotations(Simplifier.Annotation);

                            // Ensure that replacement doesn't change semantics.
                            return !ReplacementChangesSemantics(qualifiedName, replacementNode, semanticModel);
                        }

                        return false;
                    }
            }

            return false;
        }

        private static bool CanReplaceWithReducedName(
            MemberAccessExpressionSyntax memberAccess,
            ExpressionSyntax reducedName,
            SemanticModel semanticModel,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            if (!IsThisOrTypeOrNamespace(memberAccess, semanticModel))
            {
                return false;
            }

            var speculationAnalyzer = new SpeculationAnalyzer(memberAccess, reducedName, semanticModel, cancellationToken);
            if (!speculationAnalyzer.SymbolsForOriginalAndReplacedNodesAreCompatible() ||
                speculationAnalyzer.ReplacementChangesSemantics())
            {
                return false;
            }

            if (WillConflictWithExistingLocal(memberAccess, reducedName, semanticModel))
            {
                return false;
            }

            if (IsNonRemovablePartOfDynamicMethodInvocation(semanticModel, memberAccess, cancellationToken))
            {
                return false;
            }

            if (AccessMethodWithDynamicArgumentInsideStructConstructor(memberAccess, semanticModel))
            {
                return false;
            }

            if (memberAccess.Expression.Kind() == SyntaxKind.BaseExpression)
            {
                var enclosingNamedType = semanticModel.GetEnclosingNamedType(memberAccess.SpanStart, cancellationToken);
                if (enclosingNamedType != null &&
                    !enclosingNamedType.IsSealed &&
                    symbol != null &&
                    symbol.IsOverridable())
                {
                    return false;
                }
            }

            var invalidTransformation1 = ParserWouldTreatExpressionAsCast(reducedName, memberAccess);

            return !invalidTransformation1;
        }

        /// <summary>
        /// Tells if the member access is dynamically invoked and cannot be reduced. In the case of
        /// <c>NS1.NS2.T1.T2.Method(...dynamic...)</c> we can only remove the <c>NS1.NS2</c>
        /// portion. The namespace part is not encoded into the IL, but the specific types in
        /// <c>T1.T2</c> and cannot be removed.
        /// </summary>
        private static bool IsNonRemovablePartOfDynamicMethodInvocation(
            SemanticModel semanticModel, MemberAccessExpressionSyntax memberAccess, CancellationToken cancellationToken)
        {
            var ancestorInvocation = memberAccess.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (ancestorInvocation?.SpanStart == memberAccess.SpanStart)
            {
                var leftSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).GetAnySymbol();
                if (leftSymbol is INamedTypeSymbol)
                {
                    var type = semanticModel.GetTypeInfo(memberAccess.Parent, cancellationToken).Type;
                    if (type?.Kind == SymbolKind.DynamicType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /*
         * Name Reduction, to implicitly mean "this", is possible only after the initialization of all member variables but
         * since the check for initialization of all member variable is a lot of work for this simplification we don't simplify
         * even if all the member variables are initialized
         */
        private static bool AccessMethodWithDynamicArgumentInsideStructConstructor(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
        {
            var constructor = memberAccess.Ancestors().OfType<ConstructorDeclarationSyntax>().SingleOrDefault();

            if (constructor == null || constructor.Parent.Kind() != SyntaxKind.StructDeclaration)
            {
                return false;
            }

            return semanticModel.GetSymbolInfo(memberAccess.Name).CandidateReason == CandidateReason.LateBound;
        }

        // Note: The caller needs to verify that replacement doesn't change semantics of the original expression.
        private static bool TrySimplifyMemberAccessOrQualifiedName(
            ExpressionSyntax left,
            ExpressionSyntax right,
            SemanticModel semanticModel,
            out ExpressionSyntax replacementNode,
            out TextSpan issueSpan)
        {
            replacementNode = null;
            issueSpan = default;

            if (left != null && right != null)
            {
                var leftSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, left);
                if (leftSymbol != null && leftSymbol.Kind == SymbolKind.NamedType)
                {
                    var rightSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, right);
                    if (rightSymbol != null && (rightSymbol.IsStatic || rightSymbol.Kind == SymbolKind.NamedType))
                    {
                        // Static member access or nested type member access.
                        var containingType = rightSymbol.ContainingType;

                        var enclosingSymbol = semanticModel.GetEnclosingSymbol(left.SpanStart);
                        var enclosingTypeParametersInsideOut = new List<ISymbol>();

                        while (enclosingSymbol != null)
                        {
                            if (enclosingSymbol is IMethodSymbol methodSymbol)
                            {
                                if (methodSymbol.TypeArguments.Length != 0)
                                {
                                    enclosingTypeParametersInsideOut.AddRange(methodSymbol.TypeArguments);
                                }
                            }

                            if (enclosingSymbol is INamedTypeSymbol namedTypeSymbol)
                            {
                                if (namedTypeSymbol.TypeArguments.Length != 0)
                                {
                                    enclosingTypeParametersInsideOut.AddRange(namedTypeSymbol.TypeArguments);
                                }
                            }

                            enclosingSymbol = enclosingSymbol.ContainingSymbol;
                        }

                        if (containingType != null && !containingType.Equals(leftSymbol))
                        {
                            if (leftSymbol is INamedTypeSymbol &&
                                containingType.TypeArguments.Length != 0)
                            {
                                return false;
                            }

                            // We have a static member access or a nested type member access using a more derived type.
                            // Simplify syntax so as to use accessed member's most immediate containing type instead of the derived type.
                            replacementNode = containingType.GenerateTypeSyntax()
                                .WithLeadingTrivia(left.GetLeadingTrivia())
                                .WithTrailingTrivia(left.GetTrailingTrivia());
                            issueSpan = left.Span;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsThisOrTypeOrNamespace(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
        {
            if (memberAccess.Expression.Kind() == SyntaxKind.ThisExpression)
            {
                var previousToken = memberAccess.Expression.GetFirstToken().GetPreviousToken();

                var symbol = semanticModel.GetSymbolInfo(memberAccess.Name).Symbol;

                if (previousToken.Kind() == SyntaxKind.OpenParenToken &&
                    previousToken.Parent.IsKind(SyntaxKind.ParenthesizedExpression, out ParenthesizedExpressionSyntax parenExpr) &&
                    !parenExpr.IsParentKind(SyntaxKind.ParenthesizedExpression) &&
                    parenExpr.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression &&
                    symbol != null && symbol.Kind == SymbolKind.Method)
                {
                    return false;
                }

                return true;
            }

            var expressionInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
            if (SimplificationHelpers.IsValidSymbolInfo(expressionInfo.Symbol))
            {
                if (expressionInfo.Symbol is INamespaceOrTypeSymbol)
                {
                    return true;
                }

                if (expressionInfo.Symbol.IsThisParameter())
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ParserWouldTreatExpressionAsCast(ExpressionSyntax reducedNode, MemberAccessExpressionSyntax originalNode)
        {
            SyntaxNode parent = originalNode;
            while (parent != null)
            {
                if (parent.IsParentKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    parent = parent.Parent;
                    continue;
                }

                if (!parent.IsParentKind(SyntaxKind.ParenthesizedExpression))
                {
                    return false;
                }

                break;
            }

            var newExpression = parent.ReplaceNode(originalNode, reducedNode);

            // detect cast ambiguities according to C# spec #7.7.6 
            if (IsNameOrMemberAccessButNoExpression(newExpression))
            {
                var nextToken = parent.Parent.GetLastToken().GetNextToken();

                return nextToken.Kind() == SyntaxKind.OpenParenToken ||
                    nextToken.Kind() == SyntaxKind.TildeToken ||
                    nextToken.Kind() == SyntaxKind.ExclamationToken ||
                    (SyntaxFacts.IsKeywordKind(nextToken.Kind()) && !(nextToken.Kind() == SyntaxKind.AsKeyword || nextToken.Kind() == SyntaxKind.IsKeyword));
            }

            return false;
        }

        private static bool IsNameOrMemberAccessButNoExpression(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax memberAccess))
            {
                return memberAccess.Expression.IsKind(SyntaxKind.IdentifierName) ||
                    IsNameOrMemberAccessButNoExpression(memberAccess.Expression);
            }

            return node.IsKind(SyntaxKind.IdentifierName);
        }

        protected static bool ReplacementChangesSemantics(ExpressionSyntax originalExpression, ExpressionSyntax replacedExpression, SemanticModel semanticModel)
        {
            var speculationAnalyzer = new SpeculationAnalyzer(originalExpression, replacedExpression, semanticModel, CancellationToken.None);
            return speculationAnalyzer.ReplacementChangesSemantics();
        }
    }
}
