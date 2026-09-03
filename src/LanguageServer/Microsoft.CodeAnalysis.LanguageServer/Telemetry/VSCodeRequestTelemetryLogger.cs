// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.Telemetry;

/// <summary>
/// Exports a stateful <see cref="RequestTelemetryLogger"/> that reports server specific telemetry.
/// </summary>
[ExportCSharpVisualBasicLspServiceFactory(typeof(RequestTelemetryLogger), WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSCodeRequestTelemetryLoggerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new VSCodeRequestTelemetryLogger();
}

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
        var projectsLoaded = s_initialProjectLoadCompleted;
        RoslynTelemetry.Count(FunctionId.LSP_FindDocumentInWorkspace, workspaceCountMetricName, 1,
            new("server", ServerTypeName),
            new("workspace", workspaceCountMetricName),
            new("projectsLoaded", projectsLoaded));
    }
}
