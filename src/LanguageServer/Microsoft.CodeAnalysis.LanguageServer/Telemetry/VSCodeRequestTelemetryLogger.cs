// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.Telemetry;

[ExportCSharpVisualBasicStatelessLspService(typeof(RequestTelemetryLogger), serverKind: WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VSCodeRequestTelemetryLogger() : RequestTelemetryLogger(WellKnownLspServerKinds.CSharpVisualBasicLspServer.ToTelemetryString())
{
    /// <summary>
    /// Tracks whether or not the initial project load has completed.
    /// </summary>
    private static bool s_initialProjectLoadCompleted = false;

    /// <summary>
    /// Store which workspace a document came from for each request.  This is tracked separately before and after initial project load:
    ///   1.  Before initial load, almost all requests should resolve to the misc files workspace.
    ///   2.  After initial load, almost all requests should resolve to the host workspace.
    /// A large amount of misc files requests in 2 could indicate either a bug or feature improvements in order to load what the user is expecting.
    /// </summary>
    private readonly ConcurrentDictionary<bool, CountLogAggregator<string>> _findDocumentCounters = new()
    {
        [true] = new(),
        [false] = new(),
    };

    public static void ReportProjectInitializationComplete()
    {
        s_initialProjectLoadCompleted = true;
        Logger.Log(FunctionId.VSCode_Projects_Load_Completed, logLevel: LogLevel.Information);
    }

    public static void ReportProjectLoadStarted()
    {
        Logger.Log(FunctionId.VSCode_Project_Load_Started, logLevel: LogLevel.Information);
    }

    protected override void IncreaseFindDocumentCount(string workspaceInfo)
    {
        _findDocumentCounters.GetOrAdd(s_initialProjectLoadCompleted, (_) => new CountLogAggregator<string>()).IncreaseCount(workspaceInfo);
    }

    protected override void ReportFindDocumentCounter()
    {
        foreach (var (isInitialLoadComplete, counter) in _findDocumentCounters)
        {
            if (!counter.IsEmpty)
            {
                TelemetryLogging.Log(FunctionId.LSP_FindDocumentInWorkspace, KeyValueLogMessage.Create(LogType.Trace, m =>
                {
                    m["server"] = ServerTypeName;
                    m["projectsLoaded"] = isInitialLoadComplete;
                    foreach (var kvp in counter)
                    {
                        var info = kvp.Key.ToString()!;
                        m[info] = kvp.Value.GetCount();
                    }
                }));
            }
        }
        _findDocumentCounters.Clear();
    }

}
