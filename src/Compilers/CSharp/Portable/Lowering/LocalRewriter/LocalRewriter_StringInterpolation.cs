// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics;
using System.Linq;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private BoundExpression RewriteInterpolatedStringConversion(BoundConversion conversion)
        {
            Debug.Assert(conversion.ConversionKind == ConversionKind.InterpolatedString);
            BoundExpression format;
            ArrayBuilder<BoundExpression> expressions;
            MakeInterpolatedStringFormat((BoundInterpolatedString)conversion.Operand, out format, out expressions);
            expressions.Insert(0, format);
            var stringFactory = _factory.WellKnownType(WellKnownType.System_Runtime_CompilerServices_FormattableStringFactory);

            // The normal pattern for lowering is to lower subtrees before the enclosing tree. However we cannot lower
            // the arguments first in this situation because we do not know what conversions will be
            // produced for the arguments until after we've done overload resolution. So we produce the invocation
            // and then lower it along with its arguments.
            var result = _factory.StaticCall(stringFactory, "Create", expressions.ToImmutableAndFree(),
                allowUnexpandedForm: false // if an interpolation expression is the null literal, it should not match a params parameter.
                );
            if (!result.HasAnyErrors)
            {
                result = VisitExpression(result); // lower the arguments AND handle expanded form, argument conversions, etc.
                result = MakeImplicitConversion(result, conversion.Type);
            }

            return result;
        }

        /// <summary>
        /// Rewrites the given interpolated string to the set of handler creation and Append calls, returning an array builder of the append calls and the result
        /// local temp.
        /// </summary>
        /// <remarks>Caller is responsible for freeing the ArrayBuilder</remarks>
        private (ArrayBuilder<BoundExpression> HandlerPatternExpressions, BoundLocal Result) RewriteToInterpolatedStringHandlerPattern(BoundInterpolatedString node)
        {
            Debug.Assert(node.InterpolationData is { Construction: not null });
            Debug.Assert(node.Parts.All(static p => p is BoundCall or BoundStringInsert { Value: BoundCall }));
            var data = node.InterpolationData.Value;
            var builderTempSymbol = _factory.InterpolatedStringHandlerLocal(data.BuilderType, data.ScopeOfContainingExpression, node.Syntax);
            BoundLocal builderTemp = _factory.Local(builderTempSymbol);

            // PROTOTYPE(interp-string): Support dynamic creation
            // var handler = new HandlerType(baseStringLength, numFormatHoles, ...InterpolatedStringHandlerArgumentAttribute parameters, <optional> out bool handlerIsValid);
            var construction = (BoundObjectCreationExpression)data.Construction;

            BoundLocal? appendShouldProceedLocal = null;
            if (data.HasTrailingHandlerValidityParameter)
            {
                Debug.Assert(construction.ArgumentRefKindsOpt[^1] == RefKind.Out);

                BoundInterpolatedStringArgumentPlaceholder trailingParameter = data.ArgumentPlaceholders[^1];
                TypeSymbol localType = trailingParameter.Type!;
                Debug.Assert(localType.SpecialType == SpecialType.System_Boolean);
                var outLocal = _factory.SynthesizedLocal(localType);
                appendShouldProceedLocal = _factory.Local(outLocal);

                AddPlaceholderReplacement(trailingParameter, appendShouldProceedLocal);
            }

            var handlerConstructionAssignment = _factory.AssignmentExpression(builderTemp, (BoundExpression)VisitObjectCreationExpression(construction));

            var usesBoolReturn = data.UsesBoolReturns;
            var resultExpressions = ArrayBuilder<BoundExpression>.GetInstance(node.Parts.Length + 1);
            foreach (var currentPart in node.Parts)
            {
                var appendCall = (BoundCall)currentPart;
                Debug.Assert(usesBoolReturn == (appendCall.Method.ReturnType.SpecialType == SpecialType.System_Boolean));

                // The append call itself could have an interpolated string conversion that uses the builder local as the receiver
                // passed to a nested construction call. This needs to affect the current call to ensure side effects are
                // preserved, but shouldn't affect any subsequent calls.
                BoundExpression? appendReceiver = builderTemp;
                Debug.Assert(appendCall.Method.RequiresInstanceReceiver);
                var argRefKindsOpt = appendCall.ArgumentRefKindsOpt;

                var rewrittenArguments = VisitArguments(
                    appendCall.Syntax,
                    appendCall.Arguments,
                    appendCall.Method,
                    appendCall.ArgsToParamsOpt,
                    argRefKindsOpt,
                    ref appendReceiver,
                    out ArrayBuilder<LocalSymbol>? temps,
                    receiverIsArgumentSideEffectSequence: out _);

                var rewrittenAppendCall = MakeArgumentsAndCall(
                    appendCall.Syntax,
                    appendReceiver,
                    appendCall.Method,
                    rewrittenArguments,
                    appendCall.ArgumentRefKindsOpt,
                    appendCall.Expanded,
                    appendCall.InvokedAsExtensionMethod,
                    appendCall.ArgsToParamsOpt,
                    appendCall.ResultKind,
                    appendCall.Type,
                    temps,
                    appendCall);

                Debug.Assert(usesBoolReturn == (appendCall.Type!.SpecialType == SpecialType.System_Boolean));
                resultExpressions.Add(rewrittenAppendCall);
            }

            if (usesBoolReturn)
            {
                // We assume non-bool returns if there was no parts to the string, and code below is predicated on that.
                Debug.Assert(!node.Parts.IsEmpty);
                // Start the sequence with appendProceedLocal, if appropriate
                BoundExpression? currentExpression = appendShouldProceedLocal;

                foreach (var appendCall in resultExpressions)
                {
                    // previousAppendCalls && appendCall
                    currentExpression = currentExpression is null
                        ? appendCall
                        : _factory.LogicalAnd(currentExpression, appendCall);
                }

                resultExpressions.Clear();
                resultExpressions.Add(handlerConstructionAssignment);

                Debug.Assert(currentExpression != null);

                var sequence = _factory.Sequence(
                    appendShouldProceedLocal is not null
                        ? ImmutableArray.Create(appendShouldProceedLocal.LocalSymbol)
                        : ImmutableArray<LocalSymbol>.Empty,
                    resultExpressions.ToImmutableAndClear(),
                    currentExpression);

                resultExpressions.Add(sequence);
            }
            else if (appendShouldProceedLocal is not null && resultExpressions.Count > 0)
            {
                // appendCalls Sequence ending in true
                var appendCallsSequence = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, resultExpressions.ToImmutableAndClear(), _factory.Literal(value: true));

                resultExpressions.Add(handlerConstructionAssignment);

                // appendShouldProceedLocal && sequence
                var appendAnd = _factory.LogicalAnd(appendShouldProceedLocal, appendCallsSequence);

                BoundExpression result = appendAnd;

                result = _factory.Sequence(ImmutableArray.Create(appendShouldProceedLocal.LocalSymbol), resultExpressions.ToImmutableAndClear(), appendAnd);

                resultExpressions.Add(result);
            }
            else if (appendShouldProceedLocal is not null)
            {
                // Odd case of no append calls, but with an out param. We don't need to generate any jumps checking the local because there's
                // nothing to short circuit and avoid, but we do need a sequence to hold the lifetime of the local
                resultExpressions.Add(_factory.Sequence(ImmutableArray.Create(appendShouldProceedLocal.LocalSymbol), ImmutableArray<BoundExpression>.Empty, handlerConstructionAssignment));
            }
            else
            {
                resultExpressions.Insert(0, handlerConstructionAssignment);
            }

            return (resultExpressions, builderTemp);
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
                        fillin.Value.Type?.SpecialType != SpecialType.System_String ||
                        fillin.Alignment != null ||
                        fillin.Format != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void MakeInterpolatedStringFormat(BoundInterpolatedString node, out BoundExpression format, out ArrayBuilder<BoundExpression> expressions)
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
                var fillin = part as BoundStringInsert;
                if (fillin == null)
                {
                    Debug.Assert(part is BoundLiteral && part.ConstantValue != null);
                    // this is one of the literal parts
                    stringBuilder.Append(part.ConstantValue.StringValue);
                }
                else
                {
                    // this is one of the expression holes
                    stringBuilder.Append('{').Append(nextFormatPosition++);
                    if (fillin.Alignment != null && !fillin.Alignment.HasErrors)
                    {
                        Debug.Assert(fillin.Alignment.ConstantValue is { });
                        stringBuilder.Append(',').Append(fillin.Alignment.ConstantValue.Int64Value);
                    }
                    if (fillin.Format != null && !fillin.Format.HasErrors)
                    {
                        Debug.Assert(fillin.Format.ConstantValue is { });
                        stringBuilder.Append(':').Append(fillin.Format.ConstantValue.StringValue);
                    }
                    stringBuilder.Append('}');
                    var value = fillin.Value;
                    if (value.Type?.TypeKind == TypeKind.Dynamic)
                    {
                        value = MakeConversionNode(value, _compilation.ObjectType, @checked: false);
                    }

                    expressions.Add(value); // NOTE: must still be lowered
                }
            }

            format = _factory.StringLiteral(formatString.ToStringAndFree());
        }

        public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
        {
            Debug.Assert(node.Type is { SpecialType: SpecialType.System_String }); // if target-converted, we should not get here.

            BoundExpression? result;

            if (node.InterpolationData is not null)
            {
                // If we can lower to the builder pattern, do so.
                (ArrayBuilder<BoundExpression> handlerPatternExpressions, BoundLocal handlerTemp) = RewriteToInterpolatedStringHandlerPattern(node);

                // resultTemp = builderTemp.ToStringAndClear();
                var toStringAndClear = (MethodSymbol)Binder.GetWellKnownTypeMember(_compilation, WellKnownMember.System_Runtime_CompilerServices_DefaultInterpolatedStringHandler__ToStringAndClear, _diagnostics, syntax: node.Syntax);
                BoundExpression toStringAndClearCall = toStringAndClear is not null
                    ? BoundCall.Synthesized(node.Syntax, handlerTemp, toStringAndClear)
                    : new BoundBadExpression(node.Syntax, LookupResultKind.Empty, symbols: ImmutableArray<Symbol?>.Empty, childBoundNodes: ImmutableArray<BoundExpression>.Empty, node.Type);

                return _factory.Sequence(ImmutableArray.Create(handlerTemp.LocalSymbol), handlerPatternExpressions.ToImmutableAndFree(), toStringAndClearCall);
            }
            else if (CanLowerToStringConcatenation(node))
            {
                // All fill-ins, if any, are strings, and none of them have alignment or format specifiers.
                // We can lower to a more efficient string concatenation
                // The normal pattern for lowering is to lower subtrees before the enclosing tree. However in this case
                // we want to lower the entire concatenation so we get the optimizations done by that lowering (e.g. constant folding).

                int length = node.Parts.Length;
                if (length == 0)
                {
                    // $"" -> ""
                    return _factory.StringLiteral("");
                }

                result = null;
                for (int i = 0; i < length; i++)
                {
                    var part = node.Parts[i];
                    if (part is BoundStringInsert fillin)
                    {
                        // this is one of the filled-in expressions
                        part = fillin.Value;
                    }
                    else
                    {
                        // this is one of the literal parts
                        Debug.Assert(part is BoundLiteral && part.ConstantValue is { StringValue: { } });
                        part = _factory.StringLiteral(ConstantValueUtils.UnescapeInterpolatedStringLiteral(part.ConstantValue.StringValue));
                    }

                    result = result == null ?
                        part :
                        _factory.Binary(BinaryOperatorKind.StringConcatenation, node.Type, result, part);
                }

                if (length == 1)
                {
                    result = _factory.Coalesce(result!, _factory.StringLiteral(""));
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

                MakeInterpolatedStringFormat(node, out BoundExpression format, out ArrayBuilder<BoundExpression> expressions);

                // The normal pattern for lowering is to lower subtrees before the enclosing tree. However we cannot lower
                // the arguments first in this situation because we do not know what conversions will be
                // produced for the arguments until after we've done overload resolution. So we produce the invocation
                // and then lower it along with its arguments.
                expressions.Insert(0, format);
                var stringType = node.Type;
                result = _factory.StaticCall(stringType, "Format", expressions.ToImmutableAndFree(),
                    allowUnexpandedForm: false // if an interpolation expression is the null literal, it should not match a params parameter.
                    );
            }

            Debug.Assert(result is { });
            if (!result.HasAnyErrors)
            {
                result = VisitExpression(result); // lower the arguments AND handle expanded form, argument conversions, etc.
                result = MakeImplicitConversion(result, node.Type);
            }
            return result;
        }

        [Conditional("DEBUG")]
        private void AssertNoImplicitInterpolatedStringHandlerConversions(ImmutableArray<BoundExpression> arguments, bool allowConversionsWithNoContext = false)
        {
            if (allowConversionsWithNoContext)
            {
                foreach (var arg in arguments)
                {
                    if (arg is BoundConversion { Conversion: { Kind: ConversionKind.InterpolatedStringHandler }, ExplicitCastInCode: false, Operand: BoundInterpolatedString @string })
                    {
                        Debug.Assert(((BoundObjectCreationExpression)@string.InterpolationData!.Value.Construction).Arguments.All(
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
    }
}
