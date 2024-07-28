// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private BoundExpression RewriteInterpolatedStringConversion(BoundConversion conversion)
        {
            Debug.Assert(conversion.ConversionKind == ConversionKind.InterpolatedString);
            BoundExpression format;
            ArrayBuilder<BoundExpression> expressions;
            // Some cases can be optimizable in MakeInterpolatedStringFormat with the usingDirectlyAsString parameter:
            // e.g. FormattableString.Invariant($"Copyright (c) {year} {CompanyNameConst}")
            // However, this API has been superseded by InterpolatedStringHandler and string.Create, so it does not deserve to be optimized and should be kept as is.
            MakeInterpolatedStringFormat((BoundInterpolatedString)conversion.Operand, out format, out expressions);
            expressions.Insert(0, format);
            var stringFactory = _factory.WellKnownType(WellKnownType.System_Runtime_CompilerServices_FormattableStringFactory);

            // The normal pattern for lowering is to lower subtrees before the enclosing tree. However we cannot lower
            // the arguments first in this situation because we do not know what conversions will be
            // produced for the arguments until after we've done overload resolution. So we produce the invocation
            // and then lower it along with its arguments.
            var result = _factory.StaticCall(stringFactory, "Create", disallowExpandedNonArrayParams: _inExpressionLambda, expressions.ToImmutableAndFree(),
                ignoreNormalFormIfHasValidParamsParameter: true // if an interpolation expression is the null literal, it should not match a params parameter.
                );
            if (!result.HasAnyErrors)
            {
                result = VisitExpression(result); // lower the arguments AND handle expanded form, argument conversions, etc.
                result = MakeImplicitConversionForInterpolatedString(result, conversion.Type);
            }

            return result;
        }

        /// <summary>
        /// Helper method to generate a lowered conversion from the given <paramref name="rewrittenOperand"/> to the given <paramref name="rewrittenType"/>.
        /// </summary>
        private BoundExpression MakeImplicitConversionForInterpolatedString(BoundExpression rewrittenOperand, TypeSymbol rewrittenType)
        {
            Debug.Assert(rewrittenOperand.Type is object);

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo();
            Conversion conversion = _compilation.Conversions.ClassifyConversionFromType(rewrittenOperand.Type, rewrittenType, isChecked: false, ref useSiteInfo);
            _diagnostics.Add(rewrittenOperand.Syntax, useSiteInfo);
            if (!conversion.IsImplicit)
            {
                // error CS0029: Cannot implicitly convert type '{0}' to '{1}'
                _diagnostics.Add(
                    ErrorCode.ERR_NoImplicitConv,
                    rewrittenOperand.Syntax.Location,
                    rewrittenOperand.Type,
                    rewrittenType);

                return _factory.NullOrDefault(rewrittenType);
            }

            // The lack of checks is unlikely to create problems because we are operating on types coming from well-known APIs.
            // It is not worth adding complexity of performing them.
            conversion.MarkUnderlyingConversionsCheckedRecursive();

            return MakeConversionNode(rewrittenOperand.Syntax, rewrittenOperand, conversion, rewrittenType, @checked: false);
        }

        /// <summary>
        /// Rewrites the given interpolated string to the set of handler creation and Append calls, returning an array builder of the append calls and the result
        /// local temp.
        /// </summary>
        /// <remarks>Caller is responsible for freeing the ArrayBuilder</remarks>
        private InterpolationHandlerResult RewriteToInterpolatedStringHandlerPattern(InterpolatedStringHandlerData data, ImmutableArray<BoundExpression> parts, SyntaxNode syntax, int increasedLiteralLength = 0, int filledHolesCount = 0)
        {
            Debug.Assert(parts.All(static p => p is BoundCall or BoundDynamicInvocation));
            var builderTempSymbol = _factory.InterpolatedStringHandlerLocal(data.BuilderType, syntax);
            BoundLocal builderTemp = _factory.Local(builderTempSymbol);

            // var handler = new HandlerType(baseStringLength, numFormatHoles, ...InterpolatedStringHandlerArgumentAttribute parameters, <optional> out bool appendShouldProceed);
            var construction = (BoundObjectCreationExpression)data.Construction;
            if ((increasedLiteralLength, filledHolesCount) != (0, 0))
            {
                // Adjusts the arguments of the construction (length of literal parts, number of holes),
                // and swaps the constructor expression for a new one with the adjusted arguments.
                var argumentBuilder = ArrayBuilder<BoundExpression>.GetInstance(construction.Arguments.Length);
                Debug.Assert(construction.Arguments is [{ ConstantValueOpt.IsIntegral: true }, { ConstantValueOpt.IsIntegral: true }, ..]);
                argumentBuilder.Add(
                    increasedLiteralLength != 0
                        ? _factory.Literal(construction.Arguments[0].ConstantValueOpt!.Int32Value + increasedLiteralLength)
                        : construction.Arguments[0]
                );
                Debug.Assert(construction.Arguments[1].ConstantValueOpt!.Int32Value >= filledHolesCount);
                argumentBuilder.Add(
                    filledHolesCount != 0
                        ? _factory.Literal(construction.Arguments[1].ConstantValueOpt!.Int32Value - filledHolesCount)
                        : construction.Arguments[1]
                );
                argumentBuilder.AddRange(construction.Arguments[2..]);
                construction = _factory.New(construction.Constructor, argumentBuilder.ToImmutableAndFree());
            }

            BoundLocal? appendShouldProceedLocal = null;
            if (data.HasTrailingHandlerValidityParameter)
            {
#if DEBUG
                for (int i = construction.ArgumentRefKindsOpt.Length - 1; i >= 0; i--)
                {
                    if (construction.ArgumentRefKindsOpt[i] == RefKind.Out)
                    {
                        break;
                    }

                    Debug.Assert(construction.ArgumentRefKindsOpt[i] == RefKind.None);
                    Debug.Assert(construction.DefaultArguments[i]);
                }
#endif

                BoundInterpolatedStringArgumentPlaceholder trailingParameter = data.ArgumentPlaceholders[^1];
                TypeSymbol localType = trailingParameter.Type;
                Debug.Assert(localType.SpecialType == SpecialType.System_Boolean);
                var outLocal = _factory.SynthesizedLocal(localType);
                appendShouldProceedLocal = _factory.Local(outLocal);

                AddPlaceholderReplacement(trailingParameter, appendShouldProceedLocal);
            }

            var handlerConstructionAssignment = _factory.AssignmentExpression(builderTemp, (BoundExpression)VisitObjectCreationExpression(construction));

            AddPlaceholderReplacement(data.ReceiverPlaceholder, builderTemp);
            bool usesBoolReturns = data.UsesBoolReturns;
            var resultExpressions = ArrayBuilder<BoundExpression>.GetInstance(parts.Length + 1);

            foreach (var part in parts)
            {
                if (part is BoundCall call)
                {
                    Debug.Assert(call.Type.SpecialType == SpecialType.System_Boolean == usesBoolReturns);
                    resultExpressions.Add((BoundExpression)VisitCall(call));
                }
                else if (part is BoundDynamicInvocation dynamicInvocation)
                {
                    resultExpressions.Add(VisitDynamicInvocation(dynamicInvocation, resultDiscarded: !usesBoolReturns));
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(part.Kind);
                }
            }

            RemovePlaceholderReplacement(data.ReceiverPlaceholder);

            if (appendShouldProceedLocal is not null)
            {
                RemovePlaceholderReplacement(data.ArgumentPlaceholders[^1]);
            }

            if (usesBoolReturns)
            {
                // We assume non-bool returns if there was no parts to the string, and code below is predicated on that.
                Debug.Assert(!parts.IsEmpty);
                // Start the sequence with appendProceedLocal, if appropriate
                BoundExpression? currentExpression = appendShouldProceedLocal;

                var boolType = _compilation.GetSpecialType(SpecialType.System_Boolean);

                foreach (var appendCall in resultExpressions)
                {
                    var actualCall = appendCall;
                    if (actualCall.Type!.IsDynamic())
                    {
                        actualCall = _dynamicFactory.MakeDynamicConversion(actualCall, isExplicit: false, isArrayIndex: false, isChecked: false, boolType).ToExpression();
                    }

                    // previousAppendCalls && appendCall
                    currentExpression = currentExpression is null
                        ? actualCall
                        : _factory.LogicalAnd(currentExpression, actualCall);
                }

                Debug.Assert(currentExpression != null);

                resultExpressions.Clear();
                resultExpressions.Add(handlerConstructionAssignment);
                resultExpressions.Add(currentExpression);
            }
            else if (appendShouldProceedLocal is not null && resultExpressions.Count > 0)
            {
                // appendCalls as expressionStatements
                var appendCallsStatements = resultExpressions.SelectAsArray(static (appendCall, @this) => (BoundStatement)@this._factory.ExpressionStatement(appendCall), this);
                resultExpressions.Free();

                // if (appendShouldProceedLocal) { appendCallsStatements }
                var resultIf = _factory.If(appendShouldProceedLocal, _factory.StatementList(appendCallsStatements));

                return new InterpolationHandlerResult(ImmutableArray.Create(_factory.ExpressionStatement(handlerConstructionAssignment), resultIf), builderTemp, appendShouldProceedLocal.LocalSymbol, this);
            }
            else
            {
                resultExpressions.Insert(0, handlerConstructionAssignment);
            }

            return new InterpolationHandlerResult(resultExpressions.ToImmutableAndFree(), builderTemp, appendShouldProceedLocal?.LocalSymbol, this);
        }

        private bool IsTreatedAsLiteralInStringConcatenation(BoundExpression expression)
        {
            return expression is { ConstantValueOpt: { IsString: true } or { IsChar: true } or { IsNull: true } };
        }

        private bool CanLowerToStringConcatenation(BoundInterpolatedString node)
        {
            foreach (var part in node.Parts)
            {
                if (part is BoundStringInsert fillin)
                {
                    // this is one of the expression holes
                    if (_inExpressionLambda ||
                        fillin.HasErrors ||
                        fillin.Alignment != null ||
                        fillin.Format != null)
                    {
                        return false;
                    }
                    if (!IsTreatedAsLiteralInStringConcatenation(fillin.Value) &&
                        fillin.Value.Type is not { SpecialType: SpecialType.System_String })
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void MakeInterpolatedStringFormat(BoundInterpolatedString node, out BoundExpression format, out ArrayBuilder<BoundExpression> expressions, bool usingDirectlyAsString = false)
        {
            _factory.Syntax = node.Syntax;
            int n = node.Parts.Length - 1;
            var formatString = PooledStringBuilder.GetInstance();
            var stringBuilder = formatString.Builder;
            expressions = ArrayBuilder<BoundExpression>.GetInstance(n + 1);
            int nextFormatPosition = 0;
            for (int i = 0; i <= n; i++)
            {
                var part = node.Parts[i];
                if (part is BoundStringInsert fillin)
                {
                    // string or char constants without alignment or format
                    // Dedicated for interpolated strings treated as string
                    // (Something like GetArguments() might be called for most ones treated as IFormattable or FormattableString)
                    if (usingDirectlyAsString && fillin is { Alignment: null, Format: null, Value.ConstantValueOpt: { } constantValueOpt })
                    {
                        switch (constantValueOpt)
                        {
                            case { IsChar: true, CharValue: char ch }:
                                stringBuilder.Append(escapeInterpolatedStringLiteral(ch.ToString()));
                                continue;

                            case { IsString: true, StringValue: var str }:
                                stringBuilder.Append(escapeInterpolatedStringLiteral(str ?? ""));
                                continue;

                            case { IsNull: true }:
                                // null literal for string? type
                                continue;
                        }
                    }
                    // this is one of the expression holes
                    stringBuilder.Append('{').Append(nextFormatPosition++);
                    if (fillin.Alignment != null && !fillin.Alignment.HasErrors)
                    {
                        Debug.Assert(fillin.Alignment.ConstantValueOpt is { });
                        stringBuilder.Append(',').Append(fillin.Alignment.ConstantValueOpt.Int64Value);
                    }
                    if (fillin.Format != null && !fillin.Format.HasErrors)
                    {
                        Debug.Assert(fillin.Format.ConstantValueOpt is { });
                        stringBuilder.Append(':').Append(fillin.Format.ConstantValueOpt.StringValue);
                    }
                    stringBuilder.Append('}');
                    var value = fillin.Value;
                    if (value.Type?.TypeKind == TypeKind.Dynamic)
                    {
                        value = MakeConversionNode(value, _compilation.ObjectType, @checked: false);
                    }

                    expressions.Add(value); // NOTE: must still be lowered
                }
                else
                {
                    Debug.Assert(part is BoundLiteral && part.ConstantValueOpt?.StringValue != null);
                    // this is one of the literal parts.  If it contains a { or } then we need to escape those so that
                    // they're treated the same way in string.Format.
                    stringBuilder.Append(escapeInterpolatedStringLiteral(part.ConstantValueOpt.StringValue));
                }
            }

            format = _factory.StringLiteral(formatString.ToStringAndFree());
            return;

            static string escapeInterpolatedStringLiteral(string value)
            {
                var builder = PooledStringBuilder.GetInstance();
                var stringBuilder = builder.Builder;
                foreach (var c in value)
                {
                    stringBuilder.Append(c);
                    if (c is '{' or '}')
                    {
                        stringBuilder.Append(c);
                    }
                }

                // Avoid unnecessary allocation in the common case of nothing to escape.
                var result = builder.Length == value.Length
                    ? value
                    : builder.Builder.ToString();
                builder.Free();

                return result;
            }
        }

        public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
        {
            Debug.Assert(node.Type is { SpecialType: SpecialType.System_String }); // if target-converted, we should not get here.

            BoundExpression? result;

            // We must not optimize if we are in an expression tree because the result can be observed.
            // e.g.
            //     System.Linq.Expressions.Expression<Func<string, string>> expression = s => $"string: {s}";
            //     Console.WriteLine(expression.ToString());
            //
            // This must output:
            //     s => Format("string: {0}", s)
            // Therefore, we must convert string interpolations to string.Format naively there.
            bool usingDirectlyAsString = !_inExpressionLambda;

            if (node.InterpolationData is InterpolatedStringHandlerData data)
            {
                return LowerPartsToString(data, node.Parts, node.Syntax, node.Type, usingDirectlyAsString);
            }
            else if (CanLowerToStringConcatenation(node))
            {
                (result, var requiresVisitAndConversion) = LowerPartsToConcatString(node.Parts, node.Type);
                if (!requiresVisitAndConversion)
                {
                    return result;
                }
            }
            else
            {
                //
                // We lower an interpolated string into an invocation of String.Format.  For example, we translate the expression
                //
                //     $"Jenny don\'t change your number { 8675309 }"
                //
                // into
                //
                //     String.Format("Jenny don\'t change your number {0}", new object[] { 8675309 })
                //

                // Generally, we can merge string or char literals into the format string.
                //
                //     $"{nameof(argument)}'s literal is: {argument}"
                //
                // into
                //
                //     String.Format("argument's literal is: {0}", new object[] { argument })

                MakeInterpolatedStringFormat(node, out BoundExpression format, out ArrayBuilder<BoundExpression> expressions, usingDirectlyAsString);

                // The normal pattern for lowering is to lower subtrees before the enclosing tree. However we cannot lower
                // the arguments first in this situation because we do not know what conversions will be
                // produced for the arguments until after we've done overload resolution. So we produce the invocation
                // and then lower it along with its arguments.
                expressions.Insert(0, format);
                var stringType = node.Type;
                result = _factory.StaticCall(stringType, "Format", disallowExpandedNonArrayParams: _inExpressionLambda, expressions.ToImmutableAndFree(),
                    ignoreNormalFormIfHasValidParamsParameter: true // if an interpolation expression is the null literal, it should not match a params parameter.
                    );
            }

            Debug.Assert(result is { });
            if (!result.HasAnyErrors)
            {
                result = VisitExpression(result); // lower the arguments AND handle expanded form, argument conversions, etc.
                result = MakeImplicitConversionForInterpolatedString(result, node.Type);
            }
            return result;
        }

        private (BoundExpression Result, bool RequiresVisitAndConversion) LowerPartsToConcatString(ImmutableArray<BoundExpression> parts, TypeSymbol type)
        {
            BoundExpression? result = null;
            // All fill-ins, if any, are strings, and none of them have alignment or format specifiers.
            // We can lower to a more efficient string concatenation
            // The normal pattern for lowering is to lower subtrees before the enclosing tree. However in this case
            // we want to lower the entire concatenation so we get the optimizations done by that lowering (e.g. constant folding).

            int length = parts.Length;
            if (length == 0)
            {
                // $"" -> ""
                return (_factory.StringLiteral(""), false);
            }

            result = null;
            for (int i = 0; i < length; i++)
            {
                var part = parts[i];
                if (part is BoundStringInsert fillin)
                {
                    // this is one of the filled-in expressions
                    if (fillin.Value is { ConstantValueOpt: { IsChar: true, CharValue: var charLiteralValue } })
                    {
                        // Converts char to string because $"{'c'}" should be "c", not 'c'
                        part = _factory.StringLiteral(charLiteralValue.ToString());
                    }
                    else
                    {
                        part = fillin.Value;
                    }
                }
                else
                {
                    // this is one of the literal parts
                    Debug.Assert(part is BoundLiteral && part.ConstantValueOpt?.StringValue is not null);
                    part = _factory.StringLiteral(part.ConstantValueOpt.StringValue);
                }

                result = result == null ?
                    part :
                    _factory.Binary(BinaryOperatorKind.StringConcatenation, type, result, part);
            }

            // We need to ensure that the result of the interpolated string is not null. If the single part has a non-null constant value
            // or is itself an interpolated string (which by proxy cannot be null), then there's nothing else that needs to be done. Otherwise,
            // we need to test for null and ensure "" if it is.
            if (length == 1 && result is not ({ Kind: BoundKind.InterpolatedString } or { ConstantValueOpt.IsString: true }))
            {
                Debug.Assert(result is not null);
                Debug.Assert(result.Type is not null);
                Debug.Assert(result.Type.SpecialType == SpecialType.System_String || result.Type.IsErrorType());
                var placeholder = new BoundValuePlaceholder(result.Syntax, result.Type);
                result = new BoundNullCoalescingOperator(result.Syntax, result, _factory.StringLiteral(""), leftPlaceholder: placeholder, leftConversion: placeholder, BoundNullCoalescingOperatorResultKind.LeftType, @checked: false, result.Type) { WasCompilerGenerated = true };
            }

            Debug.Assert(result is not null);
            return (result!, true);
        }

        private BoundExpression LowerPartsToString(InterpolatedStringHandlerData data, ImmutableArray<BoundExpression> parts, SyntaxNode syntax, TypeSymbol type, bool usingDirectlyAsString = false)
        {
            var (optimizedParts, increasedLiteralLength, filledHolesCount) = usingDirectlyAsString ? OptimizeAppendFormattedCalls(_factory, parts) : (parts, 0, 0);
            if (usingDirectlyAsString && TryConvertPartsFromHandlerToConcat(optimizedParts) is { } partsForConcat)
            {
                var (concatResult, requiresVisitAndConversion) = LowerPartsToConcatString(partsForConcat, type);
                if (requiresVisitAndConversion && !concatResult.HasAnyErrors)
                {
                    concatResult = VisitExpression(concatResult); // lower the arguments AND handle expanded form, argument conversions, etc.
                    concatResult = MakeImplicitConversionForInterpolatedString(concatResult, type);
                }
                return concatResult;
            }
            // Optimize calls if possible.
            // If we can lower to the builder pattern, do so.
            InterpolationHandlerResult result = RewriteToInterpolatedStringHandlerPattern(data, optimizedParts, syntax, increasedLiteralLength, filledHolesCount);

            // resultTemp = builderTemp.ToStringAndClear();
            var toStringAndClear = (MethodSymbol)Binder.GetWellKnownTypeMember(_compilation, WellKnownMember.System_Runtime_CompilerServices_DefaultInterpolatedStringHandler__ToStringAndClear, _diagnostics, syntax: syntax);
            BoundExpression toStringAndClearCall = toStringAndClear is not null
                ? BoundCall.Synthesized(syntax, result.HandlerTemp, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, toStringAndClear)
                : new BoundBadExpression(syntax, LookupResultKind.Empty, symbols: ImmutableArray<Symbol?>.Empty, childBoundNodes: ImmutableArray<BoundExpression>.Empty, type);

            return result.WithFinalResult(toStringAndClearCall);
        }

        [Conditional("DEBUG")]
        private static void AssertNoImplicitInterpolatedStringHandlerConversions(ImmutableArray<BoundExpression> arguments, bool allowConversionsWithNoContext = false)
        {
            if (allowConversionsWithNoContext)
            {
                foreach (var arg in arguments)
                {
                    if (arg is BoundConversion { Conversion: { Kind: ConversionKind.InterpolatedStringHandler }, ExplicitCastInCode: false, Operand: var operand })
                    {
                        var data = operand.GetInterpolatedStringHandlerData();
                        Debug.Assert(((BoundObjectCreationExpression)data.Construction).Arguments.All(
                            a => a is BoundInterpolatedStringArgumentPlaceholder { ArgumentIndex: BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter }
                                      or not BoundInterpolatedStringArgumentPlaceholder));
                    }
                }
            }
            else
            {
                Debug.Assert(arguments.All(arg => arg is not BoundConversion { Conversion: { IsInterpolatedStringHandler: true }, ExplicitCastInCode: false }));
            }
        }

        private ImmutableArray<BoundExpression>? TryConvertPartsFromHandlerToConcat(ImmutableArray<BoundExpression> optimizedParts)
        {

            if (optimizedParts.Length >= 5)
            {
                // Prefers handler calls to concat (string[]) for 5 operands or more.
                return null;
            }
            if (!optimizedParts.All(p => p is BoundCall { Method.Name: BoundInterpolatedString.AppendLiteralMethod } or BoundCall { Method.Name: BoundInterpolatedString.AppendFormattedMethod, Arguments: [{ Type.SpecialType: SpecialType.System_String }] }))
            {
                return null;

            }
            var builder = ArrayBuilder<BoundExpression>.GetInstance();
            foreach (var part in optimizedParts)
            {
                var partCall = (BoundCall)part;
                Debug.Assert(partCall.Method.Name != BoundInterpolatedString.AppendLiteralMethod
                    || partCall.Arguments[0] is BoundLiteral);
                builder.Add(
                    partCall.Method.Name == BoundInterpolatedString.AppendLiteralMethod
                    ? partCall.Arguments[0]
                    : new BoundStringInsert(partCall.Syntax, partCall.Arguments[0], null, null, false)
                );
            }
            return builder.ToImmutableAndFree();
        }

        private static (ImmutableArray<BoundExpression> Calls, int IncreasedLiteralLength, int FilledHolesCount) OptimizeAppendFormattedCalls(SyntheticBoundNodeFactory factory, ImmutableArray<BoundExpression> parts)
        {
            var result = ArrayBuilder<BoundExpression>.GetInstance();
            // Empty while previousUnmergedAppendLiteralCall or previousUnmergedStringLiteral is not null
            // Never be left freed (must be reset to be a new instance immediately)
            var currentBeingMergedStringConst = PooledStringBuilder.GetInstance();
            // All receivers of AppendLiteral/Handler calls should be the same interpolated string handler instance.
            BoundExpression? firstNonNullHandler = null;
            // Previous AppendLiteral or AppendFormat call that has not been merged into others yet and can be reused (output as is)
            // If null, Previous one has been merged into former ones, does not exist, or is not a string literal (e.g. const, nameof, or char)
            BoundCall? previousUnmergedAppendLiteralCall = null;
            // Previous string literal that is the argument of previousUnmergedAppendLiteralCall
            // previousUnmergedStringLiteral can take a non-null value even though previousUnmergedAppendLiteralCall is still null
            BoundLiteral? previousUnmergedStringLiteral = null;
            // The below 2 affect on the constructor arguments of string handlers.
            int filledHolesCount = 0;
            int increasedLiteralLength = 0;
            MethodSymbol? appendLiteralSymbol = null;

            foreach (BoundExpression part in parts)
            {
                // BoundDynamicInvocation etc.
                if (part is not BoundCall call)
                {
                    // cannot be merged
                    if (!tryAddPendingAppendLiteralCallToResult(firstNonNullHandler))
                        return shouldBeAsIs();
                    result.Add(part);
                    continue;
                }

                firstNonNullHandler = call.ReceiverOpt ?? firstNonNullHandler;

                switch (call.Method.Name)
                {
                    case BoundInterpolatedString.AppendFormattedMethod:
                        if (!handleAppendFormattedCall(call))
                            return shouldBeAsIs();
                        break;
                    case BoundInterpolatedString.AppendLiteralMethod:
                        if (!handleAppendLiteralCall(call))
                            goto default;
                        break;
                    default:
                        if (!tryAddPendingAppendLiteralCallToResult(firstNonNullHandler))
                            return shouldBeAsIs();
                        result.Add(part);
                        break;
                }
            }
            if (!tryAddPendingAppendLiteralCallToResult(firstNonNullHandler))
            {
                return shouldBeAsIs();
            }
            currentBeingMergedStringConst.Free();
            return (result.ToImmutableAndFree(), increasedLiteralLength, filledHolesCount);

            bool handleAppendFormattedCall(BoundCall call)
            {
                // can be BoundLocal (const) or something else (e.g. nameof), so don't stick to BoundLiteral
                // Theoretically, BoundConversion (from string const to object) might come here, but we don't care about it for now because it doesn't come as long as we use the official BCL, which contains the AppendFormatted(string) overload.
                if (
                    call.Arguments is [{ ConstantValueOpt: { IsString: true } or { IsChar: true } or { IsNull: true } } literal]
                )
                {
                    if (previousUnmergedStringLiteral is not null)
                    {
                        Debug.Assert(currentBeingMergedStringConst.Length == 0);
                        mergePreviousAppendLiteralStringIntoBuilder();
                    }
                    if (literal.ConstantValueOpt is { IsString: true } or { IsNull: true })
                    {
                        // not only empty but also null can be omitted because they don't produce anything
                        if (literal.ConstantValueOpt.StringValue is { Length: int length and not 0 } value)
                        {
                            if (currentBeingMergedStringConst.Builder.Length == 0 && literal is BoundLiteral genuineLiteral)
                            {
                                previousUnmergedStringLiteral = genuineLiteral;
                            }
                            else
                            {
                                currentBeingMergedStringConst.Builder.Append(value);
                            }
                            increasedLiteralLength += length;
                        }
#if DEBUG
                        else
                        {
                            // Discard null or empty literal that doesn't go through the above if statement
                            Debug.Assert(literal.ConstantValueOpt is { IsNull: true } or { IsString: true, StringValue: null or [] });
                        }
#endif
                    }
                    else
                    {
                        // char literal
                        // this literal cannot be reused by an AppendLiteral call
                        Debug.Assert(literal.ConstantValueOpt.IsChar);
                        currentBeingMergedStringConst.Builder.Append(literal.ConstantValueOpt.CharValue);
                        increasedLiteralLength++;
                    }
                    filledHolesCount++;
                }
                else
                {
                    // Calls that cannot be merged
                    if (!tryAddPendingAppendLiteralCallToResult(call.ReceiverOpt))
                        return false;
                    result.Add(call);
                }
                return true;
            }

            bool handleAppendLiteralCall(BoundCall call)
            {
                if (call.Arguments is not [BoundLiteral { ConstantValueOpt: { IsString: true, StringValue: var value } } literal])
                {
                    // Not regular AppendLiteral call (handler.AppendLiteral("literal"))
                    // e.g. using custom InterpolatedStringHandler (rare)
                    return false;
                }

                appendLiteralSymbol = call.Method;

                // Just ignore (null or) empty literal
                if (string.IsNullOrEmpty(value))
                {
                    // We still have a chance to reuse previous (not this) AppendLiteral call
                    return true;
                }

                if (previousUnmergedStringLiteral is null && currentBeingMergedStringConst.Length == 0)
                {
                    Debug.Assert(previousUnmergedAppendLiteralCall is null);
                    (previousUnmergedAppendLiteralCall, previousUnmergedStringLiteral) = (call, literal);
                }
                else
                {
                    // already has pending AppendLiteral; merge into its parameter string
                    mergePreviousAppendLiteralStringIntoBuilder();
                    currentBeingMergedStringConst.Builder.Append(value);
                }
                return true;

            }

            (ImmutableArray<BoundExpression> Calls, int IncreasedLiteralLength, int FilledHolesCount) shouldBeAsIs()
            {
                result.Free();
                currentBeingMergedStringConst.Free();
                return (parts, 0, 0);
            }

            static MethodSymbol? tryGetAppendLiteralCallFromReceiver(BoundExpression receiver)
            {
                Debug.Assert(receiver.Type is not null);
                foreach (var candidate in receiver.Type!.GetMembers(BoundInterpolatedString.AppendLiteralMethod))
                {
                    if (candidate is not MethodSymbol methodCandidate)
                        continue;
                    if (methodCandidate.IsStatic)
                        continue;
                    if (methodCandidate is not { Parameters: [{ Type.SpecialType: SpecialType.System_String }] })
                        continue;
                    if (methodCandidate.DeclaredAccessibility != Accessibility.Public)
                        continue;
                    return methodCandidate;

                }
                return null;
            }

            // This must be called just before modifying previous... or currentBeingMergedStringConst
            void mergePreviousAppendLiteralStringIntoBuilder()
            {
                // other than null or empty literal
                if (previousUnmergedStringLiteral is { ConstantValueOpt.StringValue: { Length: not 0 } value })
                {
                    currentBeingMergedStringConst.Builder.Append(value);
                }
                // We will have to newly generate a AppendLiteral without reusing the existing one
                previousUnmergedAppendLiteralCall = null;
                previousUnmergedStringLiteral = null;
            }

            bool tryAddPendingAppendLiteralCallToResult(BoundExpression? handler)
            {
                if (previousUnmergedAppendLiteralCall is not null)
                {
                    Debug.Assert(currentBeingMergedStringConst.Length == 0);
                    // single append call; add it as is
                    result.Add(previousUnmergedAppendLiteralCall);
                }
                else
                {
                    BoundLiteral? literal = null;
                    // reuse existing previousUnmergedStringLiteral if possible
                    if (previousUnmergedStringLiteral is not null)
                    {
                        literal = previousUnmergedStringLiteral;
                        Debug.Assert(currentBeingMergedStringConst.Length == 0);

                    }
                    else if (currentBeingMergedStringConst.Length != 0)
                    {
                        literal = factory.StringLiteral(currentBeingMergedStringConst.ToStringAndFree());
                        // Don't leave currentBeingMergedStringConst freed; make it available again immediately
                        currentBeingMergedStringConst = PooledStringBuilder.GetInstance();
                    }
                    // You don't have to emit AppendLiteral call if there is no string literal
                    if (literal is null)
                    {
                        Debug.Assert(previousUnmergedStringLiteral == null);
                        return true;
                    }

                    // handler should exist because there should be at least one AppendLiteral/Formatted call
                    if (handler is null)
                        return false;

                    appendLiteralSymbol ??= tryGetAppendLiteralCallFromReceiver(handler);
                    if (appendLiteralSymbol is null)
                        return false;

                    // currentBeingMergedStringConst must have been freed here
                    result.Add(factory.Call(handler, appendLiteralSymbol, literal));
                }
                // You can now forget previous string literal that has just been merged
                previousUnmergedAppendLiteralCall = null;
                previousUnmergedStringLiteral = null;
                Debug.Assert(currentBeingMergedStringConst.Length == 0);
                return true;
            }
        }

        private readonly struct InterpolationHandlerResult
        {
            private readonly ImmutableArray<BoundStatement> _statements;
            private readonly ImmutableArray<BoundExpression> _expressions;
            private readonly LocalRewriter _rewriter;
            private readonly LocalSymbol? _outTemp;

            public readonly BoundLocal HandlerTemp;

            public InterpolationHandlerResult(ImmutableArray<BoundStatement> statements, BoundLocal handlerTemp, LocalSymbol outTemp, LocalRewriter rewriter)
            {
                _statements = statements;
                _expressions = default;
                _outTemp = outTemp;
                HandlerTemp = handlerTemp;
                _rewriter = rewriter;
            }

            public InterpolationHandlerResult(ImmutableArray<BoundExpression> expressions, BoundLocal handlerTemp, LocalSymbol? outTemp, LocalRewriter rewriter)
            {
                _statements = default;
                _expressions = expressions;
                _outTemp = outTemp;
                HandlerTemp = handlerTemp;
                _rewriter = rewriter;
            }

            public BoundExpression WithFinalResult(BoundExpression result)
            {
                Debug.Assert(_statements.IsDefault ^ _expressions.IsDefault);
                var locals = _outTemp != null
                    ? ImmutableArray.Create(HandlerTemp.LocalSymbol, _outTemp)
                    : ImmutableArray.Create(HandlerTemp.LocalSymbol);

                if (_statements.IsDefault)
                {
                    return _rewriter._factory.Sequence(locals, _expressions, result);
                }
                else
                {
                    _rewriter._needsSpilling = true;
                    return _rewriter._factory.SpillSequence(locals, _statements, result);
                }
            }
        }
    }
}
