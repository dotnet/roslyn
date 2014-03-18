// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Microsoft.CodeAnalysis.Instrumentation
{
    /// <summary>
    /// Logger class for logging ETW events.
    /// </summary>
    internal static class Logger
    {
        /// <summary>
        /// A logger that logs 'start' and 'end' messages for a supplied event along with a number and a string.
        /// </summary>
        internal struct Block : IDisposable
        {
            private readonly FunctionId functionId;
            private readonly int number;
            private readonly int blockId;
            private readonly CancellationToken cancellationToken;

            /// <summary>
            /// Returns a logger that logs 'start' and 'end' messages for a supplied event along with a number and a string.
            /// The combination of the 'start' and the 'end' message is referred to as a 'block' and each block has a unique 'blockId'.
            /// </summary>
            /// <param name="functionId">A number that uniquely identifies the event being logged.</param>
            /// <param name="number">A number that is logged as part of the 'end' message.</param>
            /// <param name="message">A string that is logged as part of the 'start' message.</param>
            /// <param name="blockId">A number that uniquely identifies the 'start' and 'end' messages for a particular occurrence of the event in the log.
            /// In other words, each occurrence of the event will result in 'start' and 'end' messages that share the same unique blockId in the log.</param>
            /// <param name="cancellationToken">A cancellation token - If the operation was cancelled then a 'cancelled' message is logged in place of a 'end' meessage for the event.</param>
            internal Block(FunctionId functionId, int number, string message, int blockId, CancellationToken cancellationToken)
            {
                Debug.Assert(functionId > 0);
                this.functionId = functionId;
                this.number = number;
                this.blockId = blockId;
                this.cancellationToken = cancellationToken;
                RoslynCompilerEventSource.Instance.BlockStart(message, functionId, blockId);
            }

            public void Dispose()
            {
                // default logger
                if (functionId == 0)
                {
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    RoslynCompilerEventSource.Instance.BlockCanceled(this.functionId, this.number, this.blockId);
                }
                else
                {
                    RoslynCompilerEventSource.Instance.BlockStop(this.functionId, this.number, this.blockId);
                }
            }
        }

        private static int lastUniqueBlockId = 0;
        private static int GetNextUniqueBlockId()
        {
            return Interlocked.Increment(ref lastUniqueBlockId);
        }

        private static bool IsEnabled(FunctionId functionId)
        {
            // TODO: For now, logging is either on for all events (FunctionIds) or off for all events.
            // We don't support selectively toggling logging for specific events.
            // In the IDE, decisions regarding which events to log or to not log
            // are left to the host (i.e. ETA / VisualStudio which may have settings dialogs
            // to toggle logging for specific events). Compiler could piggy-back on this design
            // (which should work great in scenarios where compiler APIs are consumed from within
            // the IDE) or come up with some more general mechanism for controlling this 
            // (such as using registry settings so that we can use this for events fired from within
            // other non-IDE consumers as well).
            return RoslynCompilerEventSource.Instance.IsEnabled();
        }

        private static bool IsVerbose()
        {
            return RoslynCompilerEventSource.Instance.IsEnabled(EventLevel.Verbose, ((EventKeywords)(-1)));
        }

        /// <summary>
        /// Log an event with an optional simple message string.
        /// </summary>
        public static void LogString(FunctionId functionId, string message = null)
        {
            if (!IsEnabled(functionId))
            {
                return;
            }

            message = IsVerbose() ? message : string.Empty;
            RoslynCompilerEventSource.Instance.LogString(message ?? string.Empty, functionId);
        }

        /// <summary>
        /// Log an event with a message string that will only be created when it is needed.
        /// </summary>
        public static void LogString(FunctionId functionId, Func<string> messageGetter)
        {
            if (!IsEnabled(functionId))
            {
                return;
            }

            var message = IsVerbose() ? messageGetter() : string.Empty;
            RoslynCompilerEventSource.Instance.LogString(message, functionId);
        }

        /// <summary>
        /// Log an event with a message string that will only be created when it is needed.
        /// </summary>
        public static void LogString<TArg0>(FunctionId functionId, Func<TArg0, string> messageGetter, TArg0 arg0)
        {
            if (!IsEnabled(functionId))
            {
                return;
            }

            var message = IsVerbose() ? messageGetter(arg0) : string.Empty;
            RoslynCompilerEventSource.Instance.LogString(message, functionId);
        }

        /// <summary>
        /// Log an event with a message string that will only be created when it is needed.
        /// </summary>
        public static void LogString<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1)
        {
            if (!IsEnabled(functionId))
            {
                return;
            }

            var message = IsVerbose() ? messageGetter(arg0, arg1) : string.Empty;
            RoslynCompilerEventSource.Instance.LogString(message, functionId);
        }


        /// <summary>
        /// Log an event with a message string that will only be created when it is needed.
        /// </summary>
        public static void LogString<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            if (!IsEnabled(functionId))
            {
                return;
            }

            var message = IsVerbose() ? messageGetter(arg0, arg1, arg2) : string.Empty;
            RoslynCompilerEventSource.Instance.LogString(message, functionId);
        }

        /// <summary>
        /// Log an event with a message string that will only be created when it is needed.
        /// </summary>
        public static void LogString<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            if (!IsEnabled(functionId))
            {
                return;
            }

            var message = IsVerbose() ? messageGetter(arg0, arg1, arg2, arg3) : string.Empty;
            RoslynCompilerEventSource.Instance.LogString(message, functionId);
        }

        /// <summary>
        /// Log an event with a 'start' and an 'end' component containing an optional number and an optional message string.
        /// </summary>
        public static Block LogBlock(FunctionId functionId, string message = null, int number = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsEnabled(functionId))
            {
                return default(Block);
            }

            message = IsVerbose() ? message : string.Empty;
            return new Block(functionId, number, message ?? string.Empty, GetNextUniqueBlockId(), cancellationToken);
        }

        /// <summary>
        /// Log an event with a 'start' and an 'end' component containing an optional number and a message string that will only be created when it is needed.
        /// </summary>
        public static Block LogBlock(FunctionId functionId, Func<string> messageGetter, int number = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsEnabled(functionId))
            {
                return default(Block);
            }

            var message = IsVerbose() ? messageGetter() : string.Empty;
            return new Block(functionId, number, message, GetNextUniqueBlockId(), cancellationToken);
        }

        /// <summary>
        /// Log an event with a 'start' and an 'end' component containing an optional number and a message string that will only be created when it is needed.
        /// </summary>
        public static Block LogBlock<TArg0>(FunctionId functionId, Func<TArg0, string> messageGetter, TArg0 arg0, int number = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsEnabled(functionId))
            {
                return default(Block);
            }

            var message = IsVerbose() ? messageGetter(arg0) : string.Empty;
            return new Block(functionId, number, message, GetNextUniqueBlockId(), cancellationToken);
        }

        /// <summary>
        /// Log an event with a 'start' and an 'end' component containing an optional number and a message string that will only be created when it is needed.
        /// </summary>
        public static Block LogBlock<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, int number = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsEnabled(functionId))
            {
                return default(Block);
            }

            var message = IsVerbose() ? messageGetter(arg0, arg1) : string.Empty;
            return new Block(functionId, number, message, GetNextUniqueBlockId(), cancellationToken);
        }

        /// <summary>
        /// Log an event with a 'start' and an 'end' component containing an optional number and a message string that will only be created when it is needed.
        /// </summary>
        public static Block LogBlock<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, int number = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsEnabled(functionId))
            {
                return default(Block);
            }

            var message = IsVerbose() ? messageGetter(arg0, arg1, arg2) : string.Empty;
            return new Block(functionId, number, message, GetNextUniqueBlockId(), cancellationToken);
        }

        /// <summary>
        /// Log an event with a 'start' and an 'end' component containing an optional number and a message string that will only be created when it is needed.
        /// </summary>
        public static Block LogBlock<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, int number = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsEnabled(functionId))
            {
                return default(Block);
            }

            var message = IsVerbose() ? messageGetter(arg0, arg1, arg2, arg3) : string.Empty;
            return new Block(functionId, number, message, GetNextUniqueBlockId(), cancellationToken);
        }
    }
}