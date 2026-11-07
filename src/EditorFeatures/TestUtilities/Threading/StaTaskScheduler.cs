// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Windows.Threading;

namespace Roslyn.Test.Utilities;

public sealed class StaTaskScheduler : IDisposable
{
    /// <summary>Gets the shared <see cref="StaTaskScheduler"/>.</summary>
    /// <remarks>We use a count of 1, because the editor ends up re-using <see cref="DispatcherObject"/>
    /// instances between tests, so we need to always use the same thread for our Sta tests.</remarks>
    public static StaTaskScheduler DefaultSta { get; } = new StaTaskScheduler();

    /// <summary>The STA threads used by the scheduler.</summary>
    public Thread StaThread { get; }

    public bool IsRunningInScheduler => StaThread.ManagedThreadId == Environment.CurrentManagedThreadId;

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
