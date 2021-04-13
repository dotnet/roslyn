// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
{
    internal sealed partial class MemberSignatureParser
    {
        // A subset of the VB compiler SyntaxKind enum, containing
        // just those values recognized by the signature parser.
        internal enum SyntaxKind
        {
            None,
            OfKeyword,
            ByValKeyword,
            ByRefKeyword,
            BooleanKeyword,
            CharKeyword,
            SByteKeyword,
            ByteKeyword,
            ShortKeyword,
            UShortKeyword,
            IntegerKeyword,
            UIntegerKeyword,
            LongKeyword,
            ULongKeyword,
            SingleKeyword,
            DoubleKeyword,
            StringKeyword,
            ObjectKeyword,
            DecimalKeyword,
            DateKeyword,
        }

        private static ImmutableDictionary<string, SyntaxKind> GetKeywordKinds(StringComparer comparer)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, SyntaxKind>(comparer);
            builder.Add("of", SyntaxKind.OfKeyword);
            builder.Add("byval", SyntaxKind.ByValKeyword);
            builder.Add("byref", SyntaxKind.ByRefKeyword);
            builder.Add("boolean", SyntaxKind.BooleanKeyword);
            builder.Add("char", SyntaxKind.CharKeyword);
            builder.Add("sbyte", SyntaxKind.SByteKeyword);
            builder.Add("byte", SyntaxKind.ByteKeyword);
            builder.Add("short", SyntaxKind.ShortKeyword);
            builder.Add("ushort", SyntaxKind.UShortKeyword);
            builder.Add("integer", SyntaxKind.IntegerKeyword);
            builder.Add("uinteger", SyntaxKind.UIntegerKeyword);
            builder.Add("long", SyntaxKind.LongKeyword);
            builder.Add("ulong", SyntaxKind.ULongKeyword);
            builder.Add("single", SyntaxKind.SingleKeyword);
            builder.Add("double", SyntaxKind.DoubleKeyword);
            builder.Add("string", SyntaxKind.StringKeyword);
            builder.Add("object", SyntaxKind.ObjectKeyword);
            builder.Add("decimal", SyntaxKind.DecimalKeyword);
            builder.Add("date", SyntaxKind.DateKeyword);
            return builder.ToImmutable();
        }
    }
}
