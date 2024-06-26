// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

internal static class EnumArrayConverter
{
    public static TEnum[] FromStringArray<TEnum>(string[] strings) where TEnum : struct, Enum
    {
        var enums = new TEnum[strings.Length];
        for (var i = 0; i < enums.Length; i++)
        {
            var s = strings[i];
            if (!Enum.TryParse(s, out TEnum enumValue))
                enumValue = default;

            enums[i] = enumValue;
        }

        return enums;
    }
}
