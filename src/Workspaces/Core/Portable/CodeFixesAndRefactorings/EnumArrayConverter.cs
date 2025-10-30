// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

internal static class EnumArrayConverter
{
    public static ImmutableArray<TEnum> FromStringArray<TEnum>(string[] strings) where TEnum : struct, Enum
    {
        var enums = new FixedSizeArrayBuilder<TEnum>(strings.Length);
        for (var i = 0; i < strings.Length; i++)
        {
            var s = strings[i];
            if (!Enum.TryParse(s, out TEnum enumValue))
                enumValue = default;

            enums.Add(enumValue);
        }

        return enums.MoveToImmutable();
    }
}
