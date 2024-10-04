// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Telemetry;

//namespace Microsoft.CodeAnalysis.LanguageServer.Telemetry;

//internal class VSCodeRequestTelemetryLogger : RequestTelemetryLogger
//{
//    /// <summary>
//    /// Tracks whether or not the initial project load has completed so we can see
//    /// how often we get misc file requests after we've loaded.
//    /// </summary>
//    private static bool _initialProjectLoadCompleted = false;

//    private readonly ConcurrentDictionary<bool, ConcurrentDictionary<string, Counter>> _findDocumentCounters;

//    public VSCodeRequestTelemetryLogger(string serverTypeName) : base(serverTypeName)
//    {
//    }

//    public static void ReportProjectInitializationComplete()
//    {
//        _initialProjectLoadCompleted = true;
//        Logger.Log(FunctionId.VSCode_Projects_Load_Completed, logLevel: LogLevel.Information);
//    }

//    public static void ReportProjectLoadStarted()
//    {
//        Logger.Log(FunctionId.VSCode_Project_Load_Started, logLevel: LogLevel.Information);
//    }

//    protected override void IncreaseFindDocumentCount(string workspaceInfo)
//    {
//        TelemetryLogging.LogAggregated(FunctionId.LSP_FindDocumentInWorkspace, KeyValueLogMessage.Create(m =>
//        {
//            m[TelemetryLogging.KeyName] = _serverTypeName;
//            m[TelemetryLogging.KeyValue] = (int)queuedDuration.TotalMilliseconds;
//            m[TelemetryLogging.KeyMetricName] = "Count";
//            m["server"] = _serverTypeName;
//            m["method"] = methodName;
//            m["language"] = language;
//        }));

//        base.IncreaseFindDocumentCount(workspaceInfo);
//    }

//    protected override void ReportFindDocumentCounter()
//    {
//        base.ReportFindDocumentCounter();
//    }
//}
