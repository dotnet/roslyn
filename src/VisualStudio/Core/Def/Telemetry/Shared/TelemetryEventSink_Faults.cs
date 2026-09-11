// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.Telemetry;

internal abstract partial class TelemetryEventSink
{
    /// <summary>
    /// We can no longer use the common fault description property as it has to be suppressed due to poisoned data in past releases.
    /// This means that prism will no longer show the fault description either.  We'll store the clean description in a custom
    /// property so we can access it manually if needed.
    /// </summary>
    private const string CustomFaultDescriptionPropertyName = "roslyn.fault.description";

    private static int s_dumpsSubmitted;

    public static bool IncludeServiceHubLogFiles = true;

    /// <summary>
    /// The bucket parameter for the blamed module.
    /// </summary>
    private const int P4ModuleNameDefaultIndex = 4;

    /// <summary>
    /// The bucket parameter for the blamed method.
    /// </summary>
    private const int P5MethodNameDefaultIndex = 5;

    private static readonly ImmutableArray<string> UnblameableMethodPrefixes =
    [
        // If GetRequiredDocument or similar throw, the real fault is the caller which is expecting the document but won't get it
        typeof(Shared.Extensions.ISolutionExtensions).FullName + ".GetRequired",

        // If GetRequiredService throws, the real fault is the caller which is expecting the service but won't get it
        typeof(Host.HostLanguageServices).FullName + "." + nameof(Host.HostLanguageServices.GetRequiredService),

        // Most of the members on RequestContext throw if the LSP request didn't carry a document or solution like the handler expected;
        // this should be blamed on the caller since that way we can distinguish the different handlers
        typeof(LanguageServer.Handler.RequestContext).FullName + ".",

        // Ignore everything from our Contract.Throw* helpers
        typeof(Contract).FullName + ".",

        // Ignore common framework helpers
        "System.Collections.Immutable.",
        "System.Linq.",
        "System.Runtime.CompilerServices.",
        "System.ThrowHelper.",

        // Ignore anything from our text span helpers, since any mixup of positions would be the caller's mistake
        typeof(Text.LinePosition).FullName + ".",
        typeof(Text.TextSpan).FullName + ".",
    ];

    public void ReportFault(Exception exception, ErrorSeverity severity, bool forceDump)
    {
        if (IsEnabled(FunctionId.NonFatalWatson))
            PostEvent(CreateFaultEvent(exception, severity, forceDump));
    }

    private static FaultEvent CreateFaultEvent(Exception exception, ErrorSeverity severity, bool forceDump)
    {
        var currentProcess = Process.GetCurrentProcess();
        var description = GetDescription(exception);
        var faultEvent = new FaultEvent(
            eventName: TelemetryNaming.GetEventName(FunctionId.NonFatalWatson),
            description: description,
            ConvertSeverity(severity),
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
                    if (IncludeServiceHubLogFiles)
                    {
                        foreach (var path in CollectServiceHubLogFilePaths())
                        {
                            faultUtility.AddFile(path);
                        }

                        foreach (var loghubPath in CollectLogHubFilePaths())
                        {
                            faultUtility.AddFile(loghubPath);
                        }
                    }
                }

                // Returning "0" signals that, if sampled, we should send data to Watson.
                // Any other value will cancel the Watson report. We never want to trigger a process dump manually,
                // we'll let TargetedNotifications determine if a dump should be collected.
                // See https://aka.ms/roslynnfwdocs for more details
                return 0;
            });

        faultEvent.Properties[CustomFaultDescriptionPropertyName] = description;
        return faultEvent;
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

        // If we couldn't get a stack, report a generic message.
        // The exception message is already reported in a separate cred-scanned property.
        return "Roslyn NonFatal Watson";
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

        return [];
    }

    private static IList<string> CollectServiceHubLogFilePaths()
    {
        try
        {
            var logPath = Path.Combine(Path.GetTempPath(), "servicehub", "logs");

            // Name our services more consistently to simplify filtering: https://github.com/dotnet/roslyn/issues/42582
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

        return [];
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
