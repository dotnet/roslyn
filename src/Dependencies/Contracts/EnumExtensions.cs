// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace System;

internal static partial class RoslynEnumExtensions
{
#if NET // binary compatibility
    public static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum
        => Enum.GetValues<TEnum>();

    public static string[] GetNames<TEnum>() where TEnum : struct, Enum
        => Enum.GetNames<TEnum>();
#else
    extension(Enum)
    {
        public static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum
            => (TEnum[])Enum.GetValues(typeof(TEnum));

        public static string[] GetNames<TEnum>() where TEnum : struct, Enum
            => Enum.GetNames(typeof(TEnum));
    }
#endif
}
