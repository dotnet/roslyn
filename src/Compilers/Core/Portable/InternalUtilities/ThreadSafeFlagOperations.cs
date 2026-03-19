// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public static bool Set(ref long flags, long toSet)
        {
            long oldState, newState;
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
