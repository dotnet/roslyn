// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal partial class RoslynEventSource
    {
        /// <summary>
        /// Logs an informational block with given <paramref name="entity"/>'s <see cref="object.ToString"/> representation as the message
        /// and specified <paramref name="functionId"/>.
        /// On dispose of the returned disposable object, it logs the 'tick' count between the start and end of the block.
        /// Unlike other logging methods on <see cref="RoslynEventSource"/>, this method does not check
        /// if the specified <paramref name="functionId"/> was explicitly enabled.
        /// Instead it checks if the <see cref="RoslynEventSource"/> was enabled at <see cref="EventLevel.Informational"/> level.
        /// </summary>
        public static LogBlock LogInformationalBlock(FunctionId functionId, object entity, CancellationToken cancellationToken)
            => LogBlock.Create(functionId, entity, EventLevel.Informational, cancellationToken);

        /// <summary>
        /// Logs an informational message block with the given <paramref name="message"/>> and specified <paramref name="functionId"/>.
        /// On dispose of the returned disposable object, it logs the 'tick' count between the start and end of the block.
        /// Unlike other logging methods on <see cref="RoslynEventSource"/>, this method does not check
        /// if the specified <paramref name="functionId"/> was explicitly enabled.
        /// Instead it checks if the <see cref="RoslynEventSource"/> was enabled at <see cref="EventLevel.Informational"/> level.
        /// </summary>
        public static LogBlock LogInformationalBlock(FunctionId functionId, string message, CancellationToken cancellationToken)
            => LogBlock.Create(functionId, message, EventLevel.Informational, cancellationToken);

        /// <summary>
        /// This tracks the logged message. On instantiation, it logs 'Started block' with other event data.
        /// On dispose, it logs 'Ended block' with the same event data so we can track which block started and ended when looking at logs.
        /// </summary>
        internal struct LogBlock : IDisposable
        {
            private readonly FunctionId _functionId;
            private readonly object? _entityForMessage;
            private readonly EventLevel _eventLevel;
            private readonly int _blockId;
            private readonly CancellationToken _cancellationToken;

            private int _tick;
            private bool _startLogged;
            private string? _message;

            /// <summary>
            /// next unique block id that will be given to each LogBlock
            /// </summary>
            private static int s_lastUniqueBlockId;

            private LogBlock(
                FunctionId functionId,
                string? message,
                object? entityForMessage,
                EventLevel eventLevel,
                int blockId,
                CancellationToken cancellationToken)
            {
                Debug.Assert(message != null || entityForMessage != null);

                _functionId = functionId;
                _message = message;
                _entityForMessage = entityForMessage;
                _eventLevel = eventLevel;
                _blockId = blockId;
                _cancellationToken = cancellationToken;
                _tick = Environment.TickCount;
                _startLogged = false;
            }

            public static LogBlock Create(
                FunctionId functionId,
                object entityForMessage,
                EventLevel eventLevel,
                CancellationToken cancellationToken)
            {
                var blockId = GetNextUniqueBlockId();
                var logBlock = new LogBlock(functionId, message: null, entityForMessage, eventLevel, blockId, cancellationToken);
                logBlock.OnStart();
                return logBlock;
            }

            public static LogBlock Create(
                FunctionId functionId,
                string message,
                EventLevel eventLevel,
                CancellationToken cancellationToken)
            {
                var blockId = GetNextUniqueBlockId();
                var logBlock = new LogBlock(functionId, message, entityForMessage: null, eventLevel, blockId, cancellationToken);
                logBlock.OnStart();
                return logBlock;
            }

            /// <summary>
            /// return next unique pair id
            /// </summary>
            private static int GetNextUniqueBlockId()
            {
                return Interlocked.Increment(ref s_lastUniqueBlockId);
            }

            private void OnStart()
            {
                if (EnsureMessageIfLoggingEnabled())
                {
                    Debug.Assert(_message != null);
                    Debug.Assert(!_startLogged);

                    Instance.BlockStart(_message, _functionId, _blockId);
                    _startLogged = true;
                }
            }

            private bool EnsureMessageIfLoggingEnabled()
            {
                if (Instance.IsEnabled(_eventLevel, EventKeywords.None))
                {
                    _message ??= (_entityForMessage?.ToString() ?? string.Empty);
                    return true;
                }

                return false;
            }

            public void Dispose()
            {
                if (!EnsureMessageIfLoggingEnabled())
                {
                    return;
                }

                Debug.Assert(_message != null);

                if (!_startLogged)
                {
                    // User enabled logging after the block start.
                    // We log a block start to log the message along with the block ID.
                    Instance.BlockStart(_message, _functionId, _blockId);
                    _startLogged = true;
                }

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
            }
        }
    }
}
