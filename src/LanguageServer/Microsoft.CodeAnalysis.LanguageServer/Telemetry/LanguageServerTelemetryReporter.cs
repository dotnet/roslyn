// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Composition;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.Telemetry;

[Export(typeof(ITelemetryReporter)), Shared]
internal sealed class LanguageServerTelemetryReporter : ITelemetryReporter
{
    private const string VSCodeCollectorApiKey = "0c6ae279ed8443289764825290e4f9e2-1a736e7c-1324-4338-be46-fc2a58ae4d14-7255";

    private static readonly ConcurrentDictionary<int, object> s_pendingScopes = new(concurrencyLevel: 2, capacity: 10);

    private readonly ServerConfiguration _serverConfiguration;
    private readonly ILogger _logger;
    private TelemetrySession? _telemetrySession;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LanguageServerTelemetryReporter(ServerConfiguration serverConfiguration, ILoggerFactory loggerFactory)
    {
        _serverConfiguration = serverConfiguration;
        _logger = loggerFactory.CreateLogger<LanguageServerTelemetryReporter>();
    }

    public void InitializeSession(string telemetryLevel, string? sessionId, bool isDefaultSession)
    {
        Debug.Assert(_telemetrySession is null);

        var useDevKitTelemetry = _serverConfiguration.DevKitDependencyPath is not null;
        var session = useDevKitTelemetry
            ? new TelemetrySession(CreateDevKitSessionSettings(telemetryLevel, sessionId))
            : VisualStudio.Telemetry.TelemetryService.DefaultSession;

        // The keyless VS default session is opted out until the standalone host supplies consent.
        session.IsOptedIn = telemetryLevel != "off";

        if (isDefaultSession && useDevKitTelemetry)
        {
            VisualStudio.Telemetry.TelemetryService.SetDefaultSession(session);
        }

        session.Start();
        session.RegisterForReliabilityEvent();

        _logger.LogTrace(
            "Telemetry session started with sessionID {sessionId} for {telemetryDestination}",
            session.SessionId,
            useDevKitTelemetry ? "VS Code" : "VS Raw");

        _telemetrySession = session;

        TelemetryLogger.Create(session, logDelta: false);

        FaultReporter.InitializeFatalErrorHandlers();
        FaultReporter.IncludeServiceHubLogFiles = false;
        FaultReporter.RegisterTelemetrySesssion(session);
    }

    public void Log(string name, List<KeyValuePair<string, object?>> properties)
    {
        if (_telemetrySession is null)
        {
            return;
        }

        var telemetryEvent = new TelemetryEvent(name);
        SetProperties(telemetryEvent, properties);
        _telemetrySession.PostEvent(telemetryEvent);
    }

    public void LogBlockStart(string eventName, int kind, int blockId)
    {
        if (_telemetrySession is null)
        {
            return;
        }

        s_pendingScopes[blockId] = kind switch
        {
            0 => _telemetrySession.StartOperation(eventName),
            1 => _telemetrySession.StartUserTask(eventName),
            _ => new InvalidOperationException($"Unknown BlockStart kind: {kind}")
        };
    }

    public void LogBlockEnd(int blockId, List<KeyValuePair<string, object?>> properties, CancellationToken cancellationToken)
    {
        if (!s_pendingScopes.TryRemove(blockId, out var scope))
        {
            return;
        }

        var endEvent = GetEndEvent(scope);
        SetProperties(endEvent, properties);

        var result = cancellationToken.IsCancellationRequested ? TelemetryResult.UserCancel : TelemetryResult.Success;

        if (scope is TelemetryScope<OperationEvent> operation)
            operation.End(result);
        else if (scope is TelemetryScope<UserTaskEvent> userTask)
            userTask.End(result);
        else
            throw new InvalidCastException($"Unexpected value for scope: {scope}");
    }

    public void Dispose()
    {
        TelemetryLogging.Flush();

        if (_telemetrySession is { } session)
        {
            FaultReporter.UnregisterTelemetrySesssion(session);
            session.Dispose();
            _telemetrySession = null;
        }
    }

    internal static string CreateDevKitSessionSettings(string telemetryLevel, string? sessionId)
    {
        var settings = new JsonObject
        {
            ["Id"] = sessionId ?? Guid.NewGuid().ToString(),
            ["HostName"] = "Default",
            ["TelemetryLevel"] = telemetryLevel,
            ["IsInitialSession"] = true,
            ["CollectorApiKey"] = VSCodeCollectorApiKey,
            ["AppId"] = 1010,
        };

        using var currentProcess = Process.GetCurrentProcess();
        settings["ProcessStartTime"] = currentProcess.StartTime.ToFileTimeUtc();

        return settings.ToJsonString();
    }

    private static TelemetryEvent GetEndEvent(object scope)
        => scope switch
        {
            TelemetryScope<OperationEvent> operation => operation.EndEvent,
            TelemetryScope<UserTaskEvent> userTask => userTask.EndEvent,
            _ => throw new InvalidCastException($"Unexpected value for scope: {scope}")
        };

    private static void SetProperties(TelemetryEvent telemetryEvent, List<KeyValuePair<string, object?>> properties)
    {
        foreach (var property in properties)
        {
            telemetryEvent.Properties.Add(property);
        }
    }
}
