// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Roslyn's telemetry entry point. Discrete events and scopes are recorded here and fan out to the
/// host's configured <see cref="IEventSink"/>s; aggregated measurements go to its <see cref="IMetricSink"/>.
/// <para>
/// A host configures this once at startup (see <see cref="AddEventSink"/> / <see cref="AddMetricSink"/>).
/// With nothing configured every method is a cheap no-op, which is the state the build server, the
/// CodeStyle packages, and most tests run in.
/// </para>
/// <para>
/// This file is linked into several assemblies, so "configured" means configured in the assembly the
/// caller resolves to. Only the Workspaces copy is wired up by a host; the CodeStyle copies have no
/// sinks by construction.
/// </para>
/// </summary>
internal static partial class RoslynTelemetry
{
    /// <summary>
    /// The sinks every event fans out to. A sink is added once and stays until its registration is
    /// disposed; turning one off without removing it is that sink's own
    /// <see cref="IEventSink.IsEnabled"/> returning false.
    /// </summary>
    private static ImmutableArray<IEventSink> s_eventSinks = [];

    /// <summary>
    /// next unique block id that will be given to each LogBlock
    /// </summary>
    private static int s_lastUniqueBlockId;

    /// <summary>
    /// Registers <paramref name="sink"/> to receive events, ignoring it if it is already registered.
    /// Dispose the result to unregister it; a host that keeps its sinks for the life of the process can
    /// simply never dispose.
    /// </summary>
    public static IDisposable AddEventSink(IEventSink sink)
    {
        ImmutableInterlocked.Update(ref s_eventSinks, static (sinks, sink) => sinks.Contains(sink) ? sinks : sinks.Add(sink), sink);
        return new Registration(() => ImmutableInterlocked.Update(ref s_eventSinks, static (sinks, sink) => sinks.Remove(sink), sink));
    }

    /// <summary>
    /// Undoes one <see cref="AddEventSink"/> or <see cref="AddMetricSink"/> call.
    /// </summary>
    private sealed class Registration(Action unregister) : IDisposable
    {
        public void Dispose() => unregister();
    }

    /// <summary>
    /// Whether any registered sink wants <paramref name="functionId"/>. Checked before a
    /// <see cref="LogMessage"/> is constructed, so that logging costs nothing when everything is
    /// disabled - which is the common case, since most sinks are opt-in diagnostics.
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

    internal static TestAccessor GetTestAccessor() => default;

    internal readonly struct TestAccessor
    {
        /// <summary>
        /// Unregisters every sink, so that one test cannot leak a sink into the next.
        /// </summary>
        public void RemoveAllSinks()
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
