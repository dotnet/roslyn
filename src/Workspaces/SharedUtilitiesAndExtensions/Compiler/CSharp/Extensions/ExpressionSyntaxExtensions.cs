// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class ExpressionSyntaxExtensions
    {
        public static ExpressionSyntax WalkUpParentheses(this ExpressionSyntax expression)
        {
            while (expression.IsParentKind(SyntaxKind.ParenthesizedExpression, out ExpressionSyntax? parentExpr))
                expression = parentExpr;

            return expression;
        }

        public static ExpressionSyntax WalkDownParentheses(this ExpressionSyntax expression)
        {
            while (expression.IsKind(SyntaxKind.ParenthesizedExpression, out ParenthesizedExpressionSyntax? parenExpression))
                expression = parenExpression.Expression;

            return expression;
        }

        public static bool IsQualifiedCrefName(this ExpressionSyntax expression)
            => expression.IsParentKind(SyntaxKind.NameMemberCref) && expression.Parent.IsParentKind(SyntaxKind.QualifiedCref);

        public static bool IsSimpleMemberAccessExpressionName([NotNullWhen(true)] this ExpressionSyntax? expression)
            => expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax? memberAccess) && memberAccess.Name == expression;

        public static bool IsAnyMemberAccessExpressionName(this ExpressionSyntax expression)
        {
            if (expression == null)
            {
                return false;
            }

            return expression == (expression.Parent as MemberAccessExpressionSyntax)?.Name ||
                expression.IsMemberBindingExpressionName();
        }

        public static bool IsMemberBindingExpressionName([NotNullWhen(true)] this ExpressionSyntax? expression)
            => expression.IsParentKind(SyntaxKind.MemberBindingExpression, out MemberBindingExpressionSyntax? memberBinding) &&
               memberBinding.Name == expression;

        public static bool IsRightSideOfQualifiedName([NotNullWhen(true)] this ExpressionSyntax? expression)
            => expression.IsParentKind(SyntaxKind.QualifiedName, out QualifiedNameSyntax? qualifiedName) && qualifiedName.Right == expression;

        public static bool IsRightSideOfColonColon(this ExpressionSyntax expression)
            => expression.IsParentKind(SyntaxKind.AliasQualifiedName, out AliasQualifiedNameSyntax? aliasName) && aliasName.Name == expression;

        public static bool IsRightSideOfDot(this ExpressionSyntax name)
            => IsSimpleMemberAccessExpressionName(name) || IsMemberBindingExpressionName(name) || IsRightSideOfQualifiedName(name) || IsQualifiedCrefName(name);

        public static bool IsRightSideOfDotOrArrow(this ExpressionSyntax name)
            => IsAnyMemberAccessExpressionName(name) || IsRightSideOfQualifiedName(name);

        public static bool IsRightSideOfDotOrColonColon(this ExpressionSyntax name)
            => IsRightSideOfDot(name) || IsRightSideOfColonColon(name);

        public static bool IsRightSideOfDotOrArrowOrColonColon([NotNullWhen(true)] this ExpressionSyntax name)
            => IsRightSideOfDotOrArrow(name) || IsRightSideOfColonColon(name);

        public static bool IsRightOfCloseParen(this ExpressionSyntax expression)
        {
            var firstToken = expression.GetFirstToken();
            return firstToken.Kind() != SyntaxKind.None
                && firstToken.GetPreviousToken().Kind() == SyntaxKind.CloseParenToken;
        }

        public static bool IsLeftSideOfDot([NotNullWhen(true)] this ExpressionSyntax? expression)
        {
            if (expression == null)
                return false;

            return IsLeftSideOfQualifiedName(expression) ||
                   IsLeftSideOfSimpleMemberAccessExpression(expression);
        }

        public static bool IsLeftSideOfSimpleMemberAccessExpression(this ExpressionSyntax expression)
            => (expression?.Parent).IsKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax? memberAccess) &&
               memberAccess.Expression == expression;

        public static bool IsLeftSideOfDotOrArrow(this ExpressionSyntax expression)
            => IsLeftSideOfQualifiedName(expression) ||
               (expression.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression == expression);

        public static bool IsLeftSideOfQualifiedName(this ExpressionSyntax expression)
            => (expression?.Parent).IsKind(SyntaxKind.QualifiedName, out QualifiedNameSyntax? qualifiedName) && qualifiedName.Left == expression;

        public static bool IsLeftSideOfExplicitInterfaceSpecifier([NotNullWhen(true)] this NameSyntax? name)
            => name.IsParentKind(SyntaxKind.ExplicitInterfaceSpecifier);

        public static bool IsExpressionOfInvocation(this ExpressionSyntax expression)
            => expression.IsParentKind(SyntaxKind.InvocationExpression, out InvocationExpressionSyntax? invocation) &&
               invocation.Expression == expression;

        public static bool TryGetNameParts(this ExpressionSyntax expression, [NotNullWhen(true)] out IList<string>? parts)
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
            if (expression.IsKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax? memberAccess))
            {
                if (!TryGetNameParts(memberAccess.Expression, parts))
                {
                    return false;
                }

                return AddSimpleName(memberAccess.Name, parts);
            }
            else if (expression.IsKind(SyntaxKind.QualifiedName, out QualifiedNameSyntax? qualifiedName))
            {
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
            => expression is LiteralExpressionSyntax;

        public static bool IsInConstantContext([NotNullWhen(true)] this ExpressionSyntax? expression)
        {
            if (expression == null)
                return false;

            if (expression.GetAncestor<ParameterSyntax>() != null)
                return true;

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
                return true;

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
            => IsInRefContext(expression, out _);

        /// <summary>
        /// Returns true if this expression is in some <c>ref</c> keyword context.  If <see langword="true"/> then
        /// <paramref name="refParent"/> will be the node containing the <see langword="ref"/> keyword.
        /// </summary>
        public static bool IsInRefContext([NotNullWhen(true)] this ExpressionSyntax? expression, [NotNullWhen(true)] out SyntaxNode? refParent)
        {
            while (expression?.Parent is ParenthesizedExpressionSyntax or PostfixUnaryExpressionSyntax(SyntaxKind.SuppressNullableWarningExpression))
                expression = (ExpressionSyntax)expression.Parent;

            if (expression?.Parent is RefExpressionSyntax or
                                      ArgumentSyntax { RefOrOutKeyword.RawKind: (int)SyntaxKind.RefKeyword })
            {
                refParent = expression.Parent;
                return true;
            }

            refParent = null;
            return false;
        }

        public static bool IsInInContext(this ExpressionSyntax expression)
            => (expression?.Parent as ArgumentSyntax)?.RefKindKeyword.Kind() == SyntaxKind.InKeyword;

        private static ExpressionSyntax GetExpressionToAnalyzeForWrites(ExpressionSyntax expression)
        {
            if (expression.IsRightSideOfDotOrArrow())
            {
                expression = (ExpressionSyntax)expression.GetRequiredParent();
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

        public static bool IsWrittenTo(this ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (expression == null)
                return false;

            expression = GetExpressionToAnalyzeForWrites(expression);

            if (expression.IsOnlyWrittenTo())
                return true;

            if (expression.IsInRefContext(out var refParent))
            {
                // most cases of `ref x` will count as a potential write of `x`.  An important exception is:
                // `ref readonly y = ref x`.  In that case, because 'y' can't be written to, this would not 
                // be a write of 'x'.
                if (refParent.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type: RefTypeSyntax refType } } }
                    && refType.ReadOnlyKeyword != default)
                {
                    return false;
                }

                return true;
            }

            // Similar to `ref x`, `&x` allows reads and write of the value, meaning `x` may be (but is not definitely)
            // written to.
            if (expression.Parent.IsKind(SyntaxKind.AddressOfExpression))
                return true;

            // We're written if we're used in a ++, or -- expression.
            if (expression.IsOperandOfIncrementOrDecrementExpression())
                return true;

            if (expression.IsLeftSideOfAnyAssignExpression())
                return true;

            // An extension method invocation with a ref-this parameter can write to an expression.
            if (expression.Parent is MemberAccessExpressionSyntax memberAccess &&
                expression == memberAccess.Expression)
            {
                var symbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
                if (symbol is IMethodSymbol { MethodKind: MethodKind.ReducedExtension, ReducedFrom: IMethodSymbol reducedFrom } &&
                    reducedFrom.Parameters.Length > 0 &&
                    reducedFrom.Parameters.First().RefKind == RefKind.Ref)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsAttributeNamedArgumentIdentifier([NotNullWhen(true)] this ExpressionSyntax? expression)
        {
            var nameEquals = expression?.Parent as NameEqualsSyntax;
            return nameEquals.IsParentKind(SyntaxKind.AttributeArgument);
        }

        public static bool IsOperandOfIncrementOrDecrementExpression(this ExpressionSyntax expression)
        {
            if (expression?.Parent is SyntaxNode parent)
            {
                switch (parent.Kind())
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
            => expression is IdentifierNameSyntax && expression.Parent is NameColonSyntax;

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
                case SymbolKind.FunctionPointerType:
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
                !expression.IsWrittenTo(semanticModel, cancellationToken) &&
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
                return expression is { Parent.RawKind: not (int)SyntaxKind.ConditionalAccessExpression };
            }

            if (expression.Parent == null)
                return false;

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
                case SyntaxKind.SwitchExpressionArm:
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
            while (topExpression.Parent is TypeSyntax typeSyntax)
            {
                topExpression = typeSyntax;
            }

            if (parentNonExpression != null &&
                parentNonExpression.IsKind(SyntaxKind.FromClause, out FromClauseSyntax? fromClause) &&
                topExpression != null &&
                fromClause.Type == topExpression)
            {
                return false;
            }

            // Parent type checks.
            if (expression.Parent is PostfixUnaryExpressionSyntax or
                BinaryExpressionSyntax or
                AssignmentExpressionSyntax or
                QueryClauseSyntax or
                SelectOrGroupClauseSyntax or
                CheckedExpressionSyntax)
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

                if (instanceSymbol is not INamespaceOrTypeSymbol)
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

        public static bool IsNameOfArgumentExpression(this ExpressionSyntax expression)
        {
            return expression is
            {
                Parent:
                {
                    RawKind: (int)SyntaxKind.Argument,
                    Parent:
                    {
                        RawKind: (int)SyntaxKind.ArgumentList,
                        Parent: InvocationExpressionSyntax invocation
                    }
                }
            } && invocation.IsNameOfInvocation();
        }

        public static bool IsNameOfInvocation(this InvocationExpressionSyntax invocation)
        {
            return invocation.Expression is IdentifierNameSyntax identifierName &&
                   identifierName.Identifier.IsKindOrHasMatchingText(SyntaxKind.NameOfKeyword);
        }

        public static SimpleNameSyntax? GetRightmostName(this ExpressionSyntax node)
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
                case SyntaxKind.SuppressNullableWarningExpression:
                // unsafe code
                case SyntaxKind.SizeOfExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    // From C# spec, 7.3.1:
                    // Primary: x.y  x?.y  x?[y]  f(x)  a[x]  x++  x--  new  typeof  default  checked  unchecked  delegate  x! 

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

                case SyntaxKind.RangeExpression:
                    // From C# spec, https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/ranges.md#systemrange
                    // Range: ..

                    return OperatorPrecedence.Range;

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

                case SyntaxKind.SwitchExpression:
                    return OperatorPrecedence.Switch;

                default:
                    return OperatorPrecedence.None;
            }
        }

        public static bool TryConvertToStatement(
            this ExpressionSyntax expression,
            SyntaxToken? semicolonTokenOpt,
            bool createReturnStatementForExpression,
            [NotNullWhen(true)] out StatementSyntax? statement)
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
            if (expression.IsKind(SyntaxKind.ThrowExpression, out ThrowExpressionSyntax? throwExpression))
            {
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

        public static bool IsDirectChildOfMemberAccessExpression(this ExpressionSyntax expression) =>
            expression?.Parent is MemberAccessExpressionSyntax;

        public static bool InsideCrefReference(this ExpressionSyntax expression)
            => expression.FirstAncestorOrSelf<XmlCrefAttributeSyntax>() != null;
    }
}
