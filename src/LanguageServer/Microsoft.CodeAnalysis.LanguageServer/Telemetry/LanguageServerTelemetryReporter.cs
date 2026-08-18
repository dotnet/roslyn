// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Composition;
using System.Diagnostics;
using System.Text;
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
    internal const string CopilotTelemetryLevelEnvironmentVariable = "COPILOT_TELEMETRY_LEVEL";

    /// <summary>
    /// Collector key used by C# Dev Kit to send language server telemetry to the VS Code cluster.
    /// </summary>
    private const string VSCodeCollectorApiKey = "0c6ae279ed8443289764825290e4f9e2-1a736e7c-1324-4338-be46-fc2a58ae4d14-7255";

    /// <summary>
    /// Collector key used by standalone hosts to send language server telemetry to the Visual Studio cluster.
    /// </summary>
    private const string VSCollectorApiKey = "f3e86b4023cc43f0be495508d51f588a-f70d0e59-0fb0-4473-9f19-b4024cc340be-7296";

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
            : VisualStudio.Telemetry.TelemetryService.CreateAndGetDefaultSession(VSCollectorApiKey);

        if (!useDevKitTelemetry)
        {
            // The VS default session is opted out until the standalone host supplies consent.
            session.IsOptedIn = IsCopilotCliTelemetryEnabled(telemetryLevel);

            if (telemetryLevel is not ("all" or "off"))
            {
                _logger.LogInformation("Unsupported Copilot CLI telemetry level. Telemetry will remain disabled.");
            }
        }

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

    internal static bool IsCopilotCliTelemetryEnabled(string? telemetryLevel)
        => telemetryLevel == "all";

    internal static string? GetTelemetryLevel(ServerConfiguration serverConfiguration)
        => serverConfiguration.DevKitDependencyPath is not null
            ? serverConfiguration.TelemetryLevel
            : Environment.GetEnvironmentVariable(CopilotTelemetryLevelEnvironmentVariable);

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
            0 => _telemetrySession.StartOperation(eventName), // LogType.Trace
            1 => _telemetrySession.StartUserTask(eventName),  // LogType.UserAction
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
        // Ensure that we flush any pending telemetry *before* we dispose of the telemetry session.
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
        sessionId ??= Guid.NewGuid().ToString();

        // Generate a new startTime for process to be consumed by Telemetry Settings
        using var curProcess = Process.GetCurrentProcess();
        var processStartTime = curProcess.StartTime.ToFileTimeUtc().ToString();

        var sb = new StringBuilder();

        var kvp = new Dictionary<string, string>
        {
            { "Id", StringToJsonValue(sessionId) },
            { "HostName", StringToJsonValue("Default") },

            // Insert Telemetry Level instead of Opt-Out status. The telemetry service handles
            // validation of this value so there is no need to do so on this end. If it's invalid,
            // it defaults to off.
            { "TelemetryLevel", StringToJsonValue(telemetryLevel) },

            // this sets the Telemetry Session Created by LSP Server to be the Root Initial session
            // This means that the SessionID set here by "Id" will be the SessionID used by cloned session
            // further down stream
            { "IsInitialSession", "true" },
            { "CollectorApiKey", StringToJsonValue(VSCodeCollectorApiKey) },

            // using 1010 to indicate VS Code and not to match it to devenv 1000
            { "AppId", "1010" },
            { "ProcessStartTime", processStartTime },
        };

        foreach (var keyValue in kvp)
        {
            sb.AppendFormat("\"{0}\":{1},", keyValue.Key, keyValue.Value);
        }

        return $"{{{sb.ToString().TrimEnd(',')}}}";

        static string StringToJsonValue(string? value)
        {
            if (value == null)
            {
                return "null";
            }

            return '"' + value + '"';
        }
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
