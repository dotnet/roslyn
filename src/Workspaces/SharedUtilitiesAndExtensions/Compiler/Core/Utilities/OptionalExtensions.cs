// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Roslyn.Utilities;

internal static class OptionalExtensions
{
    internal static T GetValueOrDefault<T>(this Optional<T> optional) where T : struct
        => optional.Value;

    public static Optional<TToEnum> ConvertEnum<TFromEnum, TToEnum>(this Optional<TFromEnum> optional)
        where TFromEnum : struct, Enum
        where TToEnum : struct, Enum
    {
        if (!optional.HasValue)
            return default;

        return EnumValueUtilities.ConvertEnum<TFromEnum, TToEnum>(optional.Value);
    }
}
