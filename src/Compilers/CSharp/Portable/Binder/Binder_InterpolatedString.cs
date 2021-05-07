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

                                builder.Add(new BoundStringInsert(interpolation, value, alignment, format, isInterpolatedStringHandlerAppendCall: false));
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
            //  2. The WellKnownType DefaultInterpolatedStringHandler is available, and none of the interpolation holes contain an await expression.
            //     The builder is a ref struct, and we can guarantee the lifetime won't outlive the stack if the string doesn't contain any
            //     awaits, but if it does we cannot use it. This builder is the only way that ref structs can be directly used as interpolation
            //     hole components, which means that ref structs components and await expressions cannot be combined. It is already illegal for
            //     the user to use ref structs in an async method today, but if that were to ever change, this would still need to be respected.
            //     We also cannot use this method if the interpolated string appears within a catch filter, as the builder is disposable and we
            //     cannot put a try/finally inside a filter block.
            //  3. The string is composed entirely of components that are strings themselves. We can turn this into a single call to string.Concat.
            //     We prefer the builder over this because the builder can use pooling to avoid new allocations, while this call will potentially
            //     need to allocate a param array.
            //  4. The string has heterogeneous data and either InterpolatedStringHandler is unavailable, or one of the holes contains an await
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

            BoundInterpolatedString constructWithData(ImmutableArray<BoundExpression> parts, InterpolatedStringHandlerData? data)
                => new BoundInterpolatedString(
                    unconvertedInterpolatedString.Syntax,
                    data,
                    parts,
                    unconvertedInterpolatedString.ConstantValue,
                    unconvertedInterpolatedString.Type,
                    unconvertedInterpolatedString.HasErrors);

            bool tryBindAsBuilderType([NotNullWhen(true)] out BoundInterpolatedString? result)
            {
                result = null;
                if (unconvertedInterpolatedString.Parts.ContainsAwaitExpression())
                {
                    // PROTOTYPE(interp-string): For interpolated strings used as strings, we could evaluate components eagerly
                    // and always use the builder.
                    return false;
                }

                var interpolatedStringBuilderType = Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_DefaultInterpolatedStringHandler);
                if (interpolatedStringBuilderType is MissingMetadataTypeSymbol)
                {
                    return false;
                }

                diagnostics.Add(interpolatedStringBuilderType.GetUseSiteInfo(), unconvertedInterpolatedString.Syntax.Location);

                // We satisfy the conditions for using an interpolated string builder. Bind all the builder calls unconditionally, so that if
                // there are errors we get better diagnostics than "could not convert to object."
                var (appendCalls, usesBoolReturn) = BindInterpolatedStringAppendCalls(unconvertedInterpolatedString, interpolatedStringBuilderType, diagnostics);

                // Prior to C# 10, all types in an interpolated string expression needed to be convertible to `object`. After 10, some types
                // (such as Span<T>) that are not convertible to `object` are permissible as interpolated string components, provided there
                // is an applicable AppendFormatted method that accepts them. To preserve langversion, we therefore make sure all components
                // are convertible to object if the current langversion is lower than the interpolation feature
                TypeSymbol? objectType = null;
                BindingDiagnosticBag? conversionDiagnostics = null;
                var needToCheckConversionToObject = !Compilation.IsFeatureEnabled(MessageID.IDS_FeatureImprovedInterpolatedStrings) && diagnostics.AccumulatesDiagnostics;
                if (needToCheckConversionToObject)
                {
                    objectType = GetSpecialType(SpecialType.System_Object, diagnostics, unconvertedInterpolatedString.Syntax);
                    conversionDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
                }

                Debug.Assert(appendCalls.Length == unconvertedInterpolatedString.Parts.Length);
                Debug.Assert(appendCalls.All(a => a is { HasErrors: true } or BoundCall { Arguments: { Length: > 0 } } or BoundDynamicInvocation));
                int baseStringLength = 0;
                int numFormatHoles = 0;

                foreach (var currentPart in unconvertedInterpolatedString.Parts)
                {
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
                                    reported = true;
                                }
                            }

                            if (!reported)
                            {
                                _ = GenerateConversionForAssignment(objectType, value, conversionDiagnostics);
                                if (conversionDiagnostics.HasAnyErrors())
                                {
                                    CheckFeatureAvailability(value.Syntax, MessageID.IDS_FeatureImprovedInterpolatedStrings, diagnostics);
                                }
                            }

                            conversionDiagnostics.Clear();
                        }
                    }
                    else
                    {
                        Debug.Assert(currentPart is { ConstantValue: { IsString: true } } and BoundLiteral);
                        baseStringLength += currentPart.ConstantValue.RopeValue!.Length;
                    }
                }

                conversionDiagnostics?.Free();

                var intType = GetSpecialType(SpecialType.System_Int32, diagnostics, unconvertedInterpolatedString.Syntax);
                var arguments = ImmutableArray.Create<BoundExpression>(
                    new BoundLiteral(unconvertedInterpolatedString.Syntax, ConstantValue.Create(baseStringLength), intType) { WasCompilerGenerated = true },
                    new BoundLiteral(unconvertedInterpolatedString.Syntax, ConstantValue.Create(numFormatHoles), intType) { WasCompilerGenerated = true });

                // PROTOTYPE(interp-string): Support optional out param for whether the builder was created successfully and passing in other required args
                BoundExpression? createExpression = MakeClassCreationExpression(interpolatedStringBuilderType, arguments, unconvertedInterpolatedString.Syntax, diagnostics);

                result = constructWithData(
                    appendCalls,
                    new InterpolatedStringHandlerData(
                        interpolatedStringBuilderType,
                        createExpression,
                        usesBoolReturn,
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

                        partsBuilder.Add(insert.Update(newValue, insert.Alignment, insert.Format, isInterpolatedStringHandlerAppendCall: false));
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

        private (ImmutableArray<BoundExpression> AppendFormatCalls, bool UsesBoolReturn) BindInterpolatedStringAppendCalls(BoundUnconvertedInterpolatedString source, TypeSymbol builderType, BindingDiagnosticBag diagnostics)
        {
            // PROTOTYPE(interp-string): Update the spec with the rules around InterpolatedStringHandlerAttribute. For now, we assume that any
            // type that makes it to this method is actually an interpolated string builder type, and we should fully report any binding errors
            // we encounter while doing this work.

            if (source.Parts.IsEmpty)
            {
                return (ImmutableArray<BoundExpression>.Empty, false);
            }

            var implicitBuilderReceiver = new BoundInterpolatedStringHandlerPlaceholder(source.Syntax, builderType) { WasCompilerGenerated = true };
            bool? builderPatternExpectsBool = null;
            var builderAppendCalls = ArrayBuilder<BoundExpression>.GetInstance(source.Parts.Length);
            var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(3);
            var parameterNamesAndLocationsBuilder = ArrayBuilder<(string, Location)?>.GetInstance(3);

            foreach (var part in source.Parts)
            {
                Debug.Assert(part is BoundLiteral or BoundStringInsert);
                string methodName;

                if (part is BoundStringInsert insert)
                {
                    methodName = "AppendFormatted";
                    argumentsBuilder.Add(insert.Value);
                    parameterNamesAndLocationsBuilder.Add(null);

                    if (insert.Alignment is not null)
                    {
                        argumentsBuilder.Add(insert.Alignment);
                        parameterNamesAndLocationsBuilder.Add(("alignment", insert.Alignment.Syntax.Location));
                    }
                    if (insert.Format is not null)
                    {
                        argumentsBuilder.Add(insert.Format);
                        parameterNamesAndLocationsBuilder.Add(("format", insert.Format.Syntax.Location));
                    }
                }
                else
                {
                    methodName = "AppendLiteral";
                    argumentsBuilder.Add(part);
                }

                var arguments = argumentsBuilder.ToImmutableAndClear();
                ImmutableArray<(string, Location)?> parameterNamesAndLocations;
                if (parameterNamesAndLocationsBuilder.Count > 1)
                {
                    parameterNamesAndLocations = parameterNamesAndLocationsBuilder.ToImmutableAndClear();
                }
                else
                {
                    Debug.Assert(parameterNamesAndLocationsBuilder.Count == 0 || parameterNamesAndLocationsBuilder[0] == null);
                    parameterNamesAndLocations = default;
                    parameterNamesAndLocationsBuilder.Clear();
                }

                var call = MakeInvocationExpression(part.Syntax, implicitBuilderReceiver, methodName, arguments, diagnostics, names: parameterNamesAndLocations, searchExtensionMethodsIfNecessary: false);
                builderAppendCalls.Add(call);

                // PROTOTYPE(interp-string): Handle dynamic
                Debug.Assert(call is BoundCall or { HasErrors: true });

                if (call is BoundCall { Method: { ReturnType: var returnType } method })
                {
                    bool methodReturnsBool = returnType.SpecialType == SpecialType.System_Boolean;
                    if (!methodReturnsBool && returnType.SpecialType != SpecialType.System_Void)
                    {
                        // Interpolated string handler method '{0}' is malformed. It does not return 'void' or 'bool'.
                        diagnostics.Add(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, part.Syntax.Location, method);
                    }
                    else if (builderPatternExpectsBool == null)
                    {
                        builderPatternExpectsBool = methodReturnsBool;
                    }
                    else if (builderPatternExpectsBool != methodReturnsBool)
                    {
                        // Interpolated string handler method '{0}' has inconsistent return types. Expected to return '{1}'.
                        var expected = builderPatternExpectsBool == true ? Compilation.GetSpecialType(SpecialType.System_Boolean) : Compilation.GetSpecialType(SpecialType.System_Void);
                        diagnostics.Add(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnInconsistent, part.Syntax.Location, method, expected);
                    }
                }
            }

            argumentsBuilder.Free();
            parameterNamesAndLocationsBuilder.Free();
            return (builderAppendCalls.ToImmutableAndFree(), builderPatternExpectsBool ?? false);
        }
    }
}
