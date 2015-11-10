// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal sealed class ForegroundThreadData
    {
        internal readonly Thread Thread;
        internal readonly TaskScheduler TaskScheduler;
        internal readonly ForegroundThreadDataKind Kind;

        internal ForegroundThreadData(Thread thread, TaskScheduler taskScheduler, ForegroundThreadDataKind kind)
        {
            Thread = thread;
            TaskScheduler = taskScheduler;
            Kind = kind;
        }

        internal static ForegroundThreadData CreateDefault()
        {
            var kind = ForegroundThreadDataInfo.CreateDefault();

            // None of the work posted to the foregroundTaskScheduler should block pending keyboard/mouse input from the user.
            // So instead of using the default priority which is above user input, we use Background priority which is 1 level
            // below user input.
            var taskScheduler = new SynchronizationContextTaskScheduler(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher, DispatcherPriority.Background));

            return new ForegroundThreadData(Thread.CurrentThread, taskScheduler, kind);
        }
    }

    /// <summary>
    /// Base class that allows some helpers for detecting whether we're on the main WPF foreground thread, or
    /// a background thread.  It also allows scheduling work to the foreground thread at below input priority.
    /// </summary>
    internal class ForegroundThreadAffinitizedObject
    {
        private static readonly ForegroundThreadData s_fallbackForegroundThreadData;
        private static ForegroundThreadData s_defaultForegroundThreadData;
        private readonly ForegroundThreadData _foregroundThreadData;

        internal static ForegroundThreadData DefaultForegroundThreadData
        {
            get
            {
                return s_defaultForegroundThreadData ?? s_fallbackForegroundThreadData;
            }

            set
            {
                s_defaultForegroundThreadData = value;
                ForegroundThreadDataInfo.SetDefaultForegroundThreadDataKind(s_defaultForegroundThreadData?.Kind);
            }
        }

        internal ForegroundThreadData ForegroundThreadData
        {
            get { return _foregroundThreadData; }
        }

        internal Thread ForegroundThread
        {
            get { return _foregroundThreadData.Thread; }
        }

        internal TaskScheduler ForegroundTaskScheduler
        {
            get { return _foregroundThreadData.TaskScheduler; }
        }

        // HACK: This is a dangerous way of establishing the 'foreground' thread affinity of an 
        // AppDomain.  This method should be deleted in favor of forcing derivations of this type
        // to either explicitly inherit WPF Dispatcher thread or provide an explicit thread 
        // they believe to be the foreground. 
        static ForegroundThreadAffinitizedObject()
        {
            s_fallbackForegroundThreadData = ForegroundThreadData.CreateDefault();
        }

        public ForegroundThreadAffinitizedObject(ForegroundThreadData foregroundThreadData = null, bool assertIsForeground = false)
        {
            _foregroundThreadData = foregroundThreadData ?? DefaultForegroundThreadData;

            // For sanity's sake, ensure that our idea of "foreground" is the same as WPF's
            Contract.ThrowIfFalse(Application.Current == null || Application.Current.Dispatcher.Thread == ForegroundThread);

            // ForegroundThreadAffinitizedObject might not necessarily be created on a foreground thread.
            // AssertIsForeground here only if the object must be created on a foreground thread.
            if (assertIsForeground)
            {
                AssertIsForeground();
            }
        }

        public bool IsForeground()
        {
            return Thread.CurrentThread == ForegroundThread;
        }

        /// <summary>
        /// Ensure this is a supported scheduling context like Wpf or explicit STA scheduler.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return _foregroundThreadData.Kind != ForegroundThreadDataKind.Unknown;
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
                return Task.Factory.SafeStartNew(action, cancellationToken, ForegroundTaskScheduler); 
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
