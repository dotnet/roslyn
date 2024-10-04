// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class EnumUtilities
    {
        internal static T[] GetValues<T>() where T : struct
        {
            return (T[])Enum.GetValues(typeof(T));
        }

#if DEBUG
        internal static bool ContainsAllValues<T>(int mask) where T : struct, Enum, IConvertible
        {
            foreach (T value in GetValues<T>())
            {
                int val = value.ToInt32(null);
                if ((val & mask) != val)
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool ContainsValue<T>(T value) where T : struct, Enum
        {
            return Array.IndexOf(GetValues<T>(), value) >= 0;
        }
#endif
    }
}
