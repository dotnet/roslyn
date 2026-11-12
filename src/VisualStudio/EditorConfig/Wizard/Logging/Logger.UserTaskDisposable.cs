// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Kinds;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;
using static Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.LoggerHelpers;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging;

public partial class Logger
{
    private class UserTaskDisposable<T> : IDisposable where T : ILogMessageData
    {
        private readonly TelemetrySessionAggregator _session;
        private readonly TelemetryScope<UserTaskEvent> _userTask;
        private readonly UserTask _userTaskId;
        private readonly ILogMessage<T> _message;
        private readonly CancellationToken _token;

        public UserTaskDisposable(TelemetrySessionAggregator session, TelemetryScope<UserTaskEvent> scope, UserTask userTaskId, ILogMessage<T> message, CancellationToken token)
        {
            _session = session;
            _userTask = scope;
            _userTaskId = userTaskId;
            _message = message;
            _token = token;
        }

        public void Dispose()
        {
            SetProperties(_userTask.EndEvent, _userTaskId, _message);
            _session.EndUserTask(_userTask, _token.IsCancellationRequested ? TelemetryResult.UserCancel : TelemetryResult.Success);
        }
    }
}
