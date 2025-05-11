// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.Telemetry;

[ExportCSharpVisualBasicStatelessLspService(typeof(RequestTelemetryLogger), serverKind: WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSCodeRequestTelemetryLogger() : RequestTelemetryLogger(WellKnownLspServerKinds.CSharpVisualBasicLspServer.ToTelemetryString())
{
    /// <summary>
    /// Tracks whether or not the initial project load has completed.
    /// </summary>
    private static bool s_initialProjectLoadCompleted = false;

    public static void ReportProjectInitializationComplete()
    {
        s_initialProjectLoadCompleted = true;
        Logger.Log(FunctionId.VSCode_Projects_Load_Completed, logLevel: LogLevel.Information);
    }

    public static void ReportProjectLoadStarted()
    {
        Logger.Log(FunctionId.VSCode_Project_Load_Started, logLevel: LogLevel.Information);
    }

    protected override void IncreaseFindDocumentCount(string workspaceCountMetricName)
    {
        TelemetryLogging.LogAggregatedCounter(FunctionId.LSP_FindDocumentInWorkspace, KeyValueLogMessage.Create(m =>
        {
            var projectsLoaded = s_initialProjectLoadCompleted;
            m[TelemetryLogging.KeyName] = ServerTypeName + "." + workspaceCountMetricName + "." + projectsLoaded;
            m[TelemetryLogging.KeyValue] = 1L;
            m[TelemetryLogging.KeyMetricName] = workspaceCountMetricName;
            m["server"] = ServerTypeName;
            m["workspace"] = workspaceCountMetricName;
            m["projectsLoaded"] = projectsLoaded;
        }));
    }
}
