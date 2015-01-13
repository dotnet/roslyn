// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal static partial class Logger
    {
        // Regardless of how many tasks we can run in parallel on the machine, we likely won't need more than 256
        // instrumentation points in flight at a given time.
        // Use an object pool since we may be logging up to 1-10k events/second
        private static readonly ObjectPool<RoslynLogBlock> Pool = new ObjectPool<RoslynLogBlock>(() => new RoslynLogBlock(Pool), Math.Min(Environment.ProcessorCount * 8, 256));

        private static IDisposable CreateLogBlock(FunctionId functionId, LogMessage message, int blockId, CancellationToken cancellationToken)
        {
            var block = Pool.Allocate();
            block.Construct(currentLogger, functionId, message, blockId, cancellationToken);
            return block;
        }

        /// <summary>
        /// This tracks the logged message. On instantiation, it logs 'Started block' with other event data.
        /// On dispose, it logs 'Ended block' with the same event data so we can track which block started and ended when looking at logs.
        /// </summary>
        private class RoslynLogBlock : IDisposable
        {
            private readonly ObjectPool<RoslynLogBlock> pool;

            // these need to be cleared before putting back to pool
            private ILogger logger;
            private LogMessage logMessage;
            private CancellationToken cancellationToken;

            private FunctionId functionId;
            private int tick;
            private int blockId;

            public RoslynLogBlock(ObjectPool<RoslynLogBlock> pool)
            {
                this.pool = pool;
            }

            public void Construct(ILogger logger, FunctionId functionId, LogMessage logMessage, int blockId, CancellationToken cancellationToken)
            {
                this.logger = logger;
                this.functionId = functionId;
                this.logMessage = logMessage;
                this.tick = Environment.TickCount;
                this.blockId = blockId;
                this.cancellationToken = cancellationToken;

                logger.LogBlockStart(functionId, logMessage, blockId, cancellationToken);
            }

            public void Dispose()
            {
                if (logger == null)
                {
                    return;
                }

                // This delta is valid for durations of < 25 days
                var delta = Environment.TickCount - this.tick;

                logger.LogBlockEnd(functionId, logMessage, blockId, delta, cancellationToken);

                // Free this block back to the pool
                logMessage.Free();
                logMessage = null;
                logger = null;
                cancellationToken = default(CancellationToken);

                pool.Free(this);
            }
        }
    }
}