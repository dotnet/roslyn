// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.Internal;

internal static class VSCodeTelemetryLogger
{
    private static TelemetrySession? _telemetrySession;
    private const string CollectorApiKey = "0c6ae279ed8443289764825290e4f9e2-1a736e7c-1324-4338-be46-fc2a58ae4d14-7255";
    private static int _dumpsSubmitted = 0;

    private static readonly ConcurrentDictionary<int, object> _pendingScopes = new(concurrencyLevel: 2, capacity: 10);

    public static void Initialize(string telemetryLevel)
    {
        if (_telemetrySession is not null)
        {
            _telemetrySession.Dispose();
        }

        _telemetrySession = CreateTelemetryService(telemetryLevel);
        _telemetrySession.Start();
    }

    public static void Log(string name, ImmutableDictionary<string, object?> properties)
    {
        var telemetryEvent = new TelemetryEvent(name);
        SetProperties(telemetryEvent, properties);
        PostEvent(telemetryEvent);
    }

    public static void LogBlockStart(string eventName, int kind, int blockId)
    {
        _pendingScopes[blockId] = kind switch
        {
            0 => _telemetrySession.StartOperation(eventName), // LogType.Trace
            1 => _telemetrySession.StartUserTask(eventName),  // LogType.UserAction
            _ => new InvalidOperationException($"Unknown BlockStart kind: {kind}")
        };
    }

    public static void LogBlockEnd(int blockId, ImmutableDictionary<string, object?> properties, CancellationToken cancellationToken)
    {
        var found = _pendingScopes.TryRemove(blockId, out var scope);
        Contract.Requires(found);

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

    public static void ReportFault(string eventName, string description, int logLevel, bool forceDump, int processId, Exception exception)
    {
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

        _telemetrySession!.PostEvent(faultEvent);
    }

    private static TelemetrySession CreateTelemetryService(string telemetryLevel)
    {
        var sessionSettingsJson = CreateSessionSettingsJson(telemetryLevel);
        TelemetryService.SetDefaultSession(new TelemetrySession($"{{{sessionSettingsJson}}}"));
        return TelemetryService.DefaultSession;
    }

    private static string CreateSessionSettingsJson(string telemetryLevel)
    {
        var customSessionId = Guid.NewGuid().ToString();

        // Generate a new startTime for process to be consumed by Telemetry Settings
        using var curProcess = Process.GetCurrentProcess();
        var processStartTime = curProcess.StartTime.ToFileTimeUtc().ToString();

        var sb = new StringBuilder();

        var kvp = new Dictionary<string, string>
                {
                    { "Id", StringToJsonValue(customSessionId) },
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

    private static void PostEvent(TelemetryEvent telemetryEvent)
        => _telemetrySession?.PostEvent(telemetryEvent);

    private static void SetProperties(TelemetryEvent telemetryEvent, ImmutableDictionary<string, object?> properties)
    {
        foreach (var (name, value) in properties)
        {
            telemetryEvent.Properties.Add(name, value);
        }
    }
}
