// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Debugging;

internal partial class CSharpProximityExpressionsService
{
    private static string ConvertToString(ExpressionSyntax expression)
    {
        var converted = expression.ConvertToSingleLine();
        return converted.ToString();
    }

    private static void AddExpressionTerms(ExpressionSyntax expression, IList<string> terms)
    {
        // Check here rather than at all the call sites...
        if (expression == null)
        {
            return;
        }

        // Collect terms from this expression, which returns flags indicating the validity
        // of this expression as a whole.
        var expressionType = ExpressionType.Invalid;
        AddSubExpressionTerms(expression, terms, ref expressionType);

        AddIfValidTerm(expression, expressionType, terms);
    }

    private static void AddIfValidTerm(ExpressionSyntax expression, ExpressionType type, IList<string> terms)
    {
        if (IsValidTerm(type))
        {
            // If this expression identified itself as a valid term, add it to the
            // term table
            terms.Add(ConvertToString(expression));
        }
    }

    private static bool IsValidTerm(ExpressionType type)
        => (type & ExpressionType.ValidTerm) == ExpressionType.ValidTerm;

    private static bool IsValidExpression(ExpressionType type)
        => (type & ExpressionType.ValidExpression) == ExpressionType.ValidExpression;

    private static void AddSubExpressionTerms(ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
    {
        // Check here rather than at all the call sites...
        if (expression == null)
        {
            return;
        }

        switch (expression.Kind())
        {
            case SyntaxKind.ThisExpression:
            case SyntaxKind.BaseExpression:
                // an op term is ok if it's a "this" or "base" op it allows us to see
                // "this.goo" in the autos window note: it's not a VALIDTERM since we don't
                // want "this" showing up in the auto's window twice.
                expressionType = ExpressionType.ValidExpression;
                return;

            case SyntaxKind.IdentifierName:
                // Name nodes are always valid terms
                expressionType = ExpressionType.ValidTerm;
                return;

            case SyntaxKind.CharacterLiteralExpression:
            case SyntaxKind.FalseLiteralExpression:
            case SyntaxKind.NullLiteralExpression:
            case SyntaxKind.NumericLiteralExpression:
            case SyntaxKind.StringLiteralExpression:
            case SyntaxKind.TrueLiteralExpression:
                // Constants can make up a valid term, but we don't consider them valid
                // terms themselves (since we don't want them to show up in the autos window
                // on their own).
                expressionType = ExpressionType.ValidExpression;
                return;

            case SyntaxKind.CastExpression:
                AddCastExpressionTerms((CastExpressionSyntax)expression, terms, ref expressionType);
                return;

            case SyntaxKind.SimpleMemberAccessExpression:
            case SyntaxKind.PointerMemberAccessExpression:
                AddMemberAccessExpressionTerms((MemberAccessExpressionSyntax)expression, terms, ref expressionType);
                return;

            case SyntaxKind.ObjectCreationExpression:
                AddObjectCreationExpressionTerms((ObjectCreationExpressionSyntax)expression, terms, ref expressionType);
                return;

            case SyntaxKind.ArrayCreationExpression:
                AddArrayCreationExpressionTerms((ArrayCreationExpressionSyntax)expression, terms, ref expressionType);
                return;

            case SyntaxKind.InvocationExpression:
                AddInvocationExpressionTerms((InvocationExpressionSyntax)expression, terms, ref expressionType);
                return;
        }

        // +, -, ++, --, !, etc.
        //
        // This is a valid expression if it doesn't have obvious side effects (i.e. ++, --)
        if (expression is PrefixUnaryExpressionSyntax prefixUnary)
        {
            AddPrefixUnaryExpressionTerms(prefixUnary, terms, ref expressionType);
            return;
        }

        if (expression is AwaitExpressionSyntax awaitExpression)
        {
            AddAwaitExpressionTerms(awaitExpression, terms, ref expressionType);
            return;
        }

        if (expression is PostfixUnaryExpressionSyntax postfixExpression)
        {
            AddPostfixUnaryExpressionTerms(postfixExpression, terms, ref expressionType);
            return;
        }

        if (expression is BinaryExpressionSyntax binaryExpression)
        {
            AddBinaryExpressionTerms(expression, binaryExpression.Left, binaryExpression.Right, terms, ref expressionType);
            return;
        }

        if (expression is AssignmentExpressionSyntax assignmentExpression)
        {
            AddBinaryExpressionTerms(expression, assignmentExpression.Left, assignmentExpression.Right, terms, ref expressionType);
            return;
        }

        if (expression is ConditionalExpressionSyntax conditional)
        {
            AddConditionalExpressionTerms(conditional, terms, ref expressionType);
            return;
        }

        if (expression is ParenthesizedExpressionSyntax parenthesizedExpression)
        {
            AddSubExpressionTerms(parenthesizedExpression.Expression, terms, ref expressionType);
        }

        expressionType = ExpressionType.Invalid;
    }

    private static void AddCastExpressionTerms(CastExpressionSyntax castExpression, IList<string> terms, ref ExpressionType expressionType)
    {
        // For a cast, just add the nested expression.  Note: this is technically
        // unsafe as the cast *may* have side effects.  However, in practice this is
        // extremely rare, so we allow for this since it's ok in the common case.

        var flags = ExpressionType.Invalid;

        // Ask our subexpression for terms
        AddSubExpressionTerms(castExpression.Expression, terms, ref flags);

        // Is our expression a valid term?
        AddIfValidTerm(castExpression.Expression, flags, terms);

        // If the subexpression is a valid term, so is the cast expression
        expressionType = flags;
    }

    private static void AddMemberAccessExpressionTerms(MemberAccessExpressionSyntax memberAccessExpression, IList<string> terms, ref ExpressionType expressionType)
    {
        var flags = ExpressionType.Invalid;

        // These operators always have a RHS of a name node, which we know would
        // "claim" to be a valid term, but is not valid without the LHS present.
        // So, we don't bother collecting anything from the RHS...
        AddSubExpressionTerms(memberAccessExpression.Expression, terms, ref flags);

        // If the LHS says it's a valid term, then we add it ONLY if our PARENT
        // is NOT another dot/arrow.  This allows the expression 'a.b.c.d' to
        // add both 'a.b.c.d' and 'a.b.c', but not 'a.b' and 'a'.
        if (IsValidTerm(flags) &&
            memberAccessExpression.Parent?.Kind() is not SyntaxKind.SimpleMemberAccessExpression and not SyntaxKind.PointerMemberAccessExpression)
        {
            terms.Add(ConvertToString(memberAccessExpression.Expression));
        }

        // And this expression itself is a valid term if the LHS is a valid
        // expression, and its PARENT is not an invocation.
        if (IsValidExpression(flags) &&
            !memberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression))
        {
            expressionType = ExpressionType.ValidTerm;
        }
        else
        {
            expressionType = ExpressionType.ValidExpression;
        }
    }

