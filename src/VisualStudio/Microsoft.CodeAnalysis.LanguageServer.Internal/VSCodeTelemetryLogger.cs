// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Internal;

[Export(typeof(ILspFaultLogger)), Shared]
internal class VSCodeWorkspaceTelemetryService : ILspFaultLogger
{
    private TelemetrySession? _telemetrySession;
    private const string CollectorApiKey = "0c6ae279ed8443289764825290e4f9e2-1a736e7c-1324-4338-be46-fc2a58ae4d14-7255";

    private static readonly ConcurrentDictionary<FunctionId, string> s_eventMap = new();
    private static readonly ConcurrentDictionary<(FunctionId id, string name), string> s_propertyMap = new();

    private int _dumpsSubmitted = 0;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSCodeWorkspaceTelemetryService()
    {
    }

    public void Initialize()
    {
        _telemetrySession = TelemetryService.CreateAndGetDefaultSession(CollectorApiKey);
        _telemetrySession.Start();

        FatalError.Handler = (exception, severity, forceDump) => ReportFault(exception, ConvertSeverity(severity), forceDump);
        FatalError.CopyHandlerTo(typeof(Compilation).Assembly);
    }

    public void ReportFault(Exception exception, LogLevel logLevel, bool forceDump)
    {
        Assumes.NotNull(_telemetrySession);

        try
        {
            if (exception is OperationCanceledException { InnerException: { } oceInnerException })
            {
                ReportFault(oceInnerException, logLevel, forceDump);
                return;
            }

            if (exception is AggregateException aggregateException)
            {
                // We (potentially) have multiple exceptions; let's just report each of them
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                    ReportFault(innerException, logLevel, forceDump);

                return;
            }

            var currentProcess = Process.GetCurrentProcess();
            var faultEvent = new FaultEvent(
                eventName: "vs/ide/vbcs/nonfatalwatson",
                description: GetDescription(exception),
                ConvertToFaultSeverity(logLevel),
                exceptionObject: exception,
                gatherEventDetails: faultUtility =>
                {
                    if (forceDump)
                    {
                        // Let's just send a maximum of three; number chosen arbitrarily
                        if (Interlocked.Increment(ref _dumpsSubmitted) <= 3)
                            faultUtility.AddProcessDump(currentProcess.Id);
                    }

                    if (faultUtility is FaultEvent { IsIncludedInWatsonSample: true })
                    {
                        // if needed, add any extra logs here
                    }

                    // Returning "0" signals that, if sampled, we should send data to Watson. 
                    // Any other value will cancel the Watson report. We never want to trigger a process dump manually, 
                    // we'll let TargetedNotifications determine if a dump should be collected.
                    // See https://aka.ms/roslynnfwdocs for more details
                    return 0;
                });

            _telemetrySession.PostEvent(faultEvent);
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

    private static LogLevel ConvertSeverity(ErrorSeverity severity)
    => severity switch
    {
        ErrorSeverity.Uncategorized => LogLevel.None,
        ErrorSeverity.Diagnostic => LogLevel.Debug,
        ErrorSeverity.General => LogLevel.Information,
        ErrorSeverity.Critical => LogLevel.Critical,
        _ => LogLevel.None
    };

    private static FaultSeverity ConvertToFaultSeverity(LogLevel logLevel)
        => logLevel switch
        {
            > LogLevel.Error => FaultSeverity.Critical,
            > LogLevel.Information => FaultSeverity.General,
            _ => FaultSeverity.Diagnostic
        };
}
