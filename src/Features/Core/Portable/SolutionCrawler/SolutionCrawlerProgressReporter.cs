// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService : ISolutionCrawlerRegistrationService
    {
        /// <summary>
        /// Progress reporter
        /// 
        /// this progress reporter is a best effort implementation. it doesn't stop world to find out accurate data
        /// 
        /// what this reporter care is we show start/stop background work and show things are moving or paused
        /// without too much cost.
        /// 
        /// due to how solution cralwer calls Start/Stop (see caller of those 2), those 2 can't have a race
        /// and that is all we care for this reporter
        /// </summary>
        internal sealed class SolutionCrawlerProgressReporter : ISolutionCrawlerProgressReporter
        {
            // we use ref count here since solution crawler has multiple queues per priority
            // where an item can be enqueued and dequeued independently. 
            // first item added in any of those queues will cause the "start" event to be sent
            // and the very last item processed from those queues will cause "stop" event to be sent
            // evaluating and paused is also ref counted since work in the lower priority queue can
            // be canceled due to new higher priority work item enqueued to higher queue.
            // but before lower priority work actually exit due to cancellation, higher work could
            // start processing. causing an overlap. the ref count make sure that exiting lower
            // work doesn't flip evaluating state to paused state.
            private int _progressStartCount = 0;
            private int _progressEvaluateCount = 0;

            public event EventHandler<ProgressData>? ProgressChanged;

            public bool InProgress => _progressStartCount > 0;

            public void Start() => ChangeProgressStatus(ref _progressStartCount, ProgressStatus.Started);
            public void Stop() => ChangeProgressStatus(ref _progressStartCount, ProgressStatus.Stopped);

            private void Evaluate() => ChangeProgressStatus(ref _progressEvaluateCount, ProgressStatus.Evaluating);
            private void Pause() => ChangeProgressStatus(ref _progressEvaluateCount, ProgressStatus.Paused);

            public void UpdatePendingItemCount(int pendingItemCount)
            {
                if (_progressStartCount > 0)
                {
                    var progressData = new ProgressData(ProgressStatus.PendingItemCountUpdated, pendingItemCount);
                    OnProgressChanged(progressData);
                }
            }

            /// <summary>
            /// Allows the solution crawler to start evaluating work enqueued to it. 
            /// Returns an IDisposable that the caller must dispose of to indicate that it no longer needs the crawler to continue evaluating. 
            /// Multiple callers can call into this simultaneously. 
            /// Only when the last one actually disposes the scope-object will the crawler 
            /// actually revert back to the paused state where no work proceeds.
            /// </summary>
            public IDisposable GetEvaluatingScope()
                => new ProgressStatusRAII(this);

            private void ChangeProgressStatus(ref int referenceCount, ProgressStatus status)
            {
                var start = status is ProgressStatus.Started or ProgressStatus.Evaluating;
                if (start ? (Interlocked.Increment(ref referenceCount) == 1) : (Interlocked.Decrement(ref referenceCount) == 0))
                {
                    var progressData = new ProgressData(status, pendingItemCount: null);
                    OnProgressChanged(progressData);
                }
            }

            private void OnProgressChanged(ProgressData progressData)
                => ProgressChanged?.Invoke(this, progressData);

            private readonly struct ProgressStatusRAII : IDisposable
            {
                private readonly SolutionCrawlerProgressReporter _owner;

                public ProgressStatusRAII(SolutionCrawlerProgressReporter owner)
                {
                    _owner = owner;
                    _owner.Evaluate();
                }

                public void Dispose()
                    => _owner.Pause();
            }
        }

        /// <summary>
        /// reporter that doesn't do anything
        /// </summary>
        private class NullReporter : ISolutionCrawlerProgressReporter
        {
            public static readonly NullReporter Instance = new();

            public bool InProgress => false;

            public event EventHandler<ProgressData> ProgressChanged
            {
                add { }
                remove { }
            }
        }
    }
}
