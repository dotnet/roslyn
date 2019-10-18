// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
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

        public static ExpressionSyntax Parenthesize(
            this ExpressionSyntax expression, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true)
        {
            // a 'ref' expression should never be parenthesized.  It fundamentally breaks the code.
            // This is because, from the language's perspective there is no such thing as a ref
            // expression.  instead, there are constructs like ```return ref expr``` or 
            // ```x ? ref expr1 : ref expr2```, or ```ref int a = ref expr``` in these cases, the 
            // ref's do not belong to the exprs, but instead belong to the parent construct. i.e.
            // ```return ref``` or ``` ? ref  ... : ref ... ``` or ``` ... = ref ...```.  For 
            // parsing convenience, and to prevent having to update all these constructs, we settled
            // on a ref-expression node.  But this node isn't a true expression that be operated
            // on like with everything else.
            if (expression.IsKind(SyntaxKind.RefExpression))
            {
                return expression;
            }

            var result = ParenthesizeWorker(expression, includeElasticTrivia);
            return addSimplifierAnnotation
                ? result.WithAdditionalAnnotations(Simplifier.Annotation)
                : result;
        }

        private static ExpressionSyntax ParenthesizeWorker(
            this ExpressionSyntax expression, bool includeElasticTrivia)
        {
            var withoutTrivia = expression.WithoutTrivia();
            var parenthesized = includeElasticTrivia
                ? SyntaxFactory.ParenthesizedExpression(withoutTrivia)
                : SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.OpenParenToken, SyntaxTriviaList.Empty),
                    withoutTrivia,
                    SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.CloseParenToken, SyntaxTriviaList.Empty));

            return parenthesized.WithTriviaFrom(expression);
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
            if (expression == null)
            {
                return false;
            }

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

        public static bool IsLeftSideOfExplicitInterfaceSpecifier(this NameSyntax name)
            => name.IsParentKind(SyntaxKind.ExplicitInterfaceSpecifier);

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
            else if (expression is SimpleNameSyntax simpleName)
            {
                return AddSimpleName(simpleName, parts);
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

            if (expression.IsParentKind(SyntaxKind.ConstantPattern))
            {
                return true;
            }

            // note: the above list is not intended to be exhaustive.  If more cases
            // are discovered that should be considered 'constant' contexts in the 
            // language, then this should be updated accordingly.
            return false;
        }

        public static bool IsInOutContext(this ExpressionSyntax expression)
        {
            return
                expression?.Parent is ArgumentSyntax argument &&
                argument.Expression == expression &&
                argument.RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword;
        }

        public static bool IsInRefContext(this ExpressionSyntax expression)
            => expression.IsParentKind(SyntaxKind.RefExpression) ||
               (expression?.Parent as ArgumentSyntax)?.RefOrOutKeyword.Kind() == SyntaxKind.RefKeyword;

        public static bool IsInInContext(this ExpressionSyntax expression)
            => (expression?.Parent as ArgumentSyntax)?.RefKindKeyword.Kind() == SyntaxKind.InKeyword;

        private static ExpressionSyntax GetExpressionToAnalyzeForWrites(ExpressionSyntax expression)
        {
            if (expression.IsRightSideOfDotOrArrow())
            {
                expression = expression.Parent as ExpressionSyntax;
            }

            expression = expression.WalkUpParentheses();

            return expression;
        }

        public static bool IsOnlyWrittenTo(this ExpressionSyntax expression)
        {
            expression = GetExpressionToAnalyzeForWrites(expression);

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

                    if (expression.IsAttributeNamedArgumentIdentifier())
                    {
                        return true;
                    }
                }

                if (IsExpressionOfArgumentInDeconstruction(expression))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// If this declaration or identifier is part of a deconstruction, find the deconstruction.
        /// If found, returns either an assignment expression or a foreach variable statement.
        /// Returns null otherwise.
        /// 
        /// copied from SyntaxExtensions.GetContainingDeconstruction
        /// </summary>
        private static bool IsExpressionOfArgumentInDeconstruction(ExpressionSyntax expr)
        {
            if (!expr.IsParentKind(SyntaxKind.Argument))
            {
                return false;
            }

            while (true)
            {
                var parent = expr.Parent;
                if (parent == null)
                {
                    return false;
                }

                switch (parent.Kind())
                {
                    case SyntaxKind.Argument:
                        if (parent.Parent?.Kind() == SyntaxKind.TupleExpression)
                        {
                            expr = (TupleExpressionSyntax)parent.Parent;
                            continue;
                        }

                        return false;
                    case SyntaxKind.SimpleAssignmentExpression:
                        if (((AssignmentExpressionSyntax)parent).Left == expr)
                        {
                            return true;
                        }

                        return false;
                    case SyntaxKind.ForEachVariableStatement:
                        if (((ForEachVariableStatementSyntax)parent).Variable == expr)
                        {
                            return true;
                        }

                        return false;

                    default:
                        return false;
                }
            }
        }

        public static bool IsWrittenTo(this ExpressionSyntax expression)
        {
            expression = GetExpressionToAnalyzeForWrites(expression);

            if (expression.IsOnlyWrittenTo())
            {
                return true;
            }

            if (expression.IsInRefContext())
            {
                return true;
            }

            // We're written if we're used in a ++, or -- expression.
            if (expression.IsOperandOfIncrementOrDecrementExpression())
            {
                return true;
            }

            if (expression.IsLeftSideOfAnyAssignExpression())
            {
                return true;
            }

            return false;
        }

        public static bool IsAttributeNamedArgumentIdentifier(this ExpressionSyntax expression)
        {
            var nameEquals = expression?.Parent as NameEqualsSyntax;
            return nameEquals.IsParentKind(SyntaxKind.AttributeArgument);
        }

        public static bool IsOperandOfIncrementOrDecrementExpression(this ExpressionSyntax expression)
        {
            if (expression != null)
            {
                switch (expression.Parent.Kind())
                {
                    case SyntaxKind.PostIncrementExpression:
                    case SyntaxKind.PreIncrementExpression:
                    case SyntaxKind.PostDecrementExpression:
                    case SyntaxKind.PreDecrementExpression:
                        return true;
                }
            }

            return false;
        }

        public static bool IsNamedArgumentIdentifier(this ExpressionSyntax expression)
        {
            return expression is IdentifierNameSyntax && expression.Parent is NameColonSyntax;
        }

        public static bool IsInsideNameOfExpression(
            this ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var invocation = expression?.GetAncestor<InvocationExpressionSyntax>();
            if (invocation?.Expression is IdentifierNameSyntax name &&
                name.Identifier.Text == SyntaxFacts.GetText(SyntaxKind.NameOfKeyword))
            {
                return semanticModel.GetMemberGroup(name, cancellationToken).IsDefaultOrEmpty;
            }

            return false;
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
            // i.e. you can't replace "a" in "a = b" with "Goo() = b".
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

            if (expression is TupleExpressionSyntax)
            {
                return true;
            }

            if (!(expression is ObjectCreationExpressionSyntax) &&
                !(expression is AnonymousObjectCreationExpressionSyntax) &&
                !expression.IsLeftSideOfAssignExpression())
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
                    // Technically, you could introduce an LValue for "Goo" in "Goo()" even if "Goo" binds
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
                // Case (1) : The WhenNotNull clause always starts with a MemberBindingExpression.
                //              expression '.Method()' in a?.Method()
                // Case (2) : The Expression clause always starts with a MemberBindingExpression if 
                // the grandparent is a conditional access expression.
                //              expression '.Method' in a?.Method()?.Length
                // Case (3) : The child Conditional access expression always starts with a MemberBindingExpression if
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
                case SyntaxKind.RefExpression:
                case SyntaxKind.LockStatement:
                case SyntaxKind.ElementAccessExpression:
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
            if (expression.CheckParent<CommonForEachStatementSyntax>(f => f.Expression == expression) ||
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
            if (expression.TryReduceExplicitName(semanticModel, out var replacementTypeNode, out issueSpan, optionSet, cancellationToken))
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
            issueSpan = default;

            if (expression.ContainsInterleavedDirective(cancellationToken))
            {
                return false;
            }

            if (expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var memberAccess = (MemberAccessExpressionSyntax)expression;
                return memberAccess.TryReduce(semanticModel, out replacementNode, out issueSpan, optionSet, cancellationToken);
            }

            if (expression is TypeSyntax typeName)
            {
                // First, see if we can replace this type with var if that's what the user prefers.
                // That always overrides all other simplification.
                if (typeName.IsReplaceableByVar(semanticModel, out replacementNode, out issueSpan, optionSet, cancellationToken))
                {
                    return true;
                }

                if (expression is NameSyntax name)
                {
                    return name.TryReduce(semanticModel, out replacementNode, out issueSpan, optionSet, cancellationToken);
                }
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
            issueSpan = default;

            if (memberAccess.Name == null || memberAccess.Expression == null)
            {
                return false;
            }

            if (memberAccess.Expression.IsKind(SyntaxKind.ThisExpression) &&
                !SimplificationHelpers.ShouldSimplifyMemberAccessExpression(semanticModel, memberAccess.Name, optionSet))
            {
                return false;
            }

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

            // if this node is on the left side, we could simplify to aliases
            if (!memberAccess.IsRightSideOfDot())
            {
                // Check if we need to replace this syntax with an alias identifier
                if (memberAccess.TryReplaceWithAlias(semanticModel, optionSet.GetOption(SimplificationOptions.PreferAliasToQualification), cancellationToken, out var aliasReplacement))
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

                            replacementNode = replacementNode
                                .WithAdditionalAnnotations(new SyntaxAnnotation(
                                    nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)));

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
                var actualSymbol = semanticModel.GetSymbolInfo(memberAccess.Name, cancellationToken);
                if (!TryGetReplacementCandidates(
                    semanticModel,
                    memberAccess,
                    actualSymbol,
                    out var speculativeSymbols,
                    out var speculativeNamespacesAndTypes))
                {
                    return false;
                }

                if (!IsReplacementCandidate(actualSymbol, speculativeSymbols, speculativeNamespacesAndTypes))
                {
                    return false;
                }
            }

            replacementNode = memberAccess.GetNameWithTriviaMoved();
            issueSpan = memberAccess.Expression.Span;

            if (replacementNode == null)
            {
                return false;
            }

            return memberAccess.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken);
        }

        public static SimpleNameSyntax GetNameWithTriviaMoved(this MemberAccessExpressionSyntax memberAccess)
            => memberAccess.Name
                .WithLeadingTrivia(memberAccess.GetLeadingTriviaForSimplifiedMemberAccess())
                .WithTrailingTrivia(memberAccess.GetTrailingTrivia());

        private static bool TryGetReplacementCandidates(
            SemanticModel semanticModel,
            MemberAccessExpressionSyntax memberAccess,
            SymbolInfo actualSymbol,
            out ImmutableArray<ISymbol> speculativeSymbols,
            out ImmutableArray<ISymbol> speculativeNamespacesAndTypes)
        {
            bool containsNamespaceOrTypeSymbol;
            bool containsOtherSymbol;
            if (actualSymbol.Symbol is object)
            {
                containsNamespaceOrTypeSymbol = actualSymbol.Symbol is INamespaceOrTypeSymbol;
                containsOtherSymbol = !containsNamespaceOrTypeSymbol;
            }
            else if (!actualSymbol.CandidateSymbols.IsDefaultOrEmpty)
            {
                containsNamespaceOrTypeSymbol = actualSymbol.CandidateSymbols.Any(symbol => symbol is INamespaceOrTypeSymbol);
                containsOtherSymbol = actualSymbol.CandidateSymbols.Any(symbol => !(symbol is INamespaceOrTypeSymbol));
            }
            else
            {
                speculativeSymbols = ImmutableArray<ISymbol>.Empty;
                speculativeNamespacesAndTypes = ImmutableArray<ISymbol>.Empty;
                return false;
            }

            speculativeSymbols = containsOtherSymbol
                ? semanticModel.LookupSymbols(memberAccess.SpanStart, name: memberAccess.Name.Identifier.ValueText)
                : ImmutableArray<ISymbol>.Empty;
            speculativeNamespacesAndTypes = containsNamespaceOrTypeSymbol
                ? semanticModel.LookupNamespacesAndTypes(memberAccess.SpanStart, name: memberAccess.Name.Identifier.ValueText)
                : ImmutableArray<ISymbol>.Empty;
            return true;
        }

        /// <summary>
        /// Determines if <paramref name="speculativeSymbols"/> and <paramref name="speculativeNamespacesAndTypes"/>
        /// together contain a superset of the symbols in <paramref name="actualSymbol"/>.
        /// </summary>
        private static bool IsReplacementCandidate(SymbolInfo actualSymbol, ImmutableArray<ISymbol> speculativeSymbols, ImmutableArray<ISymbol> speculativeNamespacesAndTypes)
        {
            if (speculativeSymbols.IsEmpty && speculativeNamespacesAndTypes.IsEmpty)
            {
                return false;
            }

            if (actualSymbol.Symbol is object)
            {
                return speculativeSymbols.Contains(actualSymbol.Symbol, CandidateSymbolEqualityComparer.Instance)
                    || speculativeNamespacesAndTypes.Contains(actualSymbol.Symbol, CandidateSymbolEqualityComparer.Instance);
            }

            foreach (var symbol in actualSymbol.CandidateSymbols)
            {
                if (!speculativeSymbols.Contains(symbol, CandidateSymbolEqualityComparer.Instance)
                    && !speculativeNamespacesAndTypes.Contains(symbol, CandidateSymbolEqualityComparer.Instance))
                {
                    return false;
                }
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
            {
                return obj?.OriginalDefinition.GetHashCode() ?? 0;
            }
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

        public static bool InsideCrefReference(this ExpressionSyntax expression)
        {
            var crefAttribute = expression.FirstAncestorOrSelf<XmlCrefAttributeSyntax>();
            return crefAttribute != null;
        }

        private static bool InsideNameOfExpression(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var nameOfInvocationExpr = expression.FirstAncestorOrSelf<InvocationExpressionSyntax>(
                invocationExpr =>
                {
                    return (invocationExpr.Expression is IdentifierNameSyntax identifierName) && (identifierName.Identifier.Text == "nameof") &&
                        semanticModel.GetConstantValue(invocationExpr).HasValue &&
                        (semanticModel.GetTypeInfo(invocationExpr).Type.SpecialType == SpecialType.System_String);
                });

            return nameOfInvocationExpr != null;
        }

        private static bool PreferPredefinedTypeKeywordInDeclarations(NameSyntax name, OptionSet optionSet, SemanticModel semanticModel)
        {
            return !IsInMemberAccessContext(name) &&
                   !InsideCrefReference(name) &&
                   !InsideNameOfExpression(name, semanticModel) &&
                   SimplificationHelpers.PreferPredefinedTypeKeywordInDeclarations(optionSet, semanticModel.Language);
        }

        private static bool PreferPredefinedTypeKeywordInMemberAccess(ExpressionSyntax expression, OptionSet optionSet, SemanticModel semanticModel)
        {
            return (IsInMemberAccessContext(expression) || InsideCrefReference(expression)) &&
                   !InsideNameOfExpression(expression, semanticModel) &&
                   SimplificationHelpers.PreferPredefinedTypeKeywordInMemberAccess(optionSet, semanticModel.Language);
        }

        public static bool IsInMemberAccessContext(this ExpressionSyntax expression) =>
            expression?.Parent is MemberAccessExpressionSyntax;

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

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/23582",
            Constraint = "Most trees do not have using alias directives, so avoid the expensive " + nameof(CSharpExtensions.GetSymbolInfo) + " call for this case.")]
        private static bool TryReplaceWithAlias(this ExpressionSyntax node, SemanticModel semanticModel, bool preferAliasToQualifiedName, CancellationToken cancellationToken, out IAliasSymbol aliasReplacement)
        {
            aliasReplacement = null;

            if (!node.IsAliasReplaceableExpression())
            {
                return false;
            }

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
                if (boundSymbols[0] is IAliasSymbol boundAlias && aliasReplacement.Target.Equals(symbol))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsNameOfArgumentExpression(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.Argument) &&
                expression.Parent.IsParentKind(SyntaxKind.ArgumentList) &&
                expression.Parent.Parent.Parent is InvocationExpressionSyntax invocation &&
                invocation.IsNameOfInvocation();
        }

        public static bool IsNameOfInvocation(this InvocationExpressionSyntax invocation)
        {
            return invocation.Expression is IdentifierNameSyntax identifierName &&
                   identifierName.Identifier.IsKindOrHasMatchingText(SyntaxKind.NameOfKeyword);
        }

        public static bool IsParenthesizedDeclarativeType(this DeclarationExpressionSyntax invocation)
        {
            return invocation.Designation.Kind() == SyntaxKind.ParenthesizedVariableDesignation;
        }

        public static IAliasSymbol GetAliasForSymbol(INamespaceOrTypeSymbol symbol, SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
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
            if (container is CompilationUnitSyntax compilation)
            {
                return GetNamespaceId(compilation.Members, target, ref index);
            }

            if (container is NamespaceDeclarationSyntax @namespace)
            {
                return GetNamespaceId(@namespace.Members, target, ref index);
            }

            return Contract.FailWithReturn<int>("shouldn't reach here");
        }

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

        private static bool TryReduce(
            this NameSyntax name,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default;

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
            if (name is QualifiedNameSyntax qualifiedName)
            {
                var left = qualifiedName.Left;
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

                return name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel);
            }
            else
            {
                if (!name.IsRightSideOfDotOrColonColon())
                {
                    if (name.TryReplaceWithAlias(semanticModel, optionSet.GetOption(SimplificationOptions.PreferAliasToQualification), cancellationToken, out var aliasReplacement))
                    {
                        // get the token text as it appears in source code to preserve e.g. Unicode character escaping
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
                            var qualifiedName3 = (QualifiedNameSyntax)name;

                            if (qualifiedName3.Right.Identifier.ValueText == identifierToken.ValueText)
                            {
                                issueSpan = qualifiedName3.Left.Span;
                            }
                        }

                        // first check if this would be a valid reduction
                        if (name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel))
                        {
                            // in case this alias name ends with "Attribute", we're going to see if we can also 
                            // remove that suffix.
                            if (TryReduceAttributeSuffix(
                                    name,
                                    identifierToken,
                                    out var replacementNodeWithoutAttributeSuffix,
                                    out var issueSpanWithoutAttributeSuffix))
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

                    if (name is SimpleNameSyntax simpleName)
                    {
                        if (!simpleName.Identifier.HasAnnotations(AliasAnnotation.Kind))
                        {
                            nameHasNoAlias = true;
                        }
                    }

                    if (name is QualifiedNameSyntax qualifiedName2)
                    {
                        if (!qualifiedName2.Right.HasAnnotation(Simplifier.SpecialTypeAnnotation))
                        {
                            nameHasNoAlias = true;
                        }
                    }

                    if (name is AliasQualifiedNameSyntax aliasQualifiedName)
                    {
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
                        var inDeclarationContext = PreferPredefinedTypeKeywordInDeclarations(name, optionSet, semanticModel);
                        var inMemberAccessContext = PreferPredefinedTypeKeywordInMemberAccess(name, optionSet, semanticModel);

                        if (!name.Parent.IsKind(SyntaxKind.QualifiedName) && (inDeclarationContext || inMemberAccessContext))
                        {
                            var codeStyleOptionName = inDeclarationContext
                                ? nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration)
                                : nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess);

                            var type = semanticModel.GetTypeInfo(name, cancellationToken).Type;
                            if (type != null)
                            {
                                var keywordKind = GetPredefinedKeywordKind(type.SpecialType);
                                if (keywordKind != SyntaxKind.None)
                                {
                                    return CanReplaceWithPredefinedTypeKeywordInContext(name, semanticModel, out replacementNode, ref issueSpan, keywordKind, codeStyleOptionName);
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
                                        return CanReplaceWithPredefinedTypeKeywordInContext(name, semanticModel, out replacementNode, ref issueSpan, keywordKind, codeStyleOptionName);
                                    }
                                }
                            }
                        }
                    }

                    // Nullable rewrite: Nullable<int> -> int?
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
                            if (name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel))
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
                        TryReduceAttributeSuffix(name, identifier, out replacementNode, out issueSpan);
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

        private static bool CanReplaceWithPredefinedTypeKeywordInContext(
            NameSyntax name,
            SemanticModel semanticModel,
            out TypeSyntax replacementNode,
            ref TextSpan issueSpan,
            SyntaxKind keywordKind,
            string codeStyleOptionName)
        {
            replacementNode = CreatePredefinedTypeSyntax(name, keywordKind);

            issueSpan = name.Span; // we want to show the whole name expression as unnecessary

            var canReduce = name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel);

            if (canReduce)
            {
                replacementNode = replacementNode.WithAdditionalAnnotations(new SyntaxAnnotation(codeStyleOptionName));
            }

            return canReduce;
        }

        private static TypeSyntax CreatePredefinedTypeSyntax(ExpressionSyntax expression, SyntaxKind keywordKind)
        {
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(expression.GetLeadingTrivia(), keywordKind, expression.GetTrailingTrivia()));
        }

        private static bool TryReduceAttributeSuffix(
            NameSyntax name,
            SyntaxToken identifierToken,
            out TypeSyntax replacementNode,
            out TextSpan issueSpan)
        {
            issueSpan = default;
            replacementNode = default;

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

                        // if this attribute name in source contained Unicode escaping, we will loose it now
                        // because there is no easy way to determine the substring from identifier->ToString() 
                        // which would be needed to pass to SyntaxFactory.Identifier
                        // The result is an unescaped Unicode character in source.

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
            issueSpan = default;

            switch (expression.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                    {
                        var memberAccess = (MemberAccessExpressionSyntax)expression;
                        if (IsMemberAccessADynamicInvocation(memberAccess, semanticModel))
                        {
                            return false;
                        }

                        if (TrySimplifyMemberAccessOrQualifiedName(memberAccess.Expression, memberAccess.Name, semanticModel, optionSet, out var newLeft, out issueSpan))
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
                        if (TrySimplifyMemberAccessOrQualifiedName(qualifiedName.Left, qualifiedName.Right, semanticModel, optionSet, out var newLeft, out issueSpan))
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
            issueSpan = default;

            if (left != null && right != null)
            {
                var leftSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, left);
                if (leftSymbol != null && (leftSymbol.Kind == SymbolKind.NamedType))
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
                            if (leftSymbol is INamedTypeSymbol namedType)
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

            return CanReplaceWithReducedNameInContext(name, reducedName, semanticModel);
        }

        private static bool CanReplaceWithReducedNameInContext(
            this NameSyntax name, TypeSyntax reducedName, SemanticModel semanticModel)
        {
            // Check for certain things that would prevent us from reducing this name in this context.
            // For example, you can simplify "using a = System.Int32" to "using a = int" as it's simply
            // not allowed in the C# grammar.

            if (IsNonNameSyntaxInUsingDirective(name, reducedName) ||
                WillConflictWithExistingLocal(name, reducedName) ||
                IsAmbiguousCast(name, reducedName) ||
                IsNullableTypeInPointerExpression(reducedName) ||
                name.IsNotNullableReplaceable(reducedName) ||
                IsNonReducableQualifiedNameInUsingDirective(semanticModel, name, reducedName))
            {
                return false;
            }

            return true;
        }

        private static bool IsNonReducableQualifiedNameInUsingDirective(SemanticModel model, NameSyntax name, TypeSyntax reducedName)
        {
            // Whereas most of the time we do not want to reduce namespace names, We will
            // make an exception for namespaces with the global:: alias.
            return IsQualifiedNameInUsingDirective(model, name) &&
                !IsGlobalAliasQualifiedName(name);
        }

        private static bool IsQualifiedNameInUsingDirective(SemanticModel model, NameSyntax name)
        {
            while (name.IsLeftSideOfQualifiedName())
            {
                name = (NameSyntax)name.Parent;
            }

            if (name.IsParentKind(SyntaxKind.UsingDirective) &&
                ((UsingDirectiveSyntax)name.Parent).Alias == null)
            {
                // We're a qualified name in a using.  We don't want to reduce this name as people like
                // fully qualified names in usings so they can properly tell what the name is resolving
                // to.
                // However, if this name is actually referencing the special Script class, then we do
                // want to allow that to be reduced.

                return !IsInScriptClass(model, name);
            }

            return false;
        }

        private static bool IsGlobalAliasQualifiedName(NameSyntax name)
        {
            // Checks whether the `global::` alias is applied to the name
            return name is AliasQualifiedNameSyntax aliasName &&
                aliasName.Alias.Identifier.IsKind(SyntaxKind.GlobalKeyword);
        }

        private static bool IsInScriptClass(SemanticModel model, NameSyntax name)
        {
            var symbol = model.GetSymbolInfo(name).Symbol as INamedTypeSymbol;
            while (symbol != null)
            {
                if (symbol.IsScriptClass)
                {
                    return true;
                }

                symbol = symbol.ContainingType;
            }

            return false;
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
            var typeStyle = CSharpUseImplicitTypeHelper.Instance.AnalyzeTypeName(
                simpleName, semanticModel, optionSet, cancellationToken);

            if (!typeStyle.IsStylePreferred || !typeStyle.CanConvert())
            {
                replacementNode = null;
                issueSpan = default;
                return false;
            }

            replacementNode = SyntaxFactory.IdentifierName("var")
                .WithLeadingTrivia(simpleName.GetLeadingTrivia())
                .WithTrailingTrivia(simpleName.GetTrailingTrivia());
            issueSpan = simpleName.Span;
            return true;
        }

        private static bool ContainsOpenName(NameSyntax name)
        {
            if (name is QualifiedNameSyntax qualifiedName)
            {
                return ContainsOpenName(qualifiedName.Left) || ContainsOpenName(qualifiedName.Right);
            }
            else if (name is GenericNameSyntax genericName)
            {
                return genericName.IsUnboundGenericName;
            }
            else
            {
                return false;
            }
        }

        private static bool IsNullableTypeInPointerExpression(ExpressionSyntax simplifiedNode)
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
            // parsed differently.  For example:  (Goo::Bar)+1  is a cast.  But if that simplifies to
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
        /// Returns the predefined keyword kind for a given <see cref="SpecialType"/>.
        /// </summary>
        /// <param name="specialType">The <see cref="SpecialType"/> of this type.</param>
        /// <returns>The keyword kind for a given special type, or SyntaxKind.None if the type name is not a predefined type.</returns>
        public static SyntaxKind GetPredefinedKeywordKind(SpecialType specialType)
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

        public static SimpleNameSyntax GetRightmostName(this ExpressionSyntax node)
        {
            if (node is MemberAccessExpressionSyntax memberAccess && memberAccess.Name != null)
            {
                return memberAccess.Name;
            }

            if (node is QualifiedNameSyntax qualified && qualified.Right != null)
            {
                return qualified.Right;
            }

            if (node is SimpleNameSyntax simple)
            {
                return simple;
            }

            if (node is ConditionalAccessExpressionSyntax conditional)
            {
                return conditional.WhenNotNull.GetRightmostName();
            }

            if (node is MemberBindingExpressionSyntax memberBinding)
            {
                return memberBinding.Name;
            }

            if (node is AliasQualifiedNameSyntax aliasQualifiedName && aliasQualifiedName.Name != null)
            {
                return aliasQualifiedName.Name;
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
                // unsafe code
                case SyntaxKind.SizeOfExpression:
                case SyntaxKind.PointerMemberAccessExpression:
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
                // unsafe code.
                case SyntaxKind.PointerIndirectionExpression:
                case SyntaxKind.AddressOfExpression:

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
                case SyntaxKind.IsPatternExpression:
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

        public static bool TryConvertToStatement(
            this ExpressionSyntax expression,
            SyntaxToken? semicolonTokenOpt,
            bool createReturnStatementForExpression,
            out StatementSyntax statement)
        {
            // It's tricky to convert an arrow expression with directives over to a block.
            // We'd need to find and remove the directives *after* the arrow expression and
            // move them accordingly.  So, for now, we just disallow this.
            if (expression.GetLeadingTrivia().Any(t => t.IsDirective))
            {
                statement = null;
                return false;
            }

            var semicolonToken = semicolonTokenOpt ?? SyntaxFactory.Token(SyntaxKind.SemicolonToken);

            statement = ConvertToStatement(expression, semicolonToken, createReturnStatementForExpression);
            return true;
        }

        private static StatementSyntax ConvertToStatement(ExpressionSyntax expression, SyntaxToken semicolonToken, bool createReturnStatementForExpression)
        {
            if (expression.IsKind(SyntaxKind.ThrowExpression))
            {
                var throwExpression = (ThrowExpressionSyntax)expression;
                return SyntaxFactory.ThrowStatement(throwExpression.ThrowKeyword, throwExpression.Expression, semicolonToken);
            }
            else if (createReturnStatementForExpression)
            {
                if (expression.GetLeadingTrivia().Any(t => t.IsSingleOrMultiLineComment()))
                {
                    return SyntaxFactory.ReturnStatement(expression.WithLeadingTrivia(SyntaxFactory.ElasticSpace))
                                        .WithSemicolonToken(semicolonToken)
                                        .WithLeadingTrivia(expression.GetLeadingTrivia())
                                        .WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker);
                }
                else
                {
                    return SyntaxFactory.ReturnStatement(expression)
                                        .WithSemicolonToken(semicolonToken);
                }
            }
            else
            {
                return SyntaxFactory.ExpressionStatement(expression)
                                    .WithSemicolonToken(semicolonToken);
            }
        }
    }
}
