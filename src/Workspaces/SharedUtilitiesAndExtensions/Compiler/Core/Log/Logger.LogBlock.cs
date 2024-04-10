// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log;

internal static partial class Logger
{
    // Regardless of how many tasks we can run in parallel on the machine, we likely won't need more than 256
    // instrumentation points in flight at a given time.
    // Use an object pool since we may be logging up to 1-10k events/second
    private static readonly ObjectPool<RoslynLogBlock> s_pool = new(() => new RoslynLogBlock(s_pool!), Math.Min(Environment.ProcessorCount * 8, 256));

    private static IDisposable CreateLogBlock(FunctionId functionId, LogMessage message, int blockId, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(s_currentLogger);

        var block = s_pool.Allocate();
        block.Construct(s_currentLogger, functionId, message, blockId, cancellationToken);
        return block;
    }

    /// <summary>
    /// This tracks the logged message. On instantiation, it logs 'Started block' with other event data.
    /// On dispose, it logs 'Ended block' with the same event data so we can track which block started and ended when looking at logs.
    /// </summary>
    private class RoslynLogBlock(ObjectPool<RoslynLogBlock> pool) : IDisposable
    {

        // these need to be cleared before putting back to pool
        private ILogger? _logger;
        private LogMessage? _logMessage;
        private CancellationToken _cancellationToken;

        private FunctionId _functionId;
        private int _tick;
        private int _blockId;

        public void Construct(ILogger logger, FunctionId functionId, LogMessage logMessage, int blockId, CancellationToken cancellationToken)
        {
            _logger = logger;
            _functionId = functionId;
            _logMessage = logMessage;
            _tick = Environment.TickCount;
            _blockId = blockId;
            _cancellationToken = cancellationToken;

            logger.LogBlockStart(functionId, logMessage, blockId, cancellationToken);
        }

        public void Dispose()
        {
            if (_logger == null)
            {
                return;
            }

            RoslynDebug.AssertNotNull(_logMessage);

            // This delta is valid for durations of < 25 days
            var delta = Environment.TickCount - _tick;

            _logger.LogBlockEnd(_functionId, _logMessage, _blockId, delta, _cancellationToken);

            // Free this block back to the pool
            _logMessage.Free();
            _logMessage = null;
            _logger = null;
            _cancellationToken = default;

            pool.Free(this);
        }
    }
}
