// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Roslyn.Test.Utilities
{
    public sealed class StaSynchronizationContext : SynchronizationContext, IDisposable
    {
        public static StaSynchronizationContext Default { get; } = new StaSynchronizationContext();

        /// <summary>Stores the queued tasks to be executed by our pool of STA threads.</summary>
        private BlockingCollection<Action> _toRunCollection = new BlockingCollection<Action>();

        /// <summary>The STA threads used by the scheduler.</summary>
        private readonly Thread _thread;

        internal bool IsRunningInScheduler => _thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;

        /// <summary>Initializes a new instance of the StaTaskScheduler class with the specified concurrency level.</summary>
        public StaSynchronizationContext()
        {
            _thread = new Thread(() =>
            {
                var oldContext = SynchronizationContext.Current;
                try
                {
                    SynchronizationContext.SetSynchronizationContext(this);
                    foreach (var action in _toRunCollection.GetConsumingEnumerable())
                    {
                        action();
                    }
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(oldContext);
                }
            });
            _thread.Name = $"{nameof(StaTaskScheduler)} thread";
            _thread.IsBackground = true;
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _toRunCollection.Add(() => d(state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            using (var mre = new ManualResetEvent(initialState: false))
            {
                SendOrPostCallback calback = s =>
                {
                    try
                    {
                        d(s);
                    }
                    finally
                    {
                        mre.Set();
                    }
                };
                Post(d, state);

                mre.WaitOne();
            }
        }

        /// <summary>
        /// Cleans up the scheduler by indicating that no more tasks will be queued.
        /// This method blocks until all threads successfully shutdown.
        /// </summary>
        public void Dispose()
        {
            if (_toRunCollection != null)
            {
                _toRunCollection.CompleteAdding();
                _thread.Join();
                _toRunCollection.Dispose();
                _toRunCollection = null;
            }
        }
    }
}
