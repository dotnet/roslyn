// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
        {
            //
            // We lower an interpolated string into an invocation of String.Format.  For example, we translate the expression
            //
            //     "Jenny don\'t change your number \{ 8675309 }"
            //
            // into
            //
            //     String.Format("Jenny don\'t change your number {0}", new object[] { 8675309 })
            //

            //
            // TODO: A number of optimizations would be beneficial in the generated code.
            // 
            // (1) Avoid the object array allocation by calling an overload of Format that has a fixed
            //     number of arguments. Check what is available in the platform and make the best choice,
            //     so that we benefit from any additional overloads that may be added in the future
            //
            // (2) If there is no width or format, and the argument is a value type, call .ToString()
            //     on it directly so that we avoid the boxing overhead.
            //
            // (3) For the built-in types, we can use .ToString(string format) for some format strings.
            //     Detect those cases that can be handled that way and take advantage of them.
            //

            int n = node.Parts.Length - 1;
            var formatString = PooledStringBuilder.GetInstance();
            var fillins = ArrayBuilder<BoundExpression>.GetInstance();
            int nextFormatPosition = 0;
            for (int i = 0; i <= n; i++)
            {
                var part = node.Parts[i];
                if ((i % 2) == 0)
                {
                    // this is one of the literal parts
                    foreach (var c in part.ConstantValue.StringValue)
                    {
                        // certain characters require escaping from String.Format
                        if (c == '{' || c == '}')
                        {
                            formatString.Builder.Append("{" + (nextFormatPosition++) + "}");
                            fillins.Add(factory.Convert(compilation.ObjectType, factory.StringLiteral(c.ToString())));
                        }
                        else
                        {
                            formatString.Builder.Append(c);
                        }
                    }
                }
                else
                {
                    // this is one of the expression holes
                    var fillin = (BoundStringInsert)part;
                    formatString.Builder.Append("{").Append(nextFormatPosition++);
                    if (fillin.Alignment != null && !fillin.Alignment.HasErrors)
                    {
                        formatString.Builder.Append(",").Append(fillin.Alignment.ConstantValue.Int64Value);
                    }
                    if (fillin.Format != null && !fillin.Format.HasErrors)
                    {
                        formatString.Builder.Append(":");
                        foreach (var c in fillin.Format.ConstantValue.StringValue)
                        {
                            // certain characters require escaping from String.Format
                            if (c == '{' || c == '}') formatString.Builder.Append(c);
                            formatString.Builder.Append(c);
                        }
                    }
                    formatString.Builder.Append("}");
                    fillins.Add(VisitExpression(fillin.Value));
                }
            }

            var fillinParamsArray = factory.Array(compilation.ObjectType, fillins.ToImmutableAndFree());
            return factory.StaticCall(node.Type, "Format", new BoundExpression[] { factory.StringLiteral(formatString.ToStringAndFree()), fillinParamsArray });
        }
    }
}
