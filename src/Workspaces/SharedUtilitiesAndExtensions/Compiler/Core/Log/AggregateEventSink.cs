// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Fans an event out to a fixed set of sinks. The set is decided once, when a host composes its
/// telemetry, and is not mutated afterwards: turning a sink off is that sink's own
/// <see cref="IEventSink.IsEnabled"/> returning false, not its removal from this list. That keeps a
/// sink from being registered twice (which would post its events twice) and removes the need for the
/// predicate-based add/replace/remove that used to live here.
/// </summary>
internal sealed class AggregateEventSink : IEventSink
{
    private readonly ImmutableArray<IEventSink> _sinks;

    private AggregateEventSink(ImmutableArray<IEventSink> sinks)
        => _sinks = sinks;

    public static AggregateEventSink Create(params IEventSink?[] sinks)
    {
        var set = new HashSet<IEventSink>();

        // flatten nested aggregates so a sink can never appear twice
        foreach (var sink in sinks)
        {
            if (sink is null)
                continue;

            if (sink is AggregateEventSink aggregate)
            {
                set.UnionWith(aggregate._sinks);
                continue;
            }

            set.Add(sink);
        }

        return new AggregateEventSink([.. set]);
    }

    public bool IsEnabled(FunctionId functionId)
        => true;

    public void Log(FunctionId functionId, LogMessage logMessage)
    {
        for (var i = 0; i < _sinks.Length; i++)
        {
            var sink = _sinks[i];
            if (!sink.IsEnabled(functionId))
            {
                continue;
            }

            sink.Log(functionId, logMessage);
        }
    }

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
    {
        for (var i = 0; i < _sinks.Length; i++)
        {
            var sink = _sinks[i];
            if (!sink.IsEnabled(functionId))
            {
                continue;
            }

            sink.LogBlockStart(functionId, logMessage, uniquePairId, cancellationToken);
        }
    }

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
        for (var i = 0; i < _sinks.Length; i++)
        {
            var sink = _sinks[i];
            if (!sink.IsEnabled(functionId))
            {
                continue;
            }

            sink.LogBlockEnd(functionId, logMessage, uniquePairId, delta, cancellationToken);
        }
    }
}
