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

            public event EventHandler<bool> ProgressChanged
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

#if DEBUG
            // while dogfooding, encountered a case where ref count not get to 0 once. but
            // couldn't repro it again. adding this in case it happens again then I do have
            // actionable data to find out what went wrong.
            private string _lastCallStackDebug;
#endif

            public Task Start()
            {
                if (Interlocked.Increment(ref _count) == 1)
                {
#if DEBUG
                    _lastCallStackDebug = Environment.StackTrace;
#endif

                    var asyncToken = _listener.BeginAsyncOperation("ProgressReportStart");
                    return RaiseStarted().CompletesAsyncOperation(asyncToken);
                }

                return SpecializedTasks.EmptyTask;
            }

            public Task Stop()
            {
                if (Interlocked.Decrement(ref _count) == 0)
                {
#if DEBUG
                    _lastCallStackDebug = null;
#endif

                    var asyncToken = _listener.BeginAsyncOperation("ProgressReportStop");
                    return RaiseStopped().CompletesAsyncOperation(asyncToken);
                }

                return SpecializedTasks.EmptyTask;
            }

            private Task RaiseStarted()
            {
                return RaiseEvent(nameof(ProgressChanged), started: true);
            }

            private Task RaiseStopped()
            {
                return RaiseEvent(nameof(ProgressChanged), started: false);
            }

            private Task RaiseEvent(string eventName, bool started)
            {
                // this method name doesn't have Async since it should work as async void.
                var ev = _eventMap.GetEventHandlers<EventHandler<bool>>(eventName);
                if (ev.HasHandlers)
                {
                    return _eventQueue.ScheduleTask(() =>
                    {
                        ev.RaiseEvent(handler => handler(this, started));
                    });
                }

                return SpecializedTasks.EmptyTask;
            }
        }

        /// <summary>
        /// reporter that doesn't do anything
        /// </summary>
        private class NullReporter : ISolutionCrawlerProgressReporter
        {
            public static readonly NullReporter Instance = new NullReporter();

            public bool InProgress => false;

            public event EventHandler<bool> ProgressChanged
            {
                add { }
                remove { }
            }
        }
    }
}
