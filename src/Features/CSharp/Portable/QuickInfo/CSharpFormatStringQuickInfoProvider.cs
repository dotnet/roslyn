// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

[ExportQuickInfoProvider(nameof(CSharpFormatStringQuickInfoProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(Before = QuickInfoProviderNames.Semantic)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpFormatStringQuickInfoProvider() : CommonQuickInfoProvider
{
    internal const int LiteralLimit = 32768;
    internal const int RequestLiteralLimit = 131072;
    internal const int CandidateLimit = 2048;

    internal static bool IsFormatCandidate(SyntaxToken token)
        => token.Parent is InterpolationFormatClauseSyntax clause && clause.FormatStringToken.Span.Length <= LiteralLimit ||
            (token.Span.Length <= LiteralLimit && token.Parent is LiteralExpressionSyntax candidate && candidate.IsKind(SyntaxKind.StringLiteralExpression) &&
             candidate.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax invocation } &&
             invocation.Expression.GetLastToken().ValueText is "ToString" or "TryFormat" or "ParseExact" or "TryParseExact" or "Format" or "AppendFormat" or "Write" or "WriteLine");

    protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(QuickInfoContext context, SyntaxToken token)
    {
        // Reject ordinary literals before binding. Most string hovers are not format strings.
        if (!IsFormatCandidate(token))
            return null;
        var semanticModel = await context.Document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        CSharpFormatStringValueEvaluator? evaluator = null;
        try
        {
            return TryGetFormatInfo(semanticModel, token, context.Position, ref evaluator, context.CancellationToken, out var span, out var format, out var info, out var isNullable)
                ? CreateQuickInfo(span, format, info, isNullable) : null;
        }
        finally
        {
            evaluator?.Dispose();
        }
    }

    internal static bool TryGetFormatInfo(SemanticModel semanticModel, SyntaxToken token, int position,
        ref CSharpFormatStringValueEvaluator? evaluator, CancellationToken cancellationToken,
        out TextSpan span, out string format, out CSharpFormatStringInfo info, out bool isNullable, CompositeFormatPart? compositePart = null)
    {
        span = default;
        format = "";
        info = default;
        isNullable = false;
        ITypeSymbol? type;
        CSharpFormatStringValue sourceValue = default;
        CSharpFormatStringContext context = default;

        if (token.Parent is InterpolationFormatClauseSyntax clause &&
            clause.Parent is InterpolationSyntax interpolation &&
            interpolation.Parent is InterpolatedStringExpressionSyntax interpolatedString)
        {
            // String.Create uses the framework handler with an explicit provider. Other handlers
            // and deferred formatting can assign different meanings to the format.
            var convertedType = semanticModel.GetTypeInfo(interpolatedString, cancellationToken).ConvertedType;
            if (interpolatedString.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax invocation } handlerArgument &&
                invocation.Expression.GetLastToken().ValueText == "Create" &&
                semanticModel.GetOperation(handlerArgument, cancellationToken) is IArgumentOperation
                { Parameter.Name: "handler", Parent: IInvocationOperation { TargetMethod.ContainingType.SpecialType: SpecialType.System_String } handlerInvocation })
            {
                foreach (var item in handlerInvocation.Arguments)
                {
                    if (item.Parameter?.Name == "provider")
                    {
                        if (!IsKnownProvider(item.Value, semanticModel.Compilation))
                            return false;
                        context = GetFormattingContext(item.Value, semanticModel.Compilation);
                        break;
                    }
                }
            }
            else if (convertedType?.SpecialType != SpecialType.System_String)
            {
                return false;
            }

            var chars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(clause.FormatStringToken);
            if (chars.IsDefault || chars.Length > CSharpFormatStringValueEvaluator.StringLimit)
                return false;

            format = chars.CreateString();
            type = semanticModel.GetTypeInfo(interpolation.Expression, cancellationToken).Type;
            sourceValue = CSharpFormatStringValue.Create(semanticModel.GetConstantValue(interpolation.Expression, cancellationToken), type);
            var expression = interpolation.Expression;
            while (expression is ParenthesizedExpressionSyntax or CheckedExpressionSyntax)
            {
                expression = expression is ParenthesizedExpressionSyntax parentheses ? parentheses.Expression
                    : ((CheckedExpressionSyntax)expression).Expression;
            }
            if (!sourceValue.IsKnown &&
                !(expression is IdentifierNameSyntax && semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol is IParameterSymbol))
            {
                sourceValue = GetKnownValue(semanticModel.GetOperation(expression, cancellationToken), semanticModel, ref evaluator, cancellationToken);
            }
            span = clause.Span;
        }
        else if (token.Parent is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression) &&
            literal.Parent is ArgumentSyntax argument && argument.Parent?.Parent is InvocationExpressionSyntax &&
            semanticModel.GetOperation(argument, cancellationToken) is IArgumentOperation
            { Parameter: { Name: "format" } formatParameter, Parent: IInvocationOperation operation })
        {
            var method = operation.TargetMethod;
            var isComposite = IsCompositeFormatMethod(method, semanticModel.Compilation);
            foreach (var item in operation.Arguments)
            {
                if (item.Parameter?.Name == "provider")
                {
                    if (isComposite && !IsKnownProvider(item.Value, semanticModel.Compilation))
                        return false;
                    context = GetFormattingContext(item.Value, semanticModel.Compilation);
                    break;
                }
            }
            if (method.Name is "Write" or "WriteLine")
                context = context with { ProviderKind = FormatStringProviderKind.Unresolved, HasUnknownFormatter = true };
            span = literal.Span;
            type = method.ContainingType;
            if (method.Name is "ToString" or "TryFormat" or "ParseExact" or "TryParseExact")
            {
                format = token.ValueText;
                if (format.Length > CSharpFormatStringValueEvaluator.StringLimit)
                    return false;
                if (operation.Instance is { } instance)
                {
                    var value = UnwrapConversion(instance);
                    sourceValue = GetKnownValue(value, semanticModel, ref evaluator, cancellationToken);
                    if (method.ContainingType.SpecialType == SpecialType.System_Enum)
                        type = value.Type;
                }
                // Only the format-bearing framework types in the catalog qualify below.
            }
            else if (isComposite)
            {
                if (compositePart is not { } part)
                {
                    var chars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
                    if (chars.IsDefault || !TryGetCompositeFormat(chars, position, out part))
                        return false;
                }
                if (part.Format.Length > CSharpFormatStringValueEvaluator.StringLimit)
                    return false;
                format = part.Format.CreateString();
                span = part.Span;
                var value = GetCompositeArgument(operation, formatParameter.Ordinal, part.ArgumentIndex);
                type = value?.Type;
                sourceValue = GetKnownValue(value, semanticModel, ref evaluator, cancellationToken);
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        isNullable = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
        if (isNullable)
            type = ((INamedTypeSymbol)type!).TypeArguments[0];

        if (type is null || !CSharpFormatStringInfo.TryCreate(type, semanticModel.Compilation, format, out info, sourceValue, context))
            return false;

        return true;
    }

    internal static QuickInfoItem CreateQuickInfo(TextSpan span, string format, CSharpFormatStringInfo info, bool isNullable)
    {
        var documentation = info.Description;
        if (isNullable)
            documentation += Environment.NewLine + CSharpFormatStringDocumentation.Get("interpolation", "null");

        var description = QuickInfoSection.Create(QuickInfoSectionKinds.Description,
            [new TaggedText(TextTags.Heading, string.Format(CSharpFeaturesResources.Format_0_for_1, format, info.TypeName), TaggedTextStyle.Strong, null, null)]);
        var explanation = QuickInfoSection.Create(QuickInfoSectionKinds.DocumentationComments,
            [new TaggedText(TextTags.Text, documentation)]);
        var link = QuickInfoSection.Create(QuickInfoSectionKinds.Text,
            [new TaggedText(TextTags.Text, CSharpFeaturesResources.Microsoft_Learn_format_strings, TaggedTextStyle.None, info.DocumentationUrl, info.DocumentationUrl)]);
        return QuickInfoItem.Create(span, sections: info.OutputKind == FormatStringOutputKind.Omitted
            ? [description, explanation, link]
            : [description, explanation,
                QuickInfoSection.Create(QuickInfoSectionKinds.Text,
                    [new TaggedText(TextTags.Heading, info.IsPreview ? CSharpFeaturesResources.Format_preview_heading : CSharpFeaturesResources.Format_example_heading, TaggedTextStyle.Strong, null, null)]),
                QuickInfoSection.Create(QuickInfoSectionKinds.Text,
                    [new TaggedText(TextTags.Text, info.Example, TaggedTextStyle.Code | TaggedTextStyle.PreserveWhitespace, null, null)]),
                link]);
    }

    internal static bool IsCompositeFormatMethod(IMethodSymbol method, Compilation compilation)
        => (method.ContainingType.SpecialType == SpecialType.System_String && method.Name == "Format") ||
            (method.Name == "AppendFormat" && IsFrameworkType(method.ContainingType, "System.Text.StringBuilder", compilation)) ||
            (method.Name is "Write" or "WriteLine" &&
             (IsFrameworkType(method.ContainingType, "System.Console", compilation) || IsFrameworkType(method.ContainingType, "System.IO.TextWriter", compilation)));

    private static bool IsFrameworkType(ITypeSymbol? type, string metadataName, Compilation compilation)
    {
        if (type is null)
            return false;
        // Prefer the core library symbol: project declarations can shadow framework metadata names.
        var coreAssembly = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;
        if (metadataName == "System.Console" && type.ContainingAssembly?.Name == "System.Console" &&
            !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly))
        {
            return SymbolEqualityComparer.Default.Equals(type, compilation.GetTypeByMetadataName(metadataName));
        }
        return SymbolEqualityComparer.Default.Equals(type, coreAssembly.GetTypeByMetadataName(metadataName));
    }

    private static CSharpFormatStringValue GetKnownValue(IOperation? value, SemanticModel semanticModel,
        ref CSharpFormatStringValueEvaluator? evaluator, CancellationToken cancellationToken)
    {
        if (value is null)
            return default;
        value = UnwrapConversion(value);
        if (value.ConstantValue.HasValue)
            return CSharpFormatStringValue.Create(value.ConstantValue, value.Type);
        if (value is IParameterReferenceOperation)
            return new(default, value.Type, FormatStringValueReason.UnknownInput);
        return (evaluator ??= new CSharpFormatStringValueEvaluator(semanticModel, cancellationToken)).GetValue(value);
    }

    private static IOperation UnwrapConversion(IOperation value)
    {
        while (value is IConversionOperation conversion && !conversion.Conversion.IsUserDefined &&
            (conversion.Type?.SpecialType is SpecialType.System_Object or SpecialType.System_Enum || conversion.Type?.TypeKind == TypeKind.Interface))
        {
            value = conversion.Operand;
        }
        return value;
    }

    private static bool IsKnownProvider(IOperation value, Compilation compilation)
    {
        value = UnwrapConversion(value);
        // CultureInfo is extensible: a variable can contain a subclass implementing ICustomFormatter.
        return value.ConstantValue is { HasValue: true, Value: null } ||
            (IsFrameworkType(value.Type, "System.Globalization.CultureInfo", compilation) && value switch
            {
                IObjectCreationOperation => true,
                IPropertyReferenceOperation { Property: { Name: "InvariantCulture" } property }
                    => IsFrameworkType(property.ContainingType, "System.Globalization.CultureInfo", compilation),
                IInvocationOperation { TargetMethod: { Name: "GetCultureInfo" or "CreateSpecificCulture" } method }
                    => IsFrameworkType(method.ContainingType, "System.Globalization.CultureInfo", compilation),
                _ => false,
            });
    }

    private static CSharpFormatStringContext GetFormattingContext(IOperation value, Compilation compilation)
    {
        value = UnwrapConversion(value);
        if (value.ConstantValue is { HasValue: true, Value: null })
            return new(ProviderKind: FormatStringProviderKind.Null);
        if (!IsFrameworkType(value.Type, "System.Globalization.CultureInfo", compilation))
            return new(ProviderKind: FormatStringProviderKind.Unresolved);
        if (value is IPropertyReferenceOperation { Property: { Name: "InvariantCulture" } property } &&
            IsFrameworkType(property.ContainingType, "System.Globalization.CultureInfo", compilation))
        {
            return new(CultureInfo.InvariantCulture, FormatStringProviderKind.ExplicitCulture, UsesUserOverrides: false);
        }

        var arguments = value switch
        {
            IObjectCreationOperation { Initializer: null } creation => creation.Arguments,
            IInvocationOperation { TargetMethod: { Name: "GetCultureInfo" or "CreateSpecificCulture" } method } invocation
                when IsFrameworkType(method.ContainingType, "System.Globalization.CultureInfo", compilation) && method.Parameters.Length == 1 => invocation.Arguments,
            _ => default,
        };
        var unresolved = new CSharpFormatStringContext(ProviderKind: FormatStringProviderKind.Unresolved);
        if (arguments.IsDefault)
            return unresolved;

        string? name = null;
        var useUserOverride = value is IObjectCreationOperation or IInvocationOperation { TargetMethod.Name: "CreateSpecificCulture" };
        foreach (var argument in arguments)
        {
            if (argument.Parameter?.Name == "name" && argument.Value.ConstantValue is { HasValue: true, Value: string cultureName })
                name = cultureName;
            else if (argument.Parameter?.Name == "useUserOverride" && argument.Value.ConstantValue is { HasValue: true, Value: bool useOverride })
                useUserOverride = useOverride;
            else
                return unresolved;
        }
        if (name is null || name.Length > CSharpFormatStringValueEvaluator.StringLimit)
            return unresolved;

        try
        {
            if (value is IInvocationOperation { TargetMethod.Name: "CreateSpecificCulture" })
                name = CultureInfo.CreateSpecificCulture(name).Name;
            // Standard read-only editor data keeps local user overrides out of the selected context.
            return new(CultureInfo.GetCultureInfo(name), FormatStringProviderKind.ExplicitCulture, useUserOverride);
        }
        catch (CultureNotFoundException)
        {
            return unresolved;
        }
    }

    private static IOperation? GetCompositeArgument(IInvocationOperation invocation, int formatOrdinal, int index)
    {
        var ordinal = (long)formatOrdinal + 1 + index;
        foreach (var argument in invocation.Arguments)
        {
            var parameter = argument.Parameter;
            if (parameter is null || parameter.Name is "format" or "provider")
                continue;

            var value = UnwrapConversion(argument.Value);
            if (!parameter.IsParams && parameter.Ordinal == ordinal)
                return value;
            if (!parameter.IsParams || ordinal < parameter.Ordinal)
                continue;

            var elementIndex = ordinal - parameter.Ordinal;
            if (value is IArrayCreationOperation { Initializer: { } initializer })
            {
                return elementIndex < initializer.ElementValues.Length
                    ? UnwrapConversion(initializer.ElementValues[(int)elementIndex]) : null;
            }
            if (value is not ICollectionExpressionOperation collection)
                return null;
            var elements = collection.Elements;
            for (var i = 0; i < elements.Length && i <= elementIndex; i++)
            {
                if (elements[i] is ISpreadOperation)
                    return null;
                if (i == elementIndex)
                    return UnwrapConversion(elements[i]);
            }
        }

        return null;
    }

    internal readonly record struct CompositeFormatPart(int ArgumentIndex, VirtualCharSequence Format, TextSpan Span, TextSpan ClosingBraceSpan);

    private static bool TryGetCompositeFormat(VirtualCharSequence chars, int position, out CompositeFormatPart part)
    {
        var cursor = 0;
        while (TryGetNextCompositeFormat(chars, ref cursor, out part))
        {
            if (position >= part.Span.Start && position < part.ClosingBraceSpan.Start)
                return true;
        }
        return false;
    }

    // Hover stops at its item. Inlays advance through the same grammar once per literal.
    internal static bool TryGetNextCompositeFormat(VirtualCharSequence chars, ref int position, out CompositeFormatPart part)
    {
        for (var i = position; i < chars.Length; i++)
        {
            if (chars[i].Value is not ('{' or '}'))
                continue;
            if (i + 1 < chars.Length && chars[i + 1].Value == chars[i].Value)
            {
                i++;
                continue;
            }

            if (chars[i].Value == '}')
                break;
            i++;
            if (!TryReadNumber(chars, ref i, out var index))
                break;
            while (i < chars.Length && chars[i].Value == ' ')
                i++;
            if (i < chars.Length && chars[i].Value == ',')
            {
                i++;
                while (i < chars.Length && chars[i].Value == ' ')
                    i++;
                if (i < chars.Length && chars[i].Value == '-')
                    i++;
                if (!TryReadNumber(chars, ref i, out _))
                    break;
                while (i < chars.Length && chars[i].Value == ' ')
                    i++;
            }

            if (i < chars.Length && chars[i].Value == '}')
                continue;
            if (i >= chars.Length || chars[i].Value != ':')
                break;
            var colon = i++;
            var start = i;
            while (i < chars.Length && chars[i].Value is not '}' and not '{')
                i++;
            if (i == chars.Length || chars[i].Value != '}')
                break;
            position = i + 1;
            part = new(index, chars.Slice(start, i - start),
                TextSpan.FromBounds(chars[colon].Span.Start, chars[i - 1].Span.End), chars[i].Span);
            return true;
        }

        position = chars.Length;
        part = default;
        return false;
    }

    private static bool TryReadNumber(VirtualCharSequence chars, ref int position, out int number)
    {
        number = 0;
        var start = position;
        while (position < chars.Length && chars[position].Value is >= '0' and <= '9')
        {
            var digit = chars[position].Value - '0';
            if (number > (int.MaxValue - digit) / 10)
                return false;
            number = number * 10 + digit;
            position++;
        }
        return position > start;
    }
}
