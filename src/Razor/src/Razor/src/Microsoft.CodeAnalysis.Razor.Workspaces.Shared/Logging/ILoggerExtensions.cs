// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal static class ILoggerExtensions
{
    public static void Log(this ILogger logger, LogLevel logLevel, [InterpolatedStringHandlerArgument(nameof(logger), "logLevel")] ref LogMessageInterpolatedStringHandler message)
    {
        if (message.IsEnabled)
        {
            logger.Log(logLevel, message.ToString(), exception: null);
        }
    }

    public static void Log(this ILogger logger, LogLevel logLevel, string message)
    {
        if (logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, message, exception: null);
        }
    }

    public static void LogTrace(this ILogger logger, [InterpolatedStringHandlerArgument(nameof(logger))] ref TraceLogMessageInterpolatedStringHandler message)
    {
        if (message.IsEnabled)
        {
            logger.Log(LogLevel.Trace, message.ToString(), exception: null);
        }
    }

    public static void LogTrace(this ILogger logger, string message)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.Log(LogLevel.Trace, message);
        }
    }

    public static void LogDebug(this ILogger logger, [InterpolatedStringHandlerArgument(nameof(logger))] ref DebugLogMessageInterpolatedStringHandler message)
    {
        if (message.IsEnabled)
        {
            logger.Log(LogLevel.Debug, message.ToString(), exception: null);
        }
    }

    public static void LogDebug(this ILogger logger, string message)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.Log(LogLevel.Debug, message);
        }
    }

    public static void LogInformation(this ILogger logger, [InterpolatedStringHandlerArgument(nameof(logger))] ref InformationLogMessageInterpolatedStringHandler message)
    {
        if (message.IsEnabled)
        {
            logger.Log(LogLevel.Information, message.ToString(), exception: null);
        }
    }

    public static void LogInformation(this ILogger logger, string message)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.Log(LogLevel.Information, message);
        }
    }

    public static void LogWarning(this ILogger logger, [InterpolatedStringHandlerArgument(nameof(logger))] ref WarningLogMessageInterpolatedStringHandler message)
    {
        if (message.IsEnabled)
        {
            logger.Log(LogLevel.Warning, message.ToString(), exception: null);
        }
    }

    public static void LogWarning(this ILogger logger, Exception exception, [InterpolatedStringHandlerArgument(nameof(logger))] ref WarningLogMessageInterpolatedStringHandler message)
    {
        if (message.IsEnabled)
        {
            logger.Log(LogLevel.Warning, message.ToString(), exception);
        }
    }

    public static void LogWarning(this ILogger logger, Exception exception, string message)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(LogLevel.Warning, message, exception);
        }
    }

    public static void LogWarning(this ILogger logger, string message)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(LogLevel.Warning, message);
        }
    }

    public static void LogError(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, exception.Message, exception);
        }
    }

    public static void LogError(this ILogger logger, Exception exception, [InterpolatedStringHandlerArgument(nameof(logger))] ref ErrorLogMessageInterpolatedStringHandler message)
    {
        if (message.IsEnabled)
        {
            logger.Log(LogLevel.Error, message.ToString(), exception);
        }
    }

    public static void LogError(this ILogger logger, [InterpolatedStringHandlerArgument(nameof(logger))] ref ErrorLogMessageInterpolatedStringHandler message)
    {
        if (message.IsEnabled)
        {
            logger.Log(LogLevel.Error, message.ToString(), exception: null);
        }
    }

    public static void LogError(this ILogger logger, Exception exception, string message)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, message, exception);
        }
    }

    public static void LogError(this ILogger logger, string message)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, message);
        }
    }

    public static void LogCritical(this ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Critical))
        {
            logger.Log(LogLevel.Critical, exception.Message, exception);
        }
    }

    public static void LogCritical(this ILogger logger, [InterpolatedStringHandlerArgument(nameof(logger))] ref CriticalLogMessageInterpolatedStringHandler message)
    {
        if (message.IsEnabled)
        {
            logger.Log(LogLevel.Critical, message.ToString(), exception: null);
        }
    }

    public static void LogCritical(this ILogger logger, Exception exception, [InterpolatedStringHandlerArgument(nameof(logger))] ref CriticalLogMessageInterpolatedStringHandler message)
    {
        if (message.IsEnabled)
        {
            logger.Log(LogLevel.Critical, message.ToString(), exception);
        }
    }

    public static void LogCritical(this ILogger logger, Exception exception, string message)
    {
        if (logger.IsEnabled(LogLevel.Critical))
        {
            logger.Log(LogLevel.Critical, message, exception);
        }
    }

    public static void LogCritical(this ILogger logger, string message)
    {
        if (logger.IsEnabled(LogLevel.Critical))
        {
            logger.Log(LogLevel.Critical, message);
        }
    }
}
