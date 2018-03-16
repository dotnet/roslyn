// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal static class VirtualCharExtensions
    {
        public static string CreateString(this ImmutableArray<VirtualChar> chars)
        {
            var builder = PooledStringBuilder.GetInstance();

            foreach (var vc in chars)
            {
                builder.Builder.Append(vc.Char);
            }

            return builder.ToStringAndFree();
        }
    }
}
