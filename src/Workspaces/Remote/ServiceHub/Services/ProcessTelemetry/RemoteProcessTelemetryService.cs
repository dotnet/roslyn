// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteProcessTelemetryService(
    BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteProcessTelemetryService
{
    internal sealed class Factory : FactoryBase<IRemoteProcessTelemetryService>
    {
        protected override IRemoteProcessTelemetryService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteProcessTelemetryService(arguments);
    }

    private readonly CancellationTokenSource _shutdownCancellationSource = new();

#pragma warning disable IDE0052 // Remove unread private members
    private PerformanceReporter? _performanceReporter;
#pragma warning restore

    /// <summary>
    /// Remote API. Initializes ServiceHub process global state.
    /// </summary>
    public ValueTask InitializeTelemetrySessionAsync(int hostProcessId, string serializedSession, bool logDelta, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            var services = GetWorkspace().Services;

            var telemetryService = (RemoteWorkspaceTelemetryService)services.GetRequiredService<IWorkspaceTelemetryService>();
            var telemetrySession = new TelemetrySession(serializedSession);
            telemetrySession.Start();

            telemetryService.InitializeTelemetrySession(telemetrySession, logDelta);
            telemetryService.RegisterUnexpectedExceptionLogger(TraceLogger);
            FaultReporter.InitializeFatalErrorHandlers();

            // log telemetry that service hub started
            RoslynLogger.Log(FunctionId.RemoteHost_Connect, KeyValueLogMessage.Create(static (m, hostProcessId) =>
            {
                m["Host"] = hostProcessId;
                m["Framework"] = RuntimeInformation.FrameworkDescription;
            }, hostProcessId));

            // start performance reporter
            var diagnosticAnalyzerPerformanceTracker = services.GetService<IPerformanceTrackerService>();
            if (diagnosticAnalyzerPerformanceTracker != null)
            {
                // We know in the remote layer that this type must exist.
                _performanceReporter = new PerformanceReporter(telemetrySession, diagnosticAnalyzerPerformanceTracker, _shutdownCancellationSource.Token);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask EnableLoggingAsync(ImmutableArray<string> loggerTypeNames, ImmutableArray<FunctionId> functionIds, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            var functionIdsSet = new HashSet<FunctionId>(functionIds);
            bool logChecker(FunctionId id) => functionIdsSet.Contains(id);

            // we only support 2 types of loggers
            SetRoslynLogger(loggerTypeNames, () => new EtwLogger(logChecker));
            SetRoslynLogger(loggerTypeNames, () => new TraceLogger(logChecker));
        }, cancellationToken);
    }

    private static void SetRoslynLogger<T>(ImmutableArray<string> loggerTypes, Func<T> creator) where T : ILogger
    {
        if (loggerTypes.Contains(typeof(T).Name))
        {
            RoslynLogger.SetLogger(AggregateLogger.AddOrReplace(creator(), RoslynLogger.GetLogger(), l => l is T));
        }
        else
        {
            RoslynLogger.SetLogger(AggregateLogger.Remove(RoslynLogger.GetLogger(), l => l is T));
        }
    }
}
