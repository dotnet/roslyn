// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Test.Utilities;

// Starting with 15.3 the editor took a dependency on JoinableTaskContext
// in Text.Logic and IntelliSense layers as an editor host provided service.
[Export]
internal sealed partial class TestExportJoinableTaskContext
{
    public readonly IDispatcherTaskJoiner? DispatcherTaskJoiner;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestExportJoinableTaskContext(
        [Import(AllowDefault = true)] IDispatcherTaskJoiner? dispatcherTaskJoiner = null)
    {
        DispatcherTaskJoiner = dispatcherTaskJoiner;

        var synchronizationContext = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(GetEffectiveSynchronizationContext());
            (JoinableTaskContext, SynchronizationContext) = CreateJoinableTaskContext(dispatcherTaskJoiner);
            ResetThreadAffinity(JoinableTaskContext.Factory);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        }
    }

    private static (JoinableTaskContext joinableTaskContext, SynchronizationContext synchronizationContext) CreateJoinableTaskContext(IDispatcherTaskJoiner? dispatcherTaskJoiner)
    {
        Thread mainThread;
        SynchronizationContext synchronizationContext;

        var currentContext = SynchronizationContext.Current;
        if (currentContext is not null)
        {
            Contract.ThrowIfFalse(dispatcherTaskJoiner?.IsDispatcherSynchronizationContext(currentContext) == true);

            // The current thread is the main thread, and provides a suitable synchronization context
            mainThread = Thread.CurrentThread;
            synchronizationContext = currentContext;
        }
        else
        {
            // The current thread is not known to be the main thread; we have no way to know if the
            // synchronization context of the current thread will behave in a manner consistent with main thread
            // synchronization contexts, so we use DenyExecutionSynchronizationContext to track any attempted
            // use of it.
            var denyExecutionSynchronizationContext = new DenyExecutionSynchronizationContext(currentContext);
            mainThread = denyExecutionSynchronizationContext.MainThread;
            synchronizationContext = denyExecutionSynchronizationContext;
        }

#pragma warning disable VSSDK005 // Use ThreadHelper.JoinableTaskContext singleton - N/A, used for test code
        return (new JoinableTaskContext(mainThread, synchronizationContext), synchronizationContext);
#pragma warning restore VSSDK005 // Use ThreadHelper.JoinableTaskContext singleton - N/A, used for test code
    }

    [Export]
    private JoinableTaskContext JoinableTaskContext
    {
        get;
    }

    internal SynchronizationContext SynchronizationContext
    {
        get;
    }

    internal static SynchronizationContext? GetEffectiveSynchronizationContext()
    {
        if (SynchronizationContext.Current is AsyncTestSyncContext asyncTestSyncContext)
        {
            SynchronizationContext? innerSynchronizationContext = null;
            asyncTestSyncContext.Send(
                _ =>
                {
                    innerSynchronizationContext = SynchronizationContext.Current;
                },
                null);

            return innerSynchronizationContext == asyncTestSyncContext ? null : innerSynchronizationContext;
        }
        else
        {
            return SynchronizationContext.Current;
        }
    }

    /// <summary>
    /// Reset the thread affinity, in particular the designated foreground thread, to the active 
    /// thread.  
    /// </summary>
    internal static void ResetThreadAffinity(JoinableTaskFactory joinableTaskFactory)
    {
        // HACK: When the platform team took over several of our components they created a copy
        // of ForegroundThreadAffinitizedObject.  This needs to be reset in the same way as our copy
        // does.  Reflection is the only choice at the moment. 
        var thread = joinableTaskFactory.Context.MainThread;
        var taskScheduler = new JoinableTaskFactoryTaskScheduler(joinableTaskFactory);

        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("Microsoft.VisualStudio.Language.Intellisense.Implementation.ForegroundThreadAffinitizedObject", throwOnError: false);
            if (type != null)
            {
                type.GetField("s_foregroundThread", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, thread);
                type.GetField("ForegroundTaskScheduler", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, taskScheduler);

                break;
            }
        }
    }

    // HACK: Part of ResetThreadAffinity
    private sealed class JoinableTaskFactoryTaskScheduler : TaskScheduler
    {
        private readonly JoinableTaskFactory _joinableTaskFactory;

        public JoinableTaskFactoryTaskScheduler(JoinableTaskFactory joinableTaskFactory)
            => _joinableTaskFactory = joinableTaskFactory;

        public override int MaximumConcurrencyLevel => 1;

        protected override IEnumerable<Task>? GetScheduledTasks() => null;

        protected override void QueueTask(Task task)
        {
            _joinableTaskFactory.RunAsync(async () =>
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                TryExecuteTask(task);
            });
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (_joinableTaskFactory.Context.IsOnMainThread)
            {
                return TryExecuteTask(task);
            }

            return false;
        }
    }
}
