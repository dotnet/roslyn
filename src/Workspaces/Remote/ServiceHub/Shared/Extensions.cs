// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Telemetry;
using Newtonsoft.Json;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static partial class Extensions
    {
        public static void ReportServiceHubNFW(this Exception exception, string message)
        {
            if (exception == null)
            {
                return;
            }

            WatsonReporter.Report(message, exception, ReportDetailServiceHubLogs, WatsonSeverity.Critical);
        }

        private static int ReportDetailServiceHubLogs(IFaultUtility faultUtility)
        {
            // 0 means send watson, otherwise, cancel watson
            // we always send watson since dump itself can have valuable data
            var exitCode = 0;

            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "servicehub", "logs");
                if (!Directory.Exists(logPath))
                {
                    return exitCode;
                }

                // attach all log files that are modified less than 1 day before.
                var now = DateTime.UtcNow;
                var oneDay = TimeSpan.FromDays(1);

                foreach (var file in Directory.EnumerateFiles(logPath, "*.log"))
                {
                    var lastWrite = File.GetLastWriteTimeUtc(file);
                    if (now - lastWrite > oneDay)
                    {
                        continue;
                    }

                    faultUtility.AddFile(file);
                }
            }
            catch (Exception)
            {
                // it is okay to fail on reporting watson
            }

            return exitCode;
        }
    }
}
