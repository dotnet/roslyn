// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;
using Xunit;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

[TestService]
internal partial class TelemetryInProcess
{
    internal async Task<TelemetryVerifier> EnableTestTelemetryChannelAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        TelemetryService.DetachTestChannel(LoggerTestChannel.Instance);

        LoggerTestChannel.Instance.Clear();

        TelemetryService.AttachTestChannel(LoggerTestChannel.Instance);

        return new TelemetryVerifier(TestServices);
    }

    internal async Task DisableTestTelemetryChannelAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        TelemetryService.DetachTestChannel(LoggerTestChannel.Instance);

        LoggerTestChannel.Instance.Clear();
    }

    public async Task<bool> TryWaitForTelemetryEventsAsync(string[] names, CancellationToken cancellationToken)
        => await LoggerTestChannel.Instance.TryWaitForEventsAsync(names, cancellationToken);

    public class TelemetryVerifier : IAsyncDisposable
    {
        internal TestServices _testServices;

        public TelemetryVerifier(TestServices testServices)
        {
            _testServices = testServices;
        }

        public async ValueTask DisposeAsync()
            => await _testServices.Telemetry.DisableTestTelemetryChannelAsync(CancellationToken.None);

        /// <summary>
        /// Asserts that a telemetry event of the given name was fired. Does not
        /// do any additional validation (of performance numbers, etc).
        /// </summary>
        /// <param name="expectedEventNames"></param>
        public async Task VerifyFiredAsync(string[] expectedEventNames, CancellationToken cancellationToken)
        {
            var telemetryEnabled = await _testServices.Telemetry.TryWaitForTelemetryEventsAsync(expectedEventNames, cancellationToken);
            if (string.Equals(Environment.GetEnvironmentVariable("ROSLYN_TEST_CI"), "true", StringComparison.OrdinalIgnoreCase))
            {
                // Telemetry verification is optional for developer machines, but required for CI.
                Assert.True(telemetryEnabled);
            }
        }
    }

    private sealed class LoggerTestChannel : ITelemetryTestChannel
    {
        public static readonly LoggerTestChannel Instance = new();

        private AsyncQueue<TelemetryEvent> _eventsQueue = new();

        /// <summary>
        /// Waits for one or more events with the specified names
        /// </summary>
        /// <param name="events"></param>
        public async Task<bool> TryWaitForEventsAsync(string[] events, CancellationToken cancellationToken)
        {
            if (!TelemetryService.DefaultSession.IsOptedIn)
                return false;

            var set = new HashSet<string>(events);
            while (set.Count > 0)
            {
                var result = await _eventsQueue.DequeueAsync(cancellationToken);
                set.Remove(result.Name);
            }

            return true;
        }

        /// <summary>
        /// Clear current queue.
        /// </summary>
        public void Clear()
        {
            _eventsQueue.Complete();
            _eventsQueue = new AsyncQueue<TelemetryEvent>();
        }

        /// <summary>
        /// Process incoming events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ITelemetryTestChannel.OnPostEvent(object sender, TelemetryTestChannelEventArgs e)
        {
            _eventsQueue.Enqueue(e.Event);
        }
    }
}
