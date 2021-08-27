﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

#if !CODE_STYLE
using System.Linq;
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// provide a way to log activities to various back end such as etl, code marker and etc
    /// </summary>
    internal static partial class Logger
    {
        private static ILogger? s_currentLogger;

        /// <summary>
        /// next unique block id that will be given to each LogBlock
        /// </summary>
        private static int s_lastUniqueBlockId;

        /// <summary>
        /// give a way to explicitly set/replace the logger
        /// </summary>
        public static ILogger? SetLogger(ILogger logger)
        {
            // we don't care what was there already, just replace it explicitly
            return Interlocked.Exchange(ref Logger.s_currentLogger, logger);
        }

        /// <summary>
        /// ensure we have a logger by putting one from workspace service if one is not there already.
        /// </summary>
        public static ILogger? GetLogger()
            => Logger.s_currentLogger;

        /// <summary>
        /// log a specific event with a simple context message which should be very cheap to create
        /// </summary>
        public static void Log(FunctionId functionId, string? message = null, LogLevel logLevel = LogLevel.Debug)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return;
            }

            if (!logger.IsEnabled(functionId))
            {
                return;
            }

            logger.Log(functionId, LogMessage.Create(message ?? "", logLevel: logLevel));
        }

        /// <summary>
        /// log a specific event with a context message that will only be created when it is needed.
        /// the messageGetter should be cheap to create. in another word, it shouldn't capture any locals
        /// </summary>
        public static void Log(FunctionId functionId, Func<string> messageGetter, LogLevel logLevel = LogLevel.Debug)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return;
            }

            if (!logger.IsEnabled(functionId))
            {
                return;
            }

            var logMessage = LogMessage.Create(messageGetter, logLevel);
            logger.Log(functionId, logMessage);

            logMessage.Free();
        }

        /// <summary>
        /// log a specific event with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static void Log<TArg>(FunctionId functionId, Func<TArg, string> messageGetter, TArg arg, LogLevel logLevel = LogLevel.Debug)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return;
            }

            if (!logger.IsEnabled(functionId))
            {
                return;
            }

            var logMessage = LogMessage.Create(messageGetter, arg, logLevel);
            logger.Log(functionId, logMessage);
            logMessage.Free();
        }

        /// <summary>
        /// log a specific event with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static void Log<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, LogLevel logLevel = LogLevel.Debug)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return;
            }

            if (!logger.IsEnabled(functionId))
            {
                return;
            }

            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, logLevel);
            logger.Log(functionId, logMessage);
            logMessage.Free();
        }

        /// <summary>
        /// log a specific event with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static void Log<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, LogLevel logLevel = LogLevel.Debug)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return;
            }

            if (!logger.IsEnabled(functionId))
            {
                return;
            }

            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, arg2, logLevel);
            logger.Log(functionId, logMessage);
            logMessage.Free();
        }

        /// <summary>
        /// log a specific event with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static void Log<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, LogLevel logLevel = LogLevel.Debug)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return;
            }

            if (!logger.IsEnabled(functionId))
            {
                return;
            }

            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, arg2, arg3, logLevel);
            logger.Log(functionId, logMessage);
            logMessage.Free();
        }

        /// <summary>
        /// log a specific event with a context message.
        /// </summary>
        public static void Log(FunctionId functionId, LogMessage logMessage)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return;
            }

            if (!logger.IsEnabled(functionId))
            {
                return;
            }

            logger.Log(functionId, logMessage);
            logMessage.Free();
        }

        /// <summary>
        /// return next unique pair id
        /// </summary>
        private static int GetNextUniqueBlockId()
            => Interlocked.Increment(ref s_lastUniqueBlockId);

        /// <summary>
        /// simplest way to log a start and end pair
        /// </summary>
        public static IDisposable LogBlock(FunctionId functionId, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
            => LogBlock(functionId, string.Empty, token, logLevel);

        /// <summary>
        /// simplest way to log a start and end pair with a simple context message which should be very cheap to create
        /// </summary>
        public static IDisposable LogBlock(FunctionId functionId, string? message, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return EmptyLogBlock.Instance;
            }

            if (!logger.IsEnabled(functionId))
            {
                return EmptyLogBlock.Instance;
            }

            return CreateLogBlock(functionId, LogMessage.Create(message ?? "", logLevel), GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message that will only be created when it is needed.
        /// the messageGetter should be cheap to create. in another word, it shouldn't capture any locals
        /// </summary>
        public static IDisposable LogBlock(FunctionId functionId, Func<string> messageGetter, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return EmptyLogBlock.Instance;
            }

            if (!logger.IsEnabled(functionId))
            {
                return EmptyLogBlock.Instance;
            }

            return CreateLogBlock(functionId, LogMessage.Create(messageGetter, logLevel), GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static IDisposable LogBlock<TArg>(FunctionId functionId, Func<TArg, string> messageGetter, TArg arg, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return EmptyLogBlock.Instance;
            }

            if (!logger.IsEnabled(functionId))
            {
                return EmptyLogBlock.Instance;
            }

            return CreateLogBlock(functionId, LogMessage.Create(messageGetter, arg, logLevel), GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static IDisposable LogBlock<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return EmptyLogBlock.Instance;
            }

            if (!logger.IsEnabled(functionId))
            {
                return EmptyLogBlock.Instance;
            }

            return CreateLogBlock(functionId, LogMessage.Create(messageGetter, arg0, arg1, logLevel), GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static IDisposable LogBlock<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return EmptyLogBlock.Instance;
            }

            if (!logger.IsEnabled(functionId))
            {
                return EmptyLogBlock.Instance;
            }

            return CreateLogBlock(functionId, LogMessage.Create(messageGetter, arg0, arg1, arg2, logLevel), GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static IDisposable LogBlock<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return EmptyLogBlock.Instance;
            }

            if (!logger.IsEnabled(functionId))
            {
                return EmptyLogBlock.Instance;
            }

            return CreateLogBlock(functionId, LogMessage.Create(messageGetter, arg0, arg1, arg2, arg3, logLevel), GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message.
        /// </summary>
        public static IDisposable LogBlock(FunctionId functionId, LogMessage logMessage, CancellationToken token)
        {
            var logger = GetLogger();
            if (logger == null)
            {
                return EmptyLogBlock.Instance;
            }

            if (!logger.IsEnabled(functionId))
            {
                return EmptyLogBlock.Instance;
            }

            return CreateLogBlock(functionId, logMessage, GetNextUniqueBlockId(), token);
        }

#if !CODE_STYLE
        public static Func<FunctionId, bool> GetLoggingChecker(IGlobalOptionService optionService)
        {
            var functionIds = Enum.GetValues(typeof(FunctionId)).Cast<FunctionId>();
            var functionIdOptions = functionIds.ToDictionary(id => id, id => optionService.GetOption(FunctionIdOptions.GetOption(id)));

            return functionId => functionIdOptions[functionId];
        }
#endif
    }
}
