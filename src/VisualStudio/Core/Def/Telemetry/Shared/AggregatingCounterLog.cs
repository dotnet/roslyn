// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;

namespace Microsoft.CodeAnalysis.Telemetry;

/// <summary>
/// Provides a wrapper around the VSTelemetry counter APIs to support aggregated counter telemetry. Each instance
/// of this class corresponds to a specific FunctionId operation and can support counting aggregated values for each
/// metric name logged.
/// </summary>
internal sealed class AggregatingCounterLog : AbstractAggregatingLog<ICounter<long>, long>
{
    public AggregatingCounterLog(TelemetrySession session, FunctionId functionId) : base(session, functionId)
    {
    }

    protected override ICounter<long> CreateAggregator(IMeter meter, string metricName)
    {
        return meter.CreateCounter<long>(metricName);
    }

    protected override void UpdateAggregator(ICounter<long> counter, long value)
    {
        counter.Add(value);
    }

    protected override TelemetryMetricEvent CreateTelemetryEvent(TelemetryEvent telemetryEvent, ICounter<long> counter)
    {
        return new TelemetryCounterEvent<long>(telemetryEvent, counter);
    }
}
