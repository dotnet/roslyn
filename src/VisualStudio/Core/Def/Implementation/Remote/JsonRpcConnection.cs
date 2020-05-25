// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal class JsonRpcConnection : RemoteHostClient.Connection
    {
        private readonly HostWorkspaceServices _services;

        // communication channel related to service information
        private readonly RemoteEndPoint _serviceEndPoint;

        public JsonRpcConnection(
            HostWorkspaceServices services,
            TraceSource logger,
            object? callbackTarget,
            Stream serviceStream)
        {
            _services = services;
            _serviceEndPoint = new RemoteEndPoint(serviceStream, logger, callbackTarget);
            _serviceEndPoint.UnexpectedExceptionThrown += UnexpectedExceptionThrown;
            _serviceEndPoint.StartListening();
        }

        private void UnexpectedExceptionThrown(Exception exception)
            => RemoteHostCrashInfoBar.ShowInfoBar(_services, exception);

        public override Task InvokeAsync(string targetName, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
            => _serviceEndPoint.InvokeAsync(targetName, arguments, cancellationToken);

        public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
            => _serviceEndPoint.InvokeAsync<T>(targetName, arguments, cancellationToken);

        public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object?> arguments, Func<Stream, CancellationToken, Task<T>> dataReader, CancellationToken cancellationToken)
            => _serviceEndPoint.InvokeAsync(targetName, arguments, dataReader, cancellationToken);

        protected override void DisposeImpl()
        {
            // dispose service and snapshot channels
            _serviceEndPoint.UnexpectedExceptionThrown -= UnexpectedExceptionThrown;
            _serviceEndPoint.Dispose();

            base.DisposeImpl();
        }
    }
}