    private static void AddObjectCreationExpressionTerms(ObjectCreationExpressionSyntax objectionCreationExpression, IList<string> terms, ref ExpressionType expressionType)
    {
        // Object creation can *definitely* cause side effects.  So we initially
        // mark this as something invalid.  We allow it as a valid expr if all
        // the sub arguments are valid terms.
        expressionType = ExpressionType.Invalid;

        if (objectionCreationExpression.ArgumentList != null)
        {
            var flags = ExpressionType.Invalid;
            AddArgumentTerms(objectionCreationExpression.ArgumentList, terms, ref flags);

            // If all arguments are terms, then this is possibly a valid expr that can be used
            // somewhere higher in the stack.
            if (IsValidTerm(flags))
            {
                expressionType = ExpressionType.ValidExpression;
            }
        }
    }

    private static void AddArrayCreationExpressionTerms(
        ArrayCreationExpressionSyntax arrayCreationExpression,
        IList<string> terms,
        ref ExpressionType expressionType)
    {
        var validTerm = true;

        if (arrayCreationExpression.Initializer != null)
        {
            var flags = ExpressionType.Invalid;
            arrayCreationExpression.Initializer.Expressions.Do(e => AddSubExpressionTerms(e, terms, ref flags));

            validTerm &= IsValidTerm(flags);
        }

        if (validTerm)
        {
            expressionType = ExpressionType.ValidExpression;
        }
        else
        {
            expressionType = ExpressionType.Invalid;
        }
    }

    private static void AddInvocationExpressionTerms(InvocationExpressionSyntax invocationExpression, IList<string> terms, ref ExpressionType expressionType)
    {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
        // Invocations definitely have side effects.  So we assume this
        // is invalid initially;
        expressionType = ExpressionType.Invalid;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
        ExpressionType leftFlags = ExpressionType.Invalid, rightFlags = ExpressionType.Invalid;

        AddSubExpressionTerms(invocationExpression.Expression, terms, ref leftFlags);
        AddArgumentTerms(invocationExpression.ArgumentList, terms, ref rightFlags);

        AddIfValidTerm(invocationExpression.Expression, leftFlags, terms);

        // We're valid if both children are...
        expressionType = (leftFlags & rightFlags) & ExpressionType.ValidExpression;
    }

    private static void AddPrefixUnaryExpressionTerms(PrefixUnaryExpressionSyntax prefixUnaryExpression, IList<string> terms, ref ExpressionType expressionType)
    {
        expressionType = ExpressionType.Invalid;
        var flags = ExpressionType.Invalid;

        // Ask our subexpression for terms
        AddSubExpressionTerms(prefixUnaryExpression.Operand, terms, ref flags);

        // Is our expression a valid term?
        AddIfValidTerm(prefixUnaryExpression.Operand, flags, terms);

        if (prefixUnaryExpression.Kind() is SyntaxKind.LogicalNotExpression or SyntaxKind.BitwiseNotExpression or SyntaxKind.UnaryMinusExpression or SyntaxKind.UnaryPlusExpression)
        {
            // We're a valid expression if our subexpression is...
            expressionType = flags & ExpressionType.ValidExpression;
        }
    }

