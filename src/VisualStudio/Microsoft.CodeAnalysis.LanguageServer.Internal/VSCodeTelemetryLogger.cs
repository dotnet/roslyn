// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
internal class VSCodeWorkspaceTelemetryService : ILspFaultLogger, ILogger
{
    private TelemetrySession? _telemetrySession;
    private const string CollectorApiKey = "0c6ae279ed8443289764825290e4f9e2-1a736e7c-1324-4338-be46-fc2a58ae4d14-7255";

    private static readonly ConcurrentDictionary<FunctionId, string> s_eventMap = new();
    private static readonly ConcurrentDictionary<(FunctionId id, string name), string> s_propertyMap = new();

    private readonly ConcurrentDictionary<int, object> _pendingScopes = new(concurrencyLevel: 2, capacity: 10);

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

        var currentLogger = Logger.GetLogger();
        if (currentLogger is null)
        {
            Logger.SetLogger(this);
        }
        else
        {
            Logger.SetLogger(AggregateLogger.Create(currentLogger, this));
        }
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

    public bool IsEnabled(FunctionId functionId)
        => _telemetrySession?.IsOptedIn ?? false;

    public void Log(FunctionId functionId, LogMessage logMessage)
    {
        var telemetryEvent = new TelemetryEvent(GetEventName(functionId));
        SetProperties(telemetryEvent, functionId, logMessage, delta: null);

        try
        {
            PostEvent(telemetryEvent);
        }
        catch
        {
        }
    }

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int blockId, CancellationToken cancellationToken)
    {
        var eventName = GetEventName(functionId);
        var kind = GetKind(logMessage);

        try
        {
            _pendingScopes[blockId] = Start(eventName, kind);
        }
        catch
        {
        }
    }

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int blockId, int delta, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(_pendingScopes.TryRemove(blockId, out var scope));

        var endEvent = GetEndEvent(scope);
        SetProperties(endEvent, functionId, logMessage, delta);

        var result = cancellationToken.IsCancellationRequested ? TelemetryResult.UserCancel : TelemetryResult.Success;

        try
        {
            End(scope, result);
        }
        catch
        {
        }
    }

    private object Start(string eventName, LogType type)
        => type switch
        {
            LogType.Trace => _telemetrySession.StartOperation(eventName),
            LogType.UserAction => _telemetrySession.StartUserTask(eventName),
            _ => throw ExceptionUtilities.UnexpectedValue(type),
        };

    private static void End(object scope, TelemetryResult result)
    {
        if (scope is TelemetryScope<OperationEvent> operation)
            operation.End(result);
        else if (scope is TelemetryScope<UserTaskEvent> userTask)
            userTask.End(result);
        else
            throw ExceptionUtilities.UnexpectedValue(scope);
    }

    private static TelemetryEvent GetEndEvent(object scope)
        => scope switch
        {
            TelemetryScope<OperationEvent> operation => operation.EndEvent,
            TelemetryScope<UserTaskEvent> userTask => userTask.EndEvent,
            _ => throw ExceptionUtilities.UnexpectedValue(scope)
        };

    private void PostEvent(TelemetryEvent telemetryEvent)
        => _telemetrySession?.PostEvent(telemetryEvent);

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

    private static void SetProperties(TelemetryEvent telemetryEvent, FunctionId functionId, LogMessage logMessage, int? delta)
    {
        if (logMessage is KeyValueLogMessage kvLogMessage)
        {
            AppendProperties(telemetryEvent, functionId, kvLogMessage);
        }
        else
        {
            var message = logMessage.GetMessage();
            if (!string.IsNullOrWhiteSpace(message))
            {
                var propertyName = GetPropertyName(functionId, "Message");
                telemetryEvent.Properties.Add(propertyName, message);
            }
        }

        if (delta.HasValue)
        {
            var deltaPropertyName = GetPropertyName(functionId, "Delta");
            telemetryEvent.Properties.Add(deltaPropertyName, delta.Value);
        }
    }

    private static void AppendProperties(TelemetryEvent telemetryEvent, FunctionId functionId, KeyValueLogMessage logMessage)
    {
        foreach (var (name, value) in logMessage.Properties)
        {
            // call SetProperty. VS telemetry will take care of finding correct
            // API based on given object type for us.
            // 
            // numeric data will show up in ES with measurement prefix.

            telemetryEvent.Properties.Add(GetPropertyName(functionId, name), value switch
            {
                PiiValue pii => new TelemetryPiiProperty(pii.Value),
                IEnumerable<object> items => new TelemetryComplexProperty(items.Select(item => (item is PiiValue pii) ? new TelemetryPiiProperty(pii.Value) : item)),
                _ => value
            });
        }
    }

    private const string EventPrefix = "vs/ide/vbcs/";
    private const string PropertyPrefix = "vs.ide.vbcs.";

    private static string GetEventName(FunctionId id)
        => s_eventMap.GetOrAdd(id, id => EventPrefix + GetTelemetryName(id, separator: '/'));

    private static string GetPropertyName(FunctionId id, string name)
        => s_propertyMap.GetOrAdd((id, name), key => PropertyPrefix + GetTelemetryName(id, separator: '.') + "." + key.name.ToLowerInvariant());

    private static string GetTelemetryName(FunctionId id, char separator)
            => Enum.GetName(typeof(FunctionId), id)!.Replace('_', separator).ToLowerInvariant();

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

    private static LogType GetKind(LogMessage logMessage)
            => logMessage is KeyValueLogMessage kvLogMessage
                                ? kvLogMessage.Kind
                                : logMessage.LogLevel switch
                                {
                                    >= LogLevel.Information => LogType.UserAction,
                                    _ => LogType.Trace
                                };
}
