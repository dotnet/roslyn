using System.Collections.Generic;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.CSharp.Utilities;
using Roslyn.Services.Internal.Extensions;

namespace Roslyn.Services.CSharp.Debugging
{
    internal partial class ProximityExpressionsGetter
    {
        private static string ConvertToString(ExpressionSyntax expression)
        {
            // TODO(cyrusn): Should we strip out comments?
            return expression.GetFullText();
        }

        private static void CollectExpressionTerms(int position, ExpressionSyntax expression, List<string> terms)
        {
            // Check here rather than at all the call sites...
            if (expression == null)
            {
                return;
            }

            // Collect terms from this expression, which returns flags indicating the validity
            // of this expression as a whole.
            var expressionType = ExpressionType.Invalid;
            CollectExpressionTerms(position, expression, terms, ref expressionType);

            if ((expressionType & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            {
                // If this expression identified itself as a valid term, add it to the
                // term table
                terms.Add(ConvertToString(expression));
            }
        }

        private static void CollectExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        {
            // Check here rather than at all the call sites...
            if (expression == null)
            {
                return;
            }

            switch (expression.Kind)
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
                    // For a cast, just add the nested expression.  Note: this is technically
                    // unsafe as the cast *may* have side effects.  However, in practice this is
                    // extremely rare, so we allow for this since it's ok in the common case.
                    CollectExpressionTerms(position, ((CastExpressionSyntax)expression).Expression, terms, ref expressionType);
                    return;

                case SyntaxKind.MemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    CollectMemberAccessExpressionTerms(position, expression, terms, ref expressionType);
                    return;

                case SyntaxKind.ObjectCreationExpression:
                    CollectObjectCreationExpressionTerms(position, expression, terms, ref expressionType);
                    return;

                case SyntaxKind.ArrayCreationExpression:
                    CollectArrayCreationExpressionTerms(position, expression, terms, ref expressionType);
                    return;

                case SyntaxKind.InvocationExpression:
                    CollectInvocationExpressionTerms(position, expression, terms, ref expressionType);
                    return;
            }

            // +, -, ++, --, !, etc.
            //
            // This is a valid expression if it doesn't have obvious side effects (i.e. ++, --)
            if (expression is PrefixUnaryExpressionSyntax)
            {
                CollectPrefixUnaryExpressionTerms(position, expression, terms, ref expressionType);
                return;
            }

            if (expression is PostfixUnaryExpressionSyntax)
            {
                CollectPostfixUnaryExpressionTerms(position, expression, terms, ref expressionType);
                return;
            }

            if (expression is BinaryExpressionSyntax)
            {
                CollectBinaryExpressionTerms(position, expression, terms, ref expressionType);
                return;
            }

            expressionType = ExpressionType.Invalid;
        }

        private static void CollectMemberAccessExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        {
            var flags = ExpressionType.Invalid;

            // These operators always have a RHS of a name node, which we know would
            // "claim" to be a valid term, but is not valid without the LHS present.
            // So, we don't bother collecting anything from the RHS...
            var memberAccess = (MemberAccessExpressionSyntax)expression;
            CollectExpressionTerms(position, memberAccess.Expression, terms, ref flags);

            // If the LHS says it's a valid term, then we add it ONLY if our PARENT
            // is NOT another dot/arrow.  This allows the expression 'a.b.c.d' to
            // add both 'a.b.c.d' and 'a.b.c', but not 'a.b' and 'a'.
            if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm &&
                !expression.IsParentKind(SyntaxKind.MemberAccessExpression) &&
                !expression.IsParentKind(SyntaxKind.PointerMemberAccessExpression))
            {
                terms.Add(ConvertToString(memberAccess.Expression));
            }

            // And this expression itself is a valid term if the LHS is a valid
            // expression, and its PARENT is not an invocation.
            if ((flags & ExpressionType.ValidExpression) == ExpressionType.ValidExpression &&
                !expression.IsParentKind(SyntaxKind.InvocationExpression))
            {
                expressionType = ExpressionType.ValidTerm;
            }
            else
            {
                expressionType = ExpressionType.ValidExpression;
            }
        }

        private static void CollectObjectCreationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        {
            // Object creation can *definitely* cause side effects.  So we initially
            // mark this as something invalid.  We allow it as a valid expr if all
            // the sub arguments are valid terms.
            expressionType = ExpressionType.Invalid;

            var objectionCreation = (ObjectCreationExpressionSyntax)expression;
            if (objectionCreation.ArgumentListOpt != null)
            {
                var flags = ExpressionType.Invalid;
                CollectArgumentTerms(position, objectionCreation.ArgumentListOpt, terms, ref flags);

                // If all arguments are terms, then this is possibly a valid expr
                // that can be used somewhere higher in the stack.
                if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
                {
                    expressionType = ExpressionType.ValidExpression;
                }
            }
        }

