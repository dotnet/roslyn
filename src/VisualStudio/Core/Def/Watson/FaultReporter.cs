// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal static class FaultReporter
    {
        private static readonly object _guard = new();
        private static ImmutableArray<TelemetrySession> s_telemetrySessions = ImmutableArray<TelemetrySession>.Empty;
        private static ImmutableArray<TraceSource> s_loggers = ImmutableArray<TraceSource>.Empty;

        private static int s_dumpsSubmitted;

        public static void InitializeFatalErrorHandlers()
        {
            FatalError.ErrorReporterHandler handler = static (exception, severity, forceDump) => ReportFault(exception, ConvertSeverity(severity), forceDump);
            FatalError.SetHandlers(handler, nonFatalHandler: handler);
            FatalError.CopyHandlersTo(typeof(Compilation).Assembly);
        }

        private static FaultSeverity ConvertSeverity(ErrorSeverity severity)
        {
            return severity switch
            {
                ErrorSeverity.Uncategorized => FaultSeverity.Uncategorized,
                ErrorSeverity.Diagnostic => FaultSeverity.Diagnostic,
                ErrorSeverity.General => FaultSeverity.General,
                ErrorSeverity.Critical => FaultSeverity.Critical,
                _ => FaultSeverity.Uncategorized
            };
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

        /// <summary>
        /// The bucket parameter for the blamed module.
        /// </summary>
        private const int P4ModuleNameDefaultIndex = 4;

        /// <summary>
        /// The bucket parameter for the blamed method.
        /// </summary>
        private const int P5MethodNameDefaultIndex = 5;

        private static readonly ImmutableArray<string> UnblameableMethodPrefixes = ImmutableArray.Create(
            "Microsoft.CodeAnalysis.Shared.Extensions.ISolutionExtensions.GetRequired", // Covers GetRequiredDocument, GetRequiredProject, and similar methods
            "Microsoft.CodeAnalysis.Host.HostLanguageServices.GetRequiredService",
            "Roslyn.Utilities.Contract.",
            "System.Linq.");

        /// <summary>
        /// Report Non-Fatal Watson for a given unhandled exception.
        /// </summary>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        /// <param name="forceDump">Force a dump to be created, even if the telemetry system is not
        /// requesting one; we will still do a client-side limit to avoid sending too much at once.</param>
        public static void ReportFault(Exception exception, FaultSeverity severity, bool forceDump)
        {
            try
            {
                if (exception is OperationCanceledException { InnerException: { } oceInnerException })
                {
                    ReportFault(oceInnerException, severity, forceDump);
                    return;
                }

                if (exception is AggregateException aggregateException)
                {
                    // We (potentially) have multiple exceptions; let's just report each of them
                    foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                        ReportFault(innerException, severity, forceDump);

                    return;
                }

                var currentProcess = Process.GetCurrentProcess();

                // write the exception to a log file:
                var logMessage = $"[{currentProcess.ProcessName}:{currentProcess.Id}] Unexpected exception: {exception}";
                foreach (var logger in s_loggers)
                {
                    logger.TraceEvent(TraceEventType.Error, 1, logMessage);
                }

                var faultEvent = new FaultEvent(
                    eventName: TelemetryLogger.GetEventName(FunctionId.NonFatalWatson),
                    description: GetDescription(exception),
                    severity,
                    exceptionObject: exception,
                    gatherEventDetails: faultUtility =>
                    {
                        if (forceDump)
                        {
                            // Let's just send a maximum of three; number chosen arbitrarily
                            if (Interlocked.Increment(ref s_dumpsSubmitted) <= 3)
                                faultUtility.AddProcessDump(currentProcess.Id);
                        }

                        UpdateBlamedMethod(faultUtility, exception);

                        if (faultUtility is FaultEvent { IsIncludedInWatsonSample: true })
                        {
                            // add ServiceHub log files:
                            foreach (var path in CollectServiceHubLogFilePaths())
                            {
                                faultUtility.AddFile(path);
                            }

                            foreach (var loghubPath in CollectLogHubFilePaths())
                            {
                                faultUtility.AddFile(loghubPath);
                            }
                        }

                        // Returning "0" signals that, if sampled, we should send data to Watson. 
                        // Any other value will cancel the Watson report. We never want to trigger a process dump manually, 
                        // we'll let TargetedNotifications determine if a dump should be collected.
                        // See https://aka.ms/roslynnfwdocs for more details
                        return 0;
                    });

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

        private static void UpdateBlamedMethod(IFaultUtility faultUtility, Exception exception)
        {
            var blamedMethod = faultUtility.GetBucketParameter(P5MethodNameDefaultIndex);

            // We'll only override anything if the default logic blamed something we didn't want
            if (!UnblameableMethodPrefixes.Any(p => blamedMethod.StartsWith(p)))
            {
                return;
            }

            // If anything fails here, we'll just keep the failure as is rather than potentially losing it
            try
            {
                var stackTrace = new StackTrace(exception);
                foreach (var stackFrame in stackTrace.GetFrames())
                {
                    var method = stackFrame.GetMethod();
                    if (method != null && method.DeclaringType != null)
                    {
                        // Get the full name of the method, without parameters
                        var methodName = method.DeclaringType.FullName + "." + method.Name;
                        if (!UnblameableMethodPrefixes.Any(p => methodName.StartsWith(p)))
                        {
                            faultUtility.SetBucketParameter(P4ModuleNameDefaultIndex, method.DeclaringType.Assembly.GetName().Name);
                            faultUtility.SetBucketParameter(P5MethodNameDefaultIndex, methodName);
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        private static string GetDescription(Exception exception)
        {
            const string CodeAnalysisNamespace = nameof(Microsoft) + "." + nameof(CodeAnalysis);

            // Be resilient to failing here.  If we can't get a suitable name, just fall back to the standard name we
            // used to report.
            try
            {
                // walk up the stack looking for the first call from a type that isn't in the ErrorReporting namespace.
                var frames = new StackTrace(exception).GetFrames();

                // On the .NET Framework, GetFrames() can return null even though it's not documented as such.
                // At least one case here is if the exception's stack trace itself is null.
                if (frames != null)
                {
                    foreach (var frame in frames)
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
            }
            catch
            {
            }

            // If we couldn't get a stack, do this
            return exception.Message;
        }

        private static IList<string> CollectLogHubFilePaths()
        {
            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "VSLogs");
                var logs = CollectFilePaths(logPath, "*.svclog", shouldExcludeLogFile: (name) => !name.Contains("Roslyn") && !name.Contains("LSPClient"));
                return logs;
            }
            catch (Exception)
            {
                // ignore failures
            }

            return SpecializedCollections.EmptyList<string>();
        }

        private static IList<string> CollectServiceHubLogFilePaths()
        {
            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "servicehub", "logs");

                // TODO: https://github.com/dotnet/roslyn/issues/42582 
                // name our services more consistently to simplify filtering
                var logs = CollectFilePaths(logPath, "*.log", shouldExcludeLogFile: (name) => !name.Contains("-" + ServiceDescriptor.ServiceNameTopLevelPrefix) &&
                        !name.Contains("-CodeLens") &&
                        !name.Contains("-ManagedLanguage.IDE.RemoteHostClient") &&
                        !name.Contains("-hub"));
                return logs;
            }
            catch (Exception)
            {
                // ignore failures
            }

            return SpecializedCollections.EmptyList<string>();
        }

        private static List<string> CollectFilePaths(string logDirectoryPath, string logFileExtension, Func<string, bool> shouldExcludeLogFile)
        {
            var paths = new List<string>();

            if (!Directory.Exists(logDirectoryPath))
            {
                return paths;
            }

            // attach all log files that are modified less than 1 day before.
            var now = DateTime.UtcNow;
            var oneDay = TimeSpan.FromDays(1);

            foreach (var path in Directory.EnumerateFiles(logDirectoryPath, logFileExtension))
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(path);

                    // filter logs that are not relevant to Roslyn investigation
                    if (shouldExcludeLogFile(name))
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

            return paths;
        }
    }
}
