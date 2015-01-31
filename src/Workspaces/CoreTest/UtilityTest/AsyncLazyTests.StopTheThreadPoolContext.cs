// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly int _originalWorkerThreads;
            private readonly int _originalIOThreads;
            private readonly ManualResetEventSlim _testComplete = new ManualResetEventSlim();

            public StopTheThreadPoolContext()
            {
                int numProcs = Environment.ProcessorCount;
                var barrier = new Barrier(numProcs + 1);

                ThreadPool.GetMaxThreads(out _originalWorkerThreads, out _originalIOThreads);
                ThreadPool.SetMaxThreads(numProcs, _originalIOThreads);

                for (int i = 0; i < numProcs; i++)
                {
                    ThreadPool.QueueUserWorkItem(
                        delegate
                        {
                            barrier.SignalAndWait();
                            _testComplete.Wait();
                        });
                }

                barrier.SignalAndWait();
            }

            public void Dispose()
            {
                _testComplete.Set();
                ThreadPool.SetMaxThreads(_originalWorkerThreads, _originalIOThreads);
            }
        }
    }
}
