// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    // Starting with 15.3 the editor took a dependency on JoinableTaskContext
    // in Text.Logic and Intellisense layers as an editor host provided service.
    internal class TestExportJoinableTaskContext
    {
        private JoinableTaskContext _joinableTaskContext;

        public TestExportJoinableTaskContext()
        {
            var synchronizationContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(WpfTestRunner.GetEffectiveSynchronizationContext());
                _joinableTaskContext = ThreadingContext.CreateJoinableTaskContext();
                ResetThreadAffinity(JoinableTaskContext.Factory);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            }
        }

        [Export]
        private JoinableTaskContext JoinableTaskContext => _joinableTaskContext;

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
                    type.GetField("foregroundThread", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, thread);
                    type.GetField("ForegroundTaskScheduler", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, taskScheduler);

                    break;
                }
            }
        }

        // HACK: Part of ResetThreadAffinity
        private class JoinableTaskFactoryTaskScheduler : TaskScheduler
        {
            private readonly JoinableTaskFactory _joinableTaskFactory;

            public JoinableTaskFactoryTaskScheduler(JoinableTaskFactory joinableTaskFactory)
            {
                _joinableTaskFactory = joinableTaskFactory;
            }

            public override int MaximumConcurrencyLevel => 1;

            protected override IEnumerable<Task> GetScheduledTasks() => null;

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
}
