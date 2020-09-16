// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private static Dictionary<string, string>? s_capturedFileContent;

        private static readonly object _guard = new object();
        private static ImmutableArray<TelemetrySession> s_telemetrySessions = ImmutableArray<TelemetrySession>.Empty;
        private static ImmutableArray<TraceSource> s_loggers = ImmutableArray<TraceSource>.Empty;

        public static void InitializeFatalErrorHandlers()
        {
            var fatalReporter = new Action<Exception>(ReportFatal);
            var nonFatalReporter = new Action<Exception>(ReportNonFatal);

            FatalError.Handler = fatalReporter;
            FatalError.NonFatalHandler = nonFatalReporter;

            // We also must set the FailFast handler for the compiler layer as well
            var compilerAssembly = typeof(Compilation).Assembly;
            var compilerFatalErrorType = compilerAssembly.GetType("Microsoft.CodeAnalysis.FatalError", throwOnError: true)!;
            var compilerFatalErrorHandlerProperty = compilerFatalErrorType.GetProperty(nameof(FatalError.Handler), BindingFlags.Static | BindingFlags.Public)!;
            var compilerNonFatalErrorHandlerProperty = compilerFatalErrorType.GetProperty(nameof(FatalError.NonFatalHandler), BindingFlags.Static | BindingFlags.Public)!;
            compilerFatalErrorHandlerProperty.SetValue(null, fatalReporter);
            compilerNonFatalErrorHandlerProperty.SetValue(null, nonFatalReporter);
        }

        public static void RegisterTelemetrySesssion(TelemetrySession session)
        {
            lock (_guard)
            {
                s_telemetrySessions = s_telemetrySessions.Add(session);
            }
        }

        public static void UnregisterTelemetrySesssion(TelemetrySession session)
        {
            lock (_guard)
            {
                s_telemetrySessions = s_telemetrySessions.Remove(session);
            }
        }

        public static void RegisterLogger(TraceSource logger)
        {
            lock (_guard)
            {
                s_loggers = s_loggers.Add(logger);
            }
        }

        public static void UnregisterLogger(TraceSource logger)
        {
            lock (_guard)
            {
                s_loggers = s_loggers.Remove(logger);
            }
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
            try
            {
                var emptyCallstack = exception.SetCallstackIfEmpty();
                var currentProcess = Process.GetCurrentProcess();

                // write the exception to a log file:
                var logMessage = $"[{currentProcess.ProcessName}:{currentProcess.Id}] Unexpected exception: {exception}";
                foreach (var logger in s_loggers)
                {
                    logger.TraceEvent(TraceEventType.Error, 1, logMessage);
                }

                var faultEvent = new FaultEvent(
                    eventName: FunctionId.NonFatalWatson.GetEventName(),
                    description: GetDescription(exception),
                    FaultSeverity.Diagnostic,
                    exceptionObject: exception,
                    gatherEventDetails: faultUtility =>
                    {
                        if (faultUtility is FaultEvent { IsIncludedInWatsonSample: true })
                        {
                            // add ServiceHub log files:
                            foreach (var path in CollectServiceHubLogFilePaths())
                            {
                                faultUtility.AddFile(path);
                            }
                        }

                        // Returning "0" signals that, if sampled, we should send data to Watson. 
                        // Any other value will cancel the Watson report. We never want to trigger a process dump manually, 
                        // we'll let TargetedNotifications determine if a dump should be collected.
                        // See https://aka.ms/roslynnfwdocs for more details
                        return 0;
                    });

                // add extra bucket parameters to bucket better in NFW
                // we do it here so that it gets bucketted better in both
                // watson and telemetry. 
                faultEvent.SetExtraParameters(exception, emptyCallstack);

                foreach (var session in s_telemetrySessions)
                {
                    session.PostEvent(faultEvent);
                }
            }
            catch (OutOfMemoryException)
            {
                FailFast.OnFatalException(exception);
            }
            catch (Exception e)
            {
                FailFast.OnFatalException(e);
            }
        }

        private static string GetDescription(Exception exception)
        {
            const string CodeAnalysisNamespace = nameof(Microsoft) + "." + nameof(CodeAnalysis);

            // Be resilient to failing here.  If we can't get a suitable name, just fall back to the standard name we
            // used to report.
            try
            {
                // walk up the stack looking for the first call from a type that isn't in the ErrorReporting namespace.
                foreach (var frame in new StackTrace(exception).GetFrames())
                {
                    var method = frame?.GetMethod();
                    var methodName = method?.Name;
                    if (methodName == null)
                        continue;

                    var declaringTypeName = method?.DeclaringType?.FullName;
                    if (declaringTypeName == null)
                        continue;

                    if (!declaringTypeName.StartsWith(CodeAnalysisNamespace))
                        continue;

                    return declaringTypeName + "." + methodName;
                }
            }
            catch
            {
            }

            return "Roslyn NonFatal Watson";
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
                        if (!name.Contains("-" + RemoteServiceName.Prefix) &&
                            !name.Contains("-" + RemoteServiceName.IntelliCodeServiceName) &&
                            !name.Contains("-" + RemoteServiceName.RazorServiceName) &&
                            !name.Contains("-" + RemoteServiceName.UnitTestingAnalysisServiceName) &&
                            !name.Contains("-" + RemoteServiceName.LiveUnitTestingBuildServiceName) &&
                            !name.Contains("-" + RemoteServiceName.UnitTestingSourceLookupServiceName) &&
                            !name.Contains("-CodeLens") &&
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
