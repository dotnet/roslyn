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
    private class OperationDisposable<T> : IDisposable where T : ILogMessageData
    {
        private readonly TelemetrySessionAggregator _session;
        private readonly TelemetryScope<OperationEvent> _operation;
        private readonly OperationId _operationId;
        private readonly ILogMessage<T> _message;
        private readonly CancellationToken _token;

        public OperationDisposable(TelemetrySessionAggregator session, TelemetryScope<OperationEvent> scope, OperationId operationId, ILogMessage<T> message, CancellationToken token)
        {
            _session = session;
            _operation = scope;
            _operationId = operationId;
            _message = message;
            _token = token;
        }

        public void Dispose()
        {
            SetProperties(_operation.EndEvent, _operationId, _message);
            _session.EndOperation(_operation, _token.IsCancellationRequested ? TelemetryResult.UserCancel : TelemetryResult.Success);
        }
    }
}
