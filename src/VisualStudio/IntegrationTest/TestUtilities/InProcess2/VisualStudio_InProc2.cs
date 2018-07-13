// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class VisualStudio_InProc2 : InProcComponent2
    {
        public VisualStudio_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public new IntPtr MainWindowHandle => base.MainWindowHandle;

        public string InstallationPath => throw new NotImplementedException();

#if false
        public string[] GetAvailableCommands()
        {
            List<string> result = new List<string>();
            var commands = GetDTE().Commands;
            foreach (Command command in commands)
            {
                try
                {
                    string commandName = command.Name;
                    if (command.IsAvailable)
                    {
                        result.Add(commandName);
                    }
                }
                finally { }
            }

            return result.ToArray();
        }
#endif

        public async Task ActivateMainWindowAsync(bool skipAttachingThreads = false)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetDTEAsync();

            var activeVisualStudioWindow = (IntPtr)dte.ActiveWindow.HWnd;
            Debug.WriteLine($"DTE.ActiveWindow.HWnd = {activeVisualStudioWindow}");

            if (activeVisualStudioWindow == IntPtr.Zero)
            {
                activeVisualStudioWindow = (IntPtr)dte.MainWindow.HWnd;
                Debug.WriteLine($"DTE.MainWindow.HWnd = {activeVisualStudioWindow}");
            }

            IntegrationHelper.SetForegroundWindow(activeVisualStudioWindow, skipAttachingThreads);
        }

#if false
        public int GetErrorListErrorCount()
        {
            var dte = (DTE2)GetDTE();
            var errorList = dte.ToolWindows.ErrorList;

            var errorItems = errorList.ErrorItems;
            var errorItemsCount = errorItems.Count;

            var errorCount = 0;

            try
            {
                for (var index = 1; index <= errorItemsCount; index++)
                {
                    var errorItem = errorItems.Item(index);

                    if (errorItem.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh)
                    {
                        errorCount += 1;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // It is entirely possible that the items in the error list are modified
                // after we start iterating, in which case we want to try again.
                return GetErrorListErrorCount();
            }

            return errorCount;
        }

        public void WaitForNoErrorsInErrorList()
        {
            while (GetErrorListErrorCount() != 0)
            {
                System.Threading.Thread.Yield();
            }
        }

        public void Quit()
            => GetDTE().Quit();
#endif

        public new async Task<TInterface> GetGlobalServiceAsync<TService, TInterface>()
            where TService : class
            where TInterface : class
        {
            return await base.GetGlobalServiceAsync<TService, TInterface>();
        }

        public new async Task<bool> IsCommandAvailableAsync(string commandName)
        {
            return await base.IsCommandAvailableAsync(commandName);
        }

        public new async Task ExecuteCommandAsync(string commandName, string args = "")
        {
            await base.ExecuteCommandAsync(commandName, args);
        }

        public new async Task WaitForApplicationIdleAsync(CancellationToken cancellationToken)
        {
            await InProcComponent2.WaitForApplicationIdleAsync(cancellationToken);
        }

        #region Telemetry

        public async Task<TelemetryVerifier> EnableTestTelemetryChannelAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            TelemetryService.DetachTestChannel(LoggerTestChannel.Instance);

            LoggerTestChannel.Instance.Clear();

            TelemetryService.AttachTestChannel(LoggerTestChannel.Instance);

            return new TelemetryVerifier(this);
        }

        public async Task DisableTestTelemetryChannelAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            TelemetryService.DetachTestChannel(LoggerTestChannel.Instance);

            LoggerTestChannel.Instance.Clear();
        }

        public async Task WaitForTelemetryEventsAsync(string[] names)
            => await LoggerTestChannel.Instance.WaitForEventsAsync(names);

        public sealed class TelemetryVerifier : IDisposable
        {
            private readonly VisualStudio_InProc2 _instance;

            public TelemetryVerifier(VisualStudio_InProc2 instance)
            {
                _instance = instance;
            }

            public void Dispose() => _instance.JoinableTaskFactory.Run(() => _instance.DisableTestTelemetryChannelAsync());

            /// <summary>
            /// Asserts that a telemetry event of the given name was fired. Does not
            /// do any additional validation (of performance numbers, etc).
            /// </summary>
            /// <param name="expectedEventNames"></param>
            public async Task VerifyFiredAsync(params string[] expectedEventNames)
            {
                await _instance.WaitForTelemetryEventsAsync(expectedEventNames);
            }
        }

        private sealed class LoggerTestChannel : ITelemetryTestChannel
        {
            public static readonly LoggerTestChannel Instance = new LoggerTestChannel();

            private ConcurrentBag<TelemetryEvent> eventsQueue =
                new ConcurrentBag<TelemetryEvent>();

            /// <summary>
            /// Waits for one or more events with the specified names
            /// </summary>
            /// <param name="events"></param>
            public async Task WaitForEventsAsync(string[] events)
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
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }
            }

            /// <summary>
            /// Clear current queue.
            /// </summary>
            public void Clear()
            {
                eventsQueue = new ConcurrentBag<TelemetryEvent>();
            }

            /// <summary>
            /// Process incoming events.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void ITelemetryTestChannel.OnPostEvent(object sender, TelemetryTestChannelEventArgs e)
            {
                eventsQueue.Add(e.Event);
            }
        }
        #endregion Telemetry
    }
}
