// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using static Microsoft.VisualStudio.Razor.Telemetry.AggregatingTelemetryLog;
using TelemetryResult = Microsoft.CodeAnalysis.Razor.Telemetry.TelemetryResult;

namespace Microsoft.VisualStudio.Razor.Telemetry;

internal abstract partial class TelemetryReporter : ITelemetryReporter, IDisposable
{
    private const string CodeAnalysisNamespace = $"{nameof(Microsoft)}.{nameof(CodeAnalysis)}.";
    private const string AspNetCoreNamespace = $"{nameof(Microsoft)}.{nameof(AspNetCore)}.";
    private const string MicrosoftVSRazorNamespace = $"{nameof(Microsoft)}.{nameof(VisualStudio)}.{nameof(Razor)}.";

    // Types that will not contribute to fault bucketing. Fully qualified name is
    // required in order to match correctly.
    private static readonly FrozenSet<string> s_faultIgnoredTypeNames = new string[] {
        typeof(Assumed).FullName.AssumeNotNull(),
        typeof(NullableExtensions).FullName.AssumeNotNull(),
        typeof(ThrowHelper).FullName.AssumeNotNull()
    }.ToFrozenSet();

    private TelemetrySessionManager? _manager;

    protected TelemetryReporter(TelemetrySession? telemetrySession = null)
    {
        if (telemetrySession is not null)
        {
            SetSession(telemetrySession);
        }
    }

    public void Dispose()
    {
        _manager?.Dispose();
    }

    internal static string GetEventName(string name) => "dotnet/razor/" + name;
    internal static string GetPropertyName(string name) => "dotnet.razor." + name;

#if DEBUG
    public virtual bool IsEnabled => true;
#else
    public virtual bool IsEnabled => _manager?.Session.IsOptedIn ?? false;
#endif

    public void ReportEvent(string name, Severity severity)
    {
        var telemetryEvent = new TelemetryEvent(GetEventName(name), ConvertSeverity(severity));

        Report(telemetryEvent);
    }

    public void ReportEvent(string name, Severity severity, Property property)
    {
        var telemetryEvent = new TelemetryEvent(GetEventName(name), ConvertSeverity(severity));

        AddToProperties(telemetryEvent.Properties, property);
        Report(telemetryEvent);
    }

    public void ReportEvent(string name, Severity severity, Property property1, Property property2)
    {
        var telemetryEvent = new TelemetryEvent(GetEventName(name), ConvertSeverity(severity));

        AddToProperties(telemetryEvent.Properties, property1);
        AddToProperties(telemetryEvent.Properties, property2);
        Report(telemetryEvent);
    }

    public void ReportEvent(string name, Severity severity, Property property1, Property property2, Property property3)
    {
        var telemetryEvent = new TelemetryEvent(GetEventName(name), ConvertSeverity(severity));

        AddToProperties(telemetryEvent.Properties, property1);
        AddToProperties(telemetryEvent.Properties, property2);
        AddToProperties(telemetryEvent.Properties, property3);
        Report(telemetryEvent);
    }

    public void ReportEvent(string name, Severity severity, params ReadOnlySpan<Property> properties)
    {
        var telemetryEvent = new TelemetryEvent(GetEventName(name), ConvertSeverity(severity));

        foreach (var property in properties)
        {
            AddToProperties(telemetryEvent.Properties, property);
        }

        Report(telemetryEvent);
    }

    internal static void AddToProperties(IDictionary<string, object?> properties, Property property)
    {
        if (IsComplexValue(property.Value))
        {
            properties.Add(GetPropertyName(property.Name), new TelemetryComplexProperty(property.Value));
        }
        else
        {
            properties.Add(GetPropertyName(property.Name), property.Value);
        }

        static bool IsComplexValue(object? o)
        {
            return o?.GetType() is Type type && Type.GetTypeCode(type) == TypeCode.Object;
        }
    }

