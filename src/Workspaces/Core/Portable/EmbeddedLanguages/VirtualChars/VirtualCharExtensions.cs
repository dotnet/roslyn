// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal static class VirtualCharExtensions
    {


        public static string CreateString(this ImmutableArray<VirtualChar> chars)
            => CreateString(chars, new TextSpan(0, chars.Length));

        public static string CreateString(
            this ImmutableArray<VirtualChar> chars, TextSpan span)
        {
            var builder = PooledStringBuilder.GetInstance();

            for (var i = span.Start; i < span.End; i++)
            {
                builder.Builder.Append(chars[i].Char);
            }

            return builder.ToStringAndFree();
        }
    }
}
