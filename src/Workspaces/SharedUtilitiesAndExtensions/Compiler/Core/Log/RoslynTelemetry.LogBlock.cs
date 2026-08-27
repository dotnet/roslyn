// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log;

internal static partial class RoslynTelemetry
{
    // Regardless of how many tasks we can run in parallel on the machine, we likely won't need more than 256
    // instrumentation points in flight at a given time.
    // Use an object pool since we may be logging up to 1-10k events/second
    private static readonly ObjectPool<RoslynLogBlock> s_pool = new(() => new RoslynLogBlock(s_pool!), Math.Min(Environment.ProcessorCount * 8, 256));

    public static IDisposable CreateLogBlock(ImmutableArray<IEventSink> sinks, FunctionId functionId, LogMessage message, int blockId, CancellationToken cancellationToken)
    {
        var block = s_pool.Allocate();
        block.Construct(sinks, functionId, message, blockId, cancellationToken);
        return block;
    }

    /// <summary>
    /// This tracks the logged message. On instantiation, it logs 'Started block' with other event data.
    /// On dispose, it logs 'Ended block' with the same event data so we can track which block started and ended when looking at logs.
    /// </summary>
    private sealed class RoslynLogBlock(ObjectPool<RoslynLogBlock> pool) : IDisposable
    {
        /// <summary>
        /// How many sinks <see cref="_startedSinks"/> can track, i.e. its bit width.
        /// </summary>
        private const int MaxTrackedSinks = 32;

        // these need to be cleared before putting back to pool
        private ImmutableArray<IEventSink> _sinks;
        private LogMessage? _logMessage;
        private CancellationToken _cancellationToken;

        /// <summary>
        /// Bit i is set when <c>_sinks[i]</c> received the start, so that the end goes to exactly that
        /// set. A sink's <see cref="IEventSink.IsEnabled"/> can change while a block is open -
        /// <c>TelemetryLogger</c>'s tracks the session's opt-in state - and a sink that receives an end
        /// it has no start for either throws or leaks the pending scope.
        /// </summary>
        private int _startedSinks;

        private FunctionId _functionId;
        private int _tick;
        private int _blockId;

        public void Construct(ImmutableArray<IEventSink> sinks, FunctionId functionId, LogMessage logMessage, int blockId, CancellationToken cancellationToken)
        {
            Debug.Assert(sinks.Length <= MaxTrackedSinks, "More sinks than _startedSinks has bits for.");

            _sinks = sinks;
            _functionId = functionId;
            _logMessage = logMessage;
            _tick = Environment.TickCount;
            _blockId = blockId;
            _cancellationToken = cancellationToken;
            _startedSinks = 0;

            // Bounded by the bitmask width: a sink past it gets neither start nor end, which keeps the
            // pairing correct. Shifting past the width would instead alias onto bit 0 and hand some
            // other sink an end it never started.
            var trackable = Math.Min(sinks.Length, MaxTrackedSinks);
            for (var i = 0; i < trackable; i++)
            {
                if (sinks[i].IsEnabled(functionId))
                {
                    _startedSinks |= 1 << i;
                    sinks[i].LogBlockStart(functionId, logMessage, blockId, cancellationToken);
                }
            }
        }

        public void Dispose()
        {
            if (_sinks.IsDefaultOrEmpty)
            {
                return;
            }

            RoslynDebug.AssertNotNull(_logMessage);

            // This delta is valid for durations of < 25 days
            var delta = Environment.TickCount - _tick;

            var trackable = Math.Min(_sinks.Length, MaxTrackedSinks);
            for (var i = 0; i < trackable; i++)
            {
                if ((_startedSinks & (1 << i)) != 0)
                    _sinks[i].LogBlockEnd(_functionId, _logMessage, _blockId, delta, _cancellationToken);
            }

            // Free this block back to the pool
            _logMessage.Free();
            _logMessage = null;
            _sinks = default;
            _startedSinks = 0;
            _cancellationToken = default;

            pool.Free(this);
        }
    }
}
