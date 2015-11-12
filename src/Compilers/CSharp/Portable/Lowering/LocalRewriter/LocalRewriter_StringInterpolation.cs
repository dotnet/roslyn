// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

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
            ArrayBuilder<BoundExpression> concatExpressions;
            bool isSimpleConcatString;
            bool potentiallyNull;
            MakeInterpolatedStringFormat(node, out format, out expressions,out concatExpressions, out isSimpleConcatString, out potentiallyNull);
        }

        private void MakeInterpolatedStringFormat(
            BoundInterpolatedString node, 
            out BoundExpression format, 
            out ArrayBuilder<BoundExpression> expressions, 
            out ArrayBuilder<BoundExpression> concatExpressions,
            out bool isSimpleConcatString,
            out bool potentiallyNull)
        {
            _factory.Syntax = node.Syntax;
            int n = node.Parts.Length - 1;
            var formatString = PooledStringBuilder.GetInstance();
            expressions = ArrayBuilder<BoundExpression>.GetInstance(n + 1);
            concatExpressions = ArrayBuilder<BoundExpression>.GetInstance(n + 1);
            int nextFormatPosition = 0;
            isSimpleConcatString = true;
            potentiallyNull = true;
            for (int i = 0; i <= n; i++)
            {
                var part = node.Parts[i];
                var fillin = part as BoundStringInsert;
                if (fillin == null)
                {
                    // this is one of the literal parts
                    formatString.Builder.Append(part.ConstantValue.StringValue);
                    
                    // no point in continuing to unescape when the result will be unused
                    if (isSimpleConcatString)
                    {
                        part = HandleEscapeSequences(part);
                    }
                }
                else
                {
                    // this is one of the expression holes
                    formatString.Builder.Append("{").Append(nextFormatPosition++);
                    if (fillin.Alignment != null && !fillin.Alignment.HasErrors)
                    {
                        formatString.Builder.Append(",").Append(fillin.Alignment.ConstantValue.Int64Value);
                        isSimpleConcatString = false;
                    }
                    if (fillin.Format != null && !fillin.Format.HasErrors)
                    {
                        formatString.Builder.Append(":").Append(fillin.Format.ConstantValue.StringValue);
                        isSimpleConcatString = false;
                    }
                    formatString.Builder.Append("}");
                    part = fillin.Value;
                    if (part.Type?.TypeKind == TypeKind.Dynamic) 
                    {
                        part = MakeConversion(part, _compilation.ObjectType, @checked: false);
                    }

                    expressions.Add(part); // NOTE: must still be lowered
                }
                if (!isSimpleConcatString || part.ConstantValue == ConstantValue.Null)
                {
                    continue;
                }
                potentiallyNull = potentiallyNull 
                                    && part.ConstantValue == null
                                    && part.Type?.IsReferenceType == true
                                    && part.Type?.IsNullableType() == false;
                concatExpressions.Add(part); // NOTE: must still be lowered
            }

            format = _factory.StringLiteral(formatString.ToStringAndFree());
        }

        private BoundLiteral HandleEscapeSequences(BoundExpression input) 
        {
            // There are no fill-ins. Handle the escaping of {{ and }} and return the value.
            Debug.Assert(!input.HasErrors && input.ConstantValue != null && input.ConstantValue.IsString);
            var builder = PooledStringBuilder.GetInstance();
            var formatText = input.ConstantValue.StringValue;
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

        public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
        {
            //
            // We lower an interpolated string into an invocation of String.Format or string concatenation, depending on 
            // the existence of string format instructions. Some examples:
            //
            //     $"Braces {{ }}"
            //     $"#{a}-{b}"
            //     $"Jenny don\'t change your number { 8675309 :#-0000}"
            //
            // into
            //
            //     "Braces { }"
            //     "#" + a + "-" + b
            //     String.Format("Jenny don\'t change your number {0:#-0000}", new object[] { 8675309 })
            //

            Debug.Assert(node.Type.SpecialType == SpecialType.System_String); // if target-converted, we should not get here.
            BoundExpression format;
            ArrayBuilder<BoundExpression> expressions;
            ArrayBuilder<BoundExpression> concatExpressions;
            bool isSimpleConcatString;
            bool potentiallyNull;
            MakeInterpolatedStringFormat(node, out format, out expressions, out concatExpressions, out isSimpleConcatString, out potentiallyNull);
            if (expressions.Count == 0) 
            {
                return HandleEscapeSequences(format);
            }

            var stringType = node.Type;
            BoundExpression result = null;
            // The normal pattern for lowering is to lower subtrees before the enclosing tree. However we cannot lower
            // the arguments first in this situation because we do not know what conversions will be
            // produced for the arguments until after we've done overload resolution. So we produce the invocation
            // and then lower it along with its arguments.
            if (isSimpleConcatString)
            {
                var args = concatExpressions.ToImmutableAndFree();
                if (args.Length == 0) 
                {
                     // input was $"{null}{null}{null}...
                    return _factory.StringLiteral("");
                }
                
                if (args.Length == 1 && args[0].Type == stringType) 
                { 
                    // avoid Concat(string[])
                    result = args[0];
                } 
                else 
                {
                    result = _factory.StaticCall(stringType, "Concat", args, false);
                }
                
                if (potentiallyNull)
                {
                    result = _factory.Coalesce(result, _factory.StringLiteral(""));
                }
            }
            if (result == null)
            {
                expressions.Insert(0, format);
                result = _factory.StaticCall(stringType, "Format", expressions.ToImmutableAndFree(),
                    allowUnexpandedForm: false
                    // if an interpolation expression is the null literal, it should not match a params parameter.
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
