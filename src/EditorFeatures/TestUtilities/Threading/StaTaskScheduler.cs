// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Roslyn.Test.Utilities;

public sealed class StaTaskScheduler : IDisposable
{
    /// <summary>Gets a <see cref="StaTaskScheduler"/> for the current <see cref="AppDomain"/>.</summary>
    /// <remarks>We use a count of 1, because the editor ends up re-using <see cref="DispatcherObject"/>
    /// instances between tests, so we need to always use the same thread for our Sta tests.</remarks>
    public static StaTaskScheduler DefaultSta { get; } = new StaTaskScheduler();

    /// <summary>The STA threads used by the scheduler.</summary>
    public Thread StaThread { get; }

    public bool IsRunningInScheduler => StaThread.ManagedThreadId == Environment.CurrentManagedThreadId;

    static StaTaskScheduler()
    {
        // Overwrite xunit's app domain handling to not call AppDomain.Unload
        var getDefaultDomain = typeof(AppDomain).GetMethod("GetDefaultDomain", BindingFlags.NonPublic | BindingFlags.Static);
        var defaultDomain = (AppDomain)getDefaultDomain.Invoke(null, null);
        var hook = (XunitDisposeHook)defaultDomain.CreateInstanceFromAndUnwrap(typeof(XunitDisposeHook).Assembly.CodeBase, typeof(XunitDisposeHook).FullName, ignoreCase: false, BindingFlags.CreateInstance, binder: null, args: null, culture: null, activationAttributes: null);
        hook.Execute();

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
        // to DomainUnload is a reasonable place to do it.
        AppDomain.CurrentDomain.DomainUnload += (sender, e) =>
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        };
    }

    /// <summary>Initializes a new instance of the <see cref="StaTaskScheduler"/> class.</summary>
    public StaTaskScheduler()
    {
        using (var threadStartedEvent = new ManualResetEventSlim(initialState: false))
        {
            DispatcherSynchronizationContext synchronizationContext = null;
            StaThread = new Thread(() =>
            {
                var oldContext = SynchronizationContext.Current;
                try
                {
                    // All WPF Tests need a DispatcherSynchronizationContext and we dont want to block pending keyboard
                    // or mouse input from the user. So use background priority which is a single level below user input.
                    synchronizationContext = new DispatcherSynchronizationContext();

                    // xUnit creates its own synchronization context and wraps any existing context so that messages are
                    // still pumped as necessary. So we are safe setting it here, where we are not safe setting it in test.
                    SynchronizationContext.SetSynchronizationContext(synchronizationContext);

                    threadStartedEvent.Set();

                    Dispatcher.Run();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(oldContext);
                }
            });
            StaThread.Name = $"{nameof(StaTaskScheduler)} thread";
            StaThread.IsBackground = true;
            StaThread.SetApartmentState(ApartmentState.STA);
            StaThread.Start();

            threadStartedEvent.Wait();
            DispatcherSynchronizationContext = synchronizationContext;
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
            DispatcherSynchronizationContext.Post(_ => Dispatcher.ExitAllFrames(), null);
            StaThread.Join();
        }
    }
}
