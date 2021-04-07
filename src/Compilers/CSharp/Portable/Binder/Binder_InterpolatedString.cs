// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        private BoundExpression BindInterpolatedString(InterpolatedStringExpressionSyntax node, BindingDiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<BoundExpression>.GetInstance();
            var stringType = GetSpecialType(SpecialType.System_String, diagnostics, node);
            ConstantValue? resultConstant = null;
            bool isResultConstant = true;

            if (node.Contents.Count == 0)
            {
                resultConstant = ConstantValue.Create(string.Empty);
            }
            else
            {
                var intType = GetSpecialType(SpecialType.System_Int32, diagnostics, node);
                foreach (var content in node.Contents)
                {
                    switch (content.Kind())
                    {
                        case SyntaxKind.Interpolation:
                            {
                                var interpolation = (InterpolationSyntax)content;
                                var value = BindValue(interpolation.Expression, diagnostics, BindValueKind.RValue);

                                // We need to ensure the argument is not a lambda, method group, etc. It isn't nice to wait until lowering,
                                // when we perform overload resolution, to report a problem. So we do that check by calling
                                // GenerateConversionForAssignment with objectType. However we want to preserve the original expression's
                                // natural type so that overload resolution may select a specialized implementation of string.Format,
                                // so we discard the result of that call and only preserve its diagnostics.
                                BoundExpression? alignment = null;
                                BoundLiteral? format = null;
                                if (interpolation.AlignmentClause != null)
                                {
                                    alignment = GenerateConversionForAssignment(intType, BindValue(interpolation.AlignmentClause.Value, diagnostics, Binder.BindValueKind.RValue), diagnostics);
                                    var alignmentConstant = alignment.ConstantValue;
                                    if (alignmentConstant != null && !alignmentConstant.IsBad)
                                    {
                                        const int magnitudeLimit = 32767;
                                        // check that the magnitude of the alignment is "in range".
                                        int alignmentValue = alignmentConstant.Int32Value;
                                        //  We do the arithmetic using negative numbers because the largest negative int has no corresponding positive (absolute) value.
                                        alignmentValue = (alignmentValue > 0) ? -alignmentValue : alignmentValue;
                                        if (alignmentValue < -magnitudeLimit)
                                        {
                                            diagnostics.Add(ErrorCode.WRN_AlignmentMagnitude, alignment.Syntax.Location, alignmentConstant.Int32Value, magnitudeLimit);
                                        }
                                    }
                                    else if (!alignment.HasErrors)
                                    {
                                        diagnostics.Add(ErrorCode.ERR_ConstantExpected, interpolation.AlignmentClause.Value.Location);
                                    }
                                }

                                if (interpolation.FormatClause != null)
                                {
                                    var text = interpolation.FormatClause.FormatStringToken.ValueText;
                                    char lastChar;
                                    bool hasErrors = false;
                                    if (text.Length == 0)
                                    {
                                        diagnostics.Add(ErrorCode.ERR_EmptyFormatSpecifier, interpolation.FormatClause.Location);
                                        hasErrors = true;
                                    }
                                    else if (SyntaxFacts.IsWhitespace(lastChar = text[text.Length - 1]) || SyntaxFacts.IsNewLine(lastChar))
                                    {
                                        diagnostics.Add(ErrorCode.ERR_TrailingWhitespaceInFormatSpecifier, interpolation.FormatClause.Location);
                                        hasErrors = true;
                                    }

                                    format = new BoundLiteral(interpolation.FormatClause, ConstantValue.Create(text), stringType, hasErrors);
                                }

                                builder.Add(new BoundStringInsert(interpolation, value, alignment, format, null));
                                if (!isResultConstant ||
                                    value.ConstantValue == null ||
                                    !(interpolation is { FormatClause: null, AlignmentClause: null }) ||
                                    !(value.ConstantValue is { IsString: true, IsBad: false }))
                                {
                                    isResultConstant = false;
                                    continue;
                                }
                                resultConstant = (resultConstant is null)
                                    ? value.ConstantValue
                                    : FoldStringConcatenation(BinaryOperatorKind.StringConcatenation, resultConstant, value.ConstantValue);
                                continue;
                            }
                        case SyntaxKind.InterpolatedStringText:
                            {
                                var text = ((InterpolatedStringTextSyntax)content).TextToken.ValueText;
                                builder.Add(new BoundLiteral(content, ConstantValue.Create(text, SpecialType.System_String), stringType));
                                if (isResultConstant)
                                {
                                    var constantVal = ConstantValue.Create(ConstantValueUtils.UnescapeInterpolatedStringLiteral(text), SpecialType.System_String);
                                    resultConstant = (resultConstant is null)
                                        ? constantVal
                                        : FoldStringConcatenation(BinaryOperatorKind.StringConcatenation, resultConstant, constantVal);
                                }
                                continue;
                            }
                        default:
                            throw ExceptionUtilities.UnexpectedValue(content.Kind());
                    }
                }

                if (!isResultConstant)
                {
                    resultConstant = null;
                }
            }

            Debug.Assert(isResultConstant == (resultConstant != null));
            return new BoundUnconvertedInterpolatedString(node, builder.ToImmutableAndFree(), resultConstant, stringType);
        }

        private BoundInterpolatedString BindUnconvertedInterpolatedStringToString(BoundUnconvertedInterpolatedString unconvertedInterpolatedString, BindingDiagnosticBag diagnostics)
        {
            // We have 4 possible lowering strategies, dependent on the contents of the string, in this order:
            //  1. The string is a constant value. We can just use the final value.
            //  2. The WellKnownType InterpolatedStringBuilder is available, and none of the interpolation holes contain an await expression.
            //     The builder is a ref struct, and we can guarantee the lifetime won't outlive the stack if the string doesn't contain any
            //     awaits, but if it does we cannot use it. This builder is the only way that ref structs can be directly used as interpolation
            //     hole components, which means that ref structs components and await expressions cannot be combined. It is already illegal for
            //     the user to use ref structs in an async method today, but if that were to ever change, this would still need to be respected.
            //     We also cannot use this method if the interpolated string appears within a catch filter, as the builder is disposable and we
            //     cannot put a try/finally inside a filter block.
            //  3. The string is composed entirely of components that are strings themselves. We can turn this into a single call to string.Concat.
            //     We prefer the builder over this because the builder can used pooling to avoid new allocations, while this call will potentially
            //     need to allocate a param array.
            //  4. The string has heterogeneous data and either InterpolatedStringBuilder is unavailable, or one of the holes contains an await
            //     expression. This is turned into a call to string.Format.
            //
            // We need to do the determination of 1, 2, or 3/4 up front, rather than in lowering, as it affects diagnostics (ref structs not being
            // able to be used, for example). However, between 3 and 4, we don't need to know at this point, so that logic is deferred for lowering.

            if (unconvertedInterpolatedString.ConstantValue is not null)
            {
                // Case 1
                Debug.Assert(unconvertedInterpolatedString.Parts.All(static part => part.Type is null or { SpecialType: SpecialType.System_String }));
                return constructWithData(BindInterpolatedStringParts(unconvertedInterpolatedString, diagnostics), data: null);
            }

            if (tryBindAsBuilderType(out var result))
            {
                // Case 2
                return result;
            }

            // The specifics of 3 vs 4 aren't necessary for this stage of binding. The only thing that matters is that every part needs to be convertible
            // object.
            return constructWithData(BindInterpolatedStringParts(unconvertedInterpolatedString, diagnostics), data: null);

            BoundInterpolatedString constructWithData(ImmutableArray<BoundExpression> parts, InterpolatedStringBuilderData? data)
                => new BoundInterpolatedString(
                    unconvertedInterpolatedString.Syntax,
                    data,
                    parts,
                    unconvertedInterpolatedString.ConstantValue,
                    unconvertedInterpolatedString.Type,
                    unconvertedInterpolatedString.HasErrors);

            bool tryBindAsBuilderType([NotNullWhen(true)] out BoundInterpolatedString? result)
            {
                if (this.Flags.Includes(BinderFlags.InCatchFilter))
                {
                    // CLI Spec does not permit nested tries inside a filter block, so we can't use the builder here, as it is disposable.
                    // PROTOTYPE(interp-string): Should we try to collect errors if a type not otherwise usable in an interpolated string
                    // is used in this case?
                    result = null;
                    return false;
                }

                result = null;
                var interpolatedStringBuilderType = Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_InterpolatedStringBuilder);
                if (interpolatedStringBuilderType is MissingMetadataTypeSymbol)
                {
                    return false;
                }

                if (unconvertedInterpolatedString.Parts.ContainsAwaitExpression())
                {
                    return false;
                }

                BindingDiagnosticBag applicableDiagnostics = BindingDiagnosticBag.GetInstance(template: diagnostics);
                if (!Conversions.IsApplicableInterpolatedStringBuilderType(unconvertedInterpolatedString, interpolatedStringBuilderType, applicableDiagnostics, out var builderArguments))
                {
                    applicableDiagnostics.Free();
                    return false;
                }

                diagnostics.AddRangeAndFree(applicableDiagnostics);

                // Prior to C# 10, all types in an interpolated string expression needed to be convertible to `object`. After 10, some types
                // (such as Span<T>) that are not convertible to `object` are permissible as interpolated string components, provided there
                // is an applicable TryFormatInterpolationHole that accepts them. To preserve langversion, we therefore make sure all components
                // are convertible to object if the current langversion is lower than the interpolation feature
                TypeSymbol? objectType = null;
                BindingDiagnosticBag? conversionDiagnostics = null;
                var needToCheckConversionToObject = !Compilation.IsFeatureEnabled(MessageID.IDS_FeatureImprovedInterpolatedStrings) && diagnostics.AccumulatesDiagnostics;
                if (needToCheckConversionToObject)
                {
                    objectType = GetSpecialType(SpecialType.System_Object, diagnostics, unconvertedInterpolatedString.Syntax);
                    conversionDiagnostics = BindingDiagnosticBag.GetInstance();
                }

                // Swap out the first argument of the format calls, which is the (potentially converted) part from the original string with
                // a placeholder, and put the part into the interpolated string's list of parts.
                Debug.Assert(builderArguments.Length == unconvertedInterpolatedString.Parts.Length);
                var interpolatedDataBuilder = ArrayBuilder<MethodArgumentInfo>.GetInstance(builderArguments.Length);
                var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(3);
                var partsBuilder = ArrayBuilder<BoundExpression>.GetInstance(builderArguments.Length);
                int baseStringLength = 0;
                int numFormatHoles = 0;

                for (int i = 0; i < builderArguments.Length; i++)
                {
                    var currentFormatCall = builderArguments[i];
                    Debug.Assert(currentFormatCall.Arguments.Length > 0);
                    Debug.Assert(argumentsBuilder.Count == 0);

                    var newPart = currentFormatCall.Arguments[0];
                    argumentsBuilder.Add(new BoundInterpolatedStringElementPlaceholder(newPart.Syntax, i, newPart.Type));
                    if (currentFormatCall.Arguments.Length > 1)
                    {
                        argumentsBuilder.AddRange(currentFormatCall.Arguments, 1, currentFormatCall.Arguments.Length - 1);
                    }

                    interpolatedDataBuilder.Add(currentFormatCall with { Arguments = argumentsBuilder.ToImmutableAndClear() });

                    var currentPart = unconvertedInterpolatedString.Parts[i];
                    if (currentPart is BoundStringInsert insert)
                    {
                        numFormatHoles++;

                        if (needToCheckConversionToObject)
                        {
                            Debug.Assert(conversionDiagnostics is not null);
                            var value = insert.Value;
                            bool reported = false;
                            if (value.Type is not null)
                            {
                                value = BindToNaturalType(value, conversionDiagnostics);
                                if (conversionDiagnostics.HasAnyErrors())
                                {
                                    CheckFeatureAvailability(value.Syntax, MessageID.IDS_FeatureImprovedInterpolatedStrings, diagnostics);
                                    conversionDiagnostics.Clear();
                                    reported = true;
                                }
                            }

                            if (!reported)
                            {
                                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(conversionDiagnostics);
                                var conversion = Conversions.ClassifyConversionFromExpression(value, objectType, ref useSiteInfo);
                                if (!conversion.Exists || !conversion.IsValid || useSiteInfo.HasErrors)
                                {
                                    CheckFeatureAvailability(value.Syntax, MessageID.IDS_FeatureImprovedInterpolatedStrings, diagnostics);
                                }
                            }
                        }

                        newPart = insert.Update(newPart, insert.Alignment, insert.Format, newPart.Type);
                    }
                    else
                    {
                        Debug.Assert(newPart is BoundLiteral { ConstantValue: { IsString: true } });
                        Debug.Assert(currentPart.ConstantValue is { IsString: true });
                        baseStringLength += currentPart.ConstantValue.RopeValue!.Length;
                    }

                    partsBuilder.Add(newPart);

                    ReportDiagnosticsIfObsolete(diagnostics, currentFormatCall.Method, currentPart.Syntax, hasBaseReceiver: false);
                    ReportDiagnosticsIfUnmanagedCallersOnly(diagnostics, currentFormatCall.Method, currentPart.Syntax.Location, isDelegateConversion: false);
                }

                conversionDiagnostics?.Free();

                var newParts = partsBuilder.ToImmutableAndFree();

                var createMethod = (MethodSymbol)GetWellKnownTypeMember(
                    WellKnownMember.System_Runtime_CompilerServices_InterpolatedStringBuilder__CreateInt32Int32,
                    diagnostics,
                    syntax: unconvertedInterpolatedString.Syntax);

                MethodArgumentInfo? constructorInfo = null;

                if (createMethod != null)
                {
                    var intType = GetSpecialType(SpecialType.System_Int32, diagnostics, unconvertedInterpolatedString.Syntax);
                    var arguments = ImmutableArray.Create<BoundExpression>(
                        new BoundLiteral(unconvertedInterpolatedString.Syntax, ConstantValue.Create(baseStringLength), intType) { WasCompilerGenerated = true },
                        new BoundLiteral(unconvertedInterpolatedString.Syntax, ConstantValue.Create(numFormatHoles), intType) { WasCompilerGenerated = true });

                    constructorInfo = new MethodArgumentInfo(createMethod, arguments, ArgsToParamsOpt: default, DefaultArguments: BitVector.Empty, Expanded: false);

                    ReportDiagnosticsIfObsolete(diagnostics, createMethod, unconvertedInterpolatedString.Syntax, hasBaseReceiver: false);
                    ReportDiagnosticsIfUnmanagedCallersOnly(diagnostics, createMethod, unconvertedInterpolatedString.Syntax.Location, isDelegateConversion: false);
                }

                // PROTOTYPE(interp-strings): Do we need to support versions of InterpolatedStringBuilder that do not have Dispose, or implement it
                // with a different signature, or that are not a ref struct and explicitly implement IDisposable?
                // PROTOTYPE(interp-string): Is the runtime going to expose this, or do we want to support pattern-based dispose?

                var disposeMethod = (MethodSymbol)GetWellKnownTypeMember(
                    WellKnownMember.System_Runtime_CompilerServices_InterpolatedStringBuilder__Dispose,
                    diagnostics,
                    syntax: unconvertedInterpolatedString.Syntax);

                MethodArgumentInfo? disposeInfo = null;

                if (disposeMethod != null)
                {
                    disposeInfo = new MethodArgumentInfo(disposeMethod, argumentsBuilder.ToImmutable(), ArgsToParamsOpt: default, DefaultArguments: default, Expanded: false);

                    ReportDiagnosticsIfObsolete(diagnostics, disposeMethod, unconvertedInterpolatedString.Syntax, hasBaseReceiver: false);
                    ReportDiagnosticsIfUnmanagedCallersOnly(diagnostics, disposeMethod, unconvertedInterpolatedString.Syntax.Location, isDelegateConversion: false);
                }

                argumentsBuilder.Free();
                result = constructWithData(
                    newParts,
                    new InterpolatedStringBuilderData(
                        interpolatedStringBuilderType,
                        constructorInfo,
                        interpolatedDataBuilder.ToImmutableAndFree(),
                        disposeInfo,
                        LocalScopeDepth));
                return true;
            }
        }

        private ImmutableArray<BoundExpression> BindInterpolatedStringParts(BoundUnconvertedInterpolatedString unconvertedInterpolatedString, BindingDiagnosticBag diagnostics)
        {
            ArrayBuilder<BoundExpression>? partsBuilder = null;
            var objectType = GetSpecialType(SpecialType.System_Object, diagnostics, unconvertedInterpolatedString.Syntax);
            for (int i = 0; i < unconvertedInterpolatedString.Parts.Length; i++)
            {
                var part = unconvertedInterpolatedString.Parts[i];
                if (part is BoundStringInsert insert)
                {
                    BoundExpression newValue;
                    if (insert.Value.Type is null)
                    {
                        newValue = GenerateConversionForAssignment(objectType, insert.Value, diagnostics);
                    }
                    else
                    {
                        newValue = BindToNaturalType(insert.Value, diagnostics);
                        _ = GenerateConversionForAssignment(objectType, insert.Value, diagnostics);
                    }

                    if (insert.Value != newValue)
                    {
                        if (partsBuilder is null)
                        {
                            partsBuilder = ArrayBuilder<BoundExpression>.GetInstance(unconvertedInterpolatedString.Parts.Length);
                            partsBuilder.AddRange(unconvertedInterpolatedString.Parts, i);
                        }

                        partsBuilder.Add(insert.Update(newValue, insert.Alignment, insert.Format, insert.Type));
                    }
                    else
                    {
                        partsBuilder?.Add(part);
                    }
                }
                else
                {
                    Debug.Assert(part is BoundLiteral { Type: { SpecialType: SpecialType.System_String } });
                    partsBuilder?.Add(part);
                }
            }

            return partsBuilder?.ToImmutableAndFree() ?? unconvertedInterpolatedString.Parts;
        }
    }
}
