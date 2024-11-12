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
            var interpolatedString = (BoundInterpolatedString)conversion.Operand;
            Debug.Assert(interpolatedString.InterpolationData is { Construction: not null });
            return VisitExpression(interpolatedString.InterpolationData.GetValueOrDefault().Construction);
        }

        /// <summary>
        /// Rewrites the given interpolated string to the set of handler creation and Append calls, returning an array builder of the append calls and the result
        /// local temp.
        /// </summary>
        /// <remarks>Caller is responsible for freeing the ArrayBuilder</remarks>
        private InterpolationHandlerResult RewriteToInterpolatedStringHandlerPattern(InterpolatedStringHandlerData data, ImmutableArray<BoundExpression> parts, SyntaxNode syntax)
        {
            Debug.Assert(data.BuilderType is not null);
            Debug.Assert(data.ReceiverPlaceholder is not null);
            Debug.Assert(parts.All(static p => p is BoundCall or BoundDynamicInvocation));
            var builderTempSymbol = _factory.InterpolatedStringHandlerLocal(data.BuilderType, syntax);
            BoundLocal builderTemp = _factory.Local(builderTempSymbol);

            // var handler = new HandlerType(baseStringLength, numFormatHoles, ...InterpolatedStringHandlerArgumentAttribute parameters, <optional> out bool appendShouldProceed);
            var construction = (BoundObjectCreationExpression)data.Construction;

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

        public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
        {
            Debug.Assert(node.Type is { SpecialType: SpecialType.System_String }); // if target-converted, we should not get here.

            if (node.InterpolationData is InterpolatedStringHandlerData { BuilderType: not null } data)
            {
                return LowerPartsToString(data, node.Parts, node.Syntax, node.Type);
            }
            else if (node.InterpolationData is null)
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

                BoundExpression? result = null;
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
                        Debug.Assert(part is BoundLiteral && part.ConstantValueOpt?.StringValue is not null);
                        part = _factory.StringLiteral(part.ConstantValueOpt.StringValue);
                    }

                    result = result == null ?
                        part :
                        _factory.Binary(BinaryOperatorKind.StringConcatenation, node.Type, result, part);
                }

                Debug.Assert(result is not null);

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

                return VisitExpression(result);
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

                Debug.Assert(node.InterpolationData is { Construction: not null });
                return VisitExpression(node.InterpolationData.GetValueOrDefault().Construction);
            }
        }

        private BoundExpression LowerPartsToString(InterpolatedStringHandlerData data, ImmutableArray<BoundExpression> parts, SyntaxNode syntax, TypeSymbol type)
        {
            // If we can lower to the builder pattern, do so.
            InterpolationHandlerResult result = RewriteToInterpolatedStringHandlerPattern(data, parts, syntax);

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