    public void ReportFault(Exception exception, string? message, params object?[] @params)
    {
        try
        {
            if (exception is OperationCanceledException oce)
            {
                // We don't want to report operation canceled, but don't want to miss out if there is something useful inside it
                if (oce.InnerException is not null)
                {
                    ReportFault(oce.InnerException, message, @params);
                }

                return;
            }

            if (exception is AggregateException aggregateException)
            {
                // We (potentially) have multiple exceptions; let's just report each of them
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                {
                    ReportFault(innerException, message, @params);
                }

                return;
            }

            if (HandleException(exception, message, @params))
            {
                return;
            }

            var currentProcess = Process.GetCurrentProcess();

            var faultEvent = new FaultEvent(
                eventName: GetEventName("fault"),
                description: (message is null ? string.Empty : message + ": ") + GetExceptionDetails(exception),
                FaultSeverity.General,
                exceptionObject: exception,
                gatherEventDetails: faultUtility =>
                {
                    if (message is not null)
                    {
                        faultUtility.AddErrorInformation(message);
                    }

                    foreach (var data in @params)
                    {
                        if (data is null)
                        {
                            continue;
                        }

                        faultUtility.AddErrorInformation(data.ToString());
                    }

                    // Returning "0" signals that, if sampled, we should send data to Watson.
                    // Any other value will cancel the Watson report. We never want to trigger a process dump manually,
                    // we'll let TargetedNotifications determine if a dump should be collected.
                    // See https://aka.ms/roslynnfwdocs for more details
                    return 0;
                });

            var (moduleName, methodName) = GetModifiedFaultParameters(exception);
            faultEvent.SetFailureParameters(
                failureParameter1: moduleName,
                failureParameter2: methodName);

            Report(faultEvent);
        }
        catch (Exception)
        {
        }
    }

    public virtual void ReportMetric(TelemetryInstrumentEvent metricEvent)
    {
        try
        {
#if !DEBUG
            _manager?.Session.PostMetricEvent(metricEvent);
#else
            // In debug we only log to normal logging. This makes it much easier to add and debug telemetry events
            // before we're ready to send them to the cloud
            LogTelemetry(metricEvent.Event);
#endif
        }
        catch (Exception e)
        {
            // No need to do anything here. We failed to report telemetry
            // which isn't good, but not catastrophic for a user
            LogError(e, "Failed logging telemetry event");
        }
    }

    protected void SetSession(TelemetrySession session)
    {
        _manager?.Dispose();
        _manager = TelemetrySessionManager.Create(this, session);
    }

    protected virtual void Report(TelemetryEvent telemetryEvent)
    {
        try
        {
#if !DEBUG
            _manager?.Session.PostEvent(telemetryEvent);
#else
            LogTelemetry(telemetryEvent);
#endif
        }
        catch (Exception e)
        {
            // No need to do anything here. We failed to report telemetry
            // which isn't good, but not catastrophic for a user
            LogError(e, "Failed logging telemetry event");
        }
    }

    protected virtual bool HandleException(Exception exception, string? message, params ReadOnlySpan<object?> @params)
        => false;

    protected virtual void LogTrace(string message)
    {
    }

    protected virtual void LogError(Exception exception, string message)
    {
    }

    public TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport)
        => TelemetryScope.Create(this, name, severity, minTimeToReport);

    public TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, Property property)
        => TelemetryScope.Create(this, name, severity, minTimeToReport, property);

    public TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, Property property1, Property property2)
        => TelemetryScope.Create(this, name, severity, minTimeToReport, property1, property2);

    public TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, Property property1, Property property2, Property property3)
        => TelemetryScope.Create(this, name, severity, minTimeToReport, property1, property2, property3);

    public TelemetryScope BeginBlock(string name, Severity severity, TimeSpan minTimeToReport, params ReadOnlySpan<Property> properties)
        => TelemetryScope.Create(this, name, severity, minTimeToReport, properties);

    public TelemetryScope TrackLspRequest(string lspMethodName, string languageServerName, TimeSpan minTimeToReport, Guid correlationId)
    {
        if (correlationId == Guid.Empty)
        {
            return TelemetryScope.Null;
        }

        return BeginBlock("TrackLspRequest", Severity.Normal,
            minTimeToReport,
            new("eventscope.method", lspMethodName),
            new("eventscope.languageservername", languageServerName),
            new("eventscope.correlationid", correlationId));
    }

    public void ReportRequestTiming(string name, string? language, TimeSpan queuedDuration, TimeSpan requestDuration, TelemetryResult result)
    {
        _manager?.LogRequestTelemetry(
            name,
            language,
            queuedDuration,
            requestDuration,
            result);
    }

