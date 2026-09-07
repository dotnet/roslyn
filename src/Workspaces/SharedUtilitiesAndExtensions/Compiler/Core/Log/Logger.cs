// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Forwarding shim onto <see cref="RoslynTelemetry"/>.
/// </summary>
internal static class Logger
{
    /// <inheritdoc cref="RoslynTelemetry.Log(FunctionId, string, LogLevel)"/>
    public static void Log(FunctionId functionId, string? message = null, LogLevel logLevel = LogLevel.Debug)
        => RoslynTelemetry.Log(functionId, message, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.Log(FunctionId, Func{string}, LogLevel)"/>
    public static void Log(FunctionId functionId, Func<string> messageGetter, LogLevel logLevel = LogLevel.Debug)
        => RoslynTelemetry.Log(functionId, messageGetter, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.Log{TArg}(FunctionId, Func{TArg, string}, TArg, LogLevel)"/>
    public static void Log<TArg>(FunctionId functionId, Func<TArg, string> messageGetter, TArg arg, LogLevel logLevel = LogLevel.Debug)
        => RoslynTelemetry.Log(functionId, messageGetter, arg, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.Log{TArg0, TArg1}(FunctionId, Func{TArg0, TArg1, string}, TArg0, TArg1, LogLevel)"/>
    public static void Log<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, LogLevel logLevel = LogLevel.Debug)
        => RoslynTelemetry.Log(functionId, messageGetter, arg0, arg1, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.Log{TArg0, TArg1, TArg2}(FunctionId, Func{TArg0, TArg1, TArg2, string}, TArg0, TArg1, TArg2, LogLevel)"/>
    public static void Log<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, LogLevel logLevel = LogLevel.Debug)
        => RoslynTelemetry.Log(functionId, messageGetter, arg0, arg1, arg2, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.Log{TArg0, TArg1, TArg2, TArg3}(FunctionId, Func{TArg0, TArg1, TArg2, TArg3, string}, TArg0, TArg1, TArg2, TArg3, LogLevel)"/>
    public static void Log<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, LogLevel logLevel = LogLevel.Debug)
        => RoslynTelemetry.Log(functionId, messageGetter, arg0, arg1, arg2, arg3, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.Log(FunctionId, LogMessage)"/>
    public static void Log(FunctionId functionId, LogMessage logMessage)
        => RoslynTelemetry.Log(functionId, logMessage);

    /// <inheritdoc cref="RoslynTelemetry.LogBlock(FunctionId, CancellationToken, LogLevel)"/>
    public static IDisposable LogBlock(FunctionId functionId, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => RoslynTelemetry.LogBlock(functionId, token, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.LogBlock(FunctionId, string, CancellationToken, LogLevel)"/>
    public static IDisposable LogBlock(FunctionId functionId, string? message, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => RoslynTelemetry.LogBlock(functionId, message, token, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.LogBlock(FunctionId, Func{string}, CancellationToken, LogLevel)"/>
    public static IDisposable LogBlock(FunctionId functionId, Func<string> messageGetter, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => RoslynTelemetry.LogBlock(functionId, messageGetter, token, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.LogBlock{TArg}(FunctionId, Func{TArg, string}, TArg, CancellationToken, LogLevel)"/>
    public static IDisposable LogBlock<TArg>(FunctionId functionId, Func<TArg, string> messageGetter, TArg arg, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => RoslynTelemetry.LogBlock(functionId, messageGetter, arg, token, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.LogBlock{TArg0, TArg1}(FunctionId, Func{TArg0, TArg1, string}, TArg0, TArg1, CancellationToken, LogLevel)"/>
    public static IDisposable LogBlock<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => RoslynTelemetry.LogBlock(functionId, messageGetter, arg0, arg1, token, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.LogBlock{TArg0, TArg1, TArg2}(FunctionId, Func{TArg0, TArg1, TArg2, string}, TArg0, TArg1, TArg2, CancellationToken, LogLevel)"/>
    public static IDisposable LogBlock<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => RoslynTelemetry.LogBlock(functionId, messageGetter, arg0, arg1, arg2, token, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.LogBlock{TArg0, TArg1, TArg2, TArg3}(FunctionId, Func{TArg0, TArg1, TArg2, TArg3, string}, TArg0, TArg1, TArg2, TArg3, CancellationToken, LogLevel)"/>
    public static IDisposable LogBlock<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => RoslynTelemetry.LogBlock(functionId, messageGetter, arg0, arg1, arg2, arg3, token, logLevel);

    /// <inheritdoc cref="RoslynTelemetry.LogBlock(FunctionId, LogMessage, CancellationToken)"/>
    public static IDisposable LogBlock(FunctionId functionId, LogMessage logMessage, CancellationToken token)
        => RoslynTelemetry.LogBlock(functionId, logMessage, token);
}
