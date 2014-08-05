// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// provide a way to log activities to various back end such as etl, code marker and etc
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class Logger
    {
        private static ILogger currentLogger = null;

        /// <summary>
        /// next unique block id that will be given to each LogBlock
        /// </summary>
        private static int lastUniqueBlockId = 0;

        /// <summary>
        /// give a way to explicitly set/replace the logger
        /// </summary>
        public static ILogger SetLogger(ILogger logger)
        {
            // we don't care what was there already, just replace it explicitly
            return Interlocked.Exchange(ref Logger.currentLogger, logger);
        }

        /// <summary>
        /// ensure we have a logger by putting one from workspace service if one is not there already.
        /// </summary>
        private static ILogger GetLogger()
        {
            return Logger.currentLogger;
        }

        /// <summary>
        /// log a specific event with a simple context message which should be very cheap to create
        /// </summary>
        public static void Log(FunctionId functionId, string message = null)
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

            message = logger.IsVerbose() ? message : string.Empty;
            logger.Log(functionId, message ?? string.Empty);
        }

        /// <summary>
        /// log a specific event with a context message that will only be created when it is needed.
        /// the messageGetter should be cheap to create. in another word, it shouldn't capture any locals
        /// </summary>
        public static void Log(FunctionId functionId, Func<string> messageGetter)
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

            var message = logger.IsVerbose() ? messageGetter() : string.Empty;
            logger.Log(functionId, message);
        }

        /// <summary>
        /// log a specific event with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static void Log<TArg>(FunctionId functionId, Func<TArg, string> messageGetter, TArg arg)
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

            var message = logger.IsVerbose() ? messageGetter(arg) : string.Empty;
            logger.Log(functionId, message);
        }

        /// <summary>
        /// log a specific event with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static void Log<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1)
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

            var message = logger.IsVerbose() ? messageGetter(arg0, arg1) : string.Empty;
            logger.Log(functionId, message);
        }

        /// <summary>
        /// log a specific event with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static void Log<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2)
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

            var message = logger.IsVerbose() ? messageGetter(arg0, arg1, arg2) : string.Empty;
            logger.Log(functionId, message);
        }

        /// <summary>
        /// log a specific event with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static void Log<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
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

            var message = logger.IsVerbose() ? messageGetter(arg0, arg1, arg2, arg3) : string.Empty;
            logger.Log(functionId, message);
        }

        /// <summary>
        /// return next unique pair id
        /// </summary>
        private static int GetNextUniqueBlockId()
        {
            return Interlocked.Increment(ref lastUniqueBlockId);
        }

        /// <summary>
        /// simplest way to log a start and end pair
        /// </summary>
        public static IDisposable LogBlock(FunctionId functionId, CancellationToken token)
        {
            return LogBlock(functionId, string.Empty, token);
        }

        /// <summary>
        /// simplest way to log a start and end pair with a simple context message which should be very cheap to create
        /// </summary>
        public static IDisposable LogBlock(FunctionId functionId, string message, CancellationToken token)
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

            message = logger.IsVerbose() ? message : string.Empty;
            return logger.LogBlock(functionId, message ?? string.Empty, GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message that will only be created when it is needed.
        /// the messageGetter should be cheap to create. in another word, it shouldn't capture any locals
        /// </summary>
        public static IDisposable LogBlock(FunctionId functionId, Func<string> messageGetter, CancellationToken token)
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

            var message = logger.IsVerbose() ? messageGetter() : string.Empty;
            return logger.LogBlock(functionId, message, GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static IDisposable LogBlock<TArg>(FunctionId functionId, Func<TArg, string> messageGetter, TArg arg, CancellationToken token)
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

            var message = logger.IsVerbose() ? messageGetter(arg) : string.Empty;
            return logger.LogBlock(functionId, message, GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static IDisposable LogBlock<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, CancellationToken token)
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

            var message = logger.IsVerbose() ? messageGetter(arg0, arg1) : string.Empty;
            return logger.LogBlock(functionId, message, GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static IDisposable LogBlock<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, CancellationToken token)
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

            var message = logger.IsVerbose() ? messageGetter(arg0, arg1, arg2) : string.Empty;
            return logger.LogBlock(functionId, message, GetNextUniqueBlockId(), token);
        }

        /// <summary>
        /// log a start and end pair with a context message that requires some arguments to be created when requested.
        /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
        /// </summary>
        public static IDisposable LogBlock<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token)
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

            var message = logger.IsVerbose() ? messageGetter(arg0, arg1, arg2, arg3) : string.Empty;
            return logger.LogBlock(functionId, message, GetNextUniqueBlockId(), token);
        }

        public static Func<FunctionId, bool> GetLoggingChecker(IOptionService optionService)
        {
            var functionIds = Enum.GetValues(typeof(FunctionId)).Cast<FunctionId>();
            var functionIdOptions = functionIds.ToDictionary(id => id, id => optionService.GetOption(FunctionIdOptions.GetOption(id)));

            return (functionId) => functionIdOptions[functionId];
        }
    }
}
