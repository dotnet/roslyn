// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Internal;

[ExportWorkspaceService(typeof(ILspFaultLogger)), Shared]
internal class VSCodeWorkspaceTelemetryService : TelemetryLogger, ILspFaultLogger
{
    private TelemetrySession? _telemetrySession;
    private const string CollectorApiKey = "0c6ae279ed8443289764825290e4f9e2-1a736e7c-1324-4338-be46-fc2a58ae4d14-7255";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSCodeWorkspaceTelemetryService()
    {
    }

    protected override bool LogDelta => true;

    public void Initialize()
    {
        _telemetrySession = TelemetryService.CreateAndGetDefaultSession(CollectorApiKey);
        _telemetrySession.Start();

        FaultReporter.InitializeFatalErrorHandlers();
        FaultReporter.RegisterTelemetrySesssion(_telemetrySession);

        var logger = Logger.GetLogger();
        if (logger is not null)
        {
            Logger.SetLogger(AggregateLogger.Create(logger, this));
        }
        else
        {
            Logger.SetLogger(this);
        }
    }

    public override bool IsEnabled(FunctionId functionId) => _telemetrySession?.IsOptedIn ?? false;

    public void LogFault(Exception exception, LogLevel logLevel, bool forceDump)
    {
        var faultSeverity = logLevel switch
        {
            < LogLevel.Information => FaultSeverity.Diagnostic,
            < LogLevel.Critical => FaultSeverity.General,
            _ => FaultSeverity.Critical
        };

        FaultReporter.ReportFault(exception, faultSeverity, forceDump);
    }

    protected override void PostEvent(TelemetryEvent telemetryEvent)
                => _telemetrySession?.PostEvent(telemetryEvent);

    protected override object Start(string eventName, LogType type)
        => type switch
        {
            LogType.Trace => _telemetrySession.StartOperation(eventName),
            LogType.UserAction => _telemetrySession.StartUserTask(eventName),
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
        switch (scope)
        {
            case TelemetryScope<OperationEvent> operation:
                operation.End(result);
                break;
            case TelemetryScope<UserTaskEvent> userTask:
                userTask.End(result);
                break;
            default:
                throw ExceptionUtilities.UnexpectedValue(scope);
        }
    }
}
