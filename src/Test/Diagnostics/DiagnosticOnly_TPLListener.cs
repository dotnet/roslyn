// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Roslyn.Hosting.Diagnostics
{
    /// <summary>
    /// This listens to TPL task events.
    /// </summary>
    public static class DiagnosticOnly_TPLListener
    {
        private static TPLListener s_listener = null;

        public static void Install()
        {
            // make sure TPL installs its own event source
            Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

            var local = new TPLListener();
            Interlocked.CompareExchange(ref s_listener, local, null);
        }

        public static void Uninstall()
        {
            TPLListener local = null;
            Interlocked.Exchange(ref local, s_listener);

            if (local != null)
            {
                local.Dispose();
            }
        }

        private sealed class TPLListener : EventListener
        {
            private const int EventIdTaskScheduled = 7;
            private const int EventIdTaskStarted = 8;
            private const int EventIdTaskCompleted = 9;

            public TPLListener()
            {
                var tplEventSource = EventSource.GetSources().First(e => e.Name == "System.Threading.Tasks.TplEventSource");
                EnableEvents(tplEventSource, EventLevel.LogAlways);
            }

            /// <summary>
            /// Pass TPL events to our logger.
            /// </summary>
            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                // for now, we just log what TPL already publish, later we actually want to manipulate the information
                // and publish that information
                switch (eventData.EventId)
                {
                    case EventIdTaskScheduled:
                        Logger.Log(FunctionId.TPLTask_TaskScheduled, string.Empty);
                        break;
                    case EventIdTaskStarted:
                        Logger.Log(FunctionId.TPLTask_TaskStarted, string.Empty);
                        break;
                    case EventIdTaskCompleted:
                        Logger.Log(FunctionId.TPLTask_TaskCompleted, string.Empty);
                        break;
                    default:
                        // Ignore the rest
                        break;
                }
            }
        }
    }
}
