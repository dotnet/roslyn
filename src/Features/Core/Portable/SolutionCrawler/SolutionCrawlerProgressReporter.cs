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

            public event EventHandler Started
            {
                add
                {
                    _eventMap.AddEventHandler(nameof(Started), value);
                }

                remove
                {
                    _eventMap.RemoveEventHandler(nameof(Started), value);
                }
            }

            public event EventHandler Stopped
            {
                add
                {
                    _eventMap.AddEventHandler(nameof(Stopped), value);
                }

                remove
                {
                    _eventMap.RemoveEventHandler(nameof(Stopped), value);
                }
            }

            public Task Start()
            {
                if (Interlocked.Increment(ref _count) == 1)
                {
                    var asyncToken = _listener.BeginAsyncOperation("ProgressReportStart");
                    return RaiseStarted().CompletesAsyncOperation(asyncToken);
                }

                return SpecializedTasks.EmptyTask;
            }

            public Task Stop()
            {
                if (Interlocked.Decrement(ref _count) == 0)
                {
                    var asyncToken = _listener.BeginAsyncOperation("ProgressReportStop");
                    return RaiseStopped().CompletesAsyncOperation(asyncToken);
                }

                return SpecializedTasks.EmptyTask;
            }

            private Task RaiseStarted()
            {
                return RaiseEvent(nameof(Started));
            }

            private Task RaiseStopped()
            {
                return RaiseEvent(nameof(Stopped));
            }

            private Task RaiseEvent(string eventName)
            {
                // this method name doesn't have Async since it should work as async void.
                var ev = _eventMap.GetEventHandlers<EventHandler>(eventName);
                if (ev.HasHandlers)
                {
                    return _eventQueue.ScheduleTask(() =>
                    {
                        ev.RaiseEvent(handler => handler(this, EventArgs.Empty));
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

            public bool InProgress
            {
                get
                {
                    return false;
                }
            }

            public event EventHandler Started
            {
                add { }
                remove { }
            }

            public event EventHandler Stopped
            {
                add { }
                remove { }
            }
        }
    }
}
