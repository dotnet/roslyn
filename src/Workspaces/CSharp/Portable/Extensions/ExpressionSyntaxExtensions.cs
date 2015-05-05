// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class ExpressionSyntaxExtensions
    {
        public static ExpressionSyntax WalkUpParentheses(this ExpressionSyntax expression)
        {
            while (expression.IsParentKind(SyntaxKind.ParenthesizedExpression))
            {
                expression = (ExpressionSyntax)expression.Parent;
            }

            return expression;
        }

        public static ExpressionSyntax WalkDownParentheses(this ExpressionSyntax expression)
        {
            while (expression.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                expression = ((ParenthesizedExpressionSyntax)expression).Expression;
            }

            return expression;
        }

        public static ExpressionSyntax Parenthesize(this ExpressionSyntax expression, bool includeElasticTrivia = true)
        {
            if (includeElasticTrivia)
            {
                return SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia())
                                    .WithTriviaFrom(expression)
                                    .WithAdditionalAnnotations(Simplifier.Annotation);
            }
            else
            {
                return SyntaxFactory.ParenthesizedExpression
                (
                    SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.OpenParenToken, SyntaxTriviaList.Empty),
                    expression.WithoutTrivia(),
                    SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.CloseParenToken, SyntaxTriviaList.Empty)
                )
                .WithTriviaFrom(expression)
                .WithAdditionalAnnotations(Simplifier.Annotation);
            }
        }

        public static CastExpressionSyntax Cast(
            this ExpressionSyntax expression,
            ITypeSymbol targetType)
        {
            return SyntaxFactory.CastExpression(
                type: targetType.GenerateTypeSyntax(),
                expression: expression.Parenthesize())
                .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        /// <summary>
        /// Adds to <paramref name="targetType"/> if it does not contain an anonymous
        /// type and binds to the same type at the given <paramref name="position"/>.
        /// </summary>
        public static ExpressionSyntax CastIfPossible(
            this ExpressionSyntax expression,
            ITypeSymbol targetType,
            int position,
            SemanticModel semanticModel)
        {
            if (targetType.ContainsAnonymousType())
            {
                return expression;
            }

            if (targetType.Kind == SymbolKind.DynamicType)
            {
                targetType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            }

            var typeSyntax = targetType.GenerateTypeSyntax();
            var type = semanticModel.GetSpeculativeTypeInfo(
                position,
                typeSyntax,
                SpeculativeBindingOption.BindAsTypeOrNamespace).Type;

            if (!targetType.Equals(type))
            {
                return expression;
            }

            var castExpression = expression.Cast(targetType);

            // Ensure that inserting the cast doesn't change the semantics.
            var specAnalyzer = new SpeculationAnalyzer(expression, castExpression, semanticModel, CancellationToken.None);
            var speculativeSemanticModel = specAnalyzer.SpeculativeSemanticModel;
            if (speculativeSemanticModel == null)
            {
                return expression;
            }

            var speculatedCastExpression = (CastExpressionSyntax)specAnalyzer.ReplacedExpression;
            if (!speculatedCastExpression.IsUnnecessaryCast(speculativeSemanticModel, CancellationToken.None))
            {
                return expression;
            }

            return castExpression;
        }

        public static bool IsQualifiedCrefName(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.NameMemberCref) && expression.Parent.IsParentKind(SyntaxKind.QualifiedCref);
        }

        public static bool IsMemberAccessExpressionName(this ExpressionSyntax expression)
        {
            return (expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) && ((MemberAccessExpressionSyntax)expression.Parent).Name == expression) ||
                   (IsMemberBindingExpressionName(expression));
        }

        public static bool IsAnyMemberAccessExpressionName(this ExpressionSyntax expression)
        {
            if (expression == null)
            {
                return false;
            }

            return expression == (expression.Parent as MemberAccessExpressionSyntax)?.Name ||
                expression.IsMemberBindingExpressionName();
        }

        private static bool IsMemberBindingExpressionName(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.MemberBindingExpression) &&
                ((MemberBindingExpressionSyntax)expression.Parent).Name == expression;
        }

        public static bool IsRightSideOfQualifiedName(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.QualifiedName) && ((QualifiedNameSyntax)expression.Parent).Right == expression;
        }

        public static bool IsRightSideOfColonColon(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.AliasQualifiedName) && ((AliasQualifiedNameSyntax)expression.Parent).Name == expression;
        }

        public static bool IsRightSideOfDot(this ExpressionSyntax name)
        {
            return IsMemberAccessExpressionName(name) || IsRightSideOfQualifiedName(name) || IsQualifiedCrefName(name);
        }

        public static bool IsRightSideOfDotOrArrow(this ExpressionSyntax name)
        {
            return IsAnyMemberAccessExpressionName(name) || IsRightSideOfQualifiedName(name);
        }

        public static bool IsRightSideOfDotOrColonColon(this ExpressionSyntax name)
        {
            return IsRightSideOfDot(name) || IsRightSideOfColonColon(name);
        }

        public static bool IsRightSideOfDotOrArrowOrColonColon(this ExpressionSyntax name)
        {
            return IsRightSideOfDotOrArrow(name) || IsRightSideOfColonColon(name);
        }

        public static bool IsRightOfCloseParen(this ExpressionSyntax expression)
        {
            var firstToken = expression.GetFirstToken();
            return firstToken.Kind() != SyntaxKind.None
                && firstToken.GetPreviousToken().Kind() == SyntaxKind.CloseParenToken;
        }

        public static bool IsLeftSideOfDot(this ExpressionSyntax expression)
        {
            return
                IsLeftSideOfQualifiedName(expression) ||
                (expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) && ((MemberAccessExpressionSyntax)expression.Parent).Expression == expression);
        }

        public static bool IsLeftSideOfDotOrArrow(this ExpressionSyntax expression)
        {
            return
                IsLeftSideOfQualifiedName(expression) ||
                (expression.Parent is MemberAccessExpressionSyntax && ((MemberAccessExpressionSyntax)expression.Parent).Expression == expression);
        }

        public static bool IsLeftSideOfQualifiedName(this ExpressionSyntax expression)
        {
            return
                expression.IsParentKind(SyntaxKind.QualifiedName) && ((QualifiedNameSyntax)expression.Parent).Left == expression;
        }

        public static bool IsExpressionOfInvocation(this ExpressionSyntax expression)
        {
            return
                expression.IsParentKind(SyntaxKind.InvocationExpression) && ((InvocationExpressionSyntax)expression.Parent).Expression == expression;
        }

        public static bool TryGetNameParts(this ExpressionSyntax expression, out IList<string> parts)
        {
            var partsList = new List<string>();
            if (!TryGetNameParts(expression, partsList))
            {
                parts = null;
                return false;
            }

            parts = partsList;
            return true;
        }

        public static bool TryGetNameParts(this ExpressionSyntax expression, List<string> parts)
        {
            if (expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var memberAccess = (MemberAccessExpressionSyntax)expression;
                if (!TryGetNameParts(memberAccess.Expression, parts))
                {
                    return false;
                }

                return AddSimpleName(memberAccess.Name, parts);
            }
            else if (expression.IsKind(SyntaxKind.QualifiedName))
            {
                var qualifiedName = (QualifiedNameSyntax)expression;
                if (!TryGetNameParts(qualifiedName.Left, parts))
                {
                    return false;
                }

                return AddSimpleName(qualifiedName.Right, parts);
            }
            else if (expression is SimpleNameSyntax)
            {
                return AddSimpleName((SimpleNameSyntax)expression, parts);
            }
            else
            {
                return false;
            }
        }

        private static bool AddSimpleName(SimpleNameSyntax simpleName, List<string> parts)
        {
            if (!simpleName.IsKind(SyntaxKind.IdentifierName))
            {
                return false;
            }

            parts.Add(simpleName.Identifier.ValueText);
            return true;
        }

        public static bool IsAnyLiteralExpression(this ExpressionSyntax expression)
        {
            return
                expression.IsKind(SyntaxKind.CharacterLiteralExpression) ||
                expression.IsKind(SyntaxKind.FalseLiteralExpression) ||
                expression.IsKind(SyntaxKind.NullLiteralExpression) ||
                expression.IsKind(SyntaxKind.NumericLiteralExpression) ||
                expression.IsKind(SyntaxKind.StringLiteralExpression) ||
                expression.IsKind(SyntaxKind.TrueLiteralExpression);
        }

        public static bool IsInConstantContext(this ExpressionSyntax expression)
        {
            if (expression.GetAncestor<ParameterSyntax>() != null)
            {
                return true;
            }

            var attributeArgument = expression.GetAncestor<AttributeArgumentSyntax>();
            if (attributeArgument != null)
            {
                if (attributeArgument.NameEquals == null ||
                    expression != attributeArgument.NameEquals.Name)
                {
                    return true;
                }
            }

            // TODO(cyrusn): Add more cases.
            return false;
        }

        public static bool IsInOutContext(this ExpressionSyntax expression)
        {
            var argument = expression.Parent as ArgumentSyntax;
            return
                argument != null &&
                argument.Expression == expression &&
                argument.RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword;
        }

        public static bool IsInRefContext(this ExpressionSyntax expression)
        {
            var argument = expression.Parent as ArgumentSyntax;
            return
                argument != null &&
                argument.Expression == expression &&
                argument.RefOrOutKeyword.Kind() == SyntaxKind.RefKeyword;
        }

        public static bool IsOnlyWrittenTo(this ExpressionSyntax expression)
        {
            if (expression.IsRightSideOfDotOrArrow())
            {
                expression = expression.Parent as ExpressionSyntax;
            }

            if (expression != null)
            {
                if (expression.IsInOutContext())
                {
                    return true;
                }

                if (expression.Parent != null)
                {
                    if (expression.IsLeftSideOfAssignExpression())
                    {
                        return true;
                    }

                    var nameEquals = expression.Parent as NameEqualsSyntax;
                    if (nameEquals != null && nameEquals.IsParentKind(SyntaxKind.AttributeArgument))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsWrittenTo(this ExpressionSyntax expression)
        {
            if (expression.IsOnlyWrittenTo())
            {
                return true;
            }

            if (expression.IsRightSideOfDotOrArrow())
            {
                expression = expression.Parent as ExpressionSyntax;
            }

            if (expression != null)
            {
                if (expression.IsInRefContext())
                {
                    return true;
                }

                // We're written if we're used in a ++, or -- expression.
                if (expression.Parent != null)
                {
                    switch (expression.Parent.Kind())
                    {
                        case SyntaxKind.PostIncrementExpression:
                        case SyntaxKind.PreIncrementExpression:
                        case SyntaxKind.PostDecrementExpression:
                        case SyntaxKind.PreDecrementExpression:
                            return true;
                    }

                    if (expression.IsLeftSideOfAnyAssignExpression())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsNamedArgumentIdentifier(this ExpressionSyntax expression)
        {
            return expression is IdentifierNameSyntax && expression.Parent is NameColonSyntax;
        }

        public static bool IsInsideNameOf(this ExpressionSyntax expression)
        {
            return expression.SyntaxTree.IsNameOfContext(expression.SpanStart);
        }

        private static bool CanReplace(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Local:
                case SymbolKind.Method:
                case SymbolKind.Parameter:
                case SymbolKind.Property:
                case SymbolKind.RangeVariable:
                    return true;
            }

            return false;
        }

        public static bool CanReplaceWithRValue(
            this ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // An RValue can't be written into. 
            // i.e. you can't replace "a" in "a = b" with "Foo() = b".
            return
                expression != null &&
                !expression.IsWrittenTo() &&
                CanReplaceWithLValue(expression, semanticModel, cancellationToken);
        }

        public static bool CanReplaceWithLValue(
            this ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (expression.IsKind(SyntaxKind.StackAllocArrayCreationExpression))
            {
                // Stack alloc is very interesting.  While it appears to be an expression, it is only
                // such so it can appear in a variable decl.  It is not a normal expression that can
                // go anywhere.
                return false;
            }

            if (expression.IsKind(SyntaxKind.BaseExpression) ||
                expression.IsKind(SyntaxKind.CollectionInitializerExpression) ||
                expression.IsKind(SyntaxKind.ObjectInitializerExpression) ||
                expression.IsKind(SyntaxKind.ComplexElementInitializerExpression))
            {
                return false;
            }

            // literal can be always replaced.
            if (expression is LiteralExpressionSyntax && !expression.IsParentKind(SyntaxKind.UnaryMinusExpression))
            {
                return true;
            }

            if (!(expression is ObjectCreationExpressionSyntax) && !(expression is AnonymousObjectCreationExpressionSyntax))
            {
                var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
                if (!symbolInfo.GetBestOrAllSymbols().All(CanReplace))
                {
                    // If the expression is actually a reference to a type, then it can't be replaced
                    // with an arbitrary expression.
                    return false;
                }
            }

            // If we are a conditional access expression:
            // case (1) : obj?.Method(), obj1.obj2?.Property
            // case (2) : obj?.GetAnotherObj()?.Length, obj?.AnotherObj?.Length
            // in case (1), the entire expression forms the conditional access expression, which can be replaced with an LValue.
            // in case (2), the nested conditional access expression is ".GetAnotherObj()?.Length" or ".AnotherObj()?.Length"
            // essentially, the first expression (before the operator) in a nested conditional access expression 
            // is some form of member binding expression and they cannot be replaced with an LValue.
            if (expression.IsKind(SyntaxKind.ConditionalAccessExpression))
            {
                return expression.Parent.Kind() != SyntaxKind.ConditionalAccessExpression;
            }

            switch (expression.Parent.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    // Technically, you could introduce an LValue for "Foo" in "Foo()" even if "Foo" binds
                    // to a method.  (i.e. by assigning to a Func<...> type).  However, this is so contrived
                    // and none of the features that use this extension consider this replaceable.
                    if (expression.IsKind(SyntaxKind.IdentifierName) || expression is MemberAccessExpressionSyntax)
                    {
                        // If it looks like a method then we don't allow it to be replaced if it is a
                        // method (or if it doesn't bind).

                        var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
                        return symbolInfo.GetBestOrAllSymbols().Any() && !symbolInfo.GetBestOrAllSymbols().Any(s => s is IMethodSymbol);
                    }
                    else
                    {
                        // It doesn't look like a method, we allow this to be replaced.
                        return true;
                    }

                // If the parent is a conditional access expression, we could introduce an LValue
                // for the given expression, unless it is itself a MemberBindingExpression or starts with one.
                // Case (1) : The WhenNotNull clause always starts with a memberbindingexpression.
                //              expression '.Method()' in a?.Method()
                // Case (2) : The Expression clause always starts with a memberbindingexpression if 
                // the grandparent is a conditional access expression.
                //              expression '.Method' in a?.Method()?.Length
                // Case (3) : The child Conditional access expression always starts with a memberbindingexpression if
                // the parent is a conditional access expression. This case is already covered before the parent kind switch
                case SyntaxKind.ConditionalAccessExpression:
                    var parentConditionalAccessExpression = (ConditionalAccessExpressionSyntax)expression.Parent;
                    return expression != parentConditionalAccessExpression.WhenNotNull &&
                            !parentConditionalAccessExpression.Parent.IsKind(SyntaxKind.ConditionalAccessExpression);

                case SyntaxKind.IsExpression:
                case SyntaxKind.AsExpression:
                    // Can't introduce a variable for the type portion of an is/as check.
                    var isOrAsExpression = (BinaryExpressionSyntax)expression.Parent;
                    return expression == isOrAsExpression.Left;
                case SyntaxKind.EqualsValueClause:
                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.ArrayInitializerExpression:
                case SyntaxKind.CollectionInitializerExpression:
                case SyntaxKind.Argument:
                case SyntaxKind.AttributeArgument:
                case SyntaxKind.AnonymousObjectMemberDeclarator:
                case SyntaxKind.ArrowExpressionClause:
                case SyntaxKind.AwaitExpression:
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.YieldReturnStatement:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedExpression:
                case SyntaxKind.ArrayRankSpecifier:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.IfStatement:
                case SyntaxKind.CatchFilterClause:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.DoStatement:
                case SyntaxKind.ThrowStatement:
                case SyntaxKind.SwitchStatement:
                case SyntaxKind.InterpolatedStringExpression:
                case SyntaxKind.ComplexElementInitializerExpression:
                case SyntaxKind.Interpolation:
                    // Direct parent kind checks.
                    return true;
            }

            if (expression.Parent is PrefixUnaryExpressionSyntax)
            {
                if (!(expression is LiteralExpressionSyntax && expression.IsParentKind(SyntaxKind.UnaryMinusExpression)))
                {
                    return true;
                }
            }

            var parentNonExpression = expression.GetAncestors().SkipWhile(n => n is ExpressionSyntax).FirstOrDefault();
            var topExpression = expression;
            while (topExpression.Parent is TypeSyntax)
            {
                topExpression = (TypeSyntax)topExpression.Parent;
            }

            if (parentNonExpression != null &&
                parentNonExpression.IsKind(SyntaxKind.FromClause) &&
                topExpression != null &&
                ((FromClauseSyntax)parentNonExpression).Type == topExpression)
            {
                return false;
            }

            // Parent type checks.
            if (expression.Parent is PostfixUnaryExpressionSyntax ||
                expression.Parent is BinaryExpressionSyntax ||
                expression.Parent is AssignmentExpressionSyntax ||
                expression.Parent is QueryClauseSyntax ||
                expression.Parent is SelectOrGroupClauseSyntax ||
                expression.Parent is CheckedExpressionSyntax)
            {
                return true;
            }

            // Specific child checks.
            if (expression.CheckParent<ForEachStatementSyntax>(f => f.Expression == expression) ||
                expression.CheckParent<MemberAccessExpressionSyntax>(m => m.Expression == expression) ||
                expression.CheckParent<CastExpressionSyntax>(c => c.Expression == expression))
            {
                return true;
            }

            // Misc checks.
            if ((expression.IsParentKind(SyntaxKind.NameEquals) && expression.Parent.IsParentKind(SyntaxKind.AttributeArgument)) ||
                expression.IsLeftSideOfAnyAssignExpression())
            {
                return true;
            }

            return false;
        }

        public static bool CanAccessInstanceAndStaticMembersOffOf(
            this ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // Check for the Color Color case.
            //
            // color color: if you bind "A" and you get a symbol and the type of that symbol is
            // Q; and if you bind "A" *again* as a type and you get type Q, then both A.static
            // and A.instance are permitted
            if (expression is IdentifierNameSyntax)
            {
                var instanceSymbol = semanticModel.GetSymbolInfo(expression, cancellationToken).GetAnySymbol();

                if (!(instanceSymbol is INamespaceOrTypeSymbol))
                {
                    var instanceType = instanceSymbol.GetSymbolType();
                    if (instanceType != null)
                    {
                        var speculativeSymbolInfo = semanticModel.GetSpeculativeSymbolInfo(expression.SpanStart, expression, SpeculativeBindingOption.BindAsTypeOrNamespace);
                        if (speculativeSymbolInfo.CandidateReason != CandidateReason.NotATypeOrNamespace)
                        {
                            var staticType = speculativeSymbolInfo.GetAnySymbol().GetSymbolType();

                            return SymbolEquivalenceComparer.Instance.Equals(instanceType, staticType);
                        }
                    }
                }
            }

            return false;
        }

        public static bool TryReduceOrSimplifyExplicitName(
            this ExpressionSyntax expression,
            SemanticModel semanticModel,
            out ExpressionSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            TypeSyntax replacementTypeNode;
            if (expression.TryReduceExplicitName(semanticModel, out replacementTypeNode, out issueSpan, optionSet, cancellationToken))
            {
                replacementNode = replacementTypeNode;
                return true;
            }

            return expression.TrySimplify(semanticModel, optionSet, out replacementNode, out issueSpan);
        }

        public static bool TryReduceExplicitName(
            this ExpressionSyntax expression,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default(TextSpan);

            if (expression.ContainsInterleavedDirective(cancellationToken))
            {
                return false;
            }

            if (expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var memberAccess = (MemberAccessExpressionSyntax)expression;
                return memberAccess.TryReduce(semanticModel, out replacementNode, out issueSpan, optionSet, cancellationToken);
            }
            else if (expression is NameSyntax)
            {
                var name = (NameSyntax)expression;
                return name.TryReduce(semanticModel, out replacementNode, out issueSpan, optionSet, cancellationToken);
            }
            else if (expression is TypeSyntax)
            {
                var typeName = (TypeSyntax)expression;
                return typeName.IsReplaceableByVar(semanticModel, out replacementNode, out issueSpan, optionSet, cancellationToken);
            }

            return false;
        }

        private static bool TryReduce(
            this MemberAccessExpressionSyntax memberAccess,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default(TextSpan);

            if (memberAccess.Name == null || memberAccess.Expression == null)
            {
                return false;
            }

            if (optionSet.GetOption(SimplificationOptions.QualifyMemberAccessWithThisOrMe, semanticModel.Language) &&
                memberAccess.Expression.Kind() == SyntaxKind.ThisExpression)
            {
                return false;
            }

            // if this node is annotated as being a specialtype, let's use this information.
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

            // if this node is on the left side, we could simplify to aliases
            if (!memberAccess.IsRightSideOfDot())
            {
                // Check if we need to replace this syntax with an alias identifier
                IAliasSymbol aliasReplacement;
                if (memberAccess.TryReplaceWithAlias(semanticModel, optionSet.GetOption(SimplificationOptions.PreferAliasToQualification), cancellationToken, out aliasReplacement))
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
                    var symbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
                    if (symbol != null && symbol.IsKind(SymbolKind.NamedType))
                    {
                        var keywordKind = GetPredefinedKeywordKind(((INamedTypeSymbol)symbol).SpecialType);
                        if (keywordKind != SyntaxKind.None)
                        {
                            replacementNode = CreatePredefinedTypeSyntax(memberAccess, keywordKind);

                            issueSpan = memberAccess.Span; // we want to show the whole expression as unnecessary

                            return true;
                        }
                    }
                }
            }

            replacementNode = memberAccess.Name
                .WithLeadingTrivia(memberAccess.GetLeadingTriviaForSimplifiedMemberAccess())
                .WithTrailingTrivia(memberAccess.GetTrailingTrivia());
            issueSpan = memberAccess.Expression.Span;

            if (replacementNode == null)
            {
                return false;
            }

            return memberAccess.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken);
        }

        private static SyntaxTriviaList GetLeadingTriviaForSimplifiedMemberAccess(this MemberAccessExpressionSyntax memberAccess)
        {
            // We want to include any user-typed trivia that may be present between the 'Expression', 'OperatorToken' and 'Identifier' of the MemberAccessExpression.
            // However, we don't want to include any elastic trivia that may have been introduced by the expander in these locations. This is to avoid triggering
            // aggressive formatting. Otherwise, formatter will see this elastic trivia added by the expander and use that as a cue to introduce unnecessary blank lines
            // etc. around the user's original code.
            return memberAccess.GetLeadingTrivia()
                .AddRange(memberAccess.Expression.GetTrailingTrivia().WithoutElasticTrivia())
                .AddRange(memberAccess.OperatorToken.LeadingTrivia.WithoutElasticTrivia())
                .AddRange(memberAccess.OperatorToken.TrailingTrivia.WithoutElasticTrivia())
                .AddRange(memberAccess.Name.GetLeadingTrivia().WithoutElasticTrivia());
        }

        private static IEnumerable<SyntaxTrivia> WithoutElasticTrivia(this IEnumerable<SyntaxTrivia> list)
        {
            return list.Where(t => !t.IsElastic());
        }

        private static bool InsideCrefReference(ExpressionSyntax expr)
        {
            var crefAttribute = expr.FirstAncestorOrSelf<XmlCrefAttributeSyntax>();
            return crefAttribute != null;
        }

        private static bool InsideNameOfExpression(ExpressionSyntax expr, SemanticModel semanticModel)
        {
            var nameOfInvocationExpr = expr.FirstAncestorOrSelf<InvocationExpressionSyntax>(
                invocationExpr =>
                {
                    var expression = invocationExpr.Expression as IdentifierNameSyntax;
                    return (expression != null) && (expression.Identifier.Text == "nameof") &&
                        semanticModel.GetConstantValue(invocationExpr).HasValue &&
                        (semanticModel.GetTypeInfo(invocationExpr).Type.SpecialType == SpecialType.System_String);
                });

            return nameOfInvocationExpr != null;
        }

        private static bool PreferPredefinedTypeKeywordInDeclarations(NameSyntax name, OptionSet optionSet, SemanticModel semanticModel)
        {
            return (name.Parent != null) && !(name.Parent is MemberAccessExpressionSyntax) &&
                   !InsideCrefReference(name) && !InsideNameOfExpression(name, semanticModel) &&
                   optionSet.GetOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.CSharp);
        }

        private static bool PreferPredefinedTypeKeywordInMemberAccess(ExpressionSyntax memberAccess, OptionSet optionSet, SemanticModel semanticModel)
        {
            return (((memberAccess.Parent != null) && (memberAccess.Parent is MemberAccessExpressionSyntax)) || InsideCrefReference(memberAccess)) &&
                   !InsideNameOfExpression(memberAccess, semanticModel) &&
                   optionSet.GetOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.CSharp);
        }

        public static bool IsAliasReplaceableExpression(this ExpressionSyntax expression)
        {
            if (expression.Kind() == SyntaxKind.IdentifierName ||
                expression.Kind() == SyntaxKind.QualifiedName ||
                expression.Kind() == SyntaxKind.AliasQualifiedName)
            {
                return true;
            }

            if (expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var memberAccess = (MemberAccessExpressionSyntax)expression;
                return memberAccess.Expression != null && memberAccess.Expression.IsAliasReplaceableExpression();
            }

            return false;
        }

        private static bool TryReplaceWithAlias(this ExpressionSyntax node, SemanticModel semanticModel, bool preferAliasToQualifiedName, CancellationToken cancellationToken, out IAliasSymbol aliasReplacement)
        {
            aliasReplacement = null;

            if (!node.IsAliasReplaceableExpression())
            {
                return false;
            }

            var symbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;

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
                if (node is QualifiedNameSyntax)
                {
                    var qualifiedNameNode = (QualifiedNameSyntax)node;
                    if (qualifiedNameNode.Right.Identifier.HasAnnotations(AliasAnnotation.Kind))
                    {
                        aliasAnnotationInfo = qualifiedNameNode.Right.Identifier.GetAnnotations(AliasAnnotation.Kind).Single();
                    }
                }

                if (node is AliasQualifiedNameSyntax)
                {
                    var aliasQualifiedNameNode = (AliasQualifiedNameSyntax)node;
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

            if (node is QualifiedNameSyntax)
            {
                var qualifiedName = (QualifiedNameSyntax)node;
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

            if (node is AliasQualifiedNameSyntax)
            {
                var aliasQualifiedNameSyntax = (AliasQualifiedNameSyntax)node;
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

        // We must verify that the alias actually binds back to the thing it's aliasing.
        // It's possible there's another symbol with the same name as the alias that binds
        // first
        private static bool ValidateAliasForTarget(IAliasSymbol aliasReplacement, SemanticModel semanticModel, ExpressionSyntax node, ISymbol symbol)
        {
            var aliasName = aliasReplacement.Name;

            var boundSymbols = semanticModel.LookupNamespacesAndTypes(node.SpanStart, name: aliasName);

            if (boundSymbols.Length == 1)
            {
                var boundAlias = boundSymbols[0] as IAliasSymbol;
                if (boundAlias != null && aliasReplacement.Target.Equals(symbol))
                {
                    return true;
                }
            }

            return false;
        }

        public static IAliasSymbol GetAliasForSymbol(INamespaceOrTypeSymbol symbol, SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var originalSemanticModel = semanticModel.GetOriginalSemanticModel();
            if (!originalSemanticModel.SyntaxTree.HasCompilationUnitRoot)
            {
                return null;
            }

            IAliasSymbol aliasSymbol;
            var namespaceId = GetNamespaceIdForAliasSearch(semanticModel, token, cancellationToken);
            if (namespaceId < 0)
            {
                return null;
            }

            if (!AliasSymbolCache.TryGetAliasSymbol(originalSemanticModel, namespaceId, symbol, out aliasSymbol))
            {
                // add cache
                AliasSymbolCache.AddAliasSymbols(originalSemanticModel, namespaceId, semanticModel.LookupNamespacesAndTypes(token.SpanStart).OfType<IAliasSymbol>());

                // retry
                AliasSymbolCache.TryGetAliasSymbol(originalSemanticModel, namespaceId, symbol, out aliasSymbol);
            }

            return aliasSymbol;
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

        private static int GetNamespaceId(SyntaxNode container, NamespaceDeclarationSyntax target, ref int index)
        {
            var compilation = container as CompilationUnitSyntax;
            if (compilation != null)
            {
                return GetNamespaceId(compilation.Members, target, ref index);
            }

            var @namespace = container as NamespaceDeclarationSyntax;
            if (@namespace != null)
            {
                return GetNamespaceId(@namespace.Members, target, ref index);
            }

            return Contract.FailWithReturn<int>("shouldn't reach here");
        }

        private static int GetNamespaceId(SyntaxList<MemberDeclarationSyntax> members, NamespaceDeclarationSyntax target, ref int index)
        {
            foreach (var member in members)
            {
                var childNamespace = member as NamespaceDeclarationSyntax;
                if (childNamespace == null)
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

        private static bool TryReduce(
            this NameSyntax name,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default(TextSpan);

            if (name.IsVar)
            {
                return false;
            }

            // we should not simplify a name of a namespace declaration
            if (IsPartOfNamespaceDeclarationName(name))
            {
                return false;
            }

            // We can simplify Qualified names and AliasQualifiedNames. Generally, if we have 
            // something like "A.B.C.D", we only consider the full thing something we can simplify.
            // However, in the case of "A.B.C<>.D", then we'll only consider simplifying up to the 
            // first open name.  This is because if we remove the open name, we'll often change 
            // meaning as "D" will bind to C<T>.D which is different than C<>.D!
            if (name is QualifiedNameSyntax)
            {
                var left = ((QualifiedNameSyntax)name).Left;
                if (ContainsOpenName(left))
                {
                    // Don't simplify A.B<>.C
                    return false;
                }
            }

            // 1. see whether binding the name binds to a symbol/type. if not, it is ambiguous and
            //    nothing we can do here.
            var symbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, name);
            if (symbol == null)
            {
                return false;
            }

            // treat constructor names as types
            var method = symbol as IMethodSymbol;
            if (method.IsConstructor())
            {
                symbol = method.ContainingType;
            }

            if (symbol.Kind == SymbolKind.Method && name.Kind() == SyntaxKind.GenericName)
            {
                // The option wants the generic method invocation name to be explicit, then quit the reduction
                if (!optionSet.GetOption(SimplificationOptions.PreferImplicitTypeInference))
                {
                    return false;
                }

                var genericName = (GenericNameSyntax)name;
                replacementNode = SyntaxFactory.IdentifierName(genericName.Identifier)
                    .WithLeadingTrivia(genericName.GetLeadingTrivia())
                    .WithTrailingTrivia(genericName.GetTrailingTrivia());

                issueSpan = genericName.TypeArgumentList.Span;
                return name.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken);
            }

            if (!(symbol is INamespaceOrTypeSymbol))
            {
                return false;
            }

            if (name.HasAnnotations(SpecialTypeAnnotation.Kind))
            {
                replacementNode = SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(
                        name.GetLeadingTrivia(),
                        GetPredefinedKeywordKind(SpecialTypeAnnotation.GetSpecialType(name.GetAnnotations(SpecialTypeAnnotation.Kind).First())),
                        name.GetTrailingTrivia()));

                issueSpan = name.Span;

                return name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel, cancellationToken);
            }
            else
            {
                if (!name.IsRightSideOfDotOrColonColon())
                {
                    IAliasSymbol aliasReplacement;
                    if (name.TryReplaceWithAlias(semanticModel, optionSet.GetOption(SimplificationOptions.PreferAliasToQualification), cancellationToken, out aliasReplacement))
                    {
                        // get the token text as it appears in source code to preserve e.g. unicode character escaping
                        var text = aliasReplacement.Name;
                        var syntaxRef = aliasReplacement.DeclaringSyntaxReferences.FirstOrDefault();

                        if (syntaxRef != null)
                        {
                            var declIdentifier = ((UsingDirectiveSyntax)syntaxRef.GetSyntax(cancellationToken)).Alias.Name.Identifier;
                            text = declIdentifier.IsVerbatimIdentifier() ? declIdentifier.ToString().Substring(1) : declIdentifier.ToString();
                        }

                        var identifierToken = SyntaxFactory.Identifier(
                                name.GetLeadingTrivia(),
                                SyntaxKind.IdentifierToken,
                                text,
                                aliasReplacement.Name,
                                name.GetTrailingTrivia());

                        identifierToken = CSharpSimplificationService.TryEscapeIdentifierToken(identifierToken, name, semanticModel);
                        replacementNode = SyntaxFactory.IdentifierName(identifierToken);

                        // Merge annotation to new syntax node
                        var annotatedNodesOrTokens = name.GetAnnotatedNodesAndTokens(RenameAnnotation.Kind);
                        foreach (var annotatedNodeOrToken in annotatedNodesOrTokens)
                        {
                            if (annotatedNodeOrToken.IsToken)
                            {
                                identifierToken = annotatedNodeOrToken.AsToken().CopyAnnotationsTo(identifierToken);
                            }
                            else
                            {
                                replacementNode = annotatedNodeOrToken.AsNode().CopyAnnotationsTo(replacementNode);
                            }
                        }

                        annotatedNodesOrTokens = name.GetAnnotatedNodesAndTokens(AliasAnnotation.Kind);
                        foreach (var annotatedNodeOrToken in annotatedNodesOrTokens)
                        {
                            if (annotatedNodeOrToken.IsToken)
                            {
                                identifierToken = annotatedNodeOrToken.AsToken().CopyAnnotationsTo(identifierToken);
                            }
                            else
                            {
                                replacementNode = annotatedNodeOrToken.AsNode().CopyAnnotationsTo(replacementNode);
                            }
                        }

                        replacementNode = ((SimpleNameSyntax)replacementNode).WithIdentifier(identifierToken);
                        issueSpan = name.Span;

                        // In case the alias name is the same as the last name of the alias target, we only include 
                        // the left part of the name in the unnecessary span to Not confuse uses.
                        if (name.Kind() == SyntaxKind.QualifiedName)
                        {
                            QualifiedNameSyntax qualifiedName = (QualifiedNameSyntax)name;

                            if (qualifiedName.Right.Identifier.ValueText == identifierToken.ValueText)
                            {
                                issueSpan = qualifiedName.Left.Span;
                            }
                        }

                        // first check if this would be a valid reduction
                        if (name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel, cancellationToken))
                        {
                            // in case this alias name ends with "Attribute", we're going to see if we can also 
                            // remove that suffix.
                            TypeSyntax replacementNodeWithoutAttributeSuffix;
                            TextSpan issueSpanWithoutAttributeSuffix;
                            if (TryReduceAttributeSuffix(
                                name,
                                identifierToken,
                                semanticModel,
                                out replacementNodeWithoutAttributeSuffix,
                                out issueSpanWithoutAttributeSuffix,
                                cancellationToken))
                            {
                                if (name.CanReplaceWithReducedName(replacementNodeWithoutAttributeSuffix, semanticModel, cancellationToken))
                                {
                                    replacementNode = replacementNode.CopyAnnotationsTo(replacementNodeWithoutAttributeSuffix);
                                    issueSpan = issueSpanWithoutAttributeSuffix;
                                }
                            }

                            return true;
                        }

                        return false;
                    }

                    var nameHasNoAlias = false;

                    if (name is SimpleNameSyntax)
                    {
                        var simpleName = (SimpleNameSyntax)name;
                        if (!simpleName.Identifier.HasAnnotations(AliasAnnotation.Kind))
                        {
                            nameHasNoAlias = true;
                        }
                    }

                    if (name is QualifiedNameSyntax)
                    {
                        var qualifiedName = (QualifiedNameSyntax)name;
                        if (!qualifiedName.Right.HasAnnotation(Simplifier.SpecialTypeAnnotation))
                        {
                            nameHasNoAlias = true;
                        }
                    }

                    if (name is AliasQualifiedNameSyntax)
                    {
                        var aliasQualifiedName = (AliasQualifiedNameSyntax)name;
                        if (aliasQualifiedName.Name is SimpleNameSyntax &&
                            !aliasQualifiedName.Name.Identifier.HasAnnotations(AliasAnnotation.Kind) &&
                            !aliasQualifiedName.Name.HasAnnotation(Simplifier.SpecialTypeAnnotation))
                        {
                            nameHasNoAlias = true;
                        }
                    }

                    var aliasInfo = semanticModel.GetAliasInfo(name, cancellationToken);
                    if (nameHasNoAlias && aliasInfo == null)
                    {
                        if (IsReplaceableByVar(name, semanticModel, out replacementNode, out issueSpan, optionSet, cancellationToken))
                        {
                            return true;
                        }

                        // Don't simplify to predefined type if name is part of a QualifiedName.
                        // QualifiedNames can't contain PredefinedTypeNames (although MemberAccessExpressions can).
                        // In other words, the left side of a QualifiedName can't be a PredefinedTypeName.
                        if (!name.Parent.IsKind(SyntaxKind.QualifiedName) &&
                            (PreferPredefinedTypeKeywordInDeclarations(name, optionSet, semanticModel) ||
                             PreferPredefinedTypeKeywordInMemberAccess(name, optionSet, semanticModel)))
                        {
                            var type = semanticModel.GetTypeInfo(name, cancellationToken).Type;
                            if (type != null)
                            {
                                var keywordKind = GetPredefinedKeywordKind(type.SpecialType);
                                if (keywordKind != SyntaxKind.None)
                                {
                                    return CanReplaceWithPredefinedTypeKeywordInContext(name, semanticModel, out replacementNode, ref issueSpan, keywordKind, cancellationToken);
                                }
                            }
                            else
                            {
                                var typeSymbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol;
                                if (typeSymbol.IsKind(SymbolKind.NamedType))
                                {
                                    var keywordKind = GetPredefinedKeywordKind(((INamedTypeSymbol)typeSymbol).SpecialType);
                                    if (keywordKind != SyntaxKind.None)
                                    {
                                        return CanReplaceWithPredefinedTypeKeywordInContext(name, semanticModel, out replacementNode, ref issueSpan, keywordKind, cancellationToken);
                                    }
                                }
                            }
                        }
                    }

                    // nullable rewrite: Nullable<int> -> int?
                    // Don't rewrite in the case where Nullable<int> is part of some qualified name like Nullable<int>.Something
                    if (!name.IsVar && (symbol.Kind == SymbolKind.NamedType) && !name.IsLeftSideOfQualifiedName())
                    {
                        var type = (INamedTypeSymbol)symbol;
                        if (aliasInfo == null && CanSimplifyNullable(type, name, semanticModel))
                        {
                            GenericNameSyntax genericName;
                            if (name.Kind() == SyntaxKind.QualifiedName)
                            {
                                genericName = (GenericNameSyntax)((QualifiedNameSyntax)name).Right;
                            }
                            else
                            {
                                genericName = (GenericNameSyntax)name;
                            }

                            var oldType = genericName.TypeArgumentList.Arguments.First();
                            if (oldType.Kind() == SyntaxKind.OmittedTypeArgument)
                            {
                                return false;
                            }

                            replacementNode = SyntaxFactory.NullableType(oldType)
                                .WithLeadingTrivia(name.GetLeadingTrivia())
                                    .WithTrailingTrivia(name.GetTrailingTrivia());
                            issueSpan = name.Span;

                            // we need to simplify the whole qualified name at once, because replacing the identifier on the left in
                            // System.Nullable<int> alone would be illegal.
                            // If this fails we want to continue to try at least to remove the System if possible.
                            if (name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel, cancellationToken))
                            {
                                return true;
                            }
                        }
                    }
                }

                SyntaxToken identifier;
                switch (name.Kind())
                {
                    case SyntaxKind.AliasQualifiedName:
                        var simpleName = ((AliasQualifiedNameSyntax)name).Name
                            .WithLeadingTrivia(name.GetLeadingTrivia());

                        simpleName = simpleName.ReplaceToken(simpleName.Identifier,
                            ((AliasQualifiedNameSyntax)name).Name.Identifier.CopyAnnotationsTo(
                                simpleName.Identifier.WithLeadingTrivia(
                                    ((AliasQualifiedNameSyntax)name).Alias.Identifier.LeadingTrivia)));

                        replacementNode = simpleName;

                        issueSpan = ((AliasQualifiedNameSyntax)name).Alias.Span;

                        break;

                    case SyntaxKind.QualifiedName:
                        replacementNode = ((QualifiedNameSyntax)name).Right.WithLeadingTrivia(name.GetLeadingTrivia());
                        issueSpan = ((QualifiedNameSyntax)name).Left.Span;

                        break;

                    case SyntaxKind.IdentifierName:
                        identifier = ((IdentifierNameSyntax)name).Identifier;

                        // we can try to remove the Attribute suffix if this is the attribute name
                        TryReduceAttributeSuffix(name, identifier, semanticModel, out replacementNode, out issueSpan, cancellationToken);
                        break;
                }
            }

            if (replacementNode == null)
            {
                return false;
            }

            return name.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken);
        }

        private static bool CanSimplifyNullable(INamedTypeSymbol type, NameSyntax name, SemanticModel semanticModel)
        {
            if (!type.IsNullable())
            {
                return false;
            }

            if (type.IsUnboundGenericType)
            {
                // Don't simplify unbound generic type "Nullable<>".
                return false;
            }

            if (InsideNameOfExpression(name, semanticModel))
            {
                // Nullable<T> can't be simplified to T? in nameof expressions.
                return false;
            }

            if (!InsideCrefReference(name))
            {
                // Nullable<T> can always be simplified to T? outside crefs.
                return true;
            }

            // Inside crefs, if the T in this Nullable{T} is being declared right here
            // then this Nullable{T} is not a constructed generic type and we should
            // not offer to simplify this to T?.
            //
            // For example, we should not offer the simplification in the following cases where
            // T does not bind to an existing type / type parameter in the user's code.
            // - <see cref="Nullable{T}"/>
            // - <see cref="System.Nullable{T}.Value"/>
            //
            // And we should offer the simplification in the following cases where SomeType and
            // SomeMethod bind to a type and method declared elsewhere in the users code.
            // - <see cref="SomeType.SomeMethod(Nullable{SomeType})"/>

            var argument = type.TypeArguments.SingleOrDefault();
            if (argument == null || argument.IsErrorType())
            {
                return false;
            }

            var argumentDecl = argument.DeclaringSyntaxReferences.FirstOrDefault();
            if (argumentDecl == null)
            {
                // The type argument is a type from metadata - so this is a constructed generic nullable type that can be simplified (e.g. Nullable(Of Integer)).
                return true;
            }

            return !name.Span.Contains(argumentDecl.Span);
        }

        private static bool CanReplaceWithPredefinedTypeKeywordInContext(NameSyntax name, SemanticModel semanticModel, out TypeSyntax replacementNode, ref TextSpan issueSpan, SyntaxKind keywordKind, CancellationToken cancellationToken)
        {
            replacementNode = CreatePredefinedTypeSyntax(name, keywordKind);

            issueSpan = name.Span; // we want to show the whole name expression as unnecessary

            return name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel, cancellationToken);
        }

        private static TypeSyntax CreatePredefinedTypeSyntax(ExpressionSyntax expression, SyntaxKind keywordKind)
        {
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(expression.GetLeadingTrivia(), keywordKind, expression.GetTrailingTrivia()));
        }

        private static bool TryReduceAttributeSuffix(
            NameSyntax name,
            SyntaxToken identifierToken,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            CancellationToken cancellationToken)
        {
            issueSpan = default(TextSpan);
            replacementNode = default(TypeSyntax);

            // we can try to remove the Attribute suffix if this is the attribute name
            if (SyntaxFacts.IsAttributeName(name))
            {
                if (name.Parent.Kind() == SyntaxKind.Attribute || name.IsRightSideOfDotOrColonColon())
                {
                    const string AttributeName = "Attribute";

                    // an attribute that should keep it (unnecessary "Attribute" suffix should be annotated with a DontSimplifyAnnotation
                    if (identifierToken.ValueText != AttributeName && identifierToken.ValueText.EndsWith(AttributeName, StringComparison.Ordinal) && !identifierToken.HasAnnotation(SimplificationHelpers.DontSimplifyAnnotation))
                    {
                        // weird. the semantic model is able to bind attribute syntax like "[as()]" although it's not valid code.
                        // so we need another check for keywords manually.
                        var newAttributeName = identifierToken.ValueText.Substring(0, identifierToken.ValueText.Length - 9);
                        if (SyntaxFacts.GetKeywordKind(newAttributeName) != SyntaxKind.None)
                        {
                            return false;
                        }

                        // if this attribute name in source contained unicode escaping, we will loose it now
                        // because there is no easy way to determine the substring from identifier->ToString() 
                        // which would be needed to pass to SyntaxFactory.Identifier
                        // The result is an unescaped unicode character in source.

                        // once we remove the Attribute suffix, we can't use an escaped identifier
                        var newIdentifierToken = identifierToken.CopyAnnotationsTo(
                            SyntaxFactory.Identifier(
                                identifierToken.LeadingTrivia,
                                newAttributeName,
                                identifierToken.TrailingTrivia));

                        replacementNode = SyntaxFactory.IdentifierName(newIdentifierToken)
                            .WithLeadingTrivia(name.GetLeadingTrivia());
                        issueSpan = new TextSpan(identifierToken.Span.End - 9, 9);

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the SyntaxNode is a name of a namespace declaration. To be a namespace name, the syntax
        /// must be parented by an namespace declaration and the node itself must be equal to the declaration's Name
        /// property.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsPartOfNamespaceDeclarationName(SyntaxNode node)
        {
            var parent = node;

            while (parent != null)
            {
                switch (parent.Kind())
                {
                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.QualifiedName:
                        node = parent;
                        parent = parent.Parent;
                        break;

                    case SyntaxKind.NamespaceDeclaration:
                        var namespaceDeclaration = (NamespaceDeclarationSyntax)parent;
                        return object.Equals(namespaceDeclaration.Name, node);

                    default:
                        return false;
                }
            }

            return false;
        }

        private static bool TrySimplify(
            this ExpressionSyntax expression,
            SemanticModel semanticModel,
            OptionSet optionSet,
            out ExpressionSyntax replacementNode,
            out TextSpan issueSpan)
        {
            replacementNode = null;
            issueSpan = default(TextSpan);

            switch (expression.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                    {
                        var memberAccess = (MemberAccessExpressionSyntax)expression;
                        ExpressionSyntax newLeft;

                        if (IsMemberAccessADynamicInvocation(memberAccess, semanticModel))
                        {
                            return false;
                        }

                        if (TrySimplifyMemberAccessOrQualifiedName(memberAccess.Expression, memberAccess.Name, semanticModel, optionSet, out newLeft, out issueSpan))
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
                        ExpressionSyntax newLeft;
                        if (TrySimplifyMemberAccessOrQualifiedName(qualifiedName.Left, qualifiedName.Right, semanticModel, optionSet, out newLeft, out issueSpan))
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

        private static bool ReplacementChangesSemantics(ExpressionSyntax originalExpression, ExpressionSyntax replacedExpression, SemanticModel semanticModel)
        {
            var speculationAnalyzer = new SpeculationAnalyzer(originalExpression, replacedExpression, semanticModel, CancellationToken.None);
            return speculationAnalyzer.ReplacementChangesSemantics();
        }

        // Note: The caller needs to verify that replacement doesn't change semantics of the original expression.
        private static bool TrySimplifyMemberAccessOrQualifiedName(
        ExpressionSyntax left,
            ExpressionSyntax right,
            SemanticModel semanticModel,
            OptionSet optionSet,
            out ExpressionSyntax replacementNode,
            out TextSpan issueSpan)
        {
            replacementNode = null;
            issueSpan = default(TextSpan);

            if (left != null && right != null)
            {
                var leftSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, left);
                if (leftSymbol != null && (leftSymbol.Kind == SymbolKind.NamedType))
                {
                    var rightSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, right);
                    if (rightSymbol != null && (rightSymbol.IsStatic || rightSymbol.Kind == SymbolKind.NamedType))
                    {
                        // Static member access or nested type member access.
                        INamedTypeSymbol containingType = rightSymbol.ContainingType;

                        var enclosingSymbol = semanticModel.GetEnclosingSymbol(left.SpanStart);
                        List<ISymbol> enclosingTypeParametersInsideOut = new List<ISymbol>();

                        while (enclosingSymbol != null)
                        {
                            if (enclosingSymbol is IMethodSymbol)
                            {
                                var methodSymbol = (IMethodSymbol)enclosingSymbol;
                                if (methodSymbol.TypeArguments.Length != 0)
                                {
                                    enclosingTypeParametersInsideOut.AddRange(methodSymbol.TypeArguments);
                                }
                            }

                            if (enclosingSymbol is INamedTypeSymbol)
                            {
                                var namedTypeSymbol = (INamedTypeSymbol)enclosingSymbol;
                                if (namedTypeSymbol.TypeArguments.Length != 0)
                                {
                                    enclosingTypeParametersInsideOut.AddRange(namedTypeSymbol.TypeArguments);
                                }
                            }

                            enclosingSymbol = enclosingSymbol.ContainingSymbol;
                        }

                        if (containingType != null && !containingType.Equals(leftSymbol))
                        {
                            var namedType = leftSymbol as INamedTypeSymbol;
                            if (namedType != null)
                            {
                                if ((namedType.GetBaseTypes().Contains(containingType) &&
                                    !optionSet.GetOption(SimplificationOptions.AllowSimplificationToBaseType)) ||
                                    (!optionSet.GetOption(SimplificationOptions.AllowSimplificationToGenericType) &&
                                    containingType.TypeArguments.Length != 0))
                                {
                                    return false;
                                }
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

        /// <summary>
        /// Returns True if enclosingTypeParametersInsideOut contains a symbol with the same name as the candidateSymbol
        /// thereby saying that there exists a symbol which hides the candidate Symbol
        /// </summary>
        private static bool HidingTypeParameterSymbolExists(ISymbol candidateSymbol, List<ISymbol> enclosingTypeParametersInsideOut)
        {
            foreach (var enclosingTypeParameter in enclosingTypeParametersInsideOut)
            {
                ISymbol newCandidateSymbol = candidateSymbol;
                if (candidateSymbol.IsKind(SymbolKind.ArrayType))
                {
                    newCandidateSymbol = ((IArrayTypeSymbol)candidateSymbol).ElementType;
                }

                if (newCandidateSymbol.MetadataName == enclosingTypeParameter.MetadataName)
                {
                    if (SymbolEquivalenceComparer.Instance.Equals(newCandidateSymbol.GetOriginalUnreducedDefinition(), enclosingTypeParameter.GetOriginalUnreducedDefinition()))
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        private static bool CanReplaceWithReducedName(
            this MemberAccessExpressionSyntax memberAccess,
            ExpressionSyntax reducedName,
            SemanticModel semanticModel,
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

            if (WillConflictWithExistingLocal(memberAccess, reducedName))
            {
                return false;
            }

            if (IsMemberAccessADynamicInvocation(memberAccess, semanticModel))
            {
                return false;
            }

            if (memberAccess.AccessMethodWithDynamicArgumentInsideStructConstructor(semanticModel))
            {
                return false;
            }

            if (memberAccess.Expression.Kind() == SyntaxKind.BaseExpression)
            {
                var enclosingNamedType = semanticModel.GetEnclosingNamedType(memberAccess.SpanStart, cancellationToken);
                var symbol = semanticModel.GetSymbolInfo(memberAccess.Name, cancellationToken).Symbol;
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
            if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var memberAccess = (MemberAccessExpressionSyntax)node;

                return memberAccess.Expression.IsKind(SyntaxKind.IdentifierName) ||
                    IsNameOrMemberAccessButNoExpression(memberAccess.Expression);
            }

            return node.IsKind(SyntaxKind.IdentifierName);
        }

        /// <summary>
        /// Tells if the Member access is the starting part of a Dynamic Invocation
        /// </summary>
        /// <param name="memberAccess"></param>
        /// <param name="semanticModel"></param>
        /// <returns>Return true, if the member access is the starting point of a Dynamic Invocation</returns>
        private static bool IsMemberAccessADynamicInvocation(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
        {
            var ancestorInvocation = memberAccess.FirstAncestorOrSelf<InvocationExpressionSyntax>();

            if (ancestorInvocation != null && ancestorInvocation.SpanStart == memberAccess.SpanStart)
            {
                var typeInfo = semanticModel.GetTypeInfo(ancestorInvocation);
                if (typeInfo.Type != null &&
                    typeInfo.Type.Kind == SymbolKind.DynamicType)
                {
                    return true;
                }
            }

            return false;
        }

        /*
         * Name Reduction, to implicitly mean "this", is possible only after the initialization of all member variables but
         * since the check for initialization of all member variable is a lot of work for this simplification we don't simplify
         * even if all the member variables are initialized
         */
        private static bool AccessMethodWithDynamicArgumentInsideStructConstructor(this MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
        {
            var constructor = memberAccess.Ancestors().OfType<ConstructorDeclarationSyntax>().SingleOrDefault();

            if (constructor == null || constructor.Parent.Kind() != SyntaxKind.StructDeclaration)
            {
                return false;
            }

            return semanticModel.GetSymbolInfo(memberAccess.Name).CandidateReason == CandidateReason.LateBound;
        }

        private static bool CanReplaceWithReducedName(this NameSyntax name, TypeSyntax reducedName, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var speculationAnalyzer = new SpeculationAnalyzer(name, reducedName, semanticModel, cancellationToken);
            if (speculationAnalyzer.ReplacementChangesSemantics())
            {
                return false;
            }

            return CanReplaceWithReducedNameInContext(name, reducedName, semanticModel, cancellationToken);
        }

        private static bool CanReplaceWithReducedNameInContext(this NameSyntax name, TypeSyntax reducedName, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Special case.  if this new minimal name parses out to a predefined type, then we
            // have to make sure that we're not in a using alias. That's the one place where the
            // language doesn't allow predefined types. You have to use the fully qualified name
            // instead.
            var invalidTransformation1 = IsNonNameSyntaxInUsingDirective(name, reducedName);
            var invalidTransformation2 = WillConflictWithExistingLocal(name, reducedName);
            var invalidTransformation3 = IsAmbiguousCast(name, reducedName);
            var invalidTransformation4 = IsNullableTypeInPointerExpression(name, reducedName);
            var isNotNullableReplaceable = name.IsNotNullableReplaceable(reducedName);

            if (invalidTransformation1 || invalidTransformation2 || invalidTransformation3 || invalidTransformation4
                || isNotNullableReplaceable)
            {
                return false;
            }

            return true;
        }

        private static bool IsNotNullableReplaceable(this NameSyntax name, TypeSyntax reducedName)
        {
            var isNotNullableReplaceable = false;
            var isLeftSideOfDot = name.IsLeftSideOfDot();
            var isRightSideOfDot = name.IsRightSideOfDot();

            if (reducedName.Kind() == SyntaxKind.NullableType)
            {
                if (((NullableTypeSyntax)reducedName).ElementType.Kind() == SyntaxKind.OmittedTypeArgument)
                {
                    isNotNullableReplaceable = true;
                }
                else
                {
                    isNotNullableReplaceable = name.IsLeftSideOfDot() || name.IsRightSideOfDot();
                }
            }

            return isNotNullableReplaceable;
        }

        private static bool IsThisOrTypeOrNamespace(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
        {
            if (memberAccess.Expression.Kind() == SyntaxKind.ThisExpression)
            {
                var previousToken = memberAccess.Expression.GetFirstToken().GetPreviousToken();

                var symbol = semanticModel.GetSymbolInfo(memberAccess.Name).Symbol;

                if (previousToken.Kind() == SyntaxKind.OpenParenToken &&
                    previousToken.Parent.IsKind(SyntaxKind.ParenthesizedExpression) &&
                    !previousToken.Parent.IsParentKind(SyntaxKind.ParenthesizedExpression) &&
                    ((ParenthesizedExpressionSyntax)previousToken.Parent).Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression &&
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

        private static bool IsReplaceableByVar(
            this TypeSyntax simpleName,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default(TextSpan);

            if (!optionSet.GetOption(SimplificationOptions.PreferImplicitTypeInLocalDeclaration))
            {
                return false;
            }

            // If it is already var
            if (simpleName.IsVar)
            {
                return false;
            }

            var candidateReplacementNode = SyntaxFactory.IdentifierName("var")
                                            .WithLeadingTrivia(simpleName.GetLeadingTrivia())
                                            .WithTrailingTrivia(simpleName.GetTrailingTrivia());
            var candidateIssueSpan = simpleName.Span;

            // If there exists a Type called var , fail.
            var checkSymbol = semanticModel.GetSpeculativeSymbolInfo(simpleName.SpanStart, candidateReplacementNode, SpeculativeBindingOption.BindAsTypeOrNamespace).Symbol;
            if (checkSymbol != null && checkSymbol.IsKind(SymbolKind.NamedType) && ((INamedTypeSymbol)checkSymbol).TypeKind == TypeKind.Class && checkSymbol.Name == "var")
            {
                return false;
            }

            // If the simpleName is the type of the Variable Declaration Syntax belonging to LocalDeclaration, For Statement or Using statement
            if (simpleName.IsParentKind(SyntaxKind.VariableDeclaration) &&
                ((VariableDeclarationSyntax)simpleName.Parent).Type == simpleName &&
                simpleName.Parent.Parent.IsKind(SyntaxKind.LocalDeclarationStatement, SyntaxKind.ForStatement, SyntaxKind.UsingStatement))
            {
                if (simpleName.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement) &&
                    ((LocalDeclarationStatementSyntax)simpleName.Parent.Parent).Modifiers.Any(n => n.Kind() == SyntaxKind.ConstKeyword))
                {
                    return false;
                }

                var variableDeclaration = (VariableDeclarationSyntax)simpleName.Parent;

                // Check the Initialized Value to see if it is allowed to be in the Var initialization
                if (variableDeclaration.Variables.Count != 1 ||
                    !variableDeclaration.Variables.Single().Initializer.IsKind(SyntaxKind.EqualsValueClause))
                {
                    return false;
                }

                var variable = variableDeclaration.Variables.Single();
                var initializer = variable.Initializer;
                var identifier = variable.Identifier;

                if (EqualsValueClauseNotSuitableForVar(identifier, simpleName, initializer, semanticModel, cancellationToken))
                {
                    return false;
                }

                replacementNode = candidateReplacementNode;
                issueSpan = candidateIssueSpan;
                return true;
            }

            if (simpleName.IsParentKind(SyntaxKind.ForEachStatement) &&
                ((ForEachStatementSyntax)simpleName.Parent).Type == simpleName)
            {
                replacementNode = candidateReplacementNode;
                issueSpan = candidateIssueSpan;
                return true;
            }

            return false;
        }

        private static bool EqualsValueClauseNotSuitableForVar(
            SyntaxToken identifier,
            TypeSyntax simpleName,
            EqualsClauseSyntax equalsClause,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // var cannot be assigned null
            if (equalsClause.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return true;
            }

            var type = semanticModel.GetTypeInfo(simpleName, cancellationToken).Type;

            // the variable cannot be initialized to a method group or an anonymous function
            if (type != null &&
                type.TypeKind == TypeKind.Delegate)
            {
                return true;
            }

            var initializerType = semanticModel.GetTypeInfo(equalsClause.Value, cancellationToken).Type;

            if (!type.Equals(initializerType))
            {
                return true;
            }

            // The assign expression in the initializer cannot be the same symbol as the i
            var possibleSameLocals = equalsClause.DescendantNodesAndSelf().Where(n => n.Kind() == SyntaxKind.IdentifierName && ((IdentifierNameSyntax)n).Identifier.ValueText.Equals(identifier.ValueText));
            var anyUse = possibleSameLocals.Any(n =>
            {
                var symbol = semanticModel.GetSymbolInfo(n, cancellationToken).Symbol;
                if (symbol != null && symbol.Kind == SymbolKind.Local)
                {
                    return true;
                }

                return false;
            });

            if (anyUse)
            {
                return true;
            }

            return false;
        }

        private static bool ContainsOpenName(NameSyntax name)
        {
            if (name is QualifiedNameSyntax)
            {
                var qualifiedName = (QualifiedNameSyntax)name;
                return ContainsOpenName(qualifiedName.Left) || ContainsOpenName(qualifiedName.Right);
            }
            else if (name is GenericNameSyntax)
            {
                return ((GenericNameSyntax)name).IsUnboundGenericName;
            }
            else
            {
                return false;
            }
        }

        private static bool IsNullableTypeInPointerExpression(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            // Note: nullable type syntax is not allowed in pointer type syntax
            if (simplifiedNode.Kind() == SyntaxKind.NullableType &&
                simplifiedNode.DescendantNodes().Any(n => n is PointerTypeSyntax))
            {
                return true;
            }

            return false;
        }

        private static bool IsNonNameSyntaxInUsingDirective(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            return
                expression.IsParentKind(SyntaxKind.UsingDirective) &&
                !(simplifiedNode is NameSyntax);
        }

        private static bool WillConflictWithExistingLocal(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            if (simplifiedNode.Kind() == SyntaxKind.IdentifierName && !SyntaxFacts.IsInNamespaceOrTypeContext(expression))
            {
                var identifierName = (IdentifierNameSyntax)simplifiedNode;
                var enclosingDeclarationSpace = FindImmediatelyEnclosingLocalVariableDeclarationSpace(expression);
                var enclosingMemberDeclaration = expression.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                if (enclosingDeclarationSpace != null && enclosingMemberDeclaration != null)
                {
                    var locals = enclosingMemberDeclaration.GetLocalDeclarationMap()[identifierName.Identifier.ValueText];
                    foreach (var token in locals)
                    {
                        if (token.GetAncestors<SyntaxNode>().Contains(enclosingDeclarationSpace))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsAmbiguousCast(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            // Can't simplify a type name in a cast expression if it would then cause the cast to be
            // parsed differently.  For example:  (Foo::Bar)+1  is a cast.  But if that simplifies to
            // (Bar)+1  then that's an arithmetic expression.
            if (expression.IsParentKind(SyntaxKind.CastExpression))
            {
                var castExpression = (CastExpressionSyntax)expression.Parent;
                if (castExpression.Type == expression)
                {
                    var newCastExpression = castExpression.ReplaceNode(castExpression.Type, simplifiedNode);
                    var reparsedCastExpression = SyntaxFactory.ParseExpression(newCastExpression.ToString());

                    if (!reparsedCastExpression.IsKind(SyntaxKind.CastExpression))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static SyntaxNode FindImmediatelyEnclosingLocalVariableDeclarationSpace(SyntaxNode syntax)
        {
            for (var declSpace = syntax; declSpace != null; declSpace = declSpace.Parent)
            {
                switch (declSpace.Kind())
                {
                    // These are declaration-space-defining syntaxes, by the spec:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.Block:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.AnonymousMethodExpression:
                    case SyntaxKind.SwitchStatement:
                    case SyntaxKind.ForEachKeyword:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.UsingStatement:

                    // SPEC VIOLATION: We also want to stop walking out if, say, we are in a field
                    // initializer. Technically according to the wording of the spec it should be
                    // legal to use a simple name inconsistently inside a field initializer because
                    // it does not define a local variable declaration space. In practice of course
                    // we want to check for that. (As the native compiler does as well.)

                    case SyntaxKind.FieldDeclaration:
                        return declSpace;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the predefined keyword kind for a given specialtype.
        /// </summary>
        /// <param name="specialType">The specialtype of this type.</param>
        /// <returns>The keyword kind for a given special type, or SyntaxKind.None if the type name is not a predefined type.</returns>
        public static SyntaxKind GetPredefinedKeywordKind(SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Boolean:
                    return SyntaxKind.BoolKeyword;
                case SpecialType.System_Byte:
                    return SyntaxKind.ByteKeyword;
                case SpecialType.System_SByte:
                    return SyntaxKind.SByteKeyword;
                case SpecialType.System_Int32:
                    return SyntaxKind.IntKeyword;
                case SpecialType.System_UInt32:
                    return SyntaxKind.UIntKeyword;
                case SpecialType.System_Int16:
                    return SyntaxKind.ShortKeyword;
                case SpecialType.System_UInt16:
                    return SyntaxKind.UShortKeyword;
                case SpecialType.System_Int64:
                    return SyntaxKind.LongKeyword;
                case SpecialType.System_UInt64:
                    return SyntaxKind.ULongKeyword;
                case SpecialType.System_Single:
                    return SyntaxKind.FloatKeyword;
                case SpecialType.System_Double:
                    return SyntaxKind.DoubleKeyword;
                case SpecialType.System_Decimal:
                    return SyntaxKind.DecimalKeyword;
                case SpecialType.System_String:
                    return SyntaxKind.StringKeyword;
                case SpecialType.System_Char:
                    return SyntaxKind.CharKeyword;
                case SpecialType.System_Object:
                    return SyntaxKind.ObjectKeyword;
                case SpecialType.System_Void:
                    return SyntaxKind.VoidKeyword;
                default:
                    return SyntaxKind.None;
            }
        }

        public static SimpleNameSyntax GetRightmostName(this ExpressionSyntax node)
        {
            var memberAccess = node as MemberAccessExpressionSyntax;
            if (memberAccess != null && memberAccess.Name != null)
            {
                return memberAccess.Name;
            }

            var qualified = node as QualifiedNameSyntax;
            if (qualified != null && qualified.Right != null)
            {
                return qualified.Right;
            }

            var simple = node as SimpleNameSyntax;
            if (simple != null)
            {
                return simple;
            }

            var conditional = node as ConditionalAccessExpressionSyntax;
            if (conditional != null)
            {
                return conditional.WhenNotNull.GetRightmostName();
            }

            return null;
        }

        public static OperatorPrecedence GetOperatorPrecedence(this ExpressionSyntax expression)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.ConditionalAccessExpression:
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.ElementAccessExpression:
                case SyntaxKind.PostIncrementExpression:
                case SyntaxKind.PostDecrementExpression:
                case SyntaxKind.ObjectCreationExpression:
                case SyntaxKind.TypeOfExpression:
                case SyntaxKind.DefaultExpression:
                case SyntaxKind.CheckedExpression:
                case SyntaxKind.UncheckedExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    // From C# spec, 7.3.1:
                    // Primary: x.y  x?.y  x?[y]  f(x)  a[x]  x++  x--  new  typeof  default  checked  unchecked  delegate

                    return OperatorPrecedence.Primary;

                case SyntaxKind.UnaryPlusExpression:
                case SyntaxKind.UnaryMinusExpression:
                case SyntaxKind.LogicalNotExpression:
                case SyntaxKind.BitwiseNotExpression:
                case SyntaxKind.PreIncrementExpression:
                case SyntaxKind.PreDecrementExpression:
                case SyntaxKind.CastExpression:
                case SyntaxKind.AwaitExpression:
                    // From C# spec, 7.3.1:
                    // Unary: +  -  !  ~  ++x  --x  (T)x  await Task

                    return OperatorPrecedence.Unary;

                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.DivideExpression:
                case SyntaxKind.ModuloExpression:
                    // From C# spec, 7.3.1:
                    // Multiplicative: *  /  %

                    return OperatorPrecedence.Multiplicative;

                case SyntaxKind.AddExpression:
                case SyntaxKind.SubtractExpression:
                    // From C# spec, 7.3.1:
                    // Additive: +  -

                    return OperatorPrecedence.Additive;

                case SyntaxKind.LeftShiftExpression:
                case SyntaxKind.RightShiftExpression:
                    // From C# spec, 7.3.1:
                    // Shift: <<  >>

                    return OperatorPrecedence.Shift;

                case SyntaxKind.LessThanExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                case SyntaxKind.IsExpression:
                case SyntaxKind.AsExpression:
                    // From C# spec, 7.3.1:
                    // Relational and type testing: <  >  <=  >=  is  as

                    return OperatorPrecedence.RelationalAndTypeTesting;

                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                    // From C# spec, 7.3.1:
                    // Equality: ==  !=

                    return OperatorPrecedence.Equality;

                case SyntaxKind.BitwiseAndExpression:
                    // From C# spec, 7.3.1:
                    // Logical AND: &

                    return OperatorPrecedence.LogicalAnd;

                case SyntaxKind.ExclusiveOrExpression:
                    // From C# spec, 7.3.1:
                    // Logical XOR: ^

                    return OperatorPrecedence.LogicalXor;

                case SyntaxKind.BitwiseOrExpression:
                    // From C# spec, 7.3.1:
                    // Logical OR: |

                    return OperatorPrecedence.LogicalOr;

                case SyntaxKind.LogicalAndExpression:
                    // From C# spec, 7.3.1:
                    // Conditional AND: &&

                    return OperatorPrecedence.ConditionalAnd;

                case SyntaxKind.LogicalOrExpression:
                    // From C# spec, 7.3.1:
                    // Conditional AND: ||

                    return OperatorPrecedence.ConditionalOr;

                case SyntaxKind.CoalesceExpression:
                    // From C# spec, 7.3.1:
                    // Null coalescing: ??

                    return OperatorPrecedence.NullCoalescing;

                case SyntaxKind.ConditionalExpression:
                    // From C# spec, 7.3.1:
                    // Conditional: ?:

                    return OperatorPrecedence.Conditional;

                case SyntaxKind.SimpleAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                    // From C# spec, 7.3.1:
                    // Conditional: ?:

                    return OperatorPrecedence.AssignmentAndLambdaExpression;

                default:
                    return OperatorPrecedence.None;
            }
        }
    }
}
