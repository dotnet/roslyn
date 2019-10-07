// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
