// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Boost performance of any servicehub service which is invoked by user explicit actions
    /// </summary>
    internal static class UserOperationBooster
    {
        private static int s_count = 0;

        public static IDisposable Boost()
        {
            return new Booster();
        }

        private static void Start()
        {
            var value = Interlocked.Increment(ref s_count);
            Contract.Requires(value >= 0);

            if (value == 1)
            {
                // boost to normal priority
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }
        }

        private static void Done()
        {
            var value = Interlocked.Decrement(ref s_count);
            Contract.Requires(value >= 0);

            if (value == 0)
            {
                // when boost is done, set process back to below normal priority
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            }
        }

        private class Booster : IDisposable
        {
            public Booster()
            {
                Start();
            }

            public void Dispose()
            {
                Done();

                GC.SuppressFinalize(this);
            }

            ~Booster()
            {
                if (!Environment.HasShutdownStarted)
                {
                    Contract.Fail($@"Should have been disposed!");
                }
            }
        }
    }
}
