// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal static class WatsonReporter
    {
        /// <summary>
        /// Controls whether or not we actually report the failure.
        /// There are situations where we know we're in a bad state and any further reports are unlikely to be
        /// helpful, so we shouldn't send them.
        /// </summary>
        private static bool s_report = true;

        private static Dictionary<string, string>? s_capturedFileContent;

        private static TelemetrySession? s_telemetrySession;
        private static TraceSource? s_logger;

        public static void InitializeFatalErrorHandlers(TelemetrySession session)
        {
            Debug.Assert(s_telemetrySession == null);
            s_telemetrySession = session;

            var fatalReporter = new Action<Exception>(ReportFatal);
            var nonFatalReporter = new Action<Exception>(ReportNonFatal);

            FatalError.Handler = fatalReporter;
            FatalError.NonFatalHandler = nonFatalReporter;

            // We also must set the FailFast handler for the compiler layer as well
            var compilerAssembly = typeof(Compilation).Assembly;
            var compilerFatalErrorType = compilerAssembly.GetType("Microsoft.CodeAnalysis.FatalError", throwOnError: true);
            var compilerFatalErrorHandlerProperty = compilerFatalErrorType.GetProperty(nameof(FatalError.Handler), BindingFlags.Static | BindingFlags.Public);
            var compilerNonFatalErrorHandlerProperty = compilerFatalErrorType.GetProperty(nameof(FatalError.NonFatalHandler), BindingFlags.Static | BindingFlags.Public);
            compilerFatalErrorHandlerProperty.SetValue(null, fatalReporter);
            compilerNonFatalErrorHandlerProperty.SetValue(null, nonFatalReporter);
        }

        public static void InitializeLogger(TraceSource logger)
        {
            Debug.Assert(s_logger == null);
            s_logger = logger;
        }

        public static void ReportFatal(Exception exception)
        {
            try
            {
                CaptureFilesInMemory(CollectServiceHubLogFilePaths());
            }
            catch
            {
                // ignore any exceptions (e.g. OOM)
            }

            FailFast.OnFatalException(exception);
        }

        /// <summary>
        /// Report Non-Fatal Watson for a given unhandled exception.
        /// </summary>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        public static void ReportNonFatal(Exception exception)
        {
            if (exception is OutOfMemoryException)
            {
                FailFast.OnFatalException(exception);
            }

            if (!s_report)
            {
                return;
            }

            var emptyCallstack = exception.SetCallstackIfEmpty();
            var currentProcess = Process.GetCurrentProcess();

            // write the exception to a log file:
            s_logger?.TraceEvent(TraceEventType.Error, 1, $"[{currentProcess.ProcessName}:{currentProcess.Id}] Unexpected exception: {exception}");

            var session = s_telemetrySession;
            if (session == null)
            {
                return;
            }

            var faultEvent = new FaultEvent(
                eventName: FunctionId.NonFatalWatson.GetEventName(),
                description: "Roslyn NonFatal Watson",
                FaultSeverity.Diagnostic,
                exceptionObject: exception,
                gatherEventDetails: faultUtility =>
                {
                    // add current process dump
                    faultUtility.AddProcessDump(currentProcess.Id);

                    // add ServiceHub log files:
                    foreach (var path in CollectServiceHubLogFilePaths())
                    {
                        faultUtility.AddFile(path);
                    }

                    // Returning "0" signals that we should send data to Watson; any other value will cancel the Watson report.
                    return 0;
                });

            // add extra bucket parameters to bucket better in NFW
            // we do it here so that it gets bucketted better in both
            // watson and telemetry. 
            faultEvent.SetExtraParameters(exception, emptyCallstack);

            session.PostEvent(faultEvent);
        }

        private static List<string> CollectServiceHubLogFilePaths()
        {
            var paths = new List<string>();

            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "servicehub", "logs");
                if (!Directory.Exists(logPath))
                {
                    return paths;
                }

                // attach all log files that are modified less than 1 day before.
                var now = DateTime.UtcNow;
                var oneDay = TimeSpan.FromDays(1);

                foreach (var path in Directory.EnumerateFiles(logPath, "*.log"))
                {
                    try
                    {
                        var name = Path.GetFileNameWithoutExtension(path);

                        // TODO: https://github.com/dotnet/roslyn/issues/42582 
                        // name our services more consistently to simplify filtering

                        // filter logs that are not relevant to Roslyn investigation
                        if (!name.Contains("-" + WellKnownServiceHubServices.NamePrefix) &&
                            !name.Contains("-CodeLens") &&
                            !name.Contains("-pythia") &&
                            !name.Contains("-ManagedLanguage.IDE.RemoteHostClient") &&
                            !name.Contains("-hub"))
                        {
                            continue;
                        }

                        var lastWrite = File.GetLastWriteTimeUtc(path);
                        if (now - lastWrite > oneDay)
                        {
                            continue;
                        }

                        paths.Add(path);
                    }
                    catch
                    {
                        // ignore file that can't be accessed
                    }
                }
            }
            catch (Exception)
            {
                // ignore failures
            }

            return paths;
        }

        private static void CaptureFilesInMemory(IEnumerable<string> paths)
        {
            s_capturedFileContent = new Dictionary<string, string>();

            foreach (var path in paths)
            {
                try
                {
                    s_capturedFileContent[path] = File.ReadAllText(path);
                }
                catch
                {
                    // ignore file that can't be read
                }
            }
        }
    }

    internal enum WatsonSeverity
    {
        /// <summary>
        /// Indicate that this watson is informative and not urgent
        /// </summary>
        Default,

        /// <summary>
        /// Indicate that this watson is critical and need to be addressed soon
        /// </summary>
        Critical,
    }
}
