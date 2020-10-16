﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Handles conversions from MSBuild values.
    /// </summary>
    internal static class Conversions
    {
        public static bool ToBool(string value)
            => value != null
            && (string.Equals(bool.TrueString, value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("On", value, StringComparison.OrdinalIgnoreCase));

        public static int ToInt(string value)
        {
            if (value == null)
            {
                return 0;
            }
            else
            {
                int.TryParse(value, out var result);
                return result;
            }
        }

        public static ulong ToULong(string value)
        {
            if (value == null)
            {
                return 0;
            }
            else
            {
                ulong.TryParse(value, out var result);
                return result;
            }
        }

        public static TEnum? ToEnum<TEnum>(string value, bool ignoreCase)
            where TEnum : struct
        {
            if (value == null)
            {
                return null;
            }
            else
            {
                return Enum.TryParse<TEnum>(value, ignoreCase, out var result)
                    ? result
                    : (TEnum?)null;
            }
        }
    }
}
