// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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

        public bool TryWaitForTelemetryEvents(string[] names)
            => LoggerTestChannel.Instance.TryWaitForEvents(names);

        private sealed class LoggerTestChannel : ITelemetryTestChannel
        {
            public static readonly LoggerTestChannel Instance = new LoggerTestChannel();

            private BlockingCollection<TelemetryEvent> eventsQueue =
                new BlockingCollection<TelemetryEvent>();

            /// <summary>
            /// Waits for one or more events with the specified names
            /// </summary>
            /// <param name="events"></param>
            public bool TryWaitForEvents(string[] events)
            {
                if (!TelemetryService.DefaultSession.IsOptedIn)
                    return false;

                using var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout);
                var set = new HashSet<string>(events);
                while (set.Count > 0)
                {
                    var result = eventsQueue.Take(cancellationTokenSource.Token);
                    set.Remove(result.Name);
                }

                return true;
            }

            /// <summary>
            /// Clear current queue.
            /// </summary>
            public void Clear()
            {
                this.eventsQueue.CompleteAdding();
                this.eventsQueue = new BlockingCollection<TelemetryEvent>();
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