#if DEBUG
    private void LogTelemetry(TelemetryEvent telemetryEvent)
    {
        // In debug we only log to normal logging. This makes it much easier to add and debug telemetry events
        // before we're ready to send them to the cloud
        var name = telemetryEvent.Name;
        var propertiesString = GetPropertiesString(telemetryEvent.Properties);

        LogTrace($"Telemetry Event: {name} \n Properties: {propertiesString}\n");

        if (telemetryEvent is FaultEvent)
        {
            var telemetryEventType = telemetryEvent.GetType();

            var description = telemetryEventType
                .GetProperty("Description", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(telemetryEvent, index: null);

            var exception = telemetryEventType
                .GetProperty("ExceptionObject", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(telemetryEvent, index: null);

            var message = $"Fault Event: {name} \n Exception Info: {exception ?? description} \n Properties: {propertiesString}";

            Debug.Fail(message);
        }

        static string GetPropertiesString(IDictionary<string, object> properties)
        {
            using var _ = AspNetCore.Razor.PooledObjects.StringBuilderPool.GetPooledObject(out var builder);

            var first = true;

            foreach (var (key, value) in properties)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(',');
                }

                builder.Append("[ ");
                builder.Append(key);
                builder.Append(':');
                builder.Append(value);
                builder.Append(" ]");
            }

            return builder.ToString();
        }
    }
