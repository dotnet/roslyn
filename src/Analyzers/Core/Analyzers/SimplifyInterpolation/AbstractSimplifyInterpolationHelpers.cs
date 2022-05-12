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
    internal abstract class AbstractSimplifyInterpolationHelpers
    {
        protected abstract bool PermitNonLiteralAlignmentComponents { get; }

        protected abstract SyntaxNode GetPreservedInterpolationExpressionSyntax(IOperation operation);

        public void UnwrapInterpolation<TInterpolationSyntax, TExpressionSyntax>(
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

            unwrapped = GetPreservedInterpolationExpressionSyntax(expression) as TExpressionSyntax;

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

        private void UnwrapFormatString(
            IVirtualCharService virtualCharService, ISyntaxFacts syntaxFacts, IOperation expression, out IOperation unwrapped,
            out string? formatString, List<TextSpan> unnecessarySpans)
        {
            Contract.ThrowIfNull(expression.SemanticModel);

            if (expression is IInvocationOperation { TargetMethod.Name: nameof(ToString) } invocation &&
                HasNonImplicitInstance(invocation) &&
                !syntaxFacts.IsBaseExpression(invocation.Instance!.Syntax) &&
                !invocation.Instance.Type!.IsRefLikeType)
            {
                if (invocation.Arguments.Length == 1
                    || (invocation.Arguments.Length == 2 && UsesInvariantCultureReferenceInsideFormattableStringInvariant(invocation, formatProviderArgumentIndex: 1)))
                {
                    if (invocation.Arguments[0].Value is ILiteralOperation { ConstantValue: { HasValue: true, Value: string value } } literal &&
                        FindType<System.IFormattable>(expression.SemanticModel) is { } systemIFormattable &&
                        invocation.Instance.Type.Implements(systemIFormattable))
                    {
                        unwrapped = invocation.Instance;
                        formatString = value;

                        unnecessarySpans.AddRange(invocation.Syntax.Span
                            .Subtract(GetPreservedInterpolationExpressionSyntax(invocation.Instance).FullSpan)
                            .Subtract(GetSpanWithinLiteralQuotes(virtualCharService, literal.Syntax.GetFirstToken())));
                        return;
                    }
                }

                if (IsObjectToStringOverride(invocation.TargetMethod)
                    || (invocation.Arguments.Length == 1 && UsesInvariantCultureReferenceInsideFormattableStringInvariant(invocation, formatProviderArgumentIndex: 0)))
                {
                    // A call to `.ToString()` at the end of the interpolation.  This is unnecessary.
                    // Just remove entirely.
                    unwrapped = invocation.Instance;
                    formatString = "";

                    unnecessarySpans.AddRange(invocation.Syntax.Span
                        .Subtract(GetPreservedInterpolationExpressionSyntax(invocation.Instance).FullSpan));
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

        private static bool UsesInvariantCultureReferenceInsideFormattableStringInvariant(IInvocationOperation invocation, int formatProviderArgumentIndex)
        {
            return IsInvariantCultureReference(invocation.Arguments[formatProviderArgumentIndex].Value)
                && IsInsideFormattableStringInvariant(invocation);
        }

        private static bool IsInvariantCultureReference(IOperation operation)
        {
            Contract.ThrowIfNull(operation.SemanticModel);

            if (Unwrap(operation) is IPropertyReferenceOperation { Member: { } member })
            {
                if (member.Name == nameof(CultureInfo.InvariantCulture))
                {
                    return IsType<System.Globalization.CultureInfo>(member.ContainingType, operation.SemanticModel);
                }

                if (member.Name == "InvariantInfo")
                {
                    return IsType<System.Globalization.NumberFormatInfo>(member.ContainingType, operation.SemanticModel)
                        || IsType<System.Globalization.DateTimeFormatInfo>(member.ContainingType, operation.SemanticModel);
                }
            }

            return false;
        }

        private static bool IsInsideFormattableStringInvariant(IOperation operation)
        {
            Contract.ThrowIfNull(operation.SemanticModel);

            var interpolatedStringOperation = AncestorsAndSelf(operation).OfType<IInterpolatedStringOperation>().FirstOrDefault();

            return Unwrap(interpolatedStringOperation?.Parent, towardsParent: true) is IArgumentOperation
            {
                Parent: IInvocationOperation
                {
                    TargetMethod: { Name: nameof(FormattableString.Invariant), ContainingType: var containingType },
                },
            } && IsType<System.FormattableString>(containingType, operation.SemanticModel);
        }

        private static bool IsType<T>(INamedTypeSymbol type, SemanticModel semanticModel)
        {
            return SymbolEqualityComparer.Default.Equals(type, FindType<T>(semanticModel));
        }

        private static INamedTypeSymbol? FindType<T>(SemanticModel semanticModel)
        {
            return semanticModel.Compilation.GetTypeByMetadataName(typeof(T).FullName!);
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

        private void UnwrapAlignmentPadding<TExpressionSyntax>(
            IOperation expression, out IOperation unwrapped,
            out TExpressionSyntax? alignment, out bool negate, List<TextSpan> unnecessarySpans)
            where TExpressionSyntax : SyntaxNode
        {
            if (expression is IInvocationOperation invocation &&
                HasNonImplicitInstance(invocation))
            {
                var targetName = invocation.TargetMethod.Name;
                if (targetName is nameof(string.PadLeft) or nameof(string.PadRight))
                {
                    var argCount = invocation.Arguments.Length;
                    if (argCount is 1 or 2)
                    {
                        if (argCount == 1 ||
                            IsSpaceChar(invocation.Arguments[1]))
                        {
                            var alignmentOp = invocation.Arguments[0].Value;

                            if (PermitNonLiteralAlignmentComponents
                                ? alignmentOp is { ConstantValue.HasValue: true }
                                : alignmentOp is { Kind: OperationKind.Literal })
                            {
                                var alignmentSyntax = alignmentOp.Syntax;

                                unwrapped = invocation.Instance!;
                                alignment = alignmentSyntax as TExpressionSyntax;
                                negate = targetName == nameof(string.PadRight);

                                unnecessarySpans.AddRange(invocation.Syntax.Span
                                    .Subtract(GetPreservedInterpolationExpressionSyntax(invocation.Instance!).FullSpan)
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
