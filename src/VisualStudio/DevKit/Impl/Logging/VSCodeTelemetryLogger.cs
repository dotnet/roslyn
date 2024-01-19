// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Contracts.Telemetry;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

[Export(typeof(ITelemetryReporter)), Shared]
internal sealed class VSCodeTelemetryLogger : ITelemetryReporter
{
    private TelemetrySession? _telemetrySession;
    private const string CollectorApiKey = "0c6ae279ed8443289764825290e4f9e2-1a736e7c-1324-4338-be46-fc2a58ae4d14-7255";
    private static int _dumpsSubmitted = 0;

    private static readonly ConcurrentDictionary<int, object> _pendingScopes = new(concurrencyLevel: 2, capacity: 10);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSCodeTelemetryLogger()
    {
    }

    public void InitializeSession(string telemetryLevel, string? sessionId, bool isDefaultSession)
    {
        Debug.Assert(_telemetrySession == null);

        var sessionSettingsJson = CreateSessionSettingsJson(telemetryLevel, sessionId);
        var session = new TelemetrySession($"{{{sessionSettingsJson}}}");

        if (isDefaultSession)
        {
            VisualStudio.Telemetry.TelemetryService.SetDefaultSession(session);
        }

        session.Start();
        session.RegisterForReliabilityEvent();

        _telemetrySession = session;
    }

    public void Log(string name, List<KeyValuePair<string, object?>> properties)
    {
        Debug.Assert(_telemetrySession != null);

        var telemetryEvent = new TelemetryEvent(name);
        SetProperties(telemetryEvent, properties);
        _telemetrySession.PostEvent(telemetryEvent);
    }

    public void LogBlockStart(string eventName, int kind, int blockId)
    {
        Debug.Assert(_telemetrySession != null);

        _pendingScopes[blockId] = kind switch
        {
            0 => _telemetrySession.StartOperation(eventName), // LogType.Trace
            1 => _telemetrySession.StartUserTask(eventName),  // LogType.UserAction
            _ => new InvalidOperationException($"Unknown BlockStart kind: {kind}")
        };
    }

    public void LogBlockEnd(int blockId, List<KeyValuePair<string, object?>> properties, CancellationToken cancellationToken)
    {
        var found = _pendingScopes.TryRemove(blockId, out var scope);
        Debug.Assert(found);

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

    public void ReportFault(string eventName, string description, int logLevel, bool forceDump, int processId, Exception exception)
    {
        Debug.Assert(_telemetrySession != null);

        var faultEvent = new FaultEvent(
            eventName: eventName,
            description: description,
            (FaultSeverity)logLevel,
            exceptionObject: exception,
            gatherEventDetails: faultUtility =>
            {
                if (forceDump)
                {
                    // Let's just send a maximum of three; number chosen arbitrarily
                    if (Interlocked.Increment(ref _dumpsSubmitted) <= 3)
                        faultUtility.AddProcessDump(processId);
                }

                if (faultUtility is FaultEvent { IsIncludedInWatsonSample: true })
                {
                    // if needed, add any extra logs here
                }

                // Returning "0" signals that, if sampled, we should send data to Watson. 
                // Any other value will cancel the Watson report. We never want to trigger a process dump manually, 
                // we'll let TargetedNotifications determine if a dump should be collected.
                // See https://aka.ms/roslynnfwdocs for more details
                return 0;
            });

        _telemetrySession.PostEvent(faultEvent);
    }

    public void Dispose()
    {
        _telemetrySession?.Dispose();
        _telemetrySession = null;
    }

    private static string CreateSessionSettingsJson(string telemetryLevel, string? sessionId)
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
            { "CollectorApiKey", StringToJsonValue(CollectorApiKey) },

            // using 1010 to indicate VS Code and not to match it to devenv 1000
            { "AppId", "1010" },
            { "ProcessStartTime", processStartTime },
        };

        foreach (var keyValue in kvp)
        {
            sb.AppendFormat("\"{0}\":{1},", keyValue.Key, keyValue.Value);
        }

        return sb.ToString().TrimEnd(',');

        static string StringToJsonValue(string? value)
        {
            if (value == null)
            {
                return "null";
            }

            return '"' + value + '"';
        }
    }

    private static TelemetryEvent GetEndEvent(object? scope)
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
