// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// A logger that publishes events to ETW using an EventSource.
    /// </summary>
    internal sealed class EtwLogger : ILogger
    {
        public static readonly EtwLogger Instance = new EtwLogger();
        private readonly Func<FeatureId, FunctionId, bool> loggingChecker;

        // Use an object pool since we may be logging up to 1-10k events/second
        private readonly ObjectPool<RoslynEtwLogBlock> etwBlocksPool;

        public EtwLogger()
            : this((Func<FeatureId, FunctionId, bool>)null)
        {
        }

        public EtwLogger(IOptionService optionService)
            : this(Logger.GetLoggingChecker(optionService))
        {
        }

        public EtwLogger(Func<FeatureId, FunctionId, bool> loggingChecker)
        {
            this.loggingChecker = loggingChecker;

            // Regardless of how many tasks we can run in parallel on the machine, we likely won't need more than 256
            // instrumentation points in flight at a given time.
            var poolSize = Math.Min(Environment.ProcessorCount * 8, 256);
            etwBlocksPool = new ObjectPool<RoslynEtwLogBlock>(() => new RoslynEtwLogBlock(etwBlocksPool), poolSize);
        }

        public bool IsEnabled(FeatureId featureId, FunctionId functionId)
        {
            return RoslynEventSource.Instance.IsEnabled() && (this.loggingChecker == null || this.loggingChecker(featureId, functionId));
        }

        public bool IsVerbose()
        {
            // "-1" makes this to work with any keyword
            return RoslynEventSource.Instance.IsEnabled(EventLevel.Verbose, (EventKeywords)(-1));
        }

        public void Log(FeatureId featureId, FunctionId functionId, string message)
        {
            RoslynEventSource.Instance.Log(message, featureId, functionId);
        }

        public IDisposable LogBlock(FeatureId featureId, FunctionId functionId, string message, int blockId, CancellationToken cancellationToken)
        {
            var block = etwBlocksPool.Allocate();
            block.Construct(functionId, message, blockId, cancellationToken);
            return block;
        }

        /// <summary>
        /// This tracks the logged message. On instantiation, it logs 'Started block' with other event data.
        /// On dispose, it logs 'Ended block' with the same event data so we can track which block started and ended when looking at logs.
        /// </summary>
        private sealed class RoslynEtwLogBlock : IDisposable
        {
            private FunctionId functionId;
            private int tick;
            private int blockId;
            private CancellationToken cancellationToken;
            private readonly ObjectPool<RoslynEtwLogBlock> pool;

            public RoslynEtwLogBlock(ObjectPool<RoslynEtwLogBlock> pool)
            {
                this.pool = pool;
            }

            public void Construct(FunctionId functionId, string message, int blockId, CancellationToken cancellationToken)
            {
                this.functionId = functionId;
                this.tick = Environment.TickCount;
                this.blockId = blockId;
                this.cancellationToken = cancellationToken;

                RoslynEventSource.Instance.BlockStart(message, functionId, blockId);
            }

            public void Dispose()
            {
                // This delta is valid for durations of < 25 days
                var delta = Environment.TickCount - this.tick;
                if (cancellationToken.IsCancellationRequested)
                {
                    RoslynEventSource.Instance.BlockCanceled(functionId, delta, blockId);
                }
                else
                {
                    RoslynEventSource.Instance.BlockStop(functionId, delta, blockId);
                }

                // Free this block back to the pool
                cancellationToken = default(CancellationToken);
                pool.Free(this);
            }
        }
    }
}