        private static void CollectArrayCreationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        {
            var validTerm = true;
            var arrayCreation = (ArrayCreationExpressionSyntax)expression;

            if (arrayCreation.InitializerOpt != null)
            {
                var flags = ExpressionType.Invalid;
                arrayCreation.InitializerOpt.Expressions.Do(e => CollectExpressionTerms(position, e, terms, ref flags));

                validTerm &= (flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm;
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

        private static void CollectInvocationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        {
            // Invocations definitely have side effects.  So we assume this
            // is invalid initially
            expressionType = ExpressionType.Invalid;
            ExpressionType leftFlags = ExpressionType.Invalid, rightFlags = ExpressionType.Invalid;

            var invocation = (InvocationExpressionSyntax)expression;
            CollectExpressionTerms(position, invocation.Expression, terms, ref leftFlags);
            CollectArgumentTerms(position, invocation.ArgumentList, terms, ref rightFlags);

            if ((leftFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            {
                terms.Add(ConvertToString(invocation.Expression));
            }

            // We're valid if both children are...
            expressionType = (leftFlags & rightFlags) & ExpressionType.ValidExpression;
        }

        private static void CollectPrefixUnaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        {
            expressionType = ExpressionType.Invalid;
            var flags = ExpressionType.Invalid;
            var prefixUnaryExpression = (PrefixUnaryExpressionSyntax)expression;

            // Ask our subexpression for terms
            CollectExpressionTerms(position, prefixUnaryExpression.Operand, terms, ref flags);

            // Is our expression a valid term?
            if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            {
                terms.Add(ConvertToString(prefixUnaryExpression.Operand));
            }

            if (expression.MatchesKind(SyntaxKind.LogicalNotExpression, SyntaxKind.BitwiseNotExpression, SyntaxKind.NegateExpression, SyntaxKind.PlusExpression))
            {
                // We're a valid expression if our subexpression is...
                expressionType = flags & ExpressionType.ValidExpression;
            }
        }

        private static void CollectPostfixUnaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        {
            // ++ and -- are the only postfix operators.  Since they always have side
            // effects, we never consider this an expression.
            expressionType = ExpressionType.Invalid;

            var flags = ExpressionType.Invalid;
            var postfixUnaryExpression = (PostfixUnaryExpressionSyntax)expression;

            // Ask our subexpression for terms
            CollectExpressionTerms(position, postfixUnaryExpression.Operand, terms, ref flags);

            // Is our expression a valid term?
            if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            {
                terms.Add(ConvertToString(postfixUnaryExpression.Operand));
            }
        }

        private static void CollectBinaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        {
            ExpressionType leftFlags = ExpressionType.Invalid, rightFlags = ExpressionType.Invalid;

            var binaryExpression = (BinaryExpressionSyntax)expression;
            CollectExpressionTerms(position, binaryExpression.Left, terms, ref leftFlags);
            CollectExpressionTerms(position, binaryExpression.Right, terms, ref rightFlags);

            if ((leftFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            {
                terms.Add(ConvertToString(binaryExpression.Left));
            }

            if ((rightFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            {
                terms.Add(ConvertToString(binaryExpression.Right));
            }

            // Many sorts of binops (like +=) will definitely have side effects.  We only
            // consider this valid if it's a simple expression like +, -, etc.

            switch (binaryExpression.Kind)
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

        private static void CollectArgumentTerms(int position, ArgumentListSyntax argumentList, IList<string> terms, ref ExpressionType expressionType)
        {
            var validExpr = true;

            // Process the list of expressions.  This is probably a list of
            // arguments to a function call(or a list of array index expressions)
            foreach (var arg in argumentList.Arguments)
            {
                var flags = ExpressionType.Invalid;

                CollectExpressionTerms(position, arg.Expression, terms, ref flags);
                if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
                {
                    terms.Add(ConvertToString(arg.Expression));
                }

                validExpr &= (flags & ExpressionType.ValidExpression) == ExpressionType.ValidExpression;
            }

            // We're never a valid term, but we're a valid expression if all
            // the list elements are...
            expressionType = validExpr ? ExpressionType.ValidExpression : 0;
        }

        private static void CollectVariableTerms(int position, SeparatedSyntaxList<VariableDeclaratorSyntax> declarators, List<string> terms)
        {
            foreach (var declarator in declarators)
            {
                if (declarator.InitializerOpt != null)
                {
                    CollectExpressionTerms(position, declarator.InitializerOpt.Value, terms);
                }
            }
        }
    }
}
