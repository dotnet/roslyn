// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

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
            private readonly IAsynchronousOperationListener _listener;

            // use event map and event queue so that we can guarantee snapshot and sequential ordering of events from
            // multiple consumer from possibly multiple threads
            private readonly SimpleTaskQueue _eventQueue;
            private readonly EventMap _eventMap;

            // this is to reduce number of times we report progress on same file. this is purely for perf
            private string _lastReportedFilePath;

            private int _count;

            public SolutionCrawlerProgressReporter(IAsynchronousOperationListener listener)
            {
                _listener = listener;
                _eventQueue = new SimpleTaskQueue(TaskScheduler.Default);
                _eventMap = new EventMap();

                _count = 0;
            }

            public bool InProgress
            {
                get
                {
                    return _count > 0;
                }
            }

            public event EventHandler<ProgressData> ProgressChanged
            {
                add
                {
                    _eventMap.AddEventHandler(nameof(ProgressChanged), value);
                }

                remove
                {
                    _eventMap.RemoveEventHandler(nameof(ProgressChanged), value);
                }
            }

            public Task Start()
            {
                if (Interlocked.Increment(ref _count) == 1)
                {
                    _lastReportedFilePath = null;

                    var asyncToken = _listener.BeginAsyncOperation("ProgressReportStart");
                    var progressData = new ProgressData(ProgressStatus.Started, filePathOpt: null);
                    return RaiseEvent(nameof(ProgressChanged), progressData).CompletesAsyncOperation(asyncToken);
                }

                return Task.CompletedTask;
            }

            public Task Stop()
            {
                if (Interlocked.Decrement(ref _count) == 0)
                {
                    _lastReportedFilePath = null;

                    var asyncToken = _listener.BeginAsyncOperation("ProgressReportStop");
                    var progressData = new ProgressData(ProgressStatus.Stoped, filePathOpt: null);
                    return RaiseEvent(nameof(ProgressChanged), progressData).CompletesAsyncOperation(asyncToken);
                }

                return Task.CompletedTask;
            }

            public Task Update(string filePath)
            {
                if (_count > 0)
                {
                    if (_lastReportedFilePath == filePath)
                    {
                        // don't report same file multiple times
                        return Task.CompletedTask;
                    }

                    _lastReportedFilePath = filePath;

                    var asyncToken = _listener.BeginAsyncOperation("ProgressReportUpdate");
                    var progressData = new ProgressData(ProgressStatus.Updated, filePath);
                    return RaiseEvent(nameof(ProgressChanged), progressData).CompletesAsyncOperation(asyncToken);
                }

                return Task.CompletedTask;
            }

            private Task RaiseEvent(string eventName, ProgressData progressData)
            {
                // this method name doesn't have Async since it should work as async void.
                var ev = _eventMap.GetEventHandlers<EventHandler<ProgressData>>(eventName);
                if (ev.HasHandlers)
                {
                    return _eventQueue.ScheduleTask(() =>
                    {
                        ev.RaiseEvent(handler => handler(this, progressData));
                    });
                }

                return Task.CompletedTask;
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
