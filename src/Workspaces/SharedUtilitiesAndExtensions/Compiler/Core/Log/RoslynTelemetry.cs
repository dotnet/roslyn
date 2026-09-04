// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Telemetry entry point.  Events / metrics are recorded here and fan out to the respective
/// <see cref="IEventSink"/> or <see cref="IMetricSink"/>.  When no sinks are registered calls are
/// cheap no-ops.
/// </summary>
internal static partial class RoslynTelemetry
{
    /// <summary>
    /// The registered <see cref="IEventSink"/> each event fans out to.
    /// </summary>
    private static ImmutableArray<IEventSink> s_eventSinks = [];

    /// <summary>
    /// next unique block id that will be given to each LogBlock
    /// </summary>
    private static int s_lastUniqueBlockId;

    /// <summary>
    /// Registers <paramref name="sink"/> to receive events. A sink instance may have only one active
    /// registration. Dispose the result to unregister it; a host that keeps its sinks for the life of
    /// the process can simply never dispose.
    /// </summary>
    public static IDisposable AddEventSink(IEventSink sink)
    {
        ImmutableInterlocked.Update(ref s_eventSinks, static (sinks, sink) => AddSink(sinks, sink), sink);
        return new Registration(() => ImmutableInterlocked.Update(ref s_eventSinks, static (sinks, sink) => sinks.Remove(sink, ReferenceEqualityComparer.Instance), sink));
    }

    private static ImmutableArray<TSink> AddSink<TSink>(ImmutableArray<TSink> sinks, TSink sink)
        where TSink : class
    {
        foreach (var registeredSink in sinks)
            Contract.ThrowIfTrue(ReferenceEquals(registeredSink, sink), "The sink instance is already registered.");

        return sinks.Add(sink);
    }

    private sealed class Registration(Action unregister) : IDisposable
    {
        private Action? _unregister = unregister;

        public void Dispose()
            => Interlocked.Exchange(ref _unregister, null)?.Invoke();
    }

    /// <summary>
    /// Whether any registered sink wants <paramref name="functionId"/>. Checked before a
    /// <see cref="LogMessage"/> is constructed, so that logging costs nothing when everything is
    /// disabled.
    /// </summary>
    private static bool TryGetEnabledSinks(FunctionId functionId, out ImmutableArray<IEventSink> sinks)
    {
        sinks = s_eventSinks;

        foreach (var sink in sinks)
        {
            if (sink.IsEnabled(functionId))
                return true;
        }

        return false;
    }

    private static void LogToSinks(ImmutableArray<IEventSink> sinks, FunctionId functionId, LogMessage logMessage)
    {
        foreach (var sink in sinks)
        {
            if (sink.IsEnabled(functionId))
                sink.Log(functionId, logMessage);
        }
    }

    internal static class TestAccessor
    {
        /// <summary>
        /// Unregisters every sink, so that one test cannot leak a sink into the next.
        /// </summary>
        public static void RemoveAllSinks()
        {
            ImmutableInterlocked.InterlockedExchange(ref s_eventSinks, []);
            ImmutableInterlocked.InterlockedExchange(ref s_metricSinks, []);
        }
    }

    /// <summary>
    /// log a specific event with a simple context message which should be very cheap to create
    /// </summary>
    public static void Log(FunctionId functionId, string? message = null, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetEnabledSinks(functionId, out var sinks))
        {
            var logMessage = LogMessage.Create(message ?? "", logLevel: logLevel);
            LogToSinks(sinks, functionId, logMessage);

            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that will only be created when it is needed.
    /// the messageGetter should be cheap to create. in another word, it shouldn't capture any locals
    /// </summary>
    public static void Log(FunctionId functionId, Func<string> messageGetter, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetEnabledSinks(functionId, out var sinks))
        {
            var logMessage = LogMessage.Create(messageGetter, logLevel);
            LogToSinks(sinks, functionId, logMessage);

            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg>(FunctionId functionId, Func<TArg, string> messageGetter, TArg arg, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetEnabledSinks(functionId, out var sinks))
        {
            var logMessage = LogMessage.Create(messageGetter, arg, logLevel);
            LogToSinks(sinks, functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetEnabledSinks(functionId, out var sinks))
        {
            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, logLevel);
            LogToSinks(sinks, functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetEnabledSinks(functionId, out var sinks))
        {
            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, arg2, logLevel);
            LogToSinks(sinks, functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static void Log<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetEnabledSinks(functionId, out var sinks))
        {
            var logMessage = LogMessage.Create(messageGetter, arg0, arg1, arg2, arg3, logLevel);
            LogToSinks(sinks, functionId, logMessage);
            logMessage.Free();
        }
    }

    /// <summary>
    /// log a specific event with a context message.
    /// </summary>
    public static void Log(FunctionId functionId, LogMessage logMessage)
    {
        if (TryGetEnabledSinks(functionId, out var sinks))
        {
            LogToSinks(sinks, functionId, logMessage);
        }

        // Freed unconditionally: the caller handed over ownership, so returning it to the pool cannot
        // depend on whether a sink happened to be listening.
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
        => TryGetEnabledSinks(functionId, out var sinks)
            ? CreateLogBlock(sinks, functionId, LogMessage.Create(message ?? "", logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that will only be created when it is needed.
    /// the messageGetter should be cheap to create. in another word, it shouldn't capture any locals
    /// </summary>
    public static IDisposable LogBlock(FunctionId functionId, Func<string> messageGetter, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetEnabledSinks(functionId, out var sinks)
            ? CreateLogBlock(sinks, functionId, LogMessage.Create(messageGetter, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg>(FunctionId functionId, Func<TArg, string> messageGetter, TArg arg, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetEnabledSinks(functionId, out var sinks)
            ? CreateLogBlock(sinks, functionId, LogMessage.Create(messageGetter, arg, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg0, TArg1>(FunctionId functionId, Func<TArg0, TArg1, string> messageGetter, TArg0 arg0, TArg1 arg1, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetEnabledSinks(functionId, out var sinks)
            ? CreateLogBlock(sinks, functionId, LogMessage.Create(messageGetter, arg0, arg1, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg0, TArg1, TArg2>(FunctionId functionId, Func<TArg0, TArg1, TArg2, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetEnabledSinks(functionId, out var sinks)
            ? CreateLogBlock(sinks, functionId, LogMessage.Create(messageGetter, arg0, arg1, arg2, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message that requires some arguments to be created when requested.
    /// given arguments will be passed to the messageGetter so that it can create the context message without requiring lifted locals
    /// </summary>
    public static IDisposable LogBlock<TArg0, TArg1, TArg2, TArg3>(FunctionId functionId, Func<TArg0, TArg1, TArg2, TArg3, string> messageGetter, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token, LogLevel logLevel = LogLevel.Trace)
        => TryGetEnabledSinks(functionId, out var sinks)
            ? CreateLogBlock(sinks, functionId, LogMessage.Create(messageGetter, arg0, arg1, arg2, arg3, logLevel), GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;

    /// <summary>
    /// log a start and end pair with a context message. Takes ownership of <paramref name="logMessage"/>
    /// whether or not anything is listening.
    /// </summary>
    public static IDisposable LogBlock(FunctionId functionId, LogMessage logMessage, CancellationToken token)
    {
        if (TryGetEnabledSinks(functionId, out var sinks))
            return CreateLogBlock(sinks, functionId, logMessage, GetNextUniqueBlockId(), token);

        logMessage.Free();
        return EmptyLogBlock.Instance;
    }
}
