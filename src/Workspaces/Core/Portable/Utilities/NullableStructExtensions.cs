// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.Utilities
{
    internal static class NullableStructExtensions
    {
        public static void Deconstruct<T>(this T? value, out T valueOrDefault, out bool hasValue) where T : struct
        {
            valueOrDefault = value.GetValueOrDefault();
            hasValue = value.HasValue;
        }
    }
}
