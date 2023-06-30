// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
            if (CheckFeatureAvailability(node, MessageID.IDS_FeatureInterpolatedStrings, diagnostics))
            {
                // Only bother reporting an issue for raw string literals if we didn't already report above that
                // interpolated strings are not allowed.
                if (node.StringStartToken.Kind() is SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken)
                {
                    CheckFeatureAvailability(node, MessageID.IDS_FeatureRawStringLiterals, diagnostics);
                }
            }

            var startText = node.StringStartToken.Text;
            if (startText.StartsWith("@$\"") && !Compilation.IsFeatureEnabled(MessageID.IDS_FeatureAltInterpolatedVerbatimStrings))
            {
                Error(diagnostics,
                    ErrorCode.ERR_AltInterpolatedVerbatimStringsNotAvailable,
                    node.StringStartToken.GetLocation(),
                    new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureAltInterpolatedVerbatimStrings.RequiredVersion()));
            }

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
                var isNonVerbatimInterpolatedString = node.StringStartToken.Kind() != SyntaxKind.InterpolatedVerbatimStringStartToken;
                var isRawInterpolatedString = node.StringStartToken.Kind() is SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken;
                var newLinesInInterpolationsAllowed = this.Compilation.IsFeatureEnabled(MessageID.IDS_FeatureNewLinesInInterpolations);

                var intType = GetSpecialType(SpecialType.System_Int32, diagnostics, node);
                foreach (var content in node.Contents)
                {
                    switch (content.Kind())
                    {
                        case SyntaxKind.Interpolation:
                            {
                                var interpolation = (InterpolationSyntax)content;

                                // If we're prior to C# 11 then we don't allow newlines in the interpolations of
                                // non-verbatim interpolated strings.  Check for that here and report an error
                                // if the interpolation spans multiple lines (and thus must have a newline).
                                //
                                // Note: don't bother doing this if the interpolation is otherwise malformed or
                                // we've already reported some other error within it.  No need to spam the user
                                // with multiple errors (esp as a malformed interpolation may commonly span multiple
                                // lines due to error recovery).
                                if (isNonVerbatimInterpolatedString &&
                                    !interpolation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error) &&
                                    !newLinesInInterpolationsAllowed &&
                                    !interpolation.OpenBraceToken.IsMissing &&
                                    !interpolation.CloseBraceToken.IsMissing)
                                {
                                    var text = node.SyntaxTree.GetText();
                                    if (text.Lines.GetLineFromPosition(interpolation.OpenBraceToken.SpanStart).LineNumber !=
                                        text.Lines.GetLineFromPosition(interpolation.CloseBraceToken.SpanStart).LineNumber)
                                    {
                                        diagnostics.Add(
                                            ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString,
                                            interpolation.CloseBraceToken.GetLocation(),
                                            this.Compilation.LanguageVersion.ToDisplayString(),
                                            new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureNewLinesInInterpolations.RequiredVersion()));
                                    }
                                }

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
                                    var alignmentConstant = alignment.ConstantValueOpt;
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
                                    value.ConstantValueOpt == null ||
                                    !(interpolation is { FormatClause: null, AlignmentClause: null }) ||
                                    !(value.ConstantValueOpt is { IsString: true, IsBad: false }))
                                {
                                    isResultConstant = false;
                                    continue;
                                }
                                resultConstant = (resultConstant is null)
                                    ? value.ConstantValueOpt
                                    : FoldStringConcatenation(BinaryOperatorKind.StringConcatenation, resultConstant, value.ConstantValueOpt);
                                continue;
                            }
                        case SyntaxKind.InterpolatedStringText:
                            {
                                var text = ((InterpolatedStringTextSyntax)content).TextToken.ValueText;
                                // Raw string literals have no escapes.  So there is no need to manipulate their value texts.
                                // We have to unescape normal interpolated strings as the parser stores their text without
                                // interpreting {{ and }} sequences (as '{' and '}') respectively.  Changing that at the syntax
                                // level might potentially be a breaking change, so we do the conversion here when creating the
                                // bound nodes.
                                if (!isRawInterpolatedString)
                                {
                                    text = unescapeInterpolatedStringLiteral(text);
                                }

                                var constantValue = ConstantValue.Create(text, SpecialType.System_String);
                                builder.Add(new BoundLiteral(content, constantValue, stringType));
                                if (isResultConstant)
                                {
                                    resultConstant = resultConstant is null
                                        ? constantValue
                                        : FoldStringConcatenation(BinaryOperatorKind.StringConcatenation, resultConstant, constantValue);
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

            static string unescapeInterpolatedStringLiteral(string value)
            {
                var builder = PooledStringBuilder.GetInstance();
                var stringBuilder = builder.Builder;
                for (int i = 0, formatLength = value.Length; i < formatLength; i++)
                {
                    var c = value[i];
                    stringBuilder.Append(c);
                    if (c is '{' or '}' &&
                        i + 1 < formatLength &&
                        value[i + 1] == c)
                    {
                        i++;
                    }
                }

                // Avoid unnecessary allocation in the common case of no escaped curlies.
                var result = builder.Length == value.Length
                    ? value
                    : builder.Builder.ToString();
                builder.Free();

                return result;
            }
        }

        private BoundInterpolatedString BindUnconvertedInterpolatedStringToString(BoundUnconvertedInterpolatedString unconvertedInterpolatedString, BindingDiagnosticBag diagnostics)
        {
            // We have 4 possible lowering strategies, dependent on the contents of the string, in this order:
            //  1. The string is a constant value. We can just use the final value.
            //  2. The string is composed of 4 or fewer components that are all strings, we can lower to a call to string.Concat without a
            //     params array. This is very efficient as the runtime can allocate a buffer for the string with exactly the correct length and
            //     make no intermediate allocations.
            //  3. The WellKnownType DefaultInterpolatedStringHandler is available, and none of the interpolation holes contain an await expression.
            //     The builder is a ref struct, and we can guarantee the lifetime won't outlive the stack if the string doesn't contain any
            //     awaits, but if it does we cannot use it. This builder is the only way that ref structs can be directly used as interpolation
            //     hole components, which means that ref structs components and await expressions cannot be combined. It is already illegal for
            //     the user to use ref structs in an async method today, but if that were to ever change, this would still need to be respected.
            //     We also cannot use this method if the interpolated string appears within a catch filter, as the builder is disposable and we
            //     cannot put a try/finally inside a filter block.
            //  4. The string is composed of more than 4 components that are all strings themselves. We can turn this into a single
            //     call to string.Concat. We prefer the builder over this because the builder can use pooling to avoid new allocations, while this
            //     call will need to allocate a param array.
            //  5. The string has heterogeneous data and either InterpolatedStringHandler is unavailable, or one of the holes contains an await
            //     expression. This is turned into a call to string.Format.
            //
            // We need to do the determination of 1, 2, 3, or 4/5 up front, rather than in lowering, as it affects diagnostics (ref structs not being
            // able to be used, for example). However, between 4 and 5, we don't need to know at this point, so that logic is deferred for lowering.

            if (unconvertedInterpolatedString.ConstantValueOpt is not null)
            {
                // Case 1
                Debug.Assert(unconvertedInterpolatedString.Parts.All(static part => part.Type is null or { SpecialType: SpecialType.System_String }));
                return constructWithData(BindInterpolatedStringParts(unconvertedInterpolatedString, diagnostics), data: null);
            }

            // Case 2. Attempt to see if all parts are strings.
            if (unconvertedInterpolatedString.Parts.Length <= 4 && AllInterpolatedStringPartsAreStrings(unconvertedInterpolatedString.Parts))
            {
                return constructWithData(BindInterpolatedStringParts(unconvertedInterpolatedString, diagnostics), data: null);
            }

            if (tryBindAsHandlerType(out var result))
            {
                // Case 3
                return result;
            }

            // The specifics of 4 vs 5 aren't necessary for this stage of binding. The only thing that matters is that every part needs to be convertible
            // object.
            return constructWithData(BindInterpolatedStringParts(unconvertedInterpolatedString, diagnostics), data: null);

            BoundInterpolatedString constructWithData(ImmutableArray<BoundExpression> parts, InterpolatedStringHandlerData? data)
                => new BoundInterpolatedString(
                    unconvertedInterpolatedString.Syntax,
                    data,
                    parts,
                    unconvertedInterpolatedString.ConstantValueOpt,
                    unconvertedInterpolatedString.Type,
                    unconvertedInterpolatedString.HasErrors);

            bool tryBindAsHandlerType([NotNullWhen(true)] out BoundInterpolatedString? result)
            {
                result = null;

                if (InExpressionTree || !InterpolatedStringPartsAreValidInDefaultHandler(unconvertedInterpolatedString))
                {
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

        private static bool InterpolatedStringPartsAreValidInDefaultHandler(BoundUnconvertedInterpolatedString unconvertedInterpolatedString)
            => !unconvertedInterpolatedString.Parts.ContainsAwaitExpression()
               && unconvertedInterpolatedString.Parts.All(p => p is not BoundStringInsert { Value.Type.TypeKind: TypeKind.Dynamic });

        private static bool AllInterpolatedStringPartsAreStrings(ImmutableArray<BoundExpression> parts)
            => parts.All(p => p is BoundLiteral or BoundStringInsert { Value.Type.SpecialType: SpecialType.System_String, Alignment: null, Format: null });

        private bool TryBindUnconvertedBinaryOperatorToDefaultInterpolatedStringHandler(BoundBinaryOperator binaryOperator, BindingDiagnosticBag diagnostics, [NotNullWhen(true)] out BoundBinaryOperator? convertedBinaryOperator)
        {
            // Much like BindUnconvertedInterpolatedStringToString above, we only want to use DefaultInterpolatedStringHandler if it's worth it. We therefore
            // check for cases 1 and 2: if they are present, we let normal string binary operator binding machinery handle it. Otherwise, we take care of it ourselves.
            Debug.Assert(binaryOperator.IsUnconvertedInterpolatedStringAddition);
            convertedBinaryOperator = null;

            if (InExpressionTree)
            {
                return false;
            }

            var interpolatedStringHandlerType = Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_DefaultInterpolatedStringHandler);
            if (interpolatedStringHandlerType.IsErrorType())
            {
                // Can't ever bind to the handler no matter what, so just let the default handling take care of it. Cases 4 and 5 are covered by this.
                return false;
            }

            // The constant value is folded as part of creating the unconverted operator. If there is a constant value, then the top-level binary operator
            // will have one.
            if (binaryOperator.ConstantValueOpt is not null)
            {
                // This is case 1. Let the standard machinery handle it
                return false;
            }
            var partsArrayBuilder = ArrayBuilder<ImmutableArray<BoundExpression>>.GetInstance();

            if (!binaryOperator.VisitBinaryOperatorInterpolatedString(
                    partsArrayBuilder,
                    static (BoundUnconvertedInterpolatedString unconvertedInterpolatedString, ArrayBuilder<ImmutableArray<BoundExpression>> partsArrayBuilder) =>
                    {
                        if (!InterpolatedStringPartsAreValidInDefaultHandler(unconvertedInterpolatedString))
                        {
                            return false;
                        }

                        partsArrayBuilder.Add(unconvertedInterpolatedString.Parts);
                        return true;
                    }))
            {
                partsArrayBuilder.Free();
                return false;
            }

            Debug.Assert(partsArrayBuilder.Count >= 2);

            if (partsArrayBuilder.Count <= 4 && partsArrayBuilder.All(static parts => AllInterpolatedStringPartsAreStrings(parts)))
            {
                // This is case 2. Let the standard machinery handle it
                partsArrayBuilder.Free();
                return false;
            }

            // Case 3. Bind as handler.
            var (appendCalls, data) = BindUnconvertedInterpolatedPartsToHandlerType(
                binaryOperator.Syntax,
                partsArrayBuilder.ToImmutableAndFree(),
                interpolatedStringHandlerType,
                diagnostics,
                isHandlerConversion: false,
                additionalConstructorArguments: default,
                additionalConstructorRefKinds: default);

            // Now that the parts have been bound, reconstruct the binary operators.
            convertedBinaryOperator = UpdateBinaryOperatorWithInterpolatedContents(binaryOperator, appendCalls, data, binaryOperator.Syntax, diagnostics);
            return true;
        }

        private BoundBinaryOperator UpdateBinaryOperatorWithInterpolatedContents(BoundBinaryOperator originalOperator, ImmutableArray<ImmutableArray<BoundExpression>> appendCalls, InterpolatedStringHandlerData data, SyntaxNode rootSyntax, BindingDiagnosticBag diagnostics)
        {
            var @string = GetSpecialType(SpecialType.System_String, diagnostics, rootSyntax);

            Func<BoundUnconvertedInterpolatedString, int, (ImmutableArray<ImmutableArray<BoundExpression>>, TypeSymbol), BoundExpression> interpolationFactory =
                createInterpolation;
            Func<BoundBinaryOperator, BoundExpression, BoundExpression, (ImmutableArray<ImmutableArray<BoundExpression>>, TypeSymbol), BoundExpression> binaryOperatorFactory =
                createBinaryOperator;

            var rewritten = (BoundBinaryOperator)originalOperator.RewriteInterpolatedStringAddition((appendCalls, @string), interpolationFactory, binaryOperatorFactory);

            return rewritten.Update(BoundBinaryOperator.UncommonData.InterpolatedStringHandlerAddition(data));

            static BoundInterpolatedString createInterpolation(BoundUnconvertedInterpolatedString expression, int i, (ImmutableArray<ImmutableArray<BoundExpression>> AppendCalls, TypeSymbol _) arg)
            {
                Debug.Assert(arg.AppendCalls.Length > i);
                return new BoundInterpolatedString(
                    expression.Syntax,
                    interpolationData: null,
                    arg.AppendCalls[i],
                    expression.ConstantValueOpt,
                    expression.Type,
                    expression.HasErrors);
            }

            static BoundBinaryOperator createBinaryOperator(BoundBinaryOperator original, BoundExpression left, BoundExpression right, (ImmutableArray<ImmutableArray<BoundExpression>> _, TypeSymbol @string) arg)
                => new BoundBinaryOperator(
                    original.Syntax,
                    BinaryOperatorKind.StringConcatenation,
                    left,
                    right,
                    original.ConstantValueOpt,
                    methodOpt: null,
                    constrainedToTypeOpt: null,
                    LookupResultKind.Viable,
                    originalUserDefinedOperatorsOpt: default,
                    arg.@string,
                    original.HasErrors);
        }

        private BoundExpression BindUnconvertedInterpolatedExpressionToHandlerType(
            BoundExpression unconvertedExpression,
            NamedTypeSymbol interpolatedStringHandlerType,
            BindingDiagnosticBag diagnostics,
            ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> additionalConstructorArguments = default,
            ImmutableArray<RefKind> additionalConstructorRefKinds = default)
            => unconvertedExpression switch
            {
                BoundUnconvertedInterpolatedString interpolatedString => BindUnconvertedInterpolatedStringToHandlerType(
                    interpolatedString,
                    interpolatedStringHandlerType,
                    diagnostics,
                    isHandlerConversion: true,
                    additionalConstructorArguments,
                    additionalConstructorRefKinds),
                BoundBinaryOperator binary => BindUnconvertedBinaryOperatorToInterpolatedStringHandlerType(binary, interpolatedStringHandlerType, diagnostics, additionalConstructorArguments, additionalConstructorRefKinds),
                _ => throw ExceptionUtilities.UnexpectedValue(unconvertedExpression.Kind)
            };

        private BoundInterpolatedString BindUnconvertedInterpolatedStringToHandlerType(
            BoundUnconvertedInterpolatedString unconvertedInterpolatedString,
            NamedTypeSymbol interpolatedStringHandlerType,
            BindingDiagnosticBag diagnostics,
            bool isHandlerConversion,
            ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> additionalConstructorArguments = default,
            ImmutableArray<RefKind> additionalConstructorRefKinds = default)
        {
            var (appendCalls, interpolationData) = BindUnconvertedInterpolatedPartsToHandlerType(
                unconvertedInterpolatedString.Syntax,
                ImmutableArray.Create(unconvertedInterpolatedString.Parts),
                interpolatedStringHandlerType, diagnostics,
                isHandlerConversion,
                additionalConstructorArguments,
                additionalConstructorRefKinds);

            Debug.Assert(appendCalls.Length == 1);

            return new BoundInterpolatedString(
                unconvertedInterpolatedString.Syntax,
                interpolationData,
                appendCalls[0],
                unconvertedInterpolatedString.ConstantValueOpt,
                unconvertedInterpolatedString.Type,
                unconvertedInterpolatedString.HasErrors);
        }

        private BoundBinaryOperator BindUnconvertedBinaryOperatorToInterpolatedStringHandlerType(
            BoundBinaryOperator binaryOperator,
            NamedTypeSymbol interpolatedStringHandlerType,
            BindingDiagnosticBag diagnostics,
            ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> additionalConstructorArguments,
            ImmutableArray<RefKind> additionalConstructorRefKinds)
        {
            Debug.Assert(binaryOperator.IsUnconvertedInterpolatedStringAddition);

            var partsArrayBuilder = ArrayBuilder<ImmutableArray<BoundExpression>>.GetInstance();

            binaryOperator.VisitBinaryOperatorInterpolatedString(partsArrayBuilder,
                static (BoundUnconvertedInterpolatedString unconvertedInterpolatedString, ArrayBuilder<ImmutableArray<BoundExpression>> partsArrayBuilder) =>
                {
                    partsArrayBuilder.Add(unconvertedInterpolatedString.Parts);
                    return true;
                });

            var (appendCalls, data) = BindUnconvertedInterpolatedPartsToHandlerType(
                binaryOperator.Syntax,
                partsArrayBuilder.ToImmutableAndFree(),
                interpolatedStringHandlerType,
                diagnostics,
                isHandlerConversion: true,
                additionalConstructorArguments,
                additionalConstructorRefKinds);

            var result = UpdateBinaryOperatorWithInterpolatedContents(binaryOperator, appendCalls, data, binaryOperator.Syntax, diagnostics);
            return result;
        }

        private (ImmutableArray<ImmutableArray<BoundExpression>> AppendCalls, InterpolatedStringHandlerData Data) BindUnconvertedInterpolatedPartsToHandlerType(
            SyntaxNode syntax,
            ImmutableArray<ImmutableArray<BoundExpression>> partsArray,
            NamedTypeSymbol interpolatedStringHandlerType,
            BindingDiagnosticBag diagnostics,
            bool isHandlerConversion,
            ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> additionalConstructorArguments,
            ImmutableArray<RefKind> additionalConstructorRefKinds)
        {
            Debug.Assert(additionalConstructorArguments.IsDefault
                ? additionalConstructorRefKinds.IsDefault
                : additionalConstructorArguments.Length == additionalConstructorRefKinds.Length);
            additionalConstructorArguments = additionalConstructorArguments.NullToEmpty();
            additionalConstructorRefKinds = additionalConstructorRefKinds.NullToEmpty();

            ReportUseSite(interpolatedStringHandlerType, diagnostics, syntax);

            // We satisfy the conditions for using an interpolated string builder. Bind all the builder calls unconditionally, so that if
            // there are errors we get better diagnostics than "could not convert to object."
            var implicitBuilderReceiver = new BoundInterpolatedStringHandlerPlaceholder(syntax, interpolatedStringHandlerType) { WasCompilerGenerated = true };
            var (appendCallsArray, usesBoolReturn, positionInfo, baseStringLength, numFormatHoles) = BindInterpolatedStringAppendCalls(partsArray, implicitBuilderReceiver, diagnostics);

            // Prior to C# 10, all types in an interpolated string expression needed to be convertible to `object`. After 10, some types
            // (such as Span<T>) that are not convertible to `object` are permissible as interpolated string components, provided there
            // is an applicable AppendFormatted method that accepts them. To preserve langversion, we therefore make sure all components
            // are convertible to object if the current langversion is lower than the interpolation feature and we're converting this
            // interpolation into an actual string.
            bool needToCheckConversionToObject = false;
            if (isHandlerConversion)
            {
                CheckFeatureAvailability(syntax, MessageID.IDS_FeatureImprovedInterpolatedStrings, diagnostics);
            }
            else if (!Compilation.IsFeatureEnabled(MessageID.IDS_FeatureImprovedInterpolatedStrings) && diagnostics.AccumulatesDiagnostics)
            {
                needToCheckConversionToObject = true;
            }

            Debug.Assert(appendCallsArray.Select(a => a.Length).SequenceEqual(partsArray.Select(a => a.Length)));
            Debug.Assert(appendCallsArray.All(appendCalls => appendCalls.All(a => a is { HasErrors: true } or BoundCall { Arguments: { Length: > 0 } } or BoundDynamicInvocation)));

            if (needToCheckConversionToObject)
            {
                TypeSymbol objectType = GetSpecialType(SpecialType.System_Object, diagnostics, syntax);
                BindingDiagnosticBag conversionDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
                foreach (var parts in partsArray)
                {
                    foreach (var currentPart in parts)
                    {
                        if (currentPart is BoundStringInsert insert)
                        {
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
                }

                conversionDiagnostics.Free();
            }

            var intType = GetSpecialType(SpecialType.System_Int32, diagnostics, syntax);
            int constructorArgumentLength = 3 + additionalConstructorArguments.Length;
            var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(constructorArgumentLength);

            var refKindsBuilder = ArrayBuilder<RefKind>.GetInstance(constructorArgumentLength);
            refKindsBuilder.Add(RefKind.None);
            refKindsBuilder.Add(RefKind.None);
            refKindsBuilder.AddRange(additionalConstructorRefKinds);

            // Add the trailing out validity parameter for the first attempt.Note that we intentionally use `diagnostics` for resolving System.Boolean,
            // because we want to track that we're using the type no matter what.
            var boolType = GetSpecialType(SpecialType.System_Boolean, diagnostics, syntax);
            var trailingConstructorValidityPlaceholder =
                new BoundInterpolatedStringArgumentPlaceholder(syntax, BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter, boolType)
                { WasCompilerGenerated = true };
            var outConstructorAdditionalArguments = additionalConstructorArguments.Add(trailingConstructorValidityPlaceholder);
            refKindsBuilder.Add(RefKind.Out);
            populateArguments(syntax, outConstructorAdditionalArguments, baseStringLength, numFormatHoles, intType, argumentsBuilder);

            BoundExpression constructorCall;
            var outConstructorDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: diagnostics.AccumulatesDependencies);
            var outConstructorCall = MakeConstructorInvocation(interpolatedStringHandlerType, argumentsBuilder, refKindsBuilder, syntax, outConstructorDiagnostics);
            if (outConstructorCall is not BoundObjectCreationExpression { ResultKind: LookupResultKind.Viable })
            {
                // MakeConstructorInvocation can call CoerceArguments on the builder if overload resolution succeeded ignoring accessibility, which
                // could still end up not succeeding, and that would end up changing the arguments. So we want to clear and repopulate.
                argumentsBuilder.Clear();

                // Try again without an out parameter.
                populateArguments(syntax, additionalConstructorArguments, baseStringLength, numFormatHoles, intType, argumentsBuilder);
                refKindsBuilder.RemoveLast();

                var nonOutConstructorDiagnostics = BindingDiagnosticBag.GetInstance(template: outConstructorDiagnostics);
                BoundExpression nonOutConstructorCall = MakeConstructorInvocation(interpolatedStringHandlerType, argumentsBuilder, refKindsBuilder, syntax, nonOutConstructorDiagnostics);

                if (nonOutConstructorCall is BoundObjectCreationExpression { ResultKind: LookupResultKind.Viable })
                {
                    // We successfully bound the out version, so set all the final data based on that binding
                    constructorCall = nonOutConstructorCall;
                    addAndFreeConstructorDiagnostics(target: diagnostics, source: nonOutConstructorDiagnostics);
                    outConstructorDiagnostics.Free();
                }
                else
                {
                    // We'll attempt to figure out which failure was "best" by looking to see if one failed to bind because it couldn't find
                    // a constructor with the correct number of arguments. We presume that, if one failed for this reason and the other failed
                    // for a different reason, that different reason is the one the user will want to know about. If both or neither failed
                    // because of this error, we'll report everything.

                    // https://github.com/dotnet/roslyn/issues/54396 Instead of inspecting errors, we should be capturing the results of overload
                    // resolution and attempting to determine which method considered was the best to report errors for.

                    var nonOutConstructorHasArityError = nonOutConstructorDiagnostics.DiagnosticBag?.AsEnumerableWithoutResolution().Any(d => (ErrorCode)d.Code == ErrorCode.ERR_BadCtorArgCount) ?? false;
                    var outConstructorHasArityError = outConstructorDiagnostics.DiagnosticBag?.AsEnumerableWithoutResolution().Any(d => (ErrorCode)d.Code == ErrorCode.ERR_BadCtorArgCount) ?? false;

                    switch ((nonOutConstructorHasArityError, outConstructorHasArityError))
                    {
                        case (true, false):
                            constructorCall = outConstructorCall;
                            additionalConstructorArguments = outConstructorAdditionalArguments;
                            addAndFreeConstructorDiagnostics(target: diagnostics, source: outConstructorDiagnostics);
                            nonOutConstructorDiagnostics.Free();
                            break;
                        case (false, true):
                            constructorCall = nonOutConstructorCall;
                            addAndFreeConstructorDiagnostics(target: diagnostics, source: nonOutConstructorDiagnostics);
                            outConstructorDiagnostics.Free();
                            break;
                        default:
                            // For the final output binding info, we'll go with the shorter constructor in the absence of any tiebreaker,
                            // but we'll report all diagnostics
                            constructorCall = nonOutConstructorCall;
                            addAndFreeConstructorDiagnostics(target: diagnostics, source: nonOutConstructorDiagnostics);
                            addAndFreeConstructorDiagnostics(target: diagnostics, source: outConstructorDiagnostics);
                            break;
                    }
                }
            }
            else
            {
                diagnostics.AddRangeAndFree(outConstructorDiagnostics);
                constructorCall = outConstructorCall;
                additionalConstructorArguments = outConstructorAdditionalArguments;
            }

            argumentsBuilder.Free();
            refKindsBuilder.Free();

            Debug.Assert(constructorCall.HasErrors || constructorCall is BoundObjectCreationExpression or BoundDynamicObjectCreationExpression);

            if (constructorCall is BoundDynamicObjectCreationExpression)
            {
                // An interpolated string handler construction cannot use dynamic. Manually construct an instance of '{0}'.
                diagnostics.Add(ErrorCode.ERR_InterpolatedStringHandlerCreationCannotUseDynamic, syntax.Location, interpolatedStringHandlerType.Name);
            }

            var interpolationData = new InterpolatedStringHandlerData(
                                interpolatedStringHandlerType,
                                constructorCall,
                                usesBoolReturn,
                                additionalConstructorArguments.NullToEmpty(),
                                positionInfo,
                                implicitBuilderReceiver);

            return (appendCallsArray, interpolationData);

            static void populateArguments(SyntaxNode syntax, ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> additionalConstructorArguments, int baseStringLength, int numFormatHoles, NamedTypeSymbol intType, ArrayBuilder<BoundExpression> argumentsBuilder)
            {
                // literalLength
                argumentsBuilder.Add(new BoundLiteral(syntax, ConstantValue.Create(baseStringLength), intType) { WasCompilerGenerated = true });
                // formattedCount
                argumentsBuilder.Add(new BoundLiteral(syntax, ConstantValue.Create(numFormatHoles), intType) { WasCompilerGenerated = true });
                // Any other arguments from the call site
                argumentsBuilder.AddRange(additionalConstructorArguments);
            }

            static void addAndFreeConstructorDiagnostics(BindingDiagnosticBag target, BindingDiagnosticBag source)
            {
                target.AddDependencies(source);

                if (source.DiagnosticBag is { IsEmptyWithoutResolution: false } bag)
                {
                    foreach (var diagnostic in bag.AsEnumerableWithoutResolution())
                    {
                        // Filter diagnostics that cannot be fixed since they are on the hidden interpolated string constructor.
                        if (!((ErrorCode)diagnostic.Code is ErrorCode.WRN_BadArgRef
                            or ErrorCode.WRN_RefReadonlyNotVariable
                            or ErrorCode.WRN_ArgExpectedRefOrIn
                            or ErrorCode.WRN_ArgExpectedIn))
                        {
                            target.Add(diagnostic);
                        }
                    }
                }

                source.Free();
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

        private (ImmutableArray<ImmutableArray<BoundExpression>> AppendFormatCalls, bool UsesBoolReturn, ImmutableArray<ImmutableArray<(bool IsLiteral, bool HasAlignment, bool HasFormat)>>, int BaseStringLength, int NumFormatHoles) BindInterpolatedStringAppendCalls(
            ImmutableArray<ImmutableArray<BoundExpression>> partsArray,
            BoundInterpolatedStringHandlerPlaceholder implicitBuilderReceiver,
            BindingDiagnosticBag diagnostics)
        {
            if (partsArray.IsEmpty && partsArray.All(p => p.IsEmpty))
            {
                return (ImmutableArray<ImmutableArray<BoundExpression>>.Empty, false, ImmutableArray<ImmutableArray<(bool IsLiteral, bool HasAlignment, bool HasFormat)>>.Empty, 0, 0);
            }

            bool? builderPatternExpectsBool = null;
            var firstPartsLength = partsArray[0].Length;
            var builderAppendCallsArray = ArrayBuilder<ImmutableArray<BoundExpression>>.GetInstance(partsArray.Length);
            var builderAppendCalls = ArrayBuilder<BoundExpression>.GetInstance(firstPartsLength);
            var positionInfoArray = ArrayBuilder<ImmutableArray<(bool IsLiteral, bool HasAlignment, bool HasFormat)>>.GetInstance(partsArray.Length);
            var positionInfo = ArrayBuilder<(bool IsLiteral, bool HasAlignment, bool HasFormat)>.GetInstance(firstPartsLength);
            var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(3);
            var parameterNamesAndLocationsBuilder = ArrayBuilder<(string, Location)?>.GetInstance(3);
            int baseStringLength = 0;
            int numFormatHoles = 0;

            foreach (var parts in partsArray)
            {
                foreach (var part in parts)
                {
                    Debug.Assert(part is BoundLiteral or BoundStringInsert);
                    string methodName;
                    bool isLiteral;
                    bool hasAlignment;
                    bool hasFormat;

                    if (part is BoundStringInsert insert)
                    {
                        methodName = BoundInterpolatedString.AppendFormattedMethod;
                        argumentsBuilder.Add(insert.Value);
                        parameterNamesAndLocationsBuilder.Add(null);
                        isLiteral = false;
                        hasAlignment = false;
                        hasFormat = false;

                        if (insert.Alignment is not null)
                        {
                            hasAlignment = true;
                            argumentsBuilder.Add(insert.Alignment);
                            parameterNamesAndLocationsBuilder.Add(("alignment", insert.Alignment.Syntax.Location));
                        }
                        if (insert.Format is not null)
                        {
                            hasFormat = true;
                            argumentsBuilder.Add(insert.Format);
                            parameterNamesAndLocationsBuilder.Add(("format", insert.Format.Syntax.Location));
                        }
                        numFormatHoles++;
                    }
                    else
                    {
                        var boundLiteral = (BoundLiteral)part;
                        Debug.Assert(boundLiteral.ConstantValueOpt != null && boundLiteral.ConstantValueOpt.IsString);
                        var literalText = boundLiteral.ConstantValueOpt.StringValue;
                        methodName = BoundInterpolatedString.AppendLiteralMethod;
                        argumentsBuilder.Add(boundLiteral.Update(ConstantValue.Create(literalText), boundLiteral.Type));
                        isLiteral = true;
                        hasAlignment = false;
                        hasFormat = false;
                        baseStringLength += literalText.Length;
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
                    positionInfo.Add((isLiteral, hasAlignment, hasFormat));

                    Debug.Assert(call is BoundCall or BoundDynamicInvocation or { HasErrors: true });

                    // We just assume that dynamic is going to do the right thing, and runtime will fail if it does not. If there are only dynamic calls, we assume that
                    // void is returned.
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

                builderAppendCallsArray.Add(builderAppendCalls.ToImmutableAndClear());
                positionInfoArray.Add(positionInfo.ToImmutableAndClear());
            }

            argumentsBuilder.Free();
            parameterNamesAndLocationsBuilder.Free();
            builderAppendCalls.Free();
            positionInfo.Free();
            return (builderAppendCallsArray.ToImmutableAndFree(), builderPatternExpectsBool ?? false, positionInfoArray.ToImmutableAndFree(), baseStringLength, numFormatHoles);
        }

        private BoundExpression BindInterpolatedStringHandlerInMemberCall(
            BoundExpression unconvertedString,
            ArrayBuilder<BoundExpression> arguments,
            ImmutableArray<ParameterSymbol> parameters,
            ref MemberAnalysisResult memberAnalysisResult,
            int interpolatedStringArgNum,
            BoundExpression? receiver,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(unconvertedString is BoundUnconvertedInterpolatedString or BoundBinaryOperator { IsUnconvertedInterpolatedStringAddition: true });
            var interpolatedStringConversion = memberAnalysisResult.ConversionForArg(interpolatedStringArgNum);
            Debug.Assert(interpolatedStringConversion.IsInterpolatedStringHandler);
            var interpolatedStringParameter = GetCorrespondingParameter(ref memberAnalysisResult, parameters, interpolatedStringArgNum);
            Debug.Assert(interpolatedStringParameter is { Type: NamedTypeSymbol { IsInterpolatedStringHandlerType: true } }
#pragma warning disable format
                                                     or
                                                     {
                                                         IsParams: true,
                                                         Type: ArrayTypeSymbol { ElementType: NamedTypeSymbol { IsInterpolatedStringHandlerType: true } },
                                                         InterpolatedStringHandlerArgumentIndexes.IsEmpty: true
                                                     });
#pragma warning restore format
            Debug.Assert(!interpolatedStringParameter.IsParams || memberAnalysisResult.Kind == MemberResolutionKind.ApplicableInExpandedForm);

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
                    interpolatedStringParameter.IsParams ? ((ArrayTypeSymbol)interpolatedStringParameter.Type).ElementType : interpolatedStringParameter.Type,
                    diagnostics);
            }

            Debug.Assert(handlerParameterIndexes.All((index, paramLength) => index >= BoundInterpolatedStringArgumentPlaceholder.InstanceParameter && index < paramLength,
                                                     parameters.Length));

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
                for (int handlerParameterIndex = 0; handlerParameterIndex < handlerParameterIndexes.Length; handlerParameterIndex++)
                {
                    int handlerParameter = handlerParameterIndexes[handlerParameterIndex];
                    Debug.Assert(handlerArgumentIndexesBuilder[handlerParameterIndex] is BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter);

                    if (handlerParameter == BoundInterpolatedStringArgumentPlaceholder.InstanceParameter)
                    {
                        handlerArgumentIndexesBuilder[handlerParameterIndex] = handlerParameter;
                        continue;
                    }

                    for (int argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
                    {
                        // The index in the original parameter list we're looking to match up.
                        int argumentParameterIndex = memberAnalysisResult.ParameterFromArgument(argumentIndex);
                        // Is the original parameter index of the current argument the parameter index that was specified in the attribute?
                        if (argumentParameterIndex == handlerParameter)
                        {
                            // We can't just bail out on the first match: users can duplicate parameters in attributes, causing the same value to be passed twice.
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
                switch (argumentIndex)
                {
                    case BoundInterpolatedStringArgumentPlaceholder.InstanceParameter:
                        Debug.Assert(receiver!.Type is not null);
                        refKind = RefKind.None;
                        placeholderType = receiver.Type;
                        break;
                    case BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter:
                        {
                            // Don't error if the parameter isn't optional or params: the user will already have an error for missing an optional parameter or overload resolution failed.
                            // If it is optional, then they could otherwise not specify the parameter and that's an error
                            var originalParameterIndex = handlerParameterIndexes[i];
                            var parameter = parameters[originalParameterIndex];
                            if (parameter.IsOptional || (originalParameterIndex + 1 == parameters.Length && OverloadResolution.IsValidParamsParameter(parameter)))
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
                        break;
                    default:
                        {
                            var originalParameterIndex = handlerParameterIndexes[i];
                            var parameter = parameters[originalParameterIndex];
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
                        break;
                }

                SyntaxNode placeholderSyntax;
                bool isSuppressed;

                switch (argumentIndex)
                {
                    case BoundInterpolatedStringArgumentPlaceholder.InstanceParameter:
                        Debug.Assert(receiver != null);
                        isSuppressed = receiver.IsSuppressed;
                        placeholderSyntax = receiver.Syntax;
                        break;
                    case BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter:
                        placeholderSyntax = unconvertedString.Syntax;
                        isSuppressed = false;
                        break;
                    case >= 0:
                        placeholderSyntax = arguments[argumentIndex].Syntax;
                        isSuppressed = arguments[argumentIndex].IsSuppressed;
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(argumentIndex);
                }

                argumentPlaceholdersBuilder.Add(
                    (BoundInterpolatedStringArgumentPlaceholder)(new BoundInterpolatedStringArgumentPlaceholder(
                        placeholderSyntax,
                        argumentIndex,
                        placeholderType,
                        hasErrors: argumentIndex == BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter)
                    { WasCompilerGenerated = true }.WithSuppression(isSuppressed)));
                // We use the parameter refkind, rather than what the argument was actually passed with, because that will suppress duplicated errors
                // about arguments being passed with the wrong RefKind. The user will have already gotten an error about mismatched RefKinds or it will
                // be a place where refkinds are allowed to differ
                argumentRefKindsBuilder.Add(refKind);
            }

            var interpolatedString = BindUnconvertedInterpolatedExpressionToHandlerType(
                unconvertedString,
                (NamedTypeSymbol)interpolatedStringParameter.Type,
                diagnostics,
                additionalConstructorArguments: argumentPlaceholdersBuilder.ToImmutableAndFree(),
                additionalConstructorRefKinds: argumentRefKindsBuilder.ToImmutableAndFree());

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
