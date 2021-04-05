// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics;

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

        private BoundNode RewriteToInterpolatedStringBuilderPattern(BoundInterpolatedString node)
        {
            Debug.Assert(node.InterpolationData is { Construction: not null });
            // PROTOTYPE(interp-string): Support the general builder pattern that doesn't save to a local.
            var resultSymbol = _factory.SynthesizedLocal(_compilation.GetSpecialType(SpecialType.System_String), node.Syntax);
            var resultTemp = _factory.Local(resultSymbol);
            var builderTempSymbol = _factory.InterpolatedStringBuilderLocal(node.InterpolationData.BuilderType, node.InterpolationData.ScopeOfContainingExpression, node.Syntax);
            var builderTemp = _factory.Local(builderTempSymbol);

            // PROTOTYPE(interp-string): Support optional out param for whether the builder was created successfully
            // var builder = Construction(baseStringLength, numFormatHoles);
            var builderConstruction = _factory.Assignment(builderTemp, MakeCallWithNoExplicitArgument(node.InterpolationData.Construction, node.Syntax, expression: null, assertParametersAreOptional: false));

            // For every interpolation part, call the appropriate builder.TryFormat... method.

            Debug.Assert(node.InterpolationData.BuilderFormatCalls.Length >= 1);
            Debug.Assert(node.Parts.Length == node.InterpolationData.BuilderFormatCalls.Length);
            var usesBoolReturn = node.InterpolationData.BuilderFormatCalls[0].Method.ReturnType.SpecialType == SpecialType.System_Boolean;
            var tryFormatCalls = ArrayBuilder<BoundExpression>.GetInstance(node.InterpolationData.BuilderFormatCalls.Length);
            for (int i = 0; i < node.Parts.Length; i++)
            {
                var formatInfo = node.InterpolationData.BuilderFormatCalls[i];
                var part = node.Parts[i];
                Debug.Assert(usesBoolReturn == (formatInfo.Method.ReturnType.SpecialType == SpecialType.System_Boolean));
                Debug.Assert(formatInfo.Arguments[0] is BoundInterpolatedStringElementPlaceholder { Index: var index } && index == i);
                Debug.Assert(part is BoundStringInsert or BoundLiteral);

                // Visit the part, and replace the part placeholder in the TryFormat... call with the replaced part.
                var visitedPart = VisitExpression(part is BoundStringInsert { Value: var value } ? value : part);
                var formatCall = MakeCallWithNoExplicitArgument(formatInfo with
                {
                    Arguments = formatInfo.Arguments.SetItem(0, visitedPart)
                }, part.Syntax, builderTemp, assertParametersAreOptional: false);
                tryFormatCalls.Add(formatCall);
                Debug.Assert(usesBoolReturn == (formatCall.Type!.SpecialType == SpecialType.System_Boolean));
            }

            var builderPatternStatements = ArrayBuilder<BoundStatement>.GetInstance();
            builderPatternStatements.Add(builderConstruction);
            if (usesBoolReturn)
            {
                BoundExpression? tryFormatBinaryExpressions = null;

                // builder.TryFormat... && builder.TryFormat... && ...
                foreach (var tryFormatCall in tryFormatCalls)
                {
                    tryFormatBinaryExpressions = tryFormatBinaryExpressions is null
                        ? tryFormatCall
                        : _factory.LogicalAnd(tryFormatBinaryExpressions, tryFormatCall);
                }

                Debug.Assert(tryFormatBinaryExpressions is not null);

                builderPatternStatements.Add(_factory.ExpressionStatement(tryFormatBinaryExpressions));
            }
            else
            {
                // builder.TryFormat...; builder.TryFormat...; ...
                foreach (var tryFormatCall in tryFormatCalls)
                {
                    builderPatternStatements.Add(_factory.ExpressionStatement(tryFormatCall));
                }
            }

            tryFormatCalls.Free();

            // PROTOTYPE(interp-string): This will need to be a lot more complicated to support method parameter scenarios
            // resultTemp = builder.ToString();

            builderPatternStatements.Add(_factory.Assignment(resultTemp, _factory.InstanceCall(builderTemp, "ToString")));

            // If the builder needs to be disposed after usage, wrap in a try-finally
            if (node.InterpolationData.DisposeInfo is { } disposeInfo)
            {
                // builder.Dispose();
                var disposeCall = _factory.ExpressionStatement(MakeCallWithNoExplicitArgument(disposeInfo, node.Syntax, builderTemp, assertParametersAreOptional: false));
                BoundStatement builderDisposeStatement;

                // If the node can be null, wrap in a check
                if (!node.InterpolationData.BuilderType.IsNonNullableValueType())
                {
                    // builder != null
                    var builderNullCheck = MakeNullCheck(node.Syntax, builderTemp, BinaryOperatorKind.NotEqual);

                    // if (builder != null) builder.Dispose();
                    builderDisposeStatement = _factory.If(builderNullCheck, disposeCall);
                }
                else
                {
                    builderDisposeStatement = disposeCall;
                }

                // try { builderPatternStatements } finally { dispose }
                var builderPatternStatementsBlock = _factory.Block(builderPatternStatements.ToImmutableAndClear());
                var finallyBlock = _factory.Block(builderDisposeStatement);
                builderPatternStatements.Add(_factory.Try(builderPatternStatementsBlock, catchBlocks: ImmutableArray<BoundCatchBlock>.Empty, finallyBlock));
            }

            _needsSpilling = true;
            return _factory.SpillSequence(ImmutableArray.Create(resultSymbol, builderTempSymbol), builderPatternStatements.ToImmutableAndFree(), resultTemp);
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
                return RewriteToInterpolatedStringBuilderPattern(node);
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
    }
}
