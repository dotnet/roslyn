// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService : ISolutionCrawlerRegistrationService
    {
        /// <summary>
        /// Progress reporter
        /// 
        /// initial implementation doesn't care about each specific item's progress. 
        /// but as a whole whether there is anything going on. later if there is any need to track each item's progress specifically such as
        /// higher/normal/lower queue, then we can change this to track those separately.
        /// </summary>
        private class SolutionCrawlerProgressReporter : ISolutionCrawlerProgressReporter
        {
            private int _progressStartCount = 0;
            private int _progressEvaluateCount = 0;

            public event EventHandler<ProgressData> ProgressChanged;

            public bool InProgress => _progressStartCount > 0;

            public void Start() => ChangeProgressStatus(ref _progressStartCount, ProgressStatus.Started);
            public void Stop() => ChangeProgressStatus(ref _progressStartCount, ProgressStatus.Stoped);

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

            public IDisposable Evaluating()
            {
                return new ProgressStatusRAII(this);
            }

            private void ChangeProgressStatus(ref int referenceCount, ProgressStatus status)
            {
                var start = (status == ProgressStatus.Started || status == ProgressStatus.Evaluating);
                if (start ? (Interlocked.Increment(ref referenceCount) == 1) : (Interlocked.Decrement(ref referenceCount) == 0))
                {
                    var progressData = new ProgressData(status, pendingItemCount: null);
                    OnProgressChanged(progressData);
                }
            }

            private void OnProgressChanged(ProgressData progressData)
            {
                ProgressChanged?.Invoke(this, progressData);
            }

            private struct ProgressStatusRAII : IDisposable
            {
                private readonly SolutionCrawlerProgressReporter _owner;

                public ProgressStatusRAII(SolutionCrawlerProgressReporter owner)
                {
                    _owner = owner;
                    _owner.Evaluate();
                }

                public void Dispose()
                {
                    _owner.Pause();
                }
            }
        }

        /// <summary>
        /// reporter that doesn't do anything
        /// </summary>
        private class NullReporter : ISolutionCrawlerProgressReporter
        {
            public static readonly NullReporter Instance = new NullReporter();

            public bool InProgress => false;

            public event EventHandler<ProgressData> ProgressChanged
            {
                add { }
                remove { }
            }
        }
    }
}
