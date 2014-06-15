// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal static class ThreadingUtilities
    {
        private static int threadIdDispenser;
        private static ThreadLocal<int> currentThreadId = new ThreadLocal<int>(() => Interlocked.Increment(ref threadIdDispenser));

        internal static int GetCurrentThreadId()
        {
            return currentThreadId.Value;
        }
    }
}
