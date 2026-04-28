// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.AspNetCore.Razor;

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

    public static bool SetOrClear(ref int flags, int toChange, bool value)
        => value
            ? Set(ref flags, toChange)
            : Clear(ref flags, toChange);
}
