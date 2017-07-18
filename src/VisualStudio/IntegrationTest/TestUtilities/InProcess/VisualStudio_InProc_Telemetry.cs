// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal partial class VisualStudio_InProc : InProcComponent
    {
        public void EnableTestTelemetryChannel()
        {
            InvokeOnUIThread(() =>
            {
                // detach if there is one already attached
                TelemetryService.DetachTestChannel(LoggerTestChannel.Instance);

                // make sure we clear any remaining events before attach it back
                LoggerTestChannel.Instance.Clear();

                // attach logger channel to the telemetry session
                TelemetryService.AttachTestChannel(LoggerTestChannel.Instance);
            });
        }

        public void DisableTestTelemetryChannel()
        {
            InvokeOnUIThread(() =>
            {
                // detach if there is one attached
                TelemetryService.DetachTestChannel(LoggerTestChannel.Instance);

                // make sure we clear any leftout events before attach it back
                LoggerTestChannel.Instance.Clear();
            });
        }

        public string[] GetTelemetryEventNames()
            => LoggerTestChannel.Instance.GetEvents().Select(e => e.Name).ToArray();

        /// <summary>
        /// Logger channel collects events and provide them as a list, starting from the beginning.
        /// One of the functionality is to clear current list of the events.
        /// </summary>
        private sealed class LoggerTestChannel : ITelemetryTestChannel
        {
            public static readonly LoggerTestChannel Instance = new LoggerTestChannel();

            private ConcurrentBag<TelemetryEvent> eventsQueue =
                new ConcurrentBag<TelemetryEvent>();

            /// <summary>
            /// Get the current list of the events. It is assumed that list can be dynamically changed
            /// and user just get latest snapshot.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<TelemetryEvent> GetEvents() =>
                this.eventsQueue.ToArray();

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
