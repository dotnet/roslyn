// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Kinds;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;
using System;
using System.Threading;
using static Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.LoggerHelpers;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging;

public partial class Logger
{
    private readonly TelemetrySessionAggregator _session;

    private Logger(TelemetrySessionAggregator session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    private static Lazy<Logger> _lazyInstance = new(() => new Logger(new TelemetrySessionAggregator(TelemetryService.DefaultSession)));

    public static Logger GetLogger()
    {
        return _lazyInstance.Value;
    }

    public static IDisposable LogUserAction(UserTask userTaskId, CancellationToken token = default)
    {
        return GetLogger().LogUserActionImpl(userTaskId, EmptyLogMessage.Instance, token);
    }

    public static IDisposable LogUserAction<T>(UserTask userTaskId, ILogMessage<T> message, CancellationToken token = default) where T : ILogMessageData
    {
        return GetLogger().LogUserActionImpl(userTaskId, message, token);
    }

    public static IDisposable LogOperation(OperationId operationId, CancellationToken token = default)
    {
        return GetLogger().LogOperationImpl(operationId, EmptyLogMessage.Instance, token);
    }

    public static IDisposable LogOperation<T>(OperationId operationId, ILogMessage<T> message, CancellationToken token = default) where T : ILogMessageData
    {
        return GetLogger().LogOperationImpl(operationId, message, token);
    }

    public static void LogEvent(EventId eventId)
    {
        LogEvent(eventId, EmptyLogMessage.Instance);
    }

    public static void LogEvent(EventId eventId, bool value)
    {
        LogEvent(eventId, new SingleLogMessage<bool>(value));
    }

    public static void LogEvent(EventId eventId, string value)
    {
        LogEvent(eventId, new SingleLogMessage<string>(value));
    }

    internal static void LogEvents(EventId eventId, OptionsInfo messages)
    {
        GetLogger().LogEventsImpl(eventId, messages);
    }

    public static void LogEvent<T>(EventId eventId, ILogMessage<T> message) where T : ILogMessageData
    {
        GetLogger().LogEventImpl(eventId, message);
    }

    public static void LogException(Exception ex, string description = null)
    {
        GetLogger().LogExceptionImpl(ex, description);
    }

    public static void Assert(bool condition, string description)
    {
        GetLogger().LogFaultIfFalse(condition, description);
    }

    private IDisposable LogUserActionImpl<T>(UserTask userTaskId, ILogMessage<T> message, CancellationToken token = default) where T : ILogMessageData
    {
        if (_session.IsOptedIn)
        {
            var scope = _session.StartUserTask(GetUserTaskName(userTaskId));
            return new UserTaskDisposable<T>(_session, scope, userTaskId, message, token);
        }

        return EmptyDisposable.Instance;
    }

    private IDisposable LogOperationImpl<T>(OperationId operationId, ILogMessage<T> message, CancellationToken token = default) where T : ILogMessageData
    {
        if (_session.IsOptedIn)
        {
            var scope = _session.StartOperation(GetOperationName(operationId));
            return new OperationDisposable<T>(_session, scope, operationId, message, token);
        }

        return EmptyDisposable.Instance;
    }

    private void LogEventsImpl(EventId eventId, OptionsInfo messages)
    {
        if (_session.IsOptedIn)
        {
            foreach (var message in messages)
            {
                var telemetryEvent = new TelemetryEvent(GetEventName(eventId));
                SetProperties(telemetryEvent, eventId, message);
                _session.PostEvent(telemetryEvent);
            }
        }
    }

    private void LogEventImpl<T>(EventId eventId, ILogMessage<T> message) where T : ILogMessageData
    {
        if (_session.IsOptedIn)
        {
            var telemetryEvent = new TelemetryEvent(GetEventName(eventId));
            SetProperties(telemetryEvent, eventId, message);
            _session.PostEvent(telemetryEvent);
        }
    }

    private void LogExceptionImpl(Exception ex, string description = null)
    {
        description ??= ex.Message;
        _session.PostFault(ExceptionName, description, FaultSeverity.General, ex);
    }

    private void LogFaultIfFalse(bool condition, string description)
    {
        if (!condition)
        {
            _session.PostFault(ErrorName, description);
        }
    }
}
