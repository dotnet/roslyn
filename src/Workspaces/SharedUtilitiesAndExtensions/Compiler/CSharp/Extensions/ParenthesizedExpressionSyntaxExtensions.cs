// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class ParenthesizedExpressionSyntaxExtensions
{
    public static bool CanRemoveParentheses(
        this ParenthesizedExpressionSyntax node, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (node.OpenParenToken.IsMissing || node.CloseParenToken.IsMissing)
        {
            // int x = (3;
            return false;
        }

        var nodeParent = node.Parent;
        if (nodeParent == null)
            return false;

        var expression = node.Expression;

        // The 'direct' expression that contains this parenthesized node.  Note: in the case
        // of code like: ```x is (y)``` there is an intermediary 'no-syntax' 'ConstantPattern'
        // node between the 'is-pattern' node and the parenthesized expression.  So we manually
        // jump past that as, for all intents and purposes, we want to consider the 'is' expression
        // as the parent expression of the (y) expression.
        var parentExpression = nodeParent.IsKind(SyntaxKind.ConstantPattern)
            ? nodeParent.Parent as ExpressionSyntax
            : nodeParent as ExpressionSyntax;

        // Have to be careful if we would remove parens and cause a + and a + to become a ++.
        // (same with - as well).
        var tokenBeforeParen = node.GetFirstToken().GetPreviousToken();
        var tokenAfterParen = node.Expression.GetFirstToken();
        var previousChar = tokenBeforeParen.Text.LastOrDefault();
        var nextChar = tokenAfterParen.Text.FirstOrDefault();

        if ((previousChar == '+' && nextChar == '+') ||
            (previousChar == '-' && nextChar == '-'))
        {
            return false;
        }

        // Simplest cases:
        //   ((x)) -> (x)
        if (expression.IsKind(SyntaxKind.ParenthesizedExpression) ||
            parentExpression.IsKind(SyntaxKind.ParenthesizedExpression))
        {
            return true;
        }

        if (expression is StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax)
        {
            // var span = (stackalloc byte[8]);
            // https://github.com/dotnet/roslyn/issues/44629
            // The code semantics changes if the parenthesis removed.
            // With parenthesis:    variable span is of type `Span<byte>`.
            // Without parenthesis: variable span is of type `byte*` which can only be used in unsafe context.
            if (nodeParent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax varDecl } })
            {
                // We have either `var x = (stackalloc byte[8])` or `Span<byte> x = (stackalloc byte[8])`.  The former
                // is not safe to remove. the latter is.
                if (semanticModel.GetTypeInfo(varDecl.Type, cancellationToken).Type.IsSpanOrReadOnlySpan())
                    return !varDecl.Type.IsVar;
            }

            return false;
        }

        // Don't remove parentheses around `<` and `>` if there's a reasonable chance that it might
        // pair with the opposite form, causing them to be reinterpreted as generic syntax. See
        // https://github.com/dotnet/roslyn/issues/43934 for examples.
        if (expression.Kind() is SyntaxKind.GreaterThanExpression or SyntaxKind.LessThanExpression &&
            nodeParent is ArgumentSyntax)
        {
            var opposite = expression.IsKind(SyntaxKind.GreaterThanExpression) ? SyntaxKind.LessThanExpression : SyntaxKind.GreaterThanExpression;
            if (nodeParent.GetRequiredParent().ChildNodes().OfType<ArgumentSyntax>().Any(a => a.Expression.IsKind(opposite)))
                return false;
        }

        // (throw ...) -> throw ...
        if (expression.IsKind(SyntaxKind.ThrowExpression))
            return true;

        // (x); -> x;
        if (nodeParent.IsKind(SyntaxKind.ExpressionStatement))
            return true;

        // => (x)   ->   => x
        if (nodeParent.IsKind(SyntaxKind.ArrowExpressionClause))
            return true;

        // checked((x)) -> checked(x)
        if (nodeParent.Kind() is SyntaxKind.CheckedExpression or SyntaxKind.UncheckedExpression)
            return true;

        // ((x, y)) -> (x, y)
        if (expression.IsKind(SyntaxKind.TupleExpression))
            return true;

        // Cases:
        //   {(x)} -> {x}
        if (nodeParent is InitializerExpressionSyntax)
        {
            // `{ ([]) }` can't become `{ [] }` as `[` in an initializer will be parsed as an index assignment.
            if (tokenAfterParen.Kind() == SyntaxKind.OpenBracketToken)
                return false;

            // Assignment expressions and collection expressions are not allowed in initializers
            // as they are not parsed as expressions, but as more complex constructs
            if (expression is AssignmentExpressionSyntax)
                return false;

            return true;
        }

        // ([...]) -> [...]
        if (expression is CollectionExpressionSyntax collectionExpression)
        {
            // For back compat, the language disallows a few forms of casting an collection expression.
            // For example: `(A)[1, 2, 3]`.  This is because this form already has an interpretation as 
            // indexing into a parenthesized expression.  Check for these cases and only allow if it is
            // totally safe.  For example `(List<int>)[1, 2, 3]` is still safe as that was not a legal
            // expression in the past.
            //
            // Note: because `(T)[]` is never legal (an empty indexer is not legal), that form is always
            // considered a collection expression, regardless of what T is.
            if (collectionExpression.Elements.Count == 0)
                return true;

            return parentExpression is not CastExpressionSyntax
            {
                Type: IdentifierNameSyntax or QualifiedNameSyntax { Right: IdentifierNameSyntax }
            };
        }

        // int Prop => (x); -> int Prop => x;
        if (nodeParent is ArrowExpressionClauseSyntax arrowExpressionClause && arrowExpressionClause.Expression == node)
            return true;

        // Easy statement-level cases:
        //   var y = (x);           -> var y = x;
        //   var (y, z) = (x);      -> var (y, z) = x;
        //   if ((x))               -> if (x)
        //   return (x);            -> return x;
        //   yield return (x);      -> yield return x;
        //   throw (x);             -> throw x;
        //   switch ((x))           -> switch (x)
        //   while ((x))            -> while (x)
        //   do { } while ((x))     -> do { } while (x)
        //   for(;(x);)             -> for(;x;)
        //   foreach (var y in (x)) -> foreach (var y in x)
        //   lock ((x))             -> lock (x)
        //   using ((x))            -> using (x)
        //   catch when ((x))       -> catch when (x)
        if ((nodeParent is EqualsValueClauseSyntax equalsValue && equalsValue.Value == node) ||
            (nodeParent is IfStatementSyntax ifStatement && ifStatement.Condition == node) ||
            (nodeParent is ReturnStatementSyntax returnStatement && returnStatement.Expression == node) ||
            (nodeParent is YieldStatementSyntax(SyntaxKind.YieldReturnStatement) yieldStatement && yieldStatement.Expression == node) ||
            (nodeParent is ThrowStatementSyntax throwStatement && throwStatement.Expression == node) ||
            (nodeParent is SwitchStatementSyntax switchStatement && switchStatement.Expression == node) ||
            (nodeParent is WhileStatementSyntax whileStatement && whileStatement.Condition == node) ||
            (nodeParent is DoStatementSyntax doStatement && doStatement.Condition == node) ||
            (nodeParent is ForStatementSyntax forStatement && forStatement.Condition == node) ||
            (nodeParent is CommonForEachStatementSyntax forEachStatement && forEachStatement.Expression == node) ||
            (nodeParent is LockStatementSyntax lockStatement && lockStatement.Expression == node) ||
            (nodeParent is UsingStatementSyntax usingStatement && usingStatement.Expression == node) ||
            (nodeParent is CatchFilterClauseSyntax catchFilter && catchFilter.FilterExpression == node))
        {
            return true;
        }

        // Handle expression-level ambiguities
        if (RemovalMayIntroduceCastAmbiguity(node) ||
            RemovalMayIntroduceCommaListAmbiguity(node) ||
            RemovalMayIntroduceInterpolationAmbiguity(node) ||
            RemovalWouldChangeConstantReferenceToTypeReference(node, expression, semanticModel, cancellationToken))
        {
            return false;
        }

        // Cases:
        //   (C)(this) -> (C)this
        if (nodeParent.IsKind(SyntaxKind.CastExpression) && expression.IsKind(SyntaxKind.ThisExpression))
            return true;

        // Cases:
        //   y((x)) -> y(x)
        if (nodeParent is ArgumentSyntax argument && argument.Expression == node)
            return true;

        // Cases:
        //   $"{(x)}" -> $"{x}"
        if (nodeParent.IsKind(SyntaxKind.Interpolation))
            return true;

        // Cases:
        //   ($"{x}") -> $"{x}"
        if (expression.IsKind(SyntaxKind.InterpolatedStringExpression))
            return true;

        // Cases:
        //   new {(x)} -> {x}
        //   new { a = (x)} -> { a = x }
        //   new { a = (x = c)} -> { a = x = c }
        if (nodeParent is AnonymousObjectMemberDeclaratorSyntax anonymousDeclarator)
        {
            // Assignment expressions are not allowed unless member is named
            if (anonymousDeclarator.NameEquals == null && expression is AssignmentExpressionSyntax)
                return false;

            return true;
        }

        // Cases:
        // where (x + 1 > 14) -> where x + 1 > 14
        if (nodeParent is QueryClauseSyntax)
            return true;

        // Cases:
        //   (x)   -> x
        //   (x.y) -> x.y
        if (IsSimpleOrDottedName(expression))
            return true;

        // Cases:
        //   ('')      -> ''
        //   ("")      -> ""
        //   (false)   -> false
        //   (true)    -> true
        //   (null)    -> null
        //   (default) -> default;
        //   (1)       -> 1
        if (expression is LiteralExpressionSyntax)
            return true;

        // (typeof(int)) -> typeof(int)
        // (default(int)) -> default(int)
        // (checked(1)) -> checked(1)
        // (unchecked(1)) -> unchecked(1)
        // (sizeof(int)) -> sizeof(int)
        if (expression is TypeOfExpressionSyntax or DefaultExpressionSyntax or CheckedExpressionSyntax or SizeOfExpressionSyntax)
            return true;

        // (this)   -> this
        if (expression.IsKind(SyntaxKind.ThisExpression))
            return true;

        // x is > (-1)  ->  x is > -1
        //
        // Note: the general case of removing parens from a prefix unary expression in a normal expression is handled as
        // the last step of this algorithm below.  This is only the pattern case.
        if (expression is PrefixUnaryExpressionSyntax prefixUnary &&
            parentExpression is null)
        {
            return true;
        }

        // x ?? (throw ...) -> x ?? throw ...
        if (expression.IsKind(SyntaxKind.ThrowExpression) &&
            nodeParent is BinaryExpressionSyntax(SyntaxKind.CoalesceExpression) binary &&
            binary.Right == node)
        {
            return true;
        }

        // case (x): -> case x:
        if (nodeParent.IsKind(SyntaxKind.CaseSwitchLabel))
            return true;

        // case (x) when y: -> case x when y:
        if (nodeParent.IsKind(SyntaxKind.ConstantPattern) &&
            nodeParent.IsParentKind(SyntaxKind.CasePatternSwitchLabel))
        {
            return true;
        }

        // case x when (y): -> case x when y:
        if (nodeParent.IsKind(SyntaxKind.WhenClause))
        {
            // Subtle case: `when (a || x?[0]):` cannot have parentheses removed because it would become
            // `when a || x?[0]:` which the parser interprets as `when a || x ? [0] :` (a ternary expression)
            // instead of the intended conditional access `x?[0]` followed by the `:` from when clause syntax.
            // To avoid this, we check if removing parentheses would put a conditional access at the end of the
            // expression (on the rightmost path), immediately before the `:`.
            return !ContainsConditionalAccessOnRightmostPath(expression);
        }

        // #if (x)   ->   #if x
        if (nodeParent is DirectiveTriviaSyntax)
            return true;

        // Switch expression arm
        // x => (y)
        if (nodeParent is SwitchExpressionArmSyntax arm && arm.Expression == node)
            return true;

        // [.. (expr)]    ->    [.. expr]
        //
        // Note: There is no precedence with `..` it's always just part of the collection expr, with the expr being
        // parsed independently of it.  That's why no parens are ever needed here.
        if (nodeParent is SpreadElementSyntax)
            return true;

        // If we have: (X)(++x) or (X)(--x), we don't want to remove the parens. doing so can
        // make the ++/-- now associate with the previous part of the cast expression.
        if (parentExpression.IsKind(SyntaxKind.CastExpression) &&
            expression.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression)
        {
            return false;
        }

        // (condition ? ref a : ref b ) = SomeValue, parenthesis can't be removed for when conditional expression appears at left
        // This syntax is only allowed since C# 7.2
        if (expression.IsKind(SyntaxKind.ConditionalExpression) &&
            node.IsLeftSideOfAnyAssignExpression())
        {
            return false;
        }

        // Don't change (x?.Count)... to x?.Count...
        //
        // It very much changes the semantics to have code that always executed (outside the
        // parenthesized expression) now only conditionally run depending on if 'x' is null or
        // not.
        if (expression.IsKind(SyntaxKind.ConditionalAccessExpression))
            return false;

        // Operator precedence cases:
        // - If the parent is not an expression, do not remove parentheses
        // - Otherwise, parentheses may be removed if doing so does not change operator associations.
        return parentExpression != null && !RemovalChangesAssociation(node, parentExpression, semanticModel);

        static bool ContainsConditionalAccessOnRightmostPath(ExpressionSyntax expr)
        {
            // Walk down the rightmost path of the expression tree
            for (var current = expr; current != null;)
            {
                if (current is ConditionalAccessExpressionSyntax)
                    return true;

                current = current is BinaryExpressionSyntax binaryExpression
                    ? binaryExpression.Right
                    : current.ChildNodes().FirstOrDefault() as ExpressionSyntax;
            }

            return false;
        }
    }

    private static bool RemovalWouldChangeConstantReferenceToTypeReference(
        ParenthesizedExpressionSyntax node, ExpressionSyntax expression,
        SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        // With cases like: `if (x is (Y))` then we cannot remove the parens if it would make Y now bind to a type
        // instead of a constant.
        if (node.Parent is not ConstantPatternSyntax { Parent: IsPatternExpressionSyntax })
            return false;

        var exprSymbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
        if (exprSymbol is not IFieldSymbol { IsConst: true })
            return false;

        // See if interpreting the same expression as a type in this location binds.
        var potentialType = semanticModel.GetSpeculativeTypeInfo(expression.SpanStart, expression, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
        return potentialType is not (null or IErrorTypeSymbol);
    }

    private static readonly ObjectPool<Stack<SyntaxNode>> s_nodeStackPool = SharedPools.Default<Stack<SyntaxNode>>();

    private static bool RemovalMayIntroduceInterpolationAmbiguity(ParenthesizedExpressionSyntax node)
    {
        // First, find the parenting interpolation. If we find a parenthesize expression first,
        // we can bail out early.
        InterpolationSyntax? interpolation = null;
        foreach (var ancestor in node.GetRequiredParent().AncestorsAndSelf())
        {
            if (ancestor.IsKind(SyntaxKind.ParenthesizedExpression))
                return false;

            if (ancestor.IsKind(SyntaxKind.Interpolation, out interpolation))
                break;
        }

        if (interpolation == null)
            return false;

        // In order determine whether removing this parenthesized expression will introduce a
        // parsing ambiguity, we must dig into the child tokens and nodes to determine whether
        // they include any : or :: tokens. If they do, we can't remove the parentheses because
        // the parser would assume that the first : would begin the format clause of the interpolation.

        using var _ = s_nodeStackPool.GetPooledObject(out var stack);
        stack.Push(node.Expression);

        while (stack.TryPop(out var expression))
        {
            foreach (var nodeOrToken in expression.ChildNodesAndTokens())
            {
                // Note: There's no need drill into other parenthesized expressions, since any colons in them would be unambiguous.
                if (nodeOrToken.AsNode(out var childNode))
                {
                    if (!childNode.IsKind(SyntaxKind.ParenthesizedExpression))
                        stack.Push(childNode);
                }
                else if (nodeOrToken.IsToken)
                {
                    if (nodeOrToken.Kind() is SyntaxKind.ColonToken or SyntaxKind.ColonColonToken)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool RemovalChangesAssociation(
        ParenthesizedExpressionSyntax node, ExpressionSyntax parentExpression, SemanticModel semanticModel)
    {
        var expression = node.Expression;
        var precedence = expression.GetOperatorPrecedence();
        var parentPrecedence = parentExpression.GetOperatorPrecedence();
        if (precedence == OperatorPrecedence.None || parentPrecedence == OperatorPrecedence.None)
        {
            // Be conservative if the expression or its parent has no precedence.
            return true;
        }

        if (precedence > parentPrecedence)
        {
            // Association never changes if the expression's precedence is higher than its parent.
            return false;
        }
        else if (precedence < parentPrecedence)
        {
            // Association always changes if the expression's precedence is lower that its parent.
            return true;
        }
        else if (precedence == parentPrecedence)
        {
            // If the expression's precedence is the same as its parent, and both are binary expressions,
            // check for associativity and commutability.

            if (expression is not (BinaryExpressionSyntax or AssignmentExpressionSyntax))
            {
                // If the expression is not a binary expression, association never changes.
                return false;
            }

            if (parentExpression is BinaryExpressionSyntax parentBinaryExpression)
            {
                // If both the expression and its parent are binary expressions and their kinds
                // are the same, and the parenthesized expression is on hte right and the 
                // operation is associative, it can sometimes be safe to remove these parens.
                //
                // i.e. if you have "a && (b && c)" it can be converted to "a && b && c" 
                // as that new interpretation "(a && b) && c" operates the exact same way at 
                // runtime.
                //
                // Specifically: 
                //  1) the operands are still executed in the same order: a, b, then c.
                //     So even if they have side effects, it will not matter.
                //  2) the same shortcircuiting happens.
                //  3) for logical operators the result will always be the same (there are 
                //     additional conditions that are checked for non-logical operators).
                if (IsAssociative(parentBinaryExpression.Kind()) &&
                    parentBinaryExpression.Right == node &&
                    node.Expression.IsKind(parentBinaryExpression.Kind(), out BinaryExpressionSyntax? nodeBinary))
                {
                    return !CSharpSemanticFacts.Instance.IsSafeToChangeAssociativity(
                        nodeBinary, parentBinaryExpression, semanticModel);
                }

                // Null-coalescing is right associative; removing parens from the LHS changes the association.
                if (parentExpression.IsKind(SyntaxKind.CoalesceExpression))
                {
                    return parentBinaryExpression.Left == node;
                }

                // All other binary operators are left associative; removing parens from the RHS changes the association.
                return parentBinaryExpression.Right == node;
            }

            if (parentExpression is AssignmentExpressionSyntax parentAssignmentExpression)
            {
                // Assignment expressions are right associative; removing parens from the LHS changes the association.
                return parentAssignmentExpression.Left == node;
            }

            // If the parent is not a binary expression, association never changes.
            return false;
        }

        throw ExceptionUtilities.Unreachable();
    }

    private static bool IsAssociative(SyntaxKind kind)
    {
        switch (kind)
        {
            case SyntaxKind.AddExpression:
            case SyntaxKind.MultiplyExpression:
            case SyntaxKind.BitwiseOrExpression:
            case SyntaxKind.ExclusiveOrExpression:
            case SyntaxKind.LogicalOrExpression:
            case SyntaxKind.BitwiseAndExpression:
            case SyntaxKind.LogicalAndExpression:
                return true;
        }

        return false;
    }

    private static bool RemovalMayIntroduceCastAmbiguity(ParenthesizedExpressionSyntax node)
    {
        // Be careful not to break the special case around (x)(-y)
        // as defined in section 7.7.6 of the C# language specification.
        //
        // cases we can't remove the parens for are:
        //
        //      (x)(+y)
        //      (x)(-y)
        //      (x)(&y) // unsafe code
        //      (x)(*y) // unsafe code
        //
        // Note: we can remove the parens if the (x) part is unambiguously a type.
        // i.e. if it something like:
        //
        //      (int)(...)
        //      (x[])(...)
        //      (X*)(...)
        //      (X?)(...)
        //      (global::X)(...)

        if (node?.Parent is CastExpressionSyntax castExpression)
        {
            if (castExpression.Type.Kind() is
                    SyntaxKind.PredefinedType or
                    SyntaxKind.ArrayType or
                    SyntaxKind.PointerType or
                    SyntaxKind.NullableType)
            {
                return false;
            }

            if (castExpression.Type is NameSyntax name && StartsWithAlias(name))
            {
                return false;
            }

            var expression = node.Expression;

            if (expression.Kind() is
                    SyntaxKind.UnaryMinusExpression or
                    SyntaxKind.UnaryPlusExpression or
                    SyntaxKind.PointerIndirectionExpression or
                    SyntaxKind.AddressOfExpression)
            {
                return true;
            }
        }

        return false;
    }

    private static bool StartsWithAlias(NameSyntax name)
    {
        if (name.IsKind(SyntaxKind.AliasQualifiedName))
        {
            return true;
        }

        if (name is QualifiedNameSyntax qualifiedName)
        {
            return StartsWithAlias(qualifiedName.Left);
        }

        return false;
    }

    private static bool RemovalMayIntroduceCommaListAmbiguity(ParenthesizedExpressionSyntax node)
    {
        if (IsSimpleOrDottedName(node.Expression))
        {
            // We can't remove parentheses from an identifier name in the following cases:
            //   F((x) < x, x > (1 + 2))
            //   F(x < (x), x > (1 + 2))
            //   F(x < x, (x) > (1 + 2))
            //   {(x) < x, x > (1 + 2)}
            //   {x < (x), x > (1 + 2)}
            //   {x < x, (x) > (1 + 2)}

            if (node.Parent is BinaryExpressionSyntax binaryExpression &&
                binaryExpression.Kind() is SyntaxKind.LessThanExpression or SyntaxKind.GreaterThanExpression &&
                (binaryExpression.IsParentKind(SyntaxKind.Argument) || binaryExpression.Parent is InitializerExpressionSyntax))
            {
                if (binaryExpression.IsKind(SyntaxKind.LessThanExpression))
                {
                    if ((binaryExpression.Left == node && IsSimpleOrDottedName(binaryExpression.Right)) ||
                        (binaryExpression.Right == node && IsSimpleOrDottedName(binaryExpression.Left)))
                    {
                        if (IsNextExpressionPotentiallyAmbiguous(binaryExpression))
                        {
                            return true;
                        }
                    }

                    return false;
                }
                else if (binaryExpression.IsKind(SyntaxKind.GreaterThanExpression))
                {
                    if (binaryExpression.Left == node &&
                        binaryExpression.Right.Kind() is SyntaxKind.ParenthesizedExpression or SyntaxKind.CastExpression)
                    {
                        if (IsPreviousExpressionPotentiallyAmbiguous(binaryExpression))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
        else if (node.Expression.IsKind(SyntaxKind.LessThanExpression))
        {
            // We can't remove parentheses from a less-than expression in the following cases:
            //   F((x < x), x > (1 + 2))
            //   {(x < x), x > (1 + 2)}
            return IsNextExpressionPotentiallyAmbiguous(node);
        }
        else if (node.Expression.IsKind(SyntaxKind.GreaterThanExpression))
        {
            // We can't remove parentheses from a greater-than expression in the following cases:
            //   F(x < x, (x > (1 + 2)))
            //   {x < x, (x > (1 + 2))}
            return IsPreviousExpressionPotentiallyAmbiguous(node);
        }

        return false;
    }

    private static bool IsPreviousExpressionPotentiallyAmbiguous(ExpressionSyntax node)
    {
        ExpressionSyntax? previousExpression = null;

        if (node.Parent is ArgumentSyntax argument)
        {
            if (argument.Parent is ArgumentListSyntax argumentList)
            {
                var argumentIndex = argumentList.Arguments.IndexOf(argument);
                if (argumentIndex > 0)
                {
                    previousExpression = argumentList.Arguments[argumentIndex - 1].Expression;
                }
            }
        }
        else if (node.Parent is InitializerExpressionSyntax initializer)
        {
            var expressionIndex = initializer.Expressions.IndexOf(node);
            if (expressionIndex > 0)
            {
                previousExpression = initializer.Expressions[expressionIndex - 1];
            }
        }

        if (previousExpression == null ||
            previousExpression is not BinaryExpressionSyntax(SyntaxKind.LessThanExpression) lessThanExpression)
        {
            return false;
        }

        return (IsSimpleOrDottedName(lessThanExpression.Left)
                || lessThanExpression.Left.IsKind(SyntaxKind.CastExpression))
            && IsSimpleOrDottedName(lessThanExpression.Right);
    }

    private static bool IsNextExpressionPotentiallyAmbiguous(ExpressionSyntax node)
    {
        ExpressionSyntax? nextExpression = null;

        if (node.Parent is ArgumentSyntax argument)
        {
            if (argument.Parent is ArgumentListSyntax argumentList)
            {
                var argumentIndex = argumentList.Arguments.IndexOf(argument);
                if (argumentIndex >= 0 && argumentIndex < argumentList.Arguments.Count - 1)
                {
                    nextExpression = argumentList.Arguments[argumentIndex + 1].Expression;
                }
            }
        }
        else if (node.Parent is InitializerExpressionSyntax initializer)
        {
            var expressionIndex = initializer.Expressions.IndexOf(node);
            if (expressionIndex >= 0 && expressionIndex < initializer.Expressions.Count - 1)
            {
                nextExpression = initializer.Expressions[expressionIndex + 1];
            }
        }

        if (nextExpression == null ||
            nextExpression is not BinaryExpressionSyntax(SyntaxKind.GreaterThanExpression) greaterThanExpression)
        {
            return false;
        }

        return IsSimpleOrDottedName(greaterThanExpression.Left)
            && greaterThanExpression.Right.Kind() is SyntaxKind.ParenthesizedExpression or SyntaxKind.CastExpression;
    }

    private static bool IsSimpleOrDottedName(ExpressionSyntax expression)
        => expression.Kind() is SyntaxKind.IdentifierName or SyntaxKind.QualifiedName or SyntaxKind.SimpleMemberAccessExpression;

    public static bool CanRemoveParentheses(this ParenthesizedPatternSyntax node)
    {
        if (node.OpenParenToken.IsMissing || node.CloseParenToken.IsMissing)
        {
            // int x = (3;
            return false;
        }

        var pattern = node.Pattern;

        // We wrap a parenthesized pattern and we're parenthesized.  We can remove our parens.
        if (pattern is ParenthesizedPatternSyntax)
            return true;

        // We're parenthesized discard pattern. We cannot remove parens.
        // x is (_)
        if (pattern is DiscardPatternSyntax && node.Parent is IsPatternExpressionSyntax)
            return false;

        // (not ...) -> not ...
        //
        // this is safe because unary patterns have the highest precedence, so even if you had:
        // (not ...) or (not ...)
        //
        // you can safely convert to `not ... or not ...`
        var patternPrecedence = pattern.GetOperatorPrecedence();
        if (patternPrecedence is OperatorPrecedence.Primary or OperatorPrecedence.Unary)
            return true;

        // We're parenthesized and are inside a parenthesized pattern.  We can remove our parens.
        // ((x)) -> (x)
        if (node.Parent is ParenthesizedPatternSyntax)
            return true;

        // x is (...)  ->  x is ...
        if (node.Parent is IsPatternExpressionSyntax)
            return true;

        // (x or y) => ...  ->    x or y => ...
        if (node.Parent is SwitchExpressionArmSyntax)
            return true;

        // X: (y or z)      ->    X: y or z
        if (node.Parent is SubpatternSyntax)
            return true;

        // case (x or y):   ->    case x or y:
        if (node.Parent is CasePatternSwitchLabelSyntax)
            return true;

        // Operator precedence cases:
        // - If the parent is not an expression, do not remove parentheses
        // - Otherwise, parentheses may be removed if doing so does not change operator associations.
        return node.Parent is PatternSyntax patternParent &&
               !RemovalChangesAssociation(node, patternParent);
    }

    private static bool RemovalChangesAssociation(
        ParenthesizedPatternSyntax node, PatternSyntax parentPattern)
    {
        var pattern = node.Pattern;
        var precedence = pattern.GetOperatorPrecedence();
        var parentPrecedence = parentPattern.GetOperatorPrecedence();
        if (precedence == OperatorPrecedence.None || parentPrecedence == OperatorPrecedence.None)
        {
            // Be conservative if the expression or its parent has no precedence.
            return true;
        }

        // Association always changes if the expression's precedence is lower that its parent.
        return precedence < parentPrecedence;
    }

    public static OperatorPrecedence GetOperatorPrecedence(this PatternSyntax pattern)
    {
        switch (pattern)
        {
            case ConstantPatternSyntax:
            case DiscardPatternSyntax:
            case DeclarationPatternSyntax:
            case RecursivePatternSyntax:
            case TypePatternSyntax:
            case VarPatternSyntax:
                return OperatorPrecedence.Primary;

            case UnaryPatternSyntax:
            case RelationalPatternSyntax:
                return OperatorPrecedence.Unary;

            case BinaryPatternSyntax binaryPattern:
                if (binaryPattern.IsKind(SyntaxKind.AndPattern))
                    return OperatorPrecedence.ConditionalAnd;

                if (binaryPattern.IsKind(SyntaxKind.OrPattern))
                    return OperatorPrecedence.ConditionalOr;

                break;
        }

        Debug.Fail("Unhandled pattern type");
        return OperatorPrecedence.None;
    }
}