#endif

    /// <summary>
    /// Returns values that should be set to (failureParameter1, failureParameter2) when reporting a fault.
    /// Those values represent the blamed stack frame module and method name.
    /// </summary>
    internal static (string? moduleName, string? methodName) GetModifiedFaultParameters(Exception exception)
    {
        if (!TryGetFirstRazorMethodOnCallStack(exception, SkipIfDeclaringTypeIsIgnored, out var method))
        {
            return (null, null);
        }

        var moduleName = Path.GetFileNameWithoutExtension(method.Module.Name);
        return (moduleName, method.Name);
    }

    private static string GetExceptionDetails(Exception exception)
    {
        if (!TryGetFirstRazorMethodOnCallStack(exception, shouldSkipMethod: null, out var method) ||
            !TryGetDeclaringTypeName(method, out var declaringTypeName))
        {
            return exception.Message;
        }

        return $"{declaringTypeName}.{method.Name}";
    }

    /// <summary>
    /// Gets the first stack frame in exception stack that originates from razor code based on namespace
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> containg the stack.</param>
    /// <param name="shouldSkipMethod">Optional predicate that determines whether a particular method should be skipped.</param>
    /// <param name="result">The result</param>
    private static bool TryGetFirstRazorMethodOnCallStack(
        Exception exception,
        Func<MethodBase, bool>? shouldSkipMethod,
        [NotNullWhen(true)] out MethodBase? result)
    {
        // Be resilient to failing here.  If we can't get a suitable name, just fall back to the standard name we
        // used to report.
        try
        {
            // walk up the stack looking for the first call from a type that isn't in the ErrorReporting namespace.
            var stackTrace = new StackTrace(exception);

            // On the .NET Framework, GetFrames() can return null even though it's not documented as such.
            // At least one case here is if the exception's stack trace itself is null.
            if (stackTrace.GetFrames() is { } frames)
            {
                foreach (var frame in frames)
                {
                    var method = frame?.GetMethod();

                    if (method is null || !IsInOwnedNamespace(method))
                    {
                        continue;
                    }

                    if (shouldSkipMethod is null || !shouldSkipMethod(method))
                    {
                        result = method;
                        return true;
                    }
                }
            }
        }
        catch
        {
        }

        result = null;
        return false;
    }

    private static bool TryGetDeclaringTypeName(MethodBase method, [NotNullWhen(true)] out string? declaringTypeName)
        => (declaringTypeName = method.DeclaringType?.FullName) is not null;

    private static TelemetrySeverity ConvertSeverity(Severity severity)
        => severity switch
        {
            Severity.Normal => TelemetrySeverity.Normal,
            Severity.Low => TelemetrySeverity.Low,
            Severity.High => TelemetrySeverity.High,
            _ => throw new InvalidOperationException($"Unknown severity: {severity}")
        };

    private static bool IsInOwnedNamespace(MethodBase method)
        => TryGetDeclaringTypeName(method, out var declaringTypeName) &&
           IsInOwnedNamespace(declaringTypeName);

    private static bool IsInOwnedNamespace(string declaringTypeName)
        => declaringTypeName.StartsWith(CodeAnalysisNamespace) ||
           declaringTypeName.StartsWith(AspNetCoreNamespace) ||
           declaringTypeName.StartsWith(MicrosoftVSRazorNamespace);

    private static bool SkipIfDeclaringTypeIsIgnored(MethodBase method)
        => TryGetDeclaringTypeName(method, out var declaringTypeName) &&
           s_faultIgnoredTypeNames.Contains(declaringTypeName);

    private sealed class TelemetrySessionManager : IDisposable
    {
        /// <summary>
        /// Store request counters in a concurrent dictionary as non-mutating LSP requests can
        /// run alongside other non-mutating requests.
        /// </summary>
        private readonly ConcurrentDictionary<(string Method, string? Language), Counter> _requestCounters = new();
        private readonly ITelemetryReporter _telemetryReporter;
        private readonly AggregatingTelemetryLogManager _aggregatingManager;
        private bool _isDisposed;

        private TelemetrySessionManager(ITelemetryReporter telemetryReporter, TelemetrySession session, AggregatingTelemetryLogManager aggregatingManager)
        {
            _telemetryReporter = telemetryReporter;
            _aggregatingManager = aggregatingManager;
            Session = session;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            Flush();
            if (!Session.IsDisposed)
            {
                try
                {
                    Session.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // The VS telemetry session's internal object may already be disposed
                    // by another MEF part during ExportProvider disposal. This is safe to ignore.
                }
            }
        }

        public static TelemetrySessionManager Create(TelemetryReporter telemetryReporter, TelemetrySession session)
            => new(
                telemetryReporter,
                session,
                new AggregatingTelemetryLogManager(telemetryReporter));

        public TelemetrySession Session { get; }

        private void Flush()
        {
            _aggregatingManager.Flush();
            LogRequestCounters();
        }

        public void LogRequestTelemetry(string name, string? language, TimeSpan queuedDuration, TimeSpan requestDuration, TelemetryResult result)
        {
            LogAggregated("LSP_TimeInQueue",
                "TimeInQueue",  // All time in queue events use the same histogram, no need for separate keys
                (int)queuedDuration.TotalMilliseconds,
                name);

            LogAggregated("LSP_RequestDuration",
                name, // RequestDuration requests are histogrammed by their unique name
                (int)requestDuration.TotalMilliseconds,
                name);

            _requestCounters.GetOrAdd((name, language), (_) => new Counter()).IncrementCount(result);
        }

        private void LogRequestCounters()
        {
            foreach (var kvp in _requestCounters)
            {
                _telemetryReporter.ReportEvent("LSP_RequestCounter",
                    Severity.Low,
                    new Property("method", kvp.Key.Method),
                    new Property("successful", kvp.Value.SucceededCount),
                    new Property("failed", kvp.Value.FailedCount),
                    new Property("cancelled", kvp.Value.CancelledCount));
            }

            _requestCounters.Clear();
        }

        private void LogAggregated(
            string managerKey,
            string histogramKey,
            int value,
            string method)
        {
            var aggregatingLog = _aggregatingManager?.GetLog(managerKey);
            aggregatingLog?.Log(histogramKey, value, method);
        }

        private sealed class Counter
        {
            private int _succeededCount;
            private int _failedCount;
            private int _cancelledCount;

            public int SucceededCount => _succeededCount;
            public int FailedCount => _failedCount;
            public int CancelledCount => _cancelledCount;

            public void IncrementCount(TelemetryResult result)
            {
                switch (result)
                {
                    case TelemetryResult.Succeeded:
                        Interlocked.Increment(ref _succeededCount);
                        break;
                    case TelemetryResult.Failed:
                        Interlocked.Increment(ref _failedCount);
                        break;
                    case TelemetryResult.Cancelled:
                        Interlocked.Increment(ref _cancelledCount);
                        break;
                }
            }
        }
    }
}
