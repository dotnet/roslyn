// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

#if !CODE_STYLE
using System.Linq;
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Internal.Log;

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
    public static ILogger? SetLogger(ILogger? logger)
    {
        // we don't care what was there already, just replace it explicitly
        return Interlocked.Exchange(ref s_currentLogger, logger);
    }

    /// <summary>
    /// ensure we have a logger by putting one from workspace service if one is not there already.
    /// </summary>
    public static ILogger? GetLogger()
        => s_currentLogger;

    private static bool TryGetActiveLogger(FunctionId functionId, [NotNullWhen(true)] out ILogger? activeLogger)
    {
        var logger = s_currentLogger;
        if (logger == null || !logger.IsEnabled(functionId))
        {
            activeLogger = null;
            return false;
        }

        activeLogger = logger;
        return true;
    }

    /// <summary>
    /// log a specific event with a simple context message which should be very cheap to create
    /// </summary>
    public static void Log(FunctionId functionId, string? message = null, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveLogger(functionId, out var logger))
        {
            logger.Log(functionId, LogMessage.Create(message ?? "", logLevel: logLevel));
        }
    }

    /// <summary>
    /// log a specific event with a context message that will only be created when it is needed.
    /// the messageGetter should be cheap to create. in another word, it shouldn't capture any locals
    /// </summary>
    public static void Log(FunctionId functionId, Func<string> messageGetter, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveLogger(functionId, out var logger))
        {
            var logMessage = LogMessage.Create(messageGetter, logLevel);
            logger.Log(functionId, logMessage);

            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg>(FunctionId functionId, TArg arg, Func<TArg, string> messageGetter, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveLogger(functionId, out var logger))
        {
            var logMessage = LogMessage.Create(messageGetter: messageGetter, arg: arg, logLevel: logLevel);
            logger.Log(functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveLogger(functionId, out var logger))
        {
            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, logLevel);
            logger.Log(functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveLogger(functionId, out var logger))
        {
            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, arg2, logLevel);
            logger.Log(functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveLogger(functionId, out var logger))
        {
            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, arg2, arg3, logLevel);
            logger.Log(functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message.
    /// </summary>
    public static void Log(FunctionId functionId, LogMessage logMessage)
    {
        if (TryGetActiveLogger(functionId, out var logger))
        {
            logger.Log(functionId, logMessage);
            logMessage.Free();
        }
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
        => TryGetActiveLogger(functionId, out _)
            ? CreateLogBlock(functionId, LogMessage.Create(message ?? "", logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that will only be created when it is needed.
    /// the messageGetter should be cheap to create. in another word, it shouldn't capture any locals
    /// </summary>
    public static IDisposable LogBlock(FunctionId functionId, Func<string> messageGetter, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetActiveLogger(functionId, out _)
            ? CreateLogBlock(functionId, LogMessage.Create(messageGetter, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg>(FunctionId functionId, TArg arg, Func<TArg, string> messageGetter, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetActiveLogger(functionId, out _)
            ? CreateLogBlock(functionId, LogMessage.Create(messageGetter: messageGetter, arg: arg, logLevel: logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetActiveLogger(functionId, out _)
            ? CreateLogBlock(functionId, LogMessage.Create(messageGetter, arg0, arg1, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetActiveLogger(functionId, out _)
            ? CreateLogBlock(functionId, LogMessage.Create(messageGetter, arg0, arg1, arg2, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetActiveLogger(functionId, out _)
            ? CreateLogBlock(functionId, LogMessage.Create(messageGetter, arg0, arg1, arg2, arg3, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message.
    /// </summary>
    public static IDisposable LogBlock(FunctionId functionId, LogMessage logMessage, CancellationToken token)
        => TryGetActiveLogger(functionId, out _)
            ? CreateLogBlock(functionId, logMessage, GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;
}
