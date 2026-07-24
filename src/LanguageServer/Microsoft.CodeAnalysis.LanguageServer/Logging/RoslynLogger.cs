// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

internal sealed class RoslynLogger : ILogger
{
    private static RoslynLogger? _instance;
    private static readonly ConcurrentDictionary<FunctionId, string> s_eventMap = [];
    private static readonly ConcurrentDictionary<(FunctionId id, string name), string> s_propertyMap = [];

    private readonly ConcurrentDictionary<int, object> _pendingScopes = new(concurrencyLevel: 2, capacity: 10);
    private static ITelemetryReporter? _telemetryReporter;
    private static readonly ObjectPool<List<KeyValuePair<string, object?>>> s_propertyPool = new(() => []);

    private RoslynLogger()
    {
    }

    public static void Initialize(ITelemetryReporter? reporter, string? telemetryLevel, string? sessionId)
    {
        Contract.ThrowIfTrue(_instance is not null);

        if (reporter is not null && telemetryLevel is not null)
        {
            reporter.InitializeSession(telemetryLevel, sessionId, isDefaultSession: true);
            _telemetryReporter = reporter;
        }

        _instance = new();

        var currentLogger = Logger.GetLogger();
        if (currentLogger is null)
        {
            Logger.SetLogger(_instance);
        }
        else
        {
            Logger.SetLogger(AggregateLogger.Create(currentLogger, _instance));
        }
    }

    public bool IsEnabled(FunctionId functionId)
        => _telemetryReporter is not null;

    public void Log(FunctionId functionId, LogMessage logMessage)
    {
        if (IgnoreReporting(logMessage))
        {
            return;
        }

        using var pooledObject = s_propertyPool.GetPooledObject();
        var properties = pooledObject.Object;

        var name = GetEventName(functionId);
        AddProperties(properties, functionId, logMessage, delta: null);

        try
        {
            _telemetryReporter.Log(name, properties);
        }
        catch
        {
        }
    }

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int blockId, CancellationToken cancellationToken)
    {
        if (IgnoreReporting(logMessage))
        {
            return;
        }

        var eventName = GetEventName(functionId);
        var kind = GetKind(logMessage);

        try
        {
            _telemetryReporter.LogBlockStart(eventName, (int)kind, blockId);
        }
        catch
        {
        }
    }

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int blockId, int delta, CancellationToken cancellationToken)
    {
        if (IgnoreReporting(logMessage))
        {
            return;
        }

        using var pooledObject = s_propertyPool.GetPooledObject();
        var properties = pooledObject.Object;

        AddProperties(properties, functionId, logMessage, delta);
        try
        {
            _telemetryReporter.LogBlockEnd(blockId, properties, cancellationToken);
        }
        catch
        {
        }
    }

    public static void ShutdownAndReportSessionTelemetry()
    {
        if (_instance is null)
        {
            return;
        }

        FeaturesSessionTelemetry.Report();

        (var currentReporter, _telemetryReporter) = (_telemetryReporter, null);
        currentReporter?.Dispose();
        _instance = null;
    }

    [MemberNotNullWhen(false, nameof(_telemetryReporter))]
    private static bool IgnoreReporting(LogMessage logMessage)
        => _telemetryReporter is null ||
           logMessage.LogLevel < LogLevel.Information;

    private const string EventPrefix = "vs/ide/vbcs/";
    private const string PropertyPrefix = "vs.ide.vbcs.";

    private static string GetEventName(FunctionId id)
        => s_eventMap.GetOrAdd(id, id => EventPrefix + GetTelemetryName(id, separator: '/'));

    private static string GetPropertyName(FunctionId id, string name)
        => s_propertyMap.GetOrAdd((id, name), key => PropertyPrefix + GetTelemetryName(id, separator: '.') + "." + key.name.ToLowerInvariant());

    private static string GetTelemetryName(FunctionId id, char separator)
            => Enum.GetName(typeof(FunctionId), id)!.Replace('_', separator).ToLowerInvariant();

    private static LogType GetKind(LogMessage logMessage)
            => logMessage is KeyValueLogMessage kvLogMessage
                                ? kvLogMessage.Kind
                                : logMessage.LogLevel switch
                                {
                                    >= LogLevel.Information => LogType.UserAction,
                                    _ => LogType.Trace
                                };

    private static void AddProperties(List<KeyValuePair<string, object?>> properties, FunctionId id, LogMessage logMessage, int? delta)
    {
        if (logMessage is KeyValueLogMessage kvLogMessage)
        {
            foreach (var (name, val) in kvLogMessage.Properties)
            {
                properties.Add(new(GetPropertyName(id, name), val));
            }
        }
        else
        {
            properties.Add(new(GetPropertyName(id, "Message"), logMessage.GetMessage()));
        }

        if (delta.HasValue)
        {
            properties.Add(new(GetPropertyName(id, "Delta"), delta.Value));
        }
    }
}
