// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    /// <summary>
    /// Base class that allows some helpers for detecting whether we're on the main WPF foreground thread, or
    /// a background thread.  It also allows scheduling work to the foreground thread at below input priority.
    /// </summary>
    internal class ForegroundThreadAffinitizedObject
    {
        private readonly IThreadingContext _threadingContext;

        internal IThreadingContext ThreadingContext => _threadingContext;

        public ForegroundThreadAffinitizedObject(IThreadingContext threadingContext, bool assertIsForeground = false)
        {
            _threadingContext = threadingContext ?? throw new ArgumentNullException(nameof(threadingContext));

            // ForegroundThreadAffinitizedObject might not necessarily be created on a foreground thread.
            // AssertIsForeground here only if the object must be created on a foreground thread.
            if (assertIsForeground)
            {
                // Assert we have some kind of foreground thread
                Contract.ThrowIfFalse(threadingContext.HasMainThread);

                AssertIsForeground();
            }
        }

        public bool IsForeground()
        {
            return _threadingContext.JoinableTaskContext.IsOnMainThread;
        }

        public void AssertIsForeground()
        {
            var whenCreatedThread = _threadingContext.JoinableTaskContext.MainThread;
            var currentThread = Thread.CurrentThread;

            // In debug, provide a lot more information so that we can track down unit test flakiness.
            // This is too expensive to do in retail as it creates way too many allocations.
            Debug.Assert(currentThread == whenCreatedThread,
                "When created thread id  : " + whenCreatedThread?.ManagedThreadId + "\r\n" +
                "When created thread name: " + whenCreatedThread?.Name + "\r\n" +
                "Current thread id       : " + currentThread?.ManagedThreadId + "\r\n" +
                "Current thread name     : " + currentThread?.Name);

            // But, in retail, do the check as well, so that we can catch problems that happen in the wild.
            Contract.ThrowIfFalse(_threadingContext.JoinableTaskContext.IsOnMainThread);
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

        public Task InvokeBelowInputPriorityAsync(Action action, CancellationToken cancellationToken = default)
        {
            if (IsForeground() && !IsInputPending())
            {
                // Optimize to inline the action if we're already on the foreground thread
                // and there's no pending user input.
                action();

                return Task.CompletedTask;
            }
            else
            {
                return Task.Factory.SafeStartNewFromAsync(
                    async () =>
                    {
                        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();

                        action();
                    },
                    cancellationToken,
                    TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Returns true if any keyboard or mouse button input is pending on the message queue.
        /// </summary>
        protected bool IsInputPending()
        {
            // The code below invokes into user32.dll, which is not available in non-Windows.
            if (PlatformInformation.IsUnix)
            {
                return false;
            }

            // The return value of GetQueueStatus is HIWORD:LOWORD.
            // A non-zero value in HIWORD indicates some input message in the queue.
            var result = NativeMethods.GetQueueStatus(NativeMethods.QS_INPUT);

            const uint InputMask = NativeMethods.QS_INPUT | (NativeMethods.QS_INPUT << 16);
            return (result & InputMask) != 0;
        }
    }
}
