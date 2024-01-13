// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an optional bool as a single byte.
    /// </summary>
    internal enum ThreeState : byte
    {
        Unknown = 0,
        False = 1,
        True = 2,
    }

    internal static class ThreeStateHelpers
    {
        public static ThreeState ToThreeState(this bool value)
        {
            return value ? ThreeState.True : ThreeState.False;
        }

        public static bool HasValue(this ThreeState value)
        {
            return value != ThreeState.Unknown;
        }

        public static bool Value(this ThreeState value)
        {
            Debug.Assert(value != ThreeState.Unknown);
            return value == ThreeState.True;
        }
    }
}
