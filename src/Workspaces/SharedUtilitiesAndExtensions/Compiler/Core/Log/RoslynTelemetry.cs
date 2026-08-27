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
/// A host configures this once at startup (see <see cref="SetEventSinks"/> / <see cref="SetMetricSink"/>).
/// With nothing configured every method is a cheap no-op, which is the state the build server, the
/// CodeStyle packages, and most tests run in.
/// </para>
/// </summary>
internal static partial class RoslynTelemetry
{
    /// <summary>
    /// The sinks a host composes for itself. Decided once, at startup, and not mutated afterwards:
    /// turning a sink off is that sink's own <see cref="IEventSink.IsEnabled"/> returning false.
    /// </summary>
    private static ImmutableArray<IEventSink> s_hostSinks = [];

    /// <summary>
    /// Sinks attached at runtime by components the host cannot reference (the diagnostics tool window,
    /// integration tests). Kept separate from <see cref="s_hostSinks"/> so that host composition and
    /// runtime attachment cannot clobber each other, whichever happens first.
    /// </summary>
    private static ImmutableArray<IEventSink> s_dynamicSinks = [];

    /// <summary>
    /// <see cref="s_hostSinks"/> and <see cref="s_dynamicSinks"/> combined. Maintained on the (rare)
    /// write path so that recording only ever reads one array.
    /// </summary>
    private static ImmutableArray<IEventSink> s_allSinks = [];

    private static readonly object s_sinkGate = new();

    /// <summary>
    /// next unique block id that will be given to each LogBlock
    /// </summary>
    private static int s_lastUniqueBlockId;

    /// <summary>
    /// Sets the sinks this host composes. Hosts call this once during startup and pass everything they
    /// want; tests reset it to empty during teardown. Sinks attached through
    /// <see cref="AddEventSink"/> are unaffected.
    /// </summary>
    public static void SetEventSinks(ImmutableArray<IEventSink> sinks)
    {
        lock (s_sinkGate)
        {
            s_hostSinks = sinks;
            s_allSinks = [.. s_hostSinks, .. s_dynamicSinks];
        }
    }

    /// <summary>
    /// Attaches <paramref name="sink"/> at runtime, ignoring it if it is already present. Used by
    /// diagnostic sinks that live in assemblies the host cannot reference. Such a sink attaches once and
    /// is thereafter controlled by its own <see cref="IEventSink.IsEnabled"/>; it is never detached.
    /// </summary>
    public static void AddEventSink(IEventSink sink)
    {
        lock (s_sinkGate)
        {
            if (s_dynamicSinks.Contains(sink))
                return;

            s_dynamicSinks = s_dynamicSinks.Add(sink);
            s_allSinks = [.. s_hostSinks, .. s_dynamicSinks];
        }
    }

    /// <summary>
    /// Whether any registered sink wants <paramref name="functionId"/>. Checked before a
    /// <see cref="LogMessage"/> is constructed, so that logging costs nothing when everything is
    /// disabled - which is the common case, since most sinks are opt-in diagnostics.
    /// </summary>
    private static bool TryGetEnabledSinks(FunctionId functionId, out ImmutableArray<IEventSink> sinks)
    {
        sinks = s_allSinks;

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
        /// The sinks currently recording, so a test can capture and restore them around a scenario.
        /// </summary>
        public ImmutableArray<IEventSink> EventSinks => s_allSinks;

        /// <summary>
        /// Replaces every sink, host-composed and dynamically attached alike.
        /// </summary>
        public void SetAllEventSinks(ImmutableArray<IEventSink> sinks)
        {
            lock (s_sinkGate)
            {
                s_hostSinks = sinks;
                s_dynamicSinks = [];
                s_allSinks = sinks;
            }
        }
    }
    /// <summary>
    /// log a specific event with a simple context message which should be very cheap to create
    /// </summary>
    public static void Log(FunctionId functionId, string? message = null, LogLevel logLevel = LogLevel.Debug)
    {
        if (TryGetEnabledSinks(functionId, out var sinks))
        {
            LogToSinks(sinks, functionId, LogMessage.Create(message ?? "", logLevel: logLevel));
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
    /// log a start and end pair with a context message.
    /// </summary>
    public static IDisposable LogBlock(FunctionId functionId, LogMessage logMessage, CancellationToken token)
        => TryGetEnabledSinks(functionId, out var sinks)
            ? CreateLogBlock(sinks, functionId, logMessage, GetNextUniqueBlockId(), token)
            : EmptyLogBlock.Instance;
}
