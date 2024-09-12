// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry;

internal abstract class TelemetryLogger : ILogger
{
    private sealed class Implementation : TelemetryLogger
    {
        private readonly TelemetrySession _session;

        private Implementation(TelemetrySession session, bool logDelta)
        {
            _session = session;
            LogDelta = logDelta;
        }

        public static new Implementation Create(TelemetrySession session, bool logDelta, IAsynchronousOperationListenerProvider asyncListenerProvider)
        {
            var logger = new Implementation(session, logDelta);
            var asyncListener = asyncListenerProvider.GetListener(FeatureAttribute.Telemetry);

            // Two stage initialization as TelemetryLogProvider.Create needs access to
            //  the ILogger that this class implements.
            TelemetryLogProvider.Create(session, logger, asyncListener);

            return logger;
        }

        protected override bool LogDelta { get; }

        public override bool IsEnabled(FunctionId functionId)
            => _session.IsOptedIn;

        protected override void PostEvent(TelemetryEvent telemetryEvent)
            => _session.PostEvent(telemetryEvent);

        protected override object Start(string eventName, LogType type)
            => type switch
            {
                LogType.Trace => _session.StartOperation(eventName),
                LogType.UserAction => _session.StartUserTask(eventName),
                _ => throw ExceptionUtilities.UnexpectedValue(type),
            };

        protected override TelemetryEvent GetEndEvent(object scope)
            => scope switch
            {
                TelemetryScope<OperationEvent> operation => operation.EndEvent,
                TelemetryScope<UserTaskEvent> userTask => userTask.EndEvent,
                _ => throw ExceptionUtilities.UnexpectedValue(scope)
            };

        protected override void End(object scope, TelemetryResult result)
        {
            if (scope is TelemetryScope<OperationEvent> operation)
                operation.End(result);
            else if (scope is TelemetryScope<UserTaskEvent> userTask)
                userTask.End(result);
            else
                throw ExceptionUtilities.UnexpectedValue(scope);
        }
    }

    private readonly ConcurrentDictionary<int, object> _pendingScopes = new(concurrencyLevel: 2, capacity: 10);

    private const string EventPrefix = "vs/ide/vbcs/";
    private const string PropertyPrefix = "vs.ide.vbcs.";

    // these don't have concurrency limit on purpose to reduce chance of lock contention. 
    // if that becomes a problem - by showing up in our perf investigation, then we will consider adding concurrency limit.
    private static readonly ConcurrentDictionary<FunctionId, string> s_eventMap = [];
    private static readonly ConcurrentDictionary<(FunctionId id, string name), string> s_propertyMap = [];

    protected abstract bool LogDelta { get; }

    internal static string GetEventName(FunctionId id)
         => s_eventMap.GetOrAdd(id, id => EventPrefix + GetTelemetryName(id, separator: '/'));

    internal static string GetPropertyName(FunctionId id, string name)
        => s_propertyMap.GetOrAdd((id, name), key => PropertyPrefix + GetTelemetryName(id, separator: '.') + "." + key.name.ToLowerInvariant());

    private static string GetTelemetryName(FunctionId id, char separator)
        => Enum.GetName(typeof(FunctionId), id)!.Replace('_', separator).ToLowerInvariant();

    public static TelemetryLogger Create(TelemetrySession session, bool logDelta, IAsynchronousOperationListenerProvider asyncListenerProvider)
        => Implementation.Create(session, logDelta, asyncListenerProvider);

    public abstract bool IsEnabled(FunctionId functionId);
    protected abstract void PostEvent(TelemetryEvent telemetryEvent);
    protected abstract object Start(string eventName, LogType type);
    protected abstract void End(object scope, TelemetryResult result);
    protected abstract TelemetryEvent GetEndEvent(object scope);

    public void Log(FunctionId functionId, LogMessage logMessage)
    {
        if (IgnoreMessage(logMessage))
        {
            return;
        }

        var telemetryEvent = new TelemetryEvent(GetEventName(functionId));
        SetProperties(telemetryEvent, functionId, logMessage);

        try
        {
            PostEvent(telemetryEvent);
        }
        catch
        {
        }
    }

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int blockId, CancellationToken cancellationToken)
    {
        if (IgnoreMessage(logMessage))
        {
            return;
        }

        var eventName = GetEventName(functionId);
        var kind = GetKind(logMessage);

        try
        {
            _pendingScopes[blockId] = Start(eventName, kind);
        }
        catch
        {
        }
    }

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int blockId, int delta, CancellationToken cancellationToken)
    {
        if (IgnoreMessage(logMessage))
        {
            return;
        }

        Contract.ThrowIfFalse(_pendingScopes.TryRemove(blockId, out var scope));

        var endEvent = GetEndEvent(scope);
        SetProperties(endEvent, functionId, logMessage, LogDelta ? delta : null);

        var result = cancellationToken.IsCancellationRequested ? TelemetryResult.UserCancel : TelemetryResult.Success;

        try
        {
            End(scope, result);
        }
        catch
        {
        }
    }

    private static bool IgnoreMessage(LogMessage logMessage)
        => logMessage.LogLevel < LogLevel.Information;

    private static LogType GetKind(LogMessage logMessage)
        => logMessage is KeyValueLogMessage kvLogMessage
                            ? kvLogMessage.Kind
                            : logMessage.LogLevel switch
                            {
                                >= LogLevel.Information => LogType.UserAction,
                                _ => LogType.Trace
                            };

    private static void SetProperties(TelemetryEvent telemetryEvent, FunctionId functionId, LogMessage logMessage, int? delta = null)
    {
        if (logMessage is KeyValueLogMessage kvLogMessage)
        {
            AppendProperties(telemetryEvent, functionId, kvLogMessage);
        }
        else
        {
            var message = logMessage.GetMessage();
            if (!string.IsNullOrWhiteSpace(message))
            {
                var propertyName = GetPropertyName(functionId, "Message");
                telemetryEvent.Properties.Add(propertyName, message);
            }
        }

        if (delta.HasValue)
        {
            var propertyName = GetPropertyName(functionId, "Delta");
            telemetryEvent.Properties.Add(propertyName, delta.Value);
        }
    }

    private static void AppendProperties(TelemetryEvent telemetryEvent, FunctionId functionId, KeyValueLogMessage logMessage)
    {
        foreach (var (name, value) in logMessage.Properties)
        {
            // call SetProperty. VS telemetry will take care of finding correct
            // API based on given object type for us.
            // 
            // numeric data will show up in ES with measurement prefix.

            telemetryEvent.Properties.Add(GetPropertyName(functionId, name), value switch
            {
                PiiValue pii => new TelemetryPiiProperty(pii.Value),
                IEnumerable<object> items => new TelemetryComplexProperty(items.Select(item => (item is PiiValue pii) ? new TelemetryPiiProperty(pii.Value) : item)),
                _ => value
            });
        }
    }
}
