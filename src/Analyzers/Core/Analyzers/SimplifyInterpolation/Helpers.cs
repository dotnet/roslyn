// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
        private static SyntaxNode GetPreservedInterpolationExpressionSyntax<TConditionalExpressionSyntax, TParenthesizedExpressionSyntax>(
            IOperation operation)
            where TConditionalExpressionSyntax : SyntaxNode
            where TParenthesizedExpressionSyntax : SyntaxNode
        {
            return operation.Syntax switch
            {
                TConditionalExpressionSyntax { Parent: TParenthesizedExpressionSyntax parent } => parent,
                var syntax => syntax,
            };
        }

        public static void UnwrapInterpolation<TInterpolationSyntax, TExpressionSyntax, TConditionalExpressionSyntax, TParenthesizedExpressionSyntax>(
            IVirtualCharService virtualCharService, ISyntaxFacts syntaxFacts, IInterpolationOperation interpolation,
            out TExpressionSyntax? unwrapped, out TExpressionSyntax? alignment, out bool negate,
            out string? formatString, out ImmutableArray<Location> unnecessaryLocations)
            where TInterpolationSyntax : SyntaxNode
            where TExpressionSyntax : SyntaxNode
            where TConditionalExpressionSyntax : TExpressionSyntax
            where TParenthesizedExpressionSyntax : TExpressionSyntax
        {
            alignment = null;
            negate = false;
            formatString = null;

            var unnecessarySpans = new List<TextSpan>();

            var expression = Unwrap(interpolation.Expression);
            if (interpolation.Alignment == null)
            {
                UnwrapAlignmentPadding<TExpressionSyntax, TConditionalExpressionSyntax, TParenthesizedExpressionSyntax>(
                    expression, out expression, out alignment, out negate, unnecessarySpans);
            }

            if (interpolation.FormatString == null)
            {
                UnwrapFormatString<TConditionalExpressionSyntax, TParenthesizedExpressionSyntax>(
                    virtualCharService, syntaxFacts, expression, out expression, out formatString, unnecessarySpans);
            }

            unwrapped = GetPreservedInterpolationExpressionSyntax<TConditionalExpressionSyntax, TParenthesizedExpressionSyntax>(expression) as TExpressionSyntax;

            unnecessaryLocations =
                unnecessarySpans.OrderBy(t => t.Start)
                                .SelectAsArray(interpolation.Syntax.SyntaxTree.GetLocation);
        }

        [return: NotNullIfNotNull("expression")]
        private static IOperation? Unwrap(IOperation? expression, bool towardsParent = false)
        {
            while (true)
            {
                if (towardsParent && expression?.Parent is null)
                    return expression;

                switch (expression)
                {
                    case IParenthesizedOperation parenthesized:
                        expression = towardsParent ? expression.Parent : parenthesized.Operand;
                        continue;
                    case IConversionOperation { IsImplicit: true } conversion:
                        expression = towardsParent ? expression.Parent : conversion.Operand;
                        continue;
                    default:
                        return expression;
                }
            }
        }

        private static void UnwrapFormatString<TConditionalExpressionSyntax, TParenthesizedExpressionSyntax>(
            IVirtualCharService virtualCharService, ISyntaxFacts syntaxFacts, IOperation expression, out IOperation unwrapped,
            out string? formatString, List<TextSpan> unnecessarySpans)
            where TConditionalExpressionSyntax : SyntaxNode
            where TParenthesizedExpressionSyntax : SyntaxNode
        {
            if (expression is IInvocationOperation { TargetMethod: { Name: nameof(ToString) } } invocation &&
                HasNonImplicitInstance(invocation) &&
                !syntaxFacts.IsBaseExpression(invocation.Instance!.Syntax) &&
                !invocation.Instance.Type!.IsRefLikeType)
            {
                if (invocation.Arguments.Length == 1
                    || (invocation.Arguments.Length == 2 && IsInvariantCultureReference(invocation.Arguments[1].Value) && IsInsideFormattableStringInvariant(invocation)))
                {
                    if (invocation.Arguments[0].Value is ILiteralOperation { ConstantValue: { HasValue: true, Value: string value } } literal &&
                       invocation.SemanticModel!.Compilation.GetTypeByMetadataName(typeof(System.IFormattable).FullName!) is { } systemIFormattable &&
                       invocation.Instance.Type.Implements(systemIFormattable))
                    {
                        unwrapped = invocation.Instance;
                        formatString = value;

                        var unwrappedSyntax = GetPreservedInterpolationExpressionSyntax<TConditionalExpressionSyntax, TParenthesizedExpressionSyntax>(unwrapped);
                        unnecessarySpans.AddRange(invocation.Syntax.Span
                            .Subtract(unwrappedSyntax.FullSpan)
                            .Subtract(GetSpanWithinLiteralQuotes(virtualCharService, literal.Syntax.GetFirstToken())));
                        return;
                    }
                }

                if (IsObjectToStringOverride(invocation.TargetMethod)
                    || (invocation.Arguments.Length == 1 && IsInvariantCultureReference(invocation.Arguments[0].Value) && IsInsideFormattableStringInvariant(invocation)))
                {
                    // A call to `.ToString()` at the end of the interpolation.  This is unnecessary.
                    // Just remove entirely.
                    unwrapped = invocation.Instance;
                    formatString = "";

                    var unwrappedSyntax = GetPreservedInterpolationExpressionSyntax<TConditionalExpressionSyntax, TParenthesizedExpressionSyntax>(unwrapped);
                    unnecessarySpans.AddRange(invocation.Syntax.Span
                        .Subtract(unwrappedSyntax.FullSpan));
                    return;
                }
            }

            unwrapped = expression;
            formatString = null;
        }

        private static bool IsObjectToStringOverride(IMethodSymbol method)
        {
            while (method.OverriddenMethod is not null)
                method = method.OverriddenMethod;

            return method.ContainingType.SpecialType == SpecialType.System_Object
                && method.Name == nameof(ToString);
        }

        private static bool IsInvariantCultureReference(IOperation operation)
        {
            if (Unwrap(operation) is not IPropertyReferenceOperation { Member: { } member })
                return false;

            return member.Name switch
            {
                nameof(CultureInfo.InvariantCulture) => SymbolEqualityComparer.Default.Equals(
                    member.ContainingType,
                    operation.SemanticModel!.Compilation.GetTypeByMetadataName(typeof(System.Globalization.CultureInfo).FullName!)),

                "InvariantInfo" =>
                    SymbolEqualityComparer.Default.Equals(
                        member.ContainingType,
                        operation.SemanticModel!.Compilation.GetTypeByMetadataName(typeof(System.Globalization.NumberFormatInfo).FullName!))
                    || SymbolEqualityComparer.Default.Equals(
                        member.ContainingType,
                        operation.SemanticModel!.Compilation.GetTypeByMetadataName(typeof(System.Globalization.DateTimeFormatInfo).FullName!)),

                _ => false,
            };
        }

        private static bool IsInsideFormattableStringInvariant(IOperation operation)
        {
            var interpolatedStringOperation = AncestorsAndSelf(operation).OfType<IInterpolatedStringOperation>().FirstOrDefault();

            return Unwrap(interpolatedStringOperation?.Parent, towardsParent: true) is IArgumentOperation
            {
                Parent: IInvocationOperation
                {
                    TargetMethod: { Name: nameof(FormattableString.Invariant), ContainingType: var containingType },
                },
            } && SymbolEqualityComparer.Default.Equals(containingType, operation.SemanticModel!.Compilation.GetTypeByMetadataName(typeof(System.FormattableString).FullName!));
        }

        private static IEnumerable<IOperation> AncestorsAndSelf(IOperation operation)
        {
            for (var current = operation; current is not null; current = current.Parent)
            {
                yield return current;
            }
        }

        private static TextSpan GetSpanWithinLiteralQuotes(IVirtualCharService virtualCharService, SyntaxToken formatToken)
        {
            var sequence = virtualCharService.TryConvertToVirtualChars(formatToken);
            return sequence.IsDefaultOrEmpty
                ? default
                : TextSpan.FromBounds(sequence.First().Span.Start, sequence.Last().Span.End);
        }

        private static void UnwrapAlignmentPadding<TExpressionSyntax, TConditionalExpressionSyntax, TParenthesizedExpressionSyntax>(
            IOperation expression, out IOperation unwrapped,
            out TExpressionSyntax? alignment, out bool negate, List<TextSpan> unnecessarySpans)
            where TExpressionSyntax : SyntaxNode
            where TConditionalExpressionSyntax : TExpressionSyntax
            where TParenthesizedExpressionSyntax : TExpressionSyntax
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

                                unwrapped = invocation.Instance!;
                                alignment = alignmentSyntax as TExpressionSyntax;
                                negate = targetName == nameof(string.PadRight);

                                var unwrappedSyntax = GetPreservedInterpolationExpressionSyntax<TConditionalExpressionSyntax, TParenthesizedExpressionSyntax>(unwrapped);
                                unnecessarySpans.AddRange(invocation.Syntax.Span
                                    .Subtract(unwrappedSyntax.FullSpan)
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
