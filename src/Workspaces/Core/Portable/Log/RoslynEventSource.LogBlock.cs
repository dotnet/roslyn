// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal partial class RoslynEventSource
    {
        // Regardless of how many tasks we can run in parallel on the machine, we likely won't need more than 256
        // instrumentation points in flight at a given time.
        // Use an object pool since we may be logging up to 1-10k events/second
        private static readonly ObjectPool<RoslynLogBlock> s_pool = new ObjectPool<RoslynLogBlock>(() => new RoslynLogBlock(s_pool), Math.Min(Environment.ProcessorCount * 8, 256));

        /// <summary>
        /// next unique block id that will be given to each LogBlock
        /// </summary>
        private static int s_lastUniqueBlockId;

        /// <summary>
        /// Logs a block with the given <paramref name="message"/> and specified <paramref name="functionId"/>.
        /// On dispose of the returned disposable object, it logs the 'tick' count between the start and end of the block.
        /// Unlike other logging methods on <see cref="RoslynEventSource"/>, this method does not check
        /// if the specified <paramref name="functionId"/> was explicitly enabled.
        /// Instead it checks if the <see cref="RoslynEventSource"/> was enabled at <see cref="EventLevel.Informational"/> level.
        /// </summary>
        public static IDisposable LogInformationalBlock(FunctionId functionId, string message, CancellationToken cancellationToken)
            => LogBlock(functionId, message, EventLevel.Informational, cancellationToken);

        private static IDisposable LogBlock(FunctionId functionId, string message, EventLevel requiredEventLevel, CancellationToken cancellationToken)
        {
            if (!Instance.IsEnabled(requiredEventLevel, EventKeywords.None))
            {
                return EmptyLogBlock.Instance;
            }

            return CreateLogBlock(functionId, message, cancellationToken);
        }

        /// <summary>
        /// return next unique pair id
        /// </summary>
        private static int GetNextUniqueBlockId()
        {
            return Interlocked.Increment(ref s_lastUniqueBlockId);
        }

        private static IDisposable CreateLogBlock(FunctionId functionId, string message, CancellationToken cancellationToken)
        {
            var block = s_pool.Allocate();
            var blockId = GetNextUniqueBlockId();
            block.Construct(functionId, message, blockId, cancellationToken);
            return block;
        }

        /// <summary>
        /// This tracks the logged message. On instantiation, it logs 'Started block' with other event data.
        /// On dispose, it logs 'Ended block' with the same event data so we can track which block started and ended when looking at logs.
        /// </summary>
        private class RoslynLogBlock : IDisposable
        {
            private readonly ObjectPool<RoslynLogBlock> _pool;
            private CancellationToken _cancellationToken;

            private FunctionId _functionId;
            private int _tick;
            private int _blockId;

            public RoslynLogBlock(ObjectPool<RoslynLogBlock> pool)
            {
                _pool = pool;
            }

            public void Construct(FunctionId functionId, string message, int blockId, CancellationToken cancellationToken)
            {
                _functionId = functionId;
                _tick = Environment.TickCount;
                _blockId = blockId;
                _cancellationToken = cancellationToken;

                Instance.BlockStart(message, functionId, blockId);
            }

            public void Dispose()
            {
                // This delta is valid for durations of < 25 days
                var delta = Environment.TickCount - _tick;

                if (_cancellationToken.IsCancellationRequested)
                {
                    Instance.BlockCanceled(_functionId, delta, _blockId);
                }
                else
                {
                    Instance.BlockStop(_functionId, delta, _blockId);
                }

                // Free this block back to the pool
                _pool.Free(this);
            }
        }
    }
}
