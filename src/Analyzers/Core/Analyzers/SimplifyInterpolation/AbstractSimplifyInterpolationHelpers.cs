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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SimplifyInterpolation;

internal abstract class AbstractSimplifyInterpolationHelpers<
    TInterpolationSyntax,
    TExpressionSyntax>
    where TInterpolationSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    protected abstract bool PermitNonLiteralAlignmentComponents { get; }

    protected abstract SyntaxNode GetPreservedInterpolationExpressionSyntax(IOperation operation);

    public ImmutableDictionary<IMethodSymbol, string> BuildKnownToStringFormatsLookupTable(Compilation compilation)
    {
        using var _ = PooledDictionary<IMethodSymbol, string>.GetInstance(out var builder);

        var dateTimeType = compilation.GetSpecialType(SpecialType.System_DateTime);
        AddDateMethods(dateTimeType);
        AddTimeMethods(dateTimeType);
        AddDateMethods(compilation.DateOnlyType());
        AddTimeMethods(compilation.TimeOnlyType());

        return builder.ToImmutableDictionary(SymbolEqualityComparer.Default);

        void AddDateMethods(INamedTypeSymbol? dateType)
        {
            AddMethodIfAvailable(dateType, nameof(DateTime.ToLongDateString), "D");
            AddMethodIfAvailable(dateType, nameof(DateTime.ToShortDateString), "d");
        }

        void AddTimeMethods(INamedTypeSymbol? timeType)
        {
            AddMethodIfAvailable(timeType, nameof(DateTime.ToLongTimeString), "T");
            AddMethodIfAvailable(timeType, nameof(DateTime.ToShortTimeString), "t");
        }

        void AddMethodIfAvailable(INamedTypeSymbol? type, string name, string format)
        {
            var member = type?.GetMembers(name).FirstOrDefault(m => m is IMethodSymbol { IsStatic: false, Parameters.Length: 0 });
            if (member is IMethodSymbol method)
                builder.Add(method, format);
        }
    }

    public void UnwrapInterpolation(
        IVirtualCharService virtualCharService,
        ISyntaxFacts syntaxFacts,
        IInterpolationOperation interpolation,
        ImmutableDictionary<IMethodSymbol, string> knownToStringFormats,
        INamedTypeSymbol? readOnlySpanOfCharType,
        bool handlersAvailable,
        out TExpressionSyntax? unwrapped,
        out TExpressionSyntax? alignment,
        out bool negate,
        out string? formatString,
        out ImmutableArray<Location> unnecessaryLocations)
    {
        alignment = null;
        negate = false;
        formatString = null;

        using var _ = ArrayBuilder<TextSpan>.GetInstance(out var unnecessarySpans);

        var expression = Unwrap(interpolation.Expression);
        if (interpolation.Alignment == null)
            UnwrapAlignmentPadding(expression, out expression, out alignment, out negate, unnecessarySpans);

        if (interpolation.FormatString == null)
            UnwrapFormatString(virtualCharService, syntaxFacts, expression, knownToStringFormats, readOnlySpanOfCharType, handlersAvailable, out expression, out formatString, unnecessarySpans);

        unwrapped = GetPreservedInterpolationExpressionSyntax(expression) as TExpressionSyntax;

        unnecessaryLocations = unnecessarySpans
            .OrderBy(t => t.Start)
            .SelectAsArray(interpolation.Syntax.SyntaxTree.GetLocation);
    }

    [return: NotNullIfNotNull(nameof(expression))]
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
        IVirtualCharService virtualCharService,
        ISyntaxFacts syntaxFacts,
        IOperation expression,
        ImmutableDictionary<IMethodSymbol, string> knownToStringFormats,
        INamedTypeSymbol? readOnlySpanOfCharType,
        bool handlersAvailable,
        out IOperation unwrapped,
        out string? formatString,
        ArrayBuilder<TextSpan> unnecessarySpans)
    {
        Contract.ThrowIfNull(expression.SemanticModel);

        if (expression is IInvocationOperation { TargetMethod: { } targetMethod } invocation &&
            HasNonImplicitInstance(invocation, out var instance) &&
            !syntaxFacts.IsBaseExpression(instance.Syntax))
        {
            if (targetMethod.Name == nameof(ToString))
            {
                // If type of instance is not ref-like type or is {ReadOnly}Span<char> that is allowed in interpolated strings in .NET 6+
                if (instance.Type is { IsRefLikeType: false } || IsRefLikeTypeAllowed(instance.Type))
                {
                    if (invocation.Arguments.Length == 1
                        || (invocation.Arguments.Length == 2 && UsesInvariantCultureReferenceInsideFormattableStringInvariant(invocation, formatProviderArgumentIndex: 1)))
                    {
                        if (invocation.Arguments[0].Value is ILiteralOperation { ConstantValue: { HasValue: true, Value: string value } } literal &&
                            FindType<IFormattable>(expression.SemanticModel) is { } systemIFormattable &&
                            instance.Type.Implements(systemIFormattable))
                        {
                            unwrapped = instance;
                            formatString = value;

                            unnecessarySpans.AddRange(invocation.Syntax.Span
                                .Subtract(GetPreservedInterpolationExpressionSyntax(instance).FullSpan)
                                .Subtract(GetSpanWithinLiteralQuotes(virtualCharService, literal.Syntax.GetFirstToken())));
                            return;
                        }
                    }

                    if (IsObjectToStringOverride(invocation.TargetMethod)
                        || (invocation.Arguments.Length == 1 && UsesInvariantCultureReferenceInsideFormattableStringInvariant(invocation, formatProviderArgumentIndex: 0)))
                    {
                        // A call to `.ToString()` at the end of the interpolation.  This is unnecessary.
                        // Just remove entirely.
                        unwrapped = instance;
                        formatString = "";

                        unnecessarySpans.AddRange(invocation.Syntax.Span
                            .Subtract(GetPreservedInterpolationExpressionSyntax(instance).FullSpan));
                        return;
                    }
                }
            }
            else if (knownToStringFormats.TryGetValue(targetMethod, out var format))
            {
                // A call to a known ToString-like method, e.g. `DateTime.ToLongDateString()`
                // We replace this call with predefined format specifier
                unwrapped = instance;
                formatString = format;

                unnecessarySpans.AddRange(invocation.Syntax.Span
                    .Subtract(GetPreservedInterpolationExpressionSyntax(instance).FullSpan));
                return;
            }
        }

        unwrapped = expression;
        formatString = null;

        bool IsRefLikeTypeAllowed([NotNullWhen(true)] ITypeSymbol? type)
        {
            var compilation = expression.SemanticModel.Compilation;
            // {ReadOnly}Span<char> is allowed if interpolated string handlers are available in the compilation (.NET 6+)
            return handlersAvailable && compilation.HasImplicitConversion(type, readOnlySpanOfCharType);
        }
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
                return IsType<CultureInfo>(member.ContainingType, operation.SemanticModel);

            if (member.Name == "InvariantInfo")
            {
                return IsType<NumberFormatInfo>(member.ContainingType, operation.SemanticModel)
                    || IsType<DateTimeFormatInfo>(member.ContainingType, operation.SemanticModel);
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
        } && IsType<FormattableString>(containingType, operation.SemanticModel);
    }

    private static bool IsType<T>(INamedTypeSymbol type, SemanticModel semanticModel)
        => SymbolEqualityComparer.Default.Equals(type, FindType<T>(semanticModel));

    private static INamedTypeSymbol? FindType<T>(SemanticModel semanticModel)
        => semanticModel.Compilation.GetTypeByMetadataName(typeof(T).FullName!);

    private static IEnumerable<IOperation> AncestorsAndSelf(IOperation operation)
    {
        for (var current = operation; current is not null; current = current.Parent)
            yield return current;
    }

    private static TextSpan GetSpanWithinLiteralQuotes(IVirtualCharService virtualCharService, SyntaxToken formatToken)
    {
        var sequence = virtualCharService.TryConvertToVirtualChars(formatToken);
        return sequence.IsDefaultOrEmpty()
            ? default
            : TextSpan.FromBounds(sequence[0].Span.Start, sequence[^1].Span.End);
    }

    private void UnwrapAlignmentPadding(
        IOperation expression,
        out IOperation unwrapped,
        out TExpressionSyntax? alignment,
        out bool negate,
        ArrayBuilder<TextSpan> unnecessarySpans)
    {
        if (expression is IInvocationOperation invocation &&
            HasNonImplicitInstance(invocation, out var instance))
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

                            unwrapped = instance;
                            alignment = alignmentSyntax as TExpressionSyntax;
                            negate = targetName == nameof(string.PadRight);

                            unnecessarySpans.AddRange(invocation.Syntax.Span
                                .Subtract(GetPreservedInterpolationExpressionSyntax(instance).FullSpan)
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

    private static bool HasNonImplicitInstance(IInvocationOperation invocation, [NotNullWhen(true)] out IOperation? instance)
    {
        if (invocation.Instance is { IsImplicit: false })
        {
            instance = invocation.Instance;
            return true;
        }

        instance = null;
        return false;
    }

    private static bool IsSpaceChar(IArgumentOperation argument)
        => argument.Value.ConstantValue is { HasValue: true, Value: ' ' };
}
