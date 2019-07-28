// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal partial class VisualStudio_InProc : InProcComponent
    {
        public void EnableTestTelemetryChannel()
        {
            InvokeOnUIThread(cancellationToken =>
            {
                TelemetryService.DetachTestChannel(LoggerTestChannel.Instance);

                LoggerTestChannel.Instance.Clear();

                TelemetryService.AttachTestChannel(LoggerTestChannel.Instance);
            });
        }

        public void DisableTestTelemetryChannel()
        {
            InvokeOnUIThread(cancellationToken =>
            {
                TelemetryService.DetachTestChannel(LoggerTestChannel.Instance);

                LoggerTestChannel.Instance.Clear();
            });
        }

        public void WaitForTelemetryEvents(string[] names)
            => LoggerTestChannel.Instance.WaitForEvents(names);

        private sealed class LoggerTestChannel : ITelemetryTestChannel
        {
            public static readonly LoggerTestChannel Instance = new LoggerTestChannel();

            private ConcurrentBag<TelemetryEvent> eventsQueue =
                new ConcurrentBag<TelemetryEvent>();

            /// <summary>
            /// Waits for one or more events with the specified names
            /// </summary>
            /// <param name="events"></param>
            public void WaitForEvents(string[] events)
            {
                var set = new HashSet<string>(events);
                while (true)
                {
                    if (eventsQueue.TryTake(out var result))
                    {
                        set.Remove(result.Name);
                        if (set.Count == 0)
                        {
                            return;
                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }

            /// <summary>
            /// Clear current queue.
            /// </summary>
            public void Clear()
            {
                this.eventsQueue = new ConcurrentBag<TelemetryEvent>();
            }

            /// <summary>
            /// Process incoming events.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void ITelemetryTestChannel.OnPostEvent(object sender, TelemetryTestChannelEventArgs e)
            {
                this.eventsQueue.Add(e.Event);
            }
        }
    }
}
