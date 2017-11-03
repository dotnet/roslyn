// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private static bool CanLowerToStringConcatenation(BoundInterpolatedString node)
        {
            foreach (var part in node.Parts)
            {
                if (part is BoundStringInsert fillin)
                {
                    // this is one of the expression holes
                    if (fillin.HasErrors ||
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

        private static string Unescape(string s)
        {
            var builder = PooledStringBuilder.GetInstance();
            int formatLength = s.Length;
            for (int i = 0; i < formatLength; i++)
            {
                char c = s[i];
                builder.Builder.Append(c);
                if ((c == '{' || c == '}') && (i + 1) < formatLength && s[i + 1] == c)
                {
                    i++;
                }
            }
            return builder.ToStringAndFree();
        }

        private void MakeInterpolatedStringFormat(BoundInterpolatedString node, out BoundExpression format, out ArrayBuilder<BoundExpression> expressions)
        {
            _factory.Syntax = node.Syntax;
            int n = node.Parts.Length - 1;
            var formatString = PooledStringBuilder.GetInstance();
            expressions = ArrayBuilder<BoundExpression>.GetInstance(n + 1);
            int nextFormatPosition = 0;
            for (int i = 0; i <= n; i++)
            {
                var part = node.Parts[i];
                var fillin = part as BoundStringInsert;
                if (fillin == null)
                {
                    // this is one of the literal parts
                    formatString.Builder.Append(part.ConstantValue.StringValue);
                }
                else
                {
                    // this is one of the expression holes
                    formatString.Builder.Append('{').Append(nextFormatPosition++);
                    if (fillin.Alignment != null && !fillin.Alignment.HasErrors)
                    {
                        formatString.Builder.Append(',').Append(fillin.Alignment.ConstantValue.Int64Value);
                    }
                    if (fillin.Format != null && !fillin.Format.HasErrors)
                    {
                        formatString.Builder.Append(':').Append(fillin.Format.ConstantValue.StringValue);
                    }
                    formatString.Builder.Append('}');
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
            Debug.Assert(node.Type.SpecialType == SpecialType.System_String); // if target-converted, we should not get here.

            BoundExpression result;

            if (CanLowerToStringConcatenation(node))
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

                if (length == 1)
                {
                    var part = node.Parts[0];

                    if (part is BoundStringInsert fillin)
                    {
                        // $"{part}"
                        // this is the filled-in expression
                        part = fillin.Value;

                        if (part.ConstantValue != null)
                        {
                            if (part.ConstantValue.StringValue == null)
                            {
                                return _factory.StringLiteral("");
                            }
                            else
                            {
                                result = part;
                            }
                        }
                        else if (part is BoundBinaryOperator bbo && bbo.OperatorKind == BinaryOperatorKind.StringConcatenation)
                        {
                            // $"{a + b}" -> a + b
                            result = part;
                        }
                        else
                        {
                            // TODO don't test for null if we know the expression is a string concatenation
                            result = _factory.Coalesce(part, _factory.StringLiteral(""));
                        }
                    }
                    else
                    {
                        // $"abc" -> "abc"
                        // this is the literal part
                        return _factory.StringLiteral(Unescape(part.ConstantValue.StringValue));
                    }
                }
                else
                {
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
                            part = _factory.StringLiteral(Unescape(part.ConstantValue.StringValue));
                        }

                        result = result == null ?
                            part :
                            _factory.Binary(BinaryOperatorKind.StringConcatenation, node.Type, result, part);
                    }
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

            if (!result.HasAnyErrors)
            {
                result = VisitExpression(result); // lower the arguments AND handle expanded form, argument conversions, etc.
                result = MakeImplicitConversion(result, node.Type);
            }
            return result;
        }
    }
}
