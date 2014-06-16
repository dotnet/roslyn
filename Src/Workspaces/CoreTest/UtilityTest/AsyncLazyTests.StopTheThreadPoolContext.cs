// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class AsyncLazyTests
    {
        /// <summary>
        /// For some of the AsyncLazy tests, we want to see what happens if the thread pool is
        /// really behind on processing tasks. This code blocks up the thread pool with pointless work to 
        /// ensure nothing ever runs.
        /// </summary>
        private class StopTheThreadPoolContext : IDisposable
        {
            private readonly int originalWorkerThreads;
            private readonly int originalIOThreads;
            private readonly ManualResetEventSlim testComplete = new ManualResetEventSlim();

            public StopTheThreadPoolContext()
            {
                int numProcs = Environment.ProcessorCount;
                var barrier = new Barrier(numProcs + 1);

                ThreadPool.GetMaxThreads(out originalWorkerThreads, out originalIOThreads);
                ThreadPool.SetMaxThreads(numProcs, originalIOThreads);

                for (int i = 0; i < numProcs; i++)
                {
                    ThreadPool.QueueUserWorkItem(
                        delegate
                        {
                            barrier.SignalAndWait();
                            testComplete.Wait();
                        });
                }

                barrier.SignalAndWait();
            }

            public void Dispose()
            {
                testComplete.Set();
                ThreadPool.SetMaxThreads(originalWorkerThreads, originalIOThreads);
            }
        }
    }
}
