// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SimplifyInterpolation
{
    internal static class Helpers
    {
        public static void UnwrapInterpolation<TInterpolationSyntax, TExpressionSyntax>(
            IVirtualCharService virtualCharService, ISyntaxFacts syntaxFacts, IInterpolationOperation interpolation,
            out TExpressionSyntax? unwrapped, out TExpressionSyntax? alignment, out bool negate,
            out string? formatString, out ImmutableArray<Location> unnecessaryLocations)
                where TInterpolationSyntax : SyntaxNode
                where TExpressionSyntax : SyntaxNode
        {
            alignment = null;
            negate = false;
            formatString = null;

            var unnecessarySpans = new List<TextSpan>();

            var expression = Unwrap(interpolation.Expression);
            if (interpolation.Alignment == null)
            {
                UnwrapAlignmentPadding(expression, out expression, out alignment, out negate, unnecessarySpans);
            }

            if (interpolation.FormatString == null)
            {
                UnwrapFormatString(virtualCharService, syntaxFacts, expression, out expression, out formatString, unnecessarySpans);
            }

            unwrapped = expression.Syntax as TExpressionSyntax;

            unnecessaryLocations =
                unnecessarySpans.OrderBy(t => t.Start)
                                .SelectAsArray(interpolation.Syntax.SyntaxTree.GetLocation);
        }

        private static IOperation Unwrap(IOperation expression)
        {
            while (true)
            {
                switch (expression)
                {
                    case IParenthesizedOperation parenthesized:
                        expression = parenthesized.Operand;
                        continue;
                    case IConversionOperation { IsImplicit: true } conversion:
                        expression = conversion.Operand;
                        continue;
                    default:
                        return expression;
                }
            }
        }

        private static void UnwrapFormatString(
            IVirtualCharService virtualCharService, ISyntaxFacts syntaxFacts, IOperation expression, out IOperation unwrapped,
            out string? formatString, List<TextSpan> unnecessarySpans)
        {
            if (expression is IInvocationOperation { TargetMethod: { Name: nameof(ToString) } } invocation &&
                HasNonImplicitInstance(invocation) &&
                !syntaxFacts.IsBaseExpression(invocation.Instance.Syntax) &&
                !invocation.Instance.Type.IsRefLikeType)
            {
                if (invocation.Arguments.Length == 1 &&
                    invocation.Arguments[0].Value is ILiteralOperation { ConstantValue: { HasValue: true, Value: string value } } literal &&
                    invocation.SemanticModel.Compilation.GetTypeByMetadataName(typeof(System.IFormattable).FullName!) is { } systemIFormattable &&
                    invocation.Instance.Type.Implements(systemIFormattable))
                {
                    unwrapped = invocation.Instance;
                    formatString = value;

                    unnecessarySpans.AddRange(invocation.Syntax.Span
                        .Subtract(invocation.Instance.Syntax.FullSpan)
                        .Subtract(GetSpanWithinLiteralQuotes(virtualCharService, literal.Syntax.GetFirstToken())));
                    return;
                }

                var method = invocation.TargetMethod;
                while (method.OverriddenMethod != null)
                {
                    method = method.OverriddenMethod;
                }

                if (method.ContainingType.SpecialType == SpecialType.System_Object &&
                    method.Name == nameof(ToString))
                {
                    // A call to `.ToString()` at the end of the interpolation.  This is unnecessary.
                    // Just remove entirely.
                    unwrapped = invocation.Instance;
                    formatString = "";

                    unnecessarySpans.AddRange(invocation.Syntax.Span
                        .Subtract(invocation.Instance.Syntax.FullSpan));
                    return;
                }
            }

            unwrapped = expression;
            formatString = null;
        }

        private static TextSpan GetSpanWithinLiteralQuotes(IVirtualCharService virtualCharService, SyntaxToken formatToken)
        {
            var sequence = virtualCharService.TryConvertToVirtualChars(formatToken);
            return sequence.IsDefaultOrEmpty
                ? default
                : TextSpan.FromBounds(sequence.First().Span.Start, sequence.Last().Span.End);
        }

        private static void UnwrapAlignmentPadding<TExpressionSyntax>(
            IOperation expression, out IOperation unwrapped,
            out TExpressionSyntax? alignment, out bool negate, List<TextSpan> unnecessarySpans)
            where TExpressionSyntax : SyntaxNode
        {
            if (expression is IInvocationOperation invocation &&
                HasNonImplicitInstance(invocation))
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
                            var alignmentOp = invocation.Arguments[0].Value;
                            if (alignmentOp != null && alignmentOp.ConstantValue.HasValue)
                            {
                                var alignmentSyntax = alignmentOp.Syntax;

                                unwrapped = invocation.Instance;
                                alignment = alignmentSyntax as TExpressionSyntax;
                                negate = targetName == nameof(string.PadRight);

                                unnecessarySpans.AddRange(invocation.Syntax.Span
                                    .Subtract(invocation.Instance.Syntax.FullSpan)
                                    .Subtract(alignmentSyntax.FullSpan));
                                return;
                            }
                        }
                    }
                }
            }

            unwrapped = expression;
            alignment = null;
            negate = false;
        }

        private static bool HasNonImplicitInstance(IInvocationOperation invocation)
            => invocation.Instance != null && !invocation.Instance.IsImplicit;

        private static bool IsSpaceChar(IArgumentOperation argument)
            => argument.Value.ConstantValue is { HasValue: true, Value: ' ' };
    }
}
