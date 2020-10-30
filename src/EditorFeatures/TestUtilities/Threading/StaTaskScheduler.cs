// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace Roslyn.Test.Utilities
{
    public sealed class StaTaskScheduler : IDisposable
    {
        /// <summary>Gets a <see cref="StaTaskScheduler"/> for the current <see cref="AppDomain"/>.</summary>
        /// <remarks>We use a count of 1, because the editor ends up re-using <see cref="DispatcherObject"/>
        /// instances between tests, so we need to always use the same thread for our Sta tests.</remarks>
        public static StaTaskScheduler DefaultSta { get; } = new StaTaskScheduler();

        /// <summary>The STA threads used by the scheduler.</summary>
        public Thread StaThread { get; }

        public bool IsRunningInScheduler => StaThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;

        static StaTaskScheduler()
        {
            // We've created an STA thread, which has some extra requirements for COM Runtime
            // Callable Wrappers (RCWs). If any COM object is created on the STA thread, calls to that
            // object must be made from that thread; when the RCW is no longer being used by any
            // managed code, the RCW is put into the finalizer queue, but to actually finalize it
            // it has to marshal to the STA thread to do the work. This means that in order to safely
            // clean up any RCWs, we need to ensure that the thread is pumping past the point of
            // all RCWs being finalized
            //
            // This constraint is particularly problematic if our tests are running in an AppDomain:
            // when the AppDomain is unloaded, any threads (including our STA thread) are going to be
            // aborted. Once the thread and AppDomain is being torn down, the CLR is going to try cleaning up
            // any RCWs associated them, because if the thread is gone for good there's no way
            // it could ever clean anything further up. The code there waits for the finalizer queue
            // -- but the finalizer queue might be already trying to clean up an RCW, which is marshaling
            // to the STA thread. This could then deadlock.
            //
            // The suggested workaround from the CLR team is to do an explicit GC.Collect and
            // WaitForPendingFinalizers before we let the AppDomain shut down. The belief is subscribing
            // to DomainUnload is a reasonable place to do it. We use GC.GetTotalMemory since it loops
            // over this operation until it stops releasing objects.
            AppDomain.CurrentDomain.DomainUnload += (sender, e) =>
            {
                GC.GetTotalMemory(forceFullCollection: true);
            };
        }

        /// <summary>Initializes a new instance of the <see cref="StaTaskScheduler"/> class.</summary>
        public StaTaskScheduler()
        {
            using (var threadStartedEvent = new ManualResetEventSlim(initialState: false))
            {
                Dispatcher staDispatcher = null;
                StaThread = new Thread(() =>
                {
                    staDispatcher = Dispatcher.CurrentDispatcher;
                    threadStartedEvent.Set();
                    Dispatcher.Run();
                });
                StaThread.Name = $"{nameof(StaTaskScheduler)} thread";
                StaThread.IsBackground = true;
                StaThread.SetApartmentState(ApartmentState.STA);
                StaThread.Start();

                threadStartedEvent.Wait();
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                DispatcherSynchronizationContext = (DispatcherSynchronizationContext)staDispatcher.Invoke(() => SynchronizationContext.Current);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs

                AppDomain.CurrentDomain.DomainUnload += (_, _) => Dispose();
            }

            // Work around the WeakEventTable Shutdown race conditions
            AppContext.SetSwitch("Switch.MS.Internal.DoNotInvokeInWeakEventTableShutdownListener", isEnabled: true);
        }

        public DispatcherSynchronizationContext DispatcherSynchronizationContext
        {
            get;
        }

        /// <summary>
        /// Cleans up the scheduler by indicating that no more tasks will be queued.
        /// This method blocks until all threads successfully shutdown.
        /// </summary>
        public void Dispose()
        {
            if (StaThread.IsAlive)
            {
                // The message pump is not active during AppDomain.Unload callbacks, so our only option is to abort the
                // process directly. 😢 We wait for a short timeout to give the process an opportunity to report results.
                Thread.Sleep(2000);
                Environment.Exit(0);
            }
        }
    }
}
