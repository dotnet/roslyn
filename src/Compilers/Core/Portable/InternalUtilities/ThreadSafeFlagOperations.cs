// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;

namespace Roslyn.Utilities
{
    internal static class ThreadSafeFlagOperations
    {
        public static bool Set(ref int flags, int toSet)
        {
            int oldState, newState;
            do
            {
                oldState = flags;
                newState = oldState | toSet;
                if (newState == oldState)
                {
                    return false;
                }
            }
            while (Interlocked.CompareExchange(ref flags, newState, oldState) != oldState);
            return true;
        }

        public static bool Clear(ref int flags, int toClear)
        {
            int oldState, newState;
            do
            {
                oldState = flags;
                newState = oldState & ~toClear;
                if (newState == oldState)
                {
                    return false;
                }
            }
            while (Interlocked.CompareExchange(ref flags, newState, oldState) != oldState);
            return true;
        }
    }
}
