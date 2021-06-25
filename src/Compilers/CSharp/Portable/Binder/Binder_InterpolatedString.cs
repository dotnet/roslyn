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

            if (tryBindAsHandlerType(out var result))
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

            bool tryBindAsHandlerType([NotNullWhen(true)] out BoundInterpolatedString? result)
            {
                result = null;
                if (unconvertedInterpolatedString.Parts.ContainsAwaitExpression())
                {
                    // PROTOTYPE(interp-string): For interpolated strings used as strings, we could evaluate components eagerly
                    // and always use the builder.
                    return false;
                }

                var interpolatedStringHandlerType = Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_DefaultInterpolatedStringHandler);
                if (interpolatedStringHandlerType is MissingMetadataTypeSymbol)
                {
                    return false;
                }

                result = BindUnconvertedInterpolatedStringToHandlerType(unconvertedInterpolatedString, interpolatedStringHandlerType, diagnostics, isHandlerConversion: false);

                return true;
            }
        }

        private BoundInterpolatedString BindUnconvertedInterpolatedStringToHandlerType(
            BoundUnconvertedInterpolatedString unconvertedInterpolatedString,
            NamedTypeSymbol interpolatedStringHandlerType,
            BindingDiagnosticBag diagnostics,
            bool isHandlerConversion,
            ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> additionalConstructorArguments = default,
            ImmutableArray<RefKind> refKinds = default)
        {
            Debug.Assert(additionalConstructorArguments.IsDefault
                ? refKinds.IsDefault
                : additionalConstructorArguments.Length == refKinds.Length);
            ReportUseSite(interpolatedStringHandlerType, diagnostics, unconvertedInterpolatedString.Syntax);

            // We satisfy the conditions for using an interpolated string builder. Bind all the builder calls unconditionally, so that if
            // there are errors we get better diagnostics than "could not convert to object."
            var (appendCalls, usesBoolReturn) = BindInterpolatedStringAppendCalls(unconvertedInterpolatedString, interpolatedStringHandlerType, diagnostics);

            // Prior to C# 10, all types in an interpolated string expression needed to be convertible to `object`. After 10, some types
            // (such as Span<T>) that are not convertible to `object` are permissible as interpolated string components, provided there
            // is an applicable AppendFormatted method that accepts them. To preserve langversion, we therefore make sure all components
            // are convertible to object if the current langversion is lower than the interpolation feature and we're converting this
            // interpolation into an actual string.
            TypeSymbol? objectType = null;
            BindingDiagnosticBag? conversionDiagnostics = null;
            bool needToCheckConversionToObject = false;
            if (isHandlerConversion)
            {
                CheckFeatureAvailability(unconvertedInterpolatedString.Syntax, MessageID.IDS_FeatureImprovedInterpolatedStrings, diagnostics);
            }
            else if (!Compilation.IsFeatureEnabled(MessageID.IDS_FeatureImprovedInterpolatedStrings) && diagnostics.AccumulatesDiagnostics)
            {
                needToCheckConversionToObject = true;
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
            int constructorArgumentLength = 2 + (additionalConstructorArguments.IsDefault ? 0 : additionalConstructorArguments.Length);
            var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(constructorArgumentLength);
            populateArguments(unconvertedInterpolatedString, additionalConstructorArguments, baseStringLength, numFormatHoles, intType, argumentsBuilder);

            var refKindsBuilder = ArrayBuilder<RefKind>.GetInstance(constructorArgumentLength);
            refKindsBuilder.Add(RefKind.None);
            refKindsBuilder.Add(RefKind.None);

            if (!refKinds.IsDefault)
            {
                refKindsBuilder.AddRange(refKinds);
            }

            var nonOutConstructorDiagnostics = BindingDiagnosticBag.GetInstance(template: diagnostics);
            BoundExpression constructorCall;
            BoundExpression nonOutConstructorCall = MakeConstructorInvocation(interpolatedStringHandlerType, argumentsBuilder, refKindsBuilder, unconvertedInterpolatedString.Syntax, nonOutConstructorDiagnostics);
            if (nonOutConstructorCall is not BoundObjectCreationExpression { ResultKind: LookupResultKind.Viable })
            {
                // MakeConstructorInvocation can call CoerceArguments on the builder if overload resolution succeeded ignoring accessibility, which
                // could still end up not succeeding, and that would end up changing the arguments. So we want to clear and repopulate.
                argumentsBuilder.Clear();

                // Try again with an out parameter. Note that we intentionally use `diagnostics` for resolving System.Boolean, because we want to
                // track that we're using the type no matter what.
                var boolType = GetSpecialType(SpecialType.System_Boolean, diagnostics, unconvertedInterpolatedString.Syntax);
                var trailingConstructorValidityPlaceholder = new BoundInterpolatedStringArgumentPlaceholder(unconvertedInterpolatedString.Syntax, BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter, boolType);
                var outConstructorAdditionalArguments = additionalConstructorArguments.NullToEmpty().Add(trailingConstructorValidityPlaceholder);
                populateArguments(unconvertedInterpolatedString, outConstructorAdditionalArguments, baseStringLength, numFormatHoles, intType, argumentsBuilder);

                refKindsBuilder.Add(RefKind.Out);

                var outConstructorDiagnostics = BindingDiagnosticBag.GetInstance(template: diagnostics);
                var outConstructorCall = MakeConstructorInvocation(interpolatedStringHandlerType, argumentsBuilder, refKindsBuilder, unconvertedInterpolatedString.Syntax, outConstructorDiagnostics);

                if (outConstructorCall is BoundObjectCreationExpression { ResultKind: LookupResultKind.Viable })
                {
                    // We successfully bound the out version, so set all the final data based on that binding
                    constructorCall = outConstructorCall;
                    diagnostics.AddRangeAndFree(outConstructorDiagnostics);
                    nonOutConstructorDiagnostics.Free();
                    additionalConstructorArguments = outConstructorAdditionalArguments;
                }
                else
                {
                    // We'll attempt to figure out which failure was "best" by looking to see if one failed to bind because it couldn't find
                    // a constructor with the correct number of arguments. We presume that, if one failed for this reason and the other failed
                    // for a different reason, that different reason is the one the user will want to know about. If both or neither failed
                    // because of this error, we'll report everything.

                    var nonOutConstructorHasArityError = nonOutConstructorDiagnostics.DiagnosticBag?.AsEnumerableWithoutResolution().Any(d => (ErrorCode)d.Code == ErrorCode.ERR_BadCtorArgCount) ?? false;
                    var outConstructorHasArityError = outConstructorDiagnostics.DiagnosticBag?.AsEnumerableWithoutResolution().Any(d => (ErrorCode)d.Code == ErrorCode.ERR_BadCtorArgCount) ?? false;

                    switch ((nonOutConstructorHasArityError, outConstructorHasArityError))
                    {
                        case (true, false):
                            constructorCall = outConstructorCall;
                            additionalConstructorArguments = outConstructorAdditionalArguments;
                            diagnostics.AddRangeAndFree(outConstructorDiagnostics);
                            nonOutConstructorDiagnostics.Free();
                            break;
                        case (false, true):
                            constructorCall = nonOutConstructorCall;
                            diagnostics.AddRangeAndFree(nonOutConstructorDiagnostics);
                            outConstructorDiagnostics.Free();
                            break;
                        default:
                            // For the final output binding info, we'll go with the shorter constructor in the absence of any tiebreaker,
                            // but we'll report all diagnostics
                            constructorCall = nonOutConstructorCall;
                            diagnostics.AddRangeAndFree(nonOutConstructorDiagnostics);
                            diagnostics.AddRangeAndFree(outConstructorDiagnostics);
                            break;

                    }
                }
            }
            else
            {
                diagnostics.AddRangeAndFree(nonOutConstructorDiagnostics);
                constructorCall = nonOutConstructorCall;
            }

            argumentsBuilder.Free();
            refKindsBuilder.Free();

            // PROTOTYPE(interp-string): Support dynamic
            Debug.Assert(constructorCall.HasErrors || constructorCall is BoundObjectCreationExpression);

            return new BoundInterpolatedString(
                unconvertedInterpolatedString.Syntax,
                new InterpolatedStringHandlerData(
                    interpolatedStringHandlerType,
                    constructorCall,
                    usesBoolReturn,
                    LocalScopeDepth,
                    additionalConstructorArguments.NullToEmpty()),
                appendCalls,
                unconvertedInterpolatedString.ConstantValue,
                unconvertedInterpolatedString.Type,
                unconvertedInterpolatedString.HasErrors);

            static void populateArguments(BoundUnconvertedInterpolatedString unconvertedInterpolatedString, ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> additionalConstructorArguments, int baseStringLength, int numFormatHoles, NamedTypeSymbol intType, ArrayBuilder<BoundExpression> argumentsBuilder)
            {
                // literalLength
                argumentsBuilder.Add(new BoundLiteral(unconvertedInterpolatedString.Syntax, ConstantValue.Create(baseStringLength), intType) { WasCompilerGenerated = true });
                // formattedCount
                argumentsBuilder.Add(new BoundLiteral(unconvertedInterpolatedString.Syntax, ConstantValue.Create(numFormatHoles), intType) { WasCompilerGenerated = true });
                // Any other arguments from the call site
                if (!additionalConstructorArguments.IsDefault)
                {
                    argumentsBuilder.AddRange(additionalConstructorArguments);
                }
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

        private BoundExpression BindInterpolatedStringHandlerInMemberCall(
            BoundUnconvertedInterpolatedString unconvertedString,
            ArrayBuilder<BoundExpression> arguments,
            ImmutableArray<ParameterSymbol> parameters,
            ref MemberAnalysisResult memberAnalysisResult,
            int interpolatedStringArgNum,
            TypeSymbol? receiverType,
            RefKind? receiverRefKind,
            BindingDiagnosticBag diagnostics)
        {
            var interpolatedStringConversion = memberAnalysisResult.ConversionForArg(interpolatedStringArgNum);
            Debug.Assert(interpolatedStringConversion.IsInterpolatedStringHandler);
            var interpolatedStringParameter = GetCorrespondingParameter(ref memberAnalysisResult, parameters, interpolatedStringArgNum);
            Debug.Assert(interpolatedStringParameter.Type is NamedTypeSymbol { IsInterpolatedStringHandlerType: true });

            if (interpolatedStringParameter.HasInterpolatedStringHandlerArgumentError)
            {
                // The InterpolatedStringHandlerArgumentAttribute applied to parameter '{0}' is malformed and cannot be interpreted. Construct an instance of '{1}' manually.
                diagnostics.Add(ErrorCode.ERR_InterpolatedStringHandlerArgumentAttributeMalformed, unconvertedString.Syntax.Location, interpolatedStringParameter, interpolatedStringParameter.Type);
                return CreateConversion(
                    unconvertedString.Syntax,
                    unconvertedString,
                    interpolatedStringConversion,
                    isCast: false,
                    conversionGroupOpt: null,
                    wasCompilerGenerated: false,
                    interpolatedStringParameter.Type,
                    diagnostics,
                    hasErrors: true);
            }

            var handlerParameterIndexes = interpolatedStringParameter.InterpolatedStringHandlerArgumentIndexes;
            if (handlerParameterIndexes.IsEmpty)
            {
                // No arguments, fall back to the standard conversion steps.
                return CreateConversion(
                    unconvertedString.Syntax,
                    unconvertedString,
                    interpolatedStringConversion,
                    isCast: false,
                    conversionGroupOpt: null,
                    interpolatedStringParameter.Type,
                    diagnostics);
            }

            // We need to find the appropriate argument expression for every expected parameter, and error on any that occur after the current parameter

            ImmutableArray<int> handlerArgumentIndexes;

            if (memberAnalysisResult.ArgsToParamsOpt.IsDefault && arguments.Count == parameters.Length)
            {
                // No parameters are missing and no remapped indexes, we can just use the original indexes
                handlerArgumentIndexes = handlerParameterIndexes;
            }
            else
            {
                // Args and parameters were reordered via named parameters, or parameters are missing. Find the correct argument index for each parameter.
                var handlerArgumentIndexesBuilder = ArrayBuilder<int>.GetInstance(handlerParameterIndexes.Length, fillWithValue: BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter);
                for (int argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
                {
                    // The index in the original parameter list we're looking to match up.
                    var argumentParameterIndex = memberAnalysisResult.ParameterFromArgument(argumentIndex);
                    for (int handlerParameterIndex = 0; handlerParameterIndex < handlerParameterIndexes.Length; handlerParameterIndex++)
                    {
                        // Is the original parameter index of the current argument the parameter index that was specified in the attribute?
                        if (argumentParameterIndex == handlerParameterIndexes[handlerParameterIndex])
                        {
                            // We can't just bail out on the first match: users can duplicate parameters in attributes, causing the same value to be passed twice.
                            Debug.Assert(handlerArgumentIndexesBuilder[handlerParameterIndex] == BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter);
                            handlerArgumentIndexesBuilder[handlerParameterIndex] = argumentIndex;
                        }
                    }
                }

                handlerArgumentIndexes = handlerArgumentIndexesBuilder.ToImmutableAndFree();
            }

            var argumentPlaceholdersBuilder = ArrayBuilder<BoundInterpolatedStringArgumentPlaceholder>.GetInstance(handlerArgumentIndexes.Length);
            var argumentRefKindsBuilder = ArrayBuilder<RefKind>.GetInstance(handlerArgumentIndexes.Length);
            bool hasErrors = false;

            // Now, go through all the specified arguments and see if any were specified _after_ the interpolated string, and construct
            // a set of placeholders for overload resolution.
            for (int i = 0; i < handlerArgumentIndexes.Length; i++)
            {
                int argumentIndex = handlerArgumentIndexes[i];
                Debug.Assert(argumentIndex != interpolatedStringArgNum);

                RefKind refKind;
                TypeSymbol placeholderType;
                if (argumentIndex == BoundInterpolatedStringArgumentPlaceholder.InstanceParameter)
                {
                    Debug.Assert(receiverRefKind != null && receiverType is not null);
                    refKind = receiverRefKind.GetValueOrDefault();
                    placeholderType = receiverType;
                }
                else if (argumentIndex == BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter)
                {
                    // Don't error if the parameter isn't optional: the user will already have an error for missing an optional parameter or overload resolution failed.
                    // If it is optional, then they could otherwise not specify the parameter and that's an error
                    var originalParameterIndex = handlerParameterIndexes[i];
                    var parameter = GetCorrespondingParameter(ref memberAnalysisResult, parameters, originalParameterIndex);
                    if (parameter.IsOptional)
                    {
                        // Parameter '{0}' is not explicitly provided, but is used as an argument to the interpolated string handler conversion on parameter '{1}'. Specify the value of '{0}' before '{1}'.
                        diagnostics.Add(
                            ErrorCode.ERR_InterpolatedStringHandlerArgumentOptionalNotSpecified,
                            unconvertedString.Syntax.Location,
                            parameter.Name,
                            interpolatedStringParameter.Name);
                        hasErrors = true;
                    }

                    refKind = parameter.RefKind;
                    placeholderType = parameter.Type;
                }
                else
                {
                    var parameter = GetCorrespondingParameter(ref memberAnalysisResult, parameters, argumentIndex);
                    if (argumentIndex > interpolatedStringArgNum)
                    {
                        // Parameter '{0}' is an argument to the interpolated string handler conversion on parameter '{1}', but the corresponding argument is specified after the interpolated string expression. Reorder the arguments to move '{0}' before '{1}'.
                        diagnostics.Add(
                            ErrorCode.ERR_InterpolatedStringHandlerArgumentLocatedAfterInterpolatedString,
                            arguments[argumentIndex].Syntax.Location,
                            parameter.Name,
                            interpolatedStringParameter.Name);
                        hasErrors = true;
                    }

                    refKind = parameter.RefKind;
                    placeholderType = parameter.Type;
                }

                var placeholderSyntax = argumentIndex switch
                {
                    BoundInterpolatedStringArgumentPlaceholder.InstanceParameter or BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter => unconvertedString.Syntax,
                    >= 0 => arguments[argumentIndex].Syntax,
                    _ => throw ExceptionUtilities.UnexpectedValue(argumentIndex)
                };

                argumentPlaceholdersBuilder.Add(
                    new BoundInterpolatedStringArgumentPlaceholder(
                        placeholderSyntax,
                        argumentIndex,
                        placeholderType,
                        hasErrors: argumentIndex == BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter));
                // We use the parameter refkind, rather than what the argument was actually passed with, because that will suppress duplicated errors
                // about arguments being passed with the wrong RefKind. The user will have already gotten an error about mismatched RefKinds or it will
                // be a place where refkinds are allowed to differ
                argumentRefKindsBuilder.Add(refKind);
            }

            var interpolatedString = BindUnconvertedInterpolatedStringToHandlerType(
                unconvertedString,
                (NamedTypeSymbol)interpolatedStringParameter.Type,
                diagnostics,
                isHandlerConversion: true,
                additionalConstructorArguments: argumentPlaceholdersBuilder.ToImmutableAndFree(),
                refKinds: argumentRefKindsBuilder.ToImmutableAndFree());

            return new BoundConversion(
                interpolatedString.Syntax,
                interpolatedString,
                interpolatedStringConversion,
                @checked: CheckOverflowAtRuntime,
                explicitCastInCode: false,
                conversionGroupOpt: null,
                constantValueOpt: null,
                interpolatedStringParameter.Type,
                hasErrors || interpolatedString.HasErrors);
        }
    }
}
