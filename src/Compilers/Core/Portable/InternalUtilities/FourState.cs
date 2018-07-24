// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an optional bool? as a single byte.
    /// </summary>
    internal enum FourState : byte
    {
        Unknown = 0,
        False = 1,
        True = 2,
        Unspecified = 3,
    }

    internal static class FourStateHelpers
    {
        public static FourState ToFourState(this bool? value)
        {
            switch (value)
            {
                case true:
                    return FourState.True;
                case false:
                    return FourState.False;
                default:
                    return FourState.Unspecified;
            }
        }

        public static bool HasValue(this FourState value)
        {
            return value != FourState.Unknown;
        }

        public static bool? Value(this FourState value)
        {
            Debug.Assert(value != FourState.Unknown);
            switch (value)
            {
                case FourState.True:
                    return true;
                case FourState.False:
                    return false;
                default:
                    return null;
            }
        }
    }
}
