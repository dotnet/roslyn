using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Roslyn.Hosting.Diagnostics;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class TraceEventMonitor
    {
        private const string PerformanceFunctionOption = "Performance/FunctionId";

        private const string LogMessageFunctionIdNotExist = "\t FunctionId: ({0}) {1} is not in the dictionary";
        private const string LogMessageCount = "\t COUNT of {1} {2} {3} for {0}";
        private const string LogMessageDuration = "\t {0} DURATION of {2} ms >= {3} ms for {1}";
        private const string LogMessageStartListening = "\t [StartListening] FunctionId: ({0}) {1} @[{2}]";
        private const string LogMessageWaitFor = "\t [WaitFor] FunctionId: ({0}) {1} @[{2}]";
        private const string LogDurationMessage = "\t [Duration] - Start: ({0}) {1} - End: ({2}) {3}";
        private const string LogDurationFailed = "\t [Duration Failed - {0}] Start: ({1}) {2} - End: ({3}) {4}";
        private const string LogAddEventQueueMessage = "\t [Add Event Queue] FunctionId: {0} - Event Id: {1} @[{2}]";

        private const int LogId = 0;
        private const int BlockStartId = 1;
        private const int BlockStopId = 2;

        private static List<Entry> eventQueue = new List<Entry>();
        private static HashSet<(int, int)> logSet = new HashSet<(int, int)>();

        private static ConcurrentDictionary<int, int> counters;
        private static ConcurrentDictionary<int, ConcurrentDictionary<int, long>> durations;
        private static ConcurrentDictionary<int, ManualResetEventSlim> waitingEvents;

        private static bool traceListenerEnabled = false;
        private static IEtwCallbackManager listener = null;

        public static void StartListener(Process process)
        {
            listener = new EtwListener(process);
            StartListenerCore();
        }

        public static void StartListener(InProcessEtwEventListener listener)
        {
            TraceEventMonitor.listener = listener;
            StartListenerCore();
        }

        private static void StartListenerCore()
        {
            counters = new ConcurrentDictionary<int, int>();
            durations = new ConcurrentDictionary<int, ConcurrentDictionary<int, long>>();
            waitingEvents = new ConcurrentDictionary<int, ManualResetEventSlim>();

            ClearEventQueue();

            traceListenerEnabled = true;
        }

        public static void StopListener()
        {
            traceListenerEnabled = false;

            if (listener != null)
            {
                listener.Dispose();
            }

            listener = null;
        }

        public static void StartListening(string functionIdString)
        {
            var functionId = DiagnosticOnly_Logger.GetFunctionIdValue(functionIdString);

            Debug.WriteLine(string.Format(LogMessageStartListening, functionId, functionIdString, DateTime.Now.ToString()));

            VerifyTracingIsEnabled();

            AddCounter(functionId);
            AddDurationTracker(functionId);
            AddWaitingEvent(functionId);

            listener.StartListening(functionId, CountEvent);
            listener.StartListening(functionId, BlockStart, BlockStop);
        }

        public static void LogListeningEvents()
        {
            Debug.WriteLine(Environment.NewLine + listener.GetCallbackLogsPerEvent());
        }

        public static void ClearEventQueue()
        {
            lock (eventQueue)
            {
                eventQueue.Clear();
            }
        }

        public static void WaitFor(string functionIdString)
        {
            var functionId = DiagnosticOnly_Logger.GetFunctionIdValue(functionIdString);

            Debug.WriteLine(string.Format(LogMessageWaitFor, functionId, functionIdString, DateTime.Now.ToString()));

            var @event = GetEvent(functionId, functionIdString);
            if (@event != null)
            {
                @event.Wait();
            }
        }

        public static double? GetDurationBetween(string blockFunctionIdString, double failedValue)
        {
            var functionId = DiagnosticOnly_Logger.GetFunctionIdValue(blockFunctionIdString);
            Debug.WriteLine(string.Format(LogDurationMessage, functionId, blockFunctionIdString, functionId, blockFunctionIdString));

            lock (eventQueue)
            {
                var endTimeEntry = eventQueue.FirstOrDefault(t => t.FunctionId == functionId && t.EventId == BlockStopId);
                if (endTimeEntry == null)
                {
                    Debug.WriteLine(string.Format(LogDurationFailed, "EndTime", functionId, blockFunctionIdString, functionId, blockFunctionIdString));
                    return failedValue;
                }

                var blockId = endTimeEntry.BlockId;
                var endTimeInMs = endTimeEntry.TimeStampInMS;

                var startTimeEntry = eventQueue.LastOrDefault(t => t.FunctionId == functionId && t.BlockId == blockId && t.EventId == BlockStartId);
                if (startTimeEntry == null)
                {
                    Debug.WriteLine(string.Format(LogDurationFailed, "StartTime", functionId, blockFunctionIdString, functionId, blockFunctionIdString));
                    return failedValue;
                }

                var startTimeInMs = startTimeEntry.TimeStampInMS;
                return endTimeInMs - startTimeInMs;
            }
        }

        public static double? GetDurationBetween(string startFunctionIdString, string endFunctionIdString, double failedValue)
        {
            var endFunctionId = DiagnosticOnly_Logger.GetFunctionIdValue(endFunctionIdString);
            var startFunctionId = DiagnosticOnly_Logger.GetFunctionIdValue(startFunctionIdString);
            Debug.WriteLine(string.Format(LogDurationMessage, startFunctionId, startFunctionIdString, endFunctionId, endFunctionIdString));

            lock (eventQueue)
            {
                var startTimeEntry = eventQueue.LastOrDefault(t => t.FunctionId == startFunctionId && t.EventId != BlockStopId);
                if (startTimeEntry == null)
                {
                    Debug.WriteLine(string.Format(LogDurationFailed, "StartTime", startFunctionId, startFunctionIdString, endFunctionId, endFunctionIdString));
                    return failedValue;
                }

                var startTimeInMs = startTimeEntry.TimeStampInMS;

                var endTimeEntry = eventQueue.FirstOrDefault(t => t.FunctionId == endFunctionId && t.EventId != BlockStartId && startTimeInMs <= t.TimeStampInMS);
                if (endTimeEntry == null)
                {
                    Debug.WriteLine(string.Format(LogDurationFailed, "EndTime", startFunctionId, startFunctionIdString, endFunctionId, endFunctionIdString));
                    return failedValue;
                }

                var endTimeInMs = endTimeEntry.TimeStampInMS;
                return endTimeInMs - startTimeInMs;
            }
        }

        public static void VerifyCounterEquals(string functionIdString, int expected, bool failIfThresholdExceeded = false)
        {
            var functionId = DiagnosticOnly_Logger.GetFunctionIdValue(functionIdString);

            Verify(functionId, () =>
            {
                var count = GetCount(functionId, functionIdString);
                if (count != expected)
                {
                    Debug.WriteLine(string.Format(LogMessageCount, functionId, count, "!=", expected));
                }

                counters.TryRemove(functionId, out count);
            });
        }

        public static void VerifyCounterIsLessThan(string functionIdString, int expected, bool failIfThresholdExceeded = false)
        {
            var functionId = DiagnosticOnly_Logger.GetFunctionIdValue(functionIdString);

            Verify(functionId, () =>
            {
                var count = GetCount(functionId, functionIdString);
                if (count >= expected)
                {
                    Debug.WriteLine(string.Format(LogMessageCount, functionId, count, ">=", expected));
                }

                counters.TryRemove(functionId, out count);
            });
        }

        public static void VerifyCounterIsGreaterThan(string functionIdString, int expected, bool failIfThresholdExceeded = false)
        {
            var functionId = DiagnosticOnly_Logger.GetFunctionIdValue(functionIdString);

            Verify(functionId, () =>
            {
                var count = GetCount(functionId, functionIdString);
                if (count <= expected)
                {
                    Debug.WriteLine(string.Format(LogMessageCount, functionId, count, "<=", expected));
                }

                counters.TryRemove(functionId, out count);
            });
        }

        public static void VerifyAllDurationsAreLessThan(string functionIdString, long expected, bool failIfDurationExceeded = false)
        {
            var functionId = DiagnosticOnly_Logger.GetFunctionIdValue(functionIdString);

            Verify(functionId, () =>
            {
                var duration = GetDuration(functionId, functionIdString);
                if (duration != null && !duration.All(d => d.Value < expected))
                {
                    var max = duration.Max(d => d.Value);
                    Debug.WriteLine(string.Format(LogMessageDuration, "Max", functionId, max, expected));
                }

                durations.TryRemove(functionId, out duration);
            });
        }

        public static void VerifyAverageDurationIsLessThan(string functionIdString, long expected, bool failIfDurationExceeded = false)
        {
            var functionId = DiagnosticOnly_Logger.GetFunctionIdValue(functionIdString);

            Verify(functionId, () =>
            {
                var duration = GetDuration(functionId, functionIdString);
                var averageDuration = duration == null ? double.MaxValue : duration.Average(d => d.Value);
                if (averageDuration >= expected)
                {
                    Debug.WriteLine(string.Format(LogMessageDuration, "Avg", functionId, averageDuration, expected));
                }

                durations.TryRemove(functionId, out duration);
            });
        }

        private static void AddCounter(int functionId)
        {
            do
            {
                int count;
                counters.TryRemove(functionId, out count);
            } while (!counters.TryAdd(functionId, 0));
        }

        private static void AddDurationTracker(int functionId)
        {
            do
            {
                ConcurrentDictionary<int, long> duration;
                durations.TryRemove(functionId, out duration);
            } while (!durations.TryAdd(functionId, new ConcurrentDictionary<int, long>()));
        }

        private static void AddWaitingEvent(int functionId)
        {
            do
            {
                ManualResetEventSlim @event;
                waitingEvents.TryRemove(functionId, out @event);
            } while (!waitingEvents.TryAdd(functionId, new ManualResetEventSlim(false)));
        }

        private static void VerifyTracingIsEnabled()
        {
            if (!traceListenerEnabled)
            {
                throw new InvalidOperationException("Tracing not enabled.");
            }
        }

        private static void Verify(int functionId, Action action)
        {
            VerifyTracingIsEnabled();

            action();

            listener.StopListening(functionId, CountEvent);
            listener.StopListening(functionId, BlockStart, BlockStop);
        }

        private static void CountEvent(LogEvent arg)
        {
            if (!traceListenerEnabled)
            {
                return;
            }

            // todo detect UI thread
            var functionId = arg.functionId;
            var functionIdString = DiagnosticOnly_Logger.GetFunctionId(functionId);

            AddEventQueue(functionId, LogId, -1, arg.timeStampMS);
            IncreaseCount(functionId, functionIdString);

            var @event = GetEvent(functionId, functionIdString);
            SetEvent(functionId, functionIdString, @event);
        }

        private static void BlockStart(BlockStartEvent arg)
        {
            if (!traceListenerEnabled)
            {
                return;
            }

            var functionId = arg.functionId;
            var functionIdString = DiagnosticOnly_Logger.GetFunctionId(functionId);

            AddEventQueue(functionId, BlockStartId, arg.blockId, arg.timeStampMS);
            IncreaseCount(functionId, functionIdString);

            var duration = GetDuration(functionId, functionIdString);
            if (duration != null)
            {
                duration.TryAdd(arg.blockId, 0);
            }
        }

        private static void BlockStop(BlockStopEvent arg)
        {
            if (!traceListenerEnabled)
            {
                return;
            }

            var functionId = arg.functionId;
            var functionIdString = DiagnosticOnly_Logger.GetFunctionId(functionId);

            AddEventQueue(functionId, BlockStopId, arg.blockId, arg.timeStampMS);

            var duration = GetDuration(functionId, functionIdString);
            if (duration != null && duration.ContainsKey(arg.blockId))
            {
                // todo detect UI thread
                duration[arg.blockId] = arg.tick;

                var @event = GetEvent(functionId, functionIdString);
                SetEvent(functionId, functionIdString, @event);
            }
            else
            {
                Debug.WriteLine("\t  Received BlockStop for unknown trace event function id : " + functionId + " block id : " + arg.blockId);
            }
        }

        private static void SetEvent(int functionId, string functionIdString, ManualResetEventSlim @event)
        {
            if (@event != null && !@event.IsSet)
            {
                @event.Set();
            }
        }

        private static int GetCount(int functionId, string functionIdString)
        {
            int count;
            if (!counters.TryGetValue(functionId, out count))
            {
                Debug.WriteLine(string.Format(LogMessageFunctionIdNotExist, functionId, functionIdString));
            }

            return count;
        }

        private static ConcurrentDictionary<int, long> GetDuration(int functionId, string functionIdString)
        {
            ConcurrentDictionary<int, long> duration;
            if (!durations.TryGetValue(functionId, out duration))
            {
                Debug.WriteLine(string.Format(LogMessageFunctionIdNotExist, functionId, functionIdString));
            }

            return duration;
        }

        private static ManualResetEventSlim GetEvent(int functionId, string functionIdString)
        {
            ManualResetEventSlim @event;
            if (!waitingEvents.TryGetValue(functionId, out @event))
            {
                Debug.WriteLine(string.Format(LogMessageFunctionIdNotExist, functionId, functionIdString));
            }

            return @event;
        }

        private static void IncreaseCount(int functionId, string functionIdString)
        {
            int count;
            if (!counters.TryGetValue(functionId, out count))
            {
                Debug.WriteLine(string.Format(LogMessageFunctionIdNotExist, functionId, functionIdString));
            }
            else
            {
                counters[functionId] = count + 1;
            }
        }

        private static void AddEventQueue(int functionId, int eventId, int blockId, long timestampInMS)
        {
            lock (eventQueue)
            {
                if (logSet.Add((functionId, eventId)))
                {
                    Debug.WriteLine(string.Format(LogAddEventQueueMessage, functionId, eventId, DateTime.Now.ToString()));
                }

                eventQueue.Add(new Entry(functionId, eventId, blockId, timestampInMS));
            }
        }

        private class Entry
        {
            public readonly int FunctionId;
            public readonly int EventId;
            public readonly int BlockId;
            public readonly long TimeStampInMS;

            public Entry(int functionId, int eventId, int blockId, long timestampInMS)
            {
                this.FunctionId = functionId;
                this.EventId = eventId;
                this.BlockId = blockId;
                this.TimeStampInMS = timestampInMS;
            }
        }
    }
}
