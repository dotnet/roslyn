// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    /// <summary>
    /// Base class that allows some helpers for detecting whether we're on the main WPF foreground thread, or
    /// a background thread.  It also allows scheduling work to the foreground thread at below input priority.
    /// </summary>
    internal class ForegroundThreadAffinitizedObject
    {
        private static Thread s_foregroundThread;
        private static TaskScheduler s_foregroundTaskScheduler;

        internal static Thread ForegroundThread
        {
            get { return s_foregroundThread; }
        }

        internal static TaskScheduler ForegroundTaskScheduler
        {
            get { return s_foregroundTaskScheduler; }
        }

        // HACK: This is a dangerous way of establishing the 'foreground' thread affinity of an 
        // AppDomain.  This method should be deleted in favor of forcing derivations of this type
        // to either explicitly inherit WPF Dispatcher thread or provide an explicit thread 
        // they believe to be the foreground. 
        static ForegroundThreadAffinitizedObject()
        {
            Initialize(force: true);
        }

        // This static initialization method *must* be invoked on the UI thread to ensure that the static 'foregroundThread' field is correctly initialized.
        public static ForegroundThreadAffinitizedObject Initialize(bool force = false)
        {
            if (s_foregroundThread != null && !force)
            {
                return new ForegroundThreadAffinitizedObject();
            }

            s_foregroundThread = Thread.CurrentThread;
            var previousContext = SynchronizationContext.Current;
            try
            {
                // None of the work posted to the foregroundTaskScheduler should block pending keyboard/mouse input from the user.
                // So instead of using the default priority which is above user input, we use Background priority which is 1 level
                // below user input.
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher, DispatcherPriority.Background));
                s_foregroundTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }

            return new ForegroundThreadAffinitizedObject();
        }

        public ForegroundThreadAffinitizedObject(bool assertIsForeground = false)
        {
            // For sanity's sake, ensure that our idea of "foreground" is the same as WPF's
            Contract.ThrowIfFalse(Application.Current == null || Application.Current.Dispatcher.Thread == ForegroundThreadAffinitizedObject.s_foregroundThread);

            // ForegroundThreadAffinitizedObject might not necessarily be created on a foreground thread.
            // AssertIsForeground here only if the object must be created on a foreground thread.
            if (assertIsForeground)
            {
                AssertIsForeground();
            }
        }

        public bool IsForeground()
        {
            return Thread.CurrentThread == ForegroundThreadAffinitizedObject.s_foregroundThread;
        }

        public void AssertIsForeground()
        {
            Contract.ThrowIfFalse(IsForeground());
        }

        public void AssertIsBackground()
        {
            Contract.ThrowIfTrue(IsForeground());
        }

        public Task InvokeBelowInputPriority(Action action, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (IsForeground() && !IsInputPending())
            {
                // Optimize to inline the action if we're already on the foreground thread
                // and there's no pending user input.
                action();

                return SpecializedTasks.EmptyTask;
            }
            else
            {
                return Task.Factory.SafeStartNew(action, cancellationToken, ForegroundThreadAffinitizedObject.s_foregroundTaskScheduler);
            }
        }

        /// <summary>
        /// Returns true if any keyboard or mouse button input is pending on the message queue.
        /// </summary>
        protected bool IsInputPending()
        {
            // The return value of GetQueueStatus is HIWORD:LOWORD.
            // A non-zero value in HIWORD indicates some input message in the queue.
            uint result = NativeMethods.GetQueueStatus(NativeMethods.QS_INPUT);

            const uint InputMask = NativeMethods.QS_INPUT | (NativeMethods.QS_INPUT << 16);
            return (result & InputMask) != 0;
        }
    }
}
