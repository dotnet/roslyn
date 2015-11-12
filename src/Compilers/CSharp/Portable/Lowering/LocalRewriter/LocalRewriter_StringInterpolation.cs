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
            //
            // This method rewrites expressions where interpolated strings are used where an object
            // implementing `System.IFormattable` or `System.FormattableString` is expected.
            // For example:
            //
            //    IFormattable f = $"Log dump {item.Id}: {item.Title}\n--\n{item.Message}\n\n--\n{item.Timestamp}";
            //
            // That interpolated string would be converted into an instance of IFormattable with
            // these properties:
            //
            // * `f.Format == "Log dump {0}: {1}\n--\n{2}\n\n--\n{3}"`
            // * `f.GetArguments()` returns an `object[]` containing the values
            //   `{ item.Id, item.Title, item.Message, item.Timestamp}`
            //

            Debug.Assert(conversion.ConversionKind == ConversionKind.InterpolatedString);

            BoundExpression format;
            ArrayBuilder<BoundExpression> expressions;
            MakeInterpolatedStringFormat((BoundInterpolatedString)conversion.Operand, out format, out expressions);
            expressions.Insert(0, format);
            var stringFactory = _factory.WellKnownType(WellKnownType.System_Runtime_CompilerServices_FormattableStringFactory);

            // [abnormal pattern lowering procedure]:
            // The normal pattern for lowering is to lower subtrees before the enclosing tree.
            // However we cannot lower the arguments first in this situation because we do not know
            // what conversions will be produced for the arguments until after we've done overload
            // resolution. So we produce the invocation and then lower it along with its arguments.
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
            MakeInterpolatedStringFormat(node, out format, out expressions, out concatExpressions, out isSimpleConcatString, out potentiallyNull);
        }

        private void MakeInterpolatedStringFormat(
            BoundInterpolatedString node,
            out BoundExpression format,
            out ArrayBuilder<BoundExpression> expressions,
            out ArrayBuilder<BoundExpression> concatExpressions,
            out bool isSimpleConcatString,
            out bool potentiallyNull)
        {

            //
            // The interpolated string is passed in parsed and bound to an array of parts
            // representing each segment and hole.
            //
            // A segment is the string literal between:
            //
            // * the start of the string and first open brace marking a hole
            // * close braces and open braces marking holes
            // * the final close brace of a hole and end of string
            // * the entire string if no holes exist
            //
            // Empty segments do not exist in the parts array unless the entire interpolated
            // string is $"", in which case the only element in the parts array is the string 
            // literal "". Segments may contain escaped open braces `{{` and escaped close braces 
            // `}}`. These must be unescaped if the string is going to bypass `string.Format`.
            //
            // A hole is the part of the string marked by the syntax 
            // `{ <interpolation-expression> <optional-comma-field-width> <optional-colon-format> }`.
            //
            // Thus multiple holes may be next to each other, but 2 segments never are and the parts
            // array always has at least 1 element or node has errors.
            //
            // If no holes contain field widths or formats, `node` is eligible to be lowered into a
            // `string.Concat` call instead of `string.Format`. Since `string.Concat` returns `null`
            // if all arguments are null (possible if the interpolated string contains 0 segments and
            // one or more holes that all are null), we must track this possibility and lower with
            // `string.Concat(...) ?? ""` when no argument is known to be not null after that 
            // argument has been converted to a string.
            //
            
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
                    // this is a segment
                    formatString.Builder.Append(part.ConstantValue.StringValue);
                    
                    if (isSimpleConcatString)
                    {
                        // no point in continuing to unescape when the result will be unused
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
                    // It is safe to skip adding parts to the concatExpressions array in both these cases
                    // because in the former the array will be unused and in the latter Concat will ignore
                    // the argument.
                    continue;
                }
                potentiallyNull = potentiallyNull && CallToStringCouldBeNull(part);
                concatExpressions.Add(part); // NOTE: must still be lowered
            }

            format = _factory.StringLiteral(formatString.ToStringAndFree());
        }

        private bool CallToStringCouldBeNull(BoundExpression input)
        {
            //checking if expr?.ToString() could possibly return null

            Debug.Assert(input != null);

            if (input.ConstantValue != null)
            {
                return false;
            }
            var type = input.Type;
            if (type == null)
            {
                return true;
            }
            if (type.IsReferenceType || type.IsNullableType())
            {
                return true;
            }
            var specialType = type.SpecialType;
            switch (specialType)
            {
                case SpecialType.System_Enum:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_DateTime:
                    return false;
            }
            return true;
        }

        private BoundLiteral HandleEscapeSequences(BoundExpression input)
        {
            // Handle the escaping of {{ and }} and return the value for this string literal constant.
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
            // We lower an interpolated string into an invocation of String.Format or string
            // concatenation, depending on the existence of string format instructions.
            // Some examples:
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

            Debug.Assert(node != null);
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
            // see [abnormal pattern lowering procedure]
            if (isSimpleConcatString)
            {
                var args = concatExpressions.ToImmutableAndFree();
                if (args.Length == 0)
                {
                    // input was $"{null}{null}{null}...
                    return _factory.StringLiteral("");
                }

                result = args[0];
                if (args.Length == 1 && result.Type?.IsValueType == true && result.Type?.IsNullableType() == false)
                {
                    result = _factory.Call(args[0], _factory.SpecialMethod(SpecialMember.System_Object__ToString));
                }
                else if(args.Length > 1 || result.Type != stringType)
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
                result = _factory.StaticCall(stringType, "Format", expressions.ToImmutableAndFree(), false);
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
