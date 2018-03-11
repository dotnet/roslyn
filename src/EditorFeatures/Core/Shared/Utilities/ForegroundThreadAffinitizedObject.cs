// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Utilities.ForegroundThreadDataKind;

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

        /// <summary>
        /// Creates the default ForegroundThreadData assuming that the current thread is the UI thread.
        /// </summary>
        /// <param name="defaultKind">The ForegroundThreadDataKind to fall back to if a UI thread cannot be found</param>
        /// <returns>default ForegroundThreadData values</returns>
        internal static ForegroundThreadData CreateDefault(ForegroundThreadDataKind defaultKind)
        {
            var kind = ForegroundThreadDataInfo.CreateDefault(defaultKind);

            return new ForegroundThreadData(Thread.CurrentThread, SynchronizationContext.Current == null ? TaskScheduler.Default : new SynchronizationContextTaskScheduler(SynchronizationContext.Current), kind);
        }
    }

    /// <summary>
    /// Base class that allows some helpers for detecting whether we're on the main WPF foreground thread, or
    /// a background thread.  It also allows scheduling work to the foreground thread at below input priority.
    /// </summary>
    internal class ForegroundThreadAffinitizedObject
    {
        private static readonly ForegroundThreadData s_fallbackForegroundThreadData;
        private static ForegroundThreadData s_currentForegroundThreadData;
        private readonly ForegroundThreadData _foregroundThreadDataWhenCreated;

        internal static ForegroundThreadData CurrentForegroundThreadData
        {
            get
            {
                return s_currentForegroundThreadData ?? s_fallbackForegroundThreadData;
            }

            set
            {
                s_currentForegroundThreadData = value;
                ForegroundThreadDataInfo.SetCurrentForegroundThreadDataKind(s_currentForegroundThreadData?.Kind);
            }
        }

        internal Thread ForegroundThread => _foregroundThreadDataWhenCreated.Thread;

        internal TaskScheduler ForegroundTaskScheduler => _foregroundThreadDataWhenCreated.TaskScheduler;

        internal ForegroundThreadDataKind ForegroundKind => _foregroundThreadDataWhenCreated.Kind;

        // HACK: This is a dangerous way of establishing the 'foreground' thread affinity of an 
        // AppDomain.  This method should be deleted in favor of forcing derivations of this type
        // to either explicitly inherit WPF Dispatcher thread or provide an explicit thread 
        // they believe to be the foreground. 
        static ForegroundThreadAffinitizedObject()
        {
            s_fallbackForegroundThreadData = ForegroundThreadData.CreateDefault(Unknown);
        }

        public ForegroundThreadAffinitizedObject(bool assertIsForeground = false)
        {
            _foregroundThreadDataWhenCreated = CurrentForegroundThreadData;

            // ForegroundThreadAffinitizedObject might not necessarily be created on a foreground thread.
            // AssertIsForeground here only if the object must be created on a foreground thread.
            if (assertIsForeground)
            {
                // Assert we have some kind of foreground thread
                Contract.ThrowIfTrue(CurrentForegroundThreadData.Kind == ForegroundThreadDataKind.Unknown);

                AssertIsForeground();
            }
        }

        public bool IsForeground()
        {
            return Thread.CurrentThread == ForegroundThread;
        }

        public void AssertIsForeground()
        {
            var whenCreatedThread = _foregroundThreadDataWhenCreated.Thread;
            var currentThread = Thread.CurrentThread;

            // In debug, provide a lot more information so that we can track down unit test flakeyness.
            // This is too expensive to do in retail as it creates way too many allocations.
            Debug.Assert(currentThread == whenCreatedThread,
                "When created kind       : " + _foregroundThreadDataWhenCreated.Kind + "\r\n" +
                "When created thread id  : " + whenCreatedThread?.ManagedThreadId + "\r\n" +
                "When created thread name: " + whenCreatedThread?.Name + "\r\n" +
                "Current thread id       : " + currentThread?.ManagedThreadId + "\r\n" +
                "Current thread name     : " + currentThread?.Name);

            // But, in retail, do the check as well, so that we can catch problems that happen in the wild.
            Contract.ThrowIfFalse(currentThread == whenCreatedThread);
        }

        public void AssertIsBackground()
        {
            Contract.ThrowIfTrue(IsForeground());
        }

        /// <summary>
        /// A helpful marker method that can be used by deriving classes to indicate that a 
        /// method can be called from any thread and is not foreground or background affinitized.
        /// This is useful so that every method in deriving class can have some sort of marker
        /// on each method stating the threading constraints (FG-only/BG-only/Any-thread).
        /// </summary>
        public void ThisCanBeCalledOnAnyThread()
        {
            // Does nothing.
        }

        public Task InvokeBelowInputPriority(Action action, CancellationToken cancellationToken = default)
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