    private static void AddAwaitExpressionTerms(AwaitExpressionSyntax awaitExpression, IList<string> terms, ref ExpressionType expressionType)
    {
        expressionType = ExpressionType.Invalid;
        var flags = ExpressionType.Invalid;

        // Ask our subexpression for terms
        AddSubExpressionTerms(awaitExpression.Expression, terms, ref flags);

        // Is our expression a valid term?
        AddIfValidTerm(awaitExpression.Expression, flags, terms);
    }

    private static void AddPostfixUnaryExpressionTerms(PostfixUnaryExpressionSyntax postfixUnaryExpression, IList<string> terms, ref ExpressionType expressionType)
    {
        // ++ and -- are the only postfix operators.  Since they always have side
        // effects, we never consider this an expression.
        expressionType = ExpressionType.Invalid;

        var flags = ExpressionType.Invalid;

        // Ask our subexpression for terms
        AddSubExpressionTerms(postfixUnaryExpression.Operand, terms, ref flags);

        // Is our expression a valid term?
        AddIfValidTerm(postfixUnaryExpression.Operand, flags, terms);
    }

    private static void AddConditionalExpressionTerms(ConditionalExpressionSyntax conditionalExpression, IList<string> terms, ref ExpressionType expressionType)
    {
        ExpressionType conditionFlags = ExpressionType.Invalid, trueFlags = ExpressionType.Invalid, falseFlags = ExpressionType.Invalid;

        AddSubExpressionTerms(conditionalExpression.Condition, terms, ref conditionFlags);
        AddSubExpressionTerms(conditionalExpression.WhenTrue, terms, ref trueFlags);
        AddSubExpressionTerms(conditionalExpression.WhenFalse, terms, ref falseFlags);

        AddIfValidTerm(conditionalExpression.Condition, conditionFlags, terms);
        AddIfValidTerm(conditionalExpression.WhenTrue, trueFlags, terms);
        AddIfValidTerm(conditionalExpression.WhenFalse, falseFlags, terms);

        // We're valid if all children are...
        expressionType = (conditionFlags & trueFlags & falseFlags) & ExpressionType.ValidExpression;
    }

    private static void AddBinaryExpressionTerms(ExpressionSyntax binaryExpression, ExpressionSyntax left, ExpressionSyntax right, IList<string> terms, ref ExpressionType expressionType)
    {
        ExpressionType leftFlags = ExpressionType.Invalid, rightFlags = ExpressionType.Invalid;

        AddSubExpressionTerms(left, terms, ref leftFlags);
        AddSubExpressionTerms(right, terms, ref rightFlags);

        if (IsValidTerm(leftFlags))
        {
            terms.Add(ConvertToString(left));
        }

        if (IsValidTerm(rightFlags))
        {
            terms.Add(ConvertToString(right));
        }

        // Many sorts of binops (like +=) will definitely have side effects.  We only
        // consider this valid if it's a simple expression like +, -, etc.

        switch (binaryExpression.Kind())
        {
            case SyntaxKind.AddExpression:
            case SyntaxKind.SubtractExpression:
            case SyntaxKind.MultiplyExpression:
            case SyntaxKind.DivideExpression:
            case SyntaxKind.ModuloExpression:
            case SyntaxKind.LeftShiftExpression:
            case SyntaxKind.RightShiftExpression:
            case SyntaxKind.LogicalOrExpression:
            case SyntaxKind.LogicalAndExpression:
            case SyntaxKind.BitwiseOrExpression:
            case SyntaxKind.BitwiseAndExpression:
            case SyntaxKind.ExclusiveOrExpression:
            case SyntaxKind.EqualsExpression:
            case SyntaxKind.NotEqualsExpression:
            case SyntaxKind.LessThanExpression:
            case SyntaxKind.LessThanOrEqualExpression:
            case SyntaxKind.GreaterThanExpression:
            case SyntaxKind.GreaterThanOrEqualExpression:
            case SyntaxKind.IsExpression:
            case SyntaxKind.AsExpression:
            case SyntaxKind.CoalesceExpression:
                // We're valid if both children are...
                expressionType = (leftFlags & rightFlags) & ExpressionType.ValidExpression;
                return;

            default:
                expressionType = ExpressionType.Invalid;
                return;
        }
    }

    private static void AddArgumentTerms(ArgumentListSyntax argumentList, IList<string> terms, ref ExpressionType expressionType)
    {
        var validExpr = true;
        var validTerm = true;

        // Process the list of expressions.  This is probably a list of
        // arguments to a function call(or a list of array index expressions)
        foreach (var arg in argumentList.Arguments)
        {
            var flags = ExpressionType.Invalid;

            AddSubExpressionTerms(arg.Expression, terms, ref flags);
            if (IsValidTerm(flags))
            {
                terms.Add(ConvertToString(arg.Expression));
            }

            validExpr &= IsValidExpression(flags);
            validTerm &= IsValidTerm(flags);
        }

        // We're never a valid term if all arguments were valid terms.  If not, we're a valid
        // expression if all arguments where.  Otherwise, we're just invalid.
        expressionType = validTerm
            ? ExpressionType.ValidTerm
            : validExpr
                ? ExpressionType.ValidExpression : ExpressionType.Invalid;
    }
}
