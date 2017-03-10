// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
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
                    formatString.Builder.Append("{").Append(nextFormatPosition++);
                    if (fillin.Alignment != null && !fillin.Alignment.HasErrors)
                    {
                        formatString.Builder.Append(",").Append(fillin.Alignment.ConstantValue.Int64Value);
                    }
                    if (fillin.Format != null && !fillin.Format.HasErrors)
                    {
                        formatString.Builder.Append(":").Append(fillin.Format.ConstantValue.StringValue);
                    }
                    formatString.Builder.Append("}");
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
            //
            // We lower an interpolated string into an invocation of String.Format.  For example, we translate the expression
            //
            //     $"Jenny don\'t change your number { 8675309 }"
            //
            // into
            //
            //     String.Format("Jenny don\'t change your number {0}", new object[] { 8675309 })
            //

            Debug.Assert(node.Type.SpecialType == SpecialType.System_String); // if target-converted, we should not get here.
            BoundExpression format;
            ArrayBuilder<BoundExpression> expressions;
            MakeInterpolatedStringFormat(node, out format, out expressions);
            if (expressions.Count == 0)
            {
                // There are no fill-ins. Handle the escaping of {{ and }} and return the value.
                Debug.Assert(!format.HasErrors && format.ConstantValue != null && format.ConstantValue.IsString);
                var builder = PooledStringBuilder.GetInstance();
                var formatText = format.ConstantValue.StringValue;
                int formatLength = formatText.Length;
                for (int i = 0; i < formatLength; i++)
                {
                    char c = formatText[i];
                    builder.Builder.Append(c);
                    if ((c == '{' || c == '}') && (i + 1) < formatLength && formatText[i + 1] == c)
                    {
                        i++;
                    }
                }
                return _factory.StringLiteral(builder.ToStringAndFree());
            }

            // The normal pattern for lowering is to lower subtrees before the enclosing tree. However we cannot lower
            // the arguments first in this situation because we do not know what conversions will be
            // produced for the arguments until after we've done overload resolution. So we produce the invocation
            // and then lower it along with its arguments.
            expressions.Insert(0, format);
            var stringType = node.Type;
            var result = _factory.StaticCall(stringType, "Format", expressions.ToImmutableAndFree(),
                allowUnexpandedForm: false // if an interpolation expression is the null literal, it should not match a params parameter.
                );
            if (!result.HasAnyErrors)
            {
                result = VisitExpression(result); // lower the arguments AND handle expanded form, argument conversions, etc.
                result = MakeImplicitConversion(result, node.Type);
            }
            return result;
        }
    }
}
