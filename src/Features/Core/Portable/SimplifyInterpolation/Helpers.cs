// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.SimplifyInterpolation
{
    internal static class Helpers
    {
        public static void UnwrapInterpolation<TInterpolationSyntax, TExpressionSyntax>(
            IInterpolationOperation interpolation,
            out TExpressionSyntax unwrapped, out TExpressionSyntax alignment,
            out bool negate, out string formatString)
                where TInterpolationSyntax : SyntaxNode
                where TExpressionSyntax : SyntaxNode
        {
            alignment = null;
            negate = false;
            formatString = null;

            var expression = Unwrap(interpolation.Expression);
            if (interpolation.Alignment == null)
            {
                UnwrapAlignmentPadding(expression, out expression, out alignment, out negate);
            }

            if (interpolation.FormatString == null)
            {
                UnwrapFormatString(expression, out expression, out formatString);
            }

            unwrapped = expression.Syntax as TExpressionSyntax;
        }

        private static IOperation Unwrap(IOperation expression)
        {
            while (true)
            {
                if (expression is IParenthesizedOperation parenthesized)
                {
                    expression = parenthesized.Operand;
                    continue;
                }

                if (expression is IConversionOperation conversion &&
                    conversion.IsImplicit)
                {
                    expression = conversion.Operand;
                    continue;
                }

                return expression;
            }
        }

        private static void UnwrapFormatString(
            IOperation expression, out IOperation unwrapped, out string formatString)
        {
            unwrapped = null;
            formatString = null;

            if (expression is IInvocationOperation { TargetMethod: { Name: nameof(ToString) } } invocation)
            {
                if (invocation.Arguments.Length == 1 &&
                    invocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string format })
                {
                    unwrapped = invocation.Instance;
                    formatString = format;
                    return;
                }

                if (invocation.Arguments.Length == 0)
                {
                    unwrapped = invocation.Instance;
                    formatString = "";
                    return;
                }
            }
        }

        private static void UnwrapAlignmentPadding<TExpressionSyntax>(
            IOperation expression, out IOperation unwrapped, out TExpressionSyntax alignment, out bool negate)
            where TExpressionSyntax : SyntaxNode
        {
            if (expression is IInvocationOperation invocation)
            {
                var targetName = invocation.TargetMethod.Name;
                if (targetName == nameof(string.PadLeft) || targetName == nameof(string.PadRight))
                {
                    var argCount = invocation.Arguments.Length;
                    if (argCount == 1 || argCount == 2)
                    {
                        if (argCount == 1 ||
                            IsSpaceChar(invocation.Arguments[1]))
                        {
                            unwrapped = invocation.Instance;
                            alignment = invocation.Arguments[0].Value.Syntax as TExpressionSyntax;
                            negate = targetName == nameof(string.PadLeft);
                            return;
                        }
                    }
                }
            }

            unwrapped = null;
            alignment = null;
            negate = false;
        }

        private static bool IsSpaceChar(IArgumentOperation argument)
            => argument.Value.ConstantValue is { HasValue: true, Value: ' ' };
    }
}
