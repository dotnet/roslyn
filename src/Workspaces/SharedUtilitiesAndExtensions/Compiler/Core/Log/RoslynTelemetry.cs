// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Roslyn's telemetry entry point. Discrete events and scopes are recorded here and fan out to the
/// host's configured <see cref="IEventSink"/>; aggregated measurements go to its <see cref="IMetricSink"/>.
/// <para>
/// A host configures this once at startup (see <see cref="SetEventSink"/> / <see cref="SetMetricSink"/>).
/// With nothing configured every method is a cheap no-op, which is what the build server, the CodeStyle
/// packages, and most tests rely on.
/// </para>
/// </summary>
internal static partial class RoslynTelemetry
{
    private static IEventSink? s_currentEventSink;

    /// <summary>
    /// next unique block id that will be given to each LogBlock
    /// </summary>
    private static int s_lastUniqueBlockId;

    /// <summary>
    /// Replaces the active event sink. Hosts call this once during startup; tests reset it to
    /// <see langword="null"/> during teardown.
    /// </summary>
    public static IEventSink? SetEventSink(IEventSink? sink)
    {
        // we don't care what was there already, just replace it explicitly
        return Interlocked.Exchange(ref s_currentEventSink, sink);
    }

    public static IEventSink? GetEventSink()
        => s_currentEventSink;

    /// <summary>
    /// Atomically adds <paramref name="sink"/> alongside whatever is already registered. Used by
    /// diagnostic sinks that live in assemblies the composition root cannot reference (the diagnostics
    /// tool window, integration tests), which attach once and are thereafter controlled by their own
    /// <see cref="IEventSink.IsEnabled"/> rather than by being detached.
    /// </summary>
    public static void AddEventSink(IEventSink sink)
    {
        while (true)
        {
            var existing = s_currentEventSink;
            var combined = existing is null ? sink : AggregateEventSink.Create(existing, sink);
            if (Interlocked.CompareExchange(ref s_currentEventSink, combined, existing) == existing)
                return;
        }
    }

    private static bool TryGetActiveEventSink(FunctionId functionId, [NotNullWhen(true)] out IEventSink? activeSink)
    {
        var sink = s_currentEventSink;
        if (sink == null || !sink.IsEnabled(functionId))
        {
            activeSink = null;
            return false;
        }

        activeSink = sink;
        return true;
    }


    /// <summary>
    /// log a specific event with a simple context message which should be very cheap to create
    /// </summary>
    public static void Log(FunctionId functionId, string? message = null, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveEventSink(functionId, out var sink))
        {
            sink.Log(functionId, LogMessage.Create(message ?? "", logLevel: logLevel));
        }
    }

    /// <summary>
    /// log a specific event with a context message that will only be created when it is needed.
    /// the messageGetter should be cheap to create. in another word, it shouldn't capture any locals
    /// </summary>
    public static void Log(FunctionId functionId, Func<string> messageGetter, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveEventSink(functionId, out var sink))
        {
            var logMessage = LogMessage.Create(messageGetter, logLevel);
            sink.Log(functionId, logMessage);

            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg>(FunctionId functionId, Func<TArg, string> messageGetter, TArg arg, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveEventSink(functionId, out var sink))
        {
            var logMessage = LogMessage.Create(messageGetter, arg, logLevel);
            sink.Log(functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveEventSink(functionId, out var sink))
        {
            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, logLevel);
            sink.Log(functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveEventSink(functionId, out var sink))
        {
            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, arg2, logLevel);
            sink.Log(functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetActiveEventSink(functionId, out var sink))
        {
            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, arg2, arg3, logLevel);
            sink.Log(functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message.
    /// </summary>
    public static void Log(FunctionId functionId, LogMessage logMessage)
    {
        if (TryGetActiveEventSink(functionId, out var sink))
        {
            sink.Log(functionId, logMessage);
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
        => TryGetActiveEventSink(functionId, out var sink)
            ? CreateLogBlock(sink, functionId, LogMessage.Create(message ?? "", logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that will only be created when it is needed.
    /// the messageGetter should be cheap to create. in another word, it shouldn't capture any locals
    /// </summary>
    public static IDisposable LogBlock(FunctionId functionId, Func<string> messageGetter, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetActiveEventSink(functionId, out var sink)
            ? CreateLogBlock(sink, functionId, LogMessage.Create(messageGetter, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg>(FunctionId functionId, Func<TArg, string> messageGetter, TArg arg, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetActiveEventSink(functionId, out var sink)
            ? CreateLogBlock(sink, functionId, LogMessage.Create(messageGetter, arg, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetActiveEventSink(functionId, out var sink)
            ? CreateLogBlock(sink, functionId, LogMessage.Create(messageGetter, arg0, arg1, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetActiveEventSink(functionId, out var sink)
            ? CreateLogBlock(sink, functionId, LogMessage.Create(messageGetter, arg0, arg1, arg2, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetActiveEventSink(functionId, out var sink)
            ? CreateLogBlock(sink, functionId, LogMessage.Create(messageGetter, arg0, arg1, arg2, arg3, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message.
    /// </summary>
    public static IDisposable LogBlock(FunctionId functionId, LogMessage logMessage, CancellationToken token)
        => TryGetActiveEventSink(functionId, out var sink)
            ? CreateLogBlock(sink, functionId, logMessage, GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;
}
