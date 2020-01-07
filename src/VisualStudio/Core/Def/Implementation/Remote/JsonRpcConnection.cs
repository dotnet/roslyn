﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal class JsonRpcConnection : RemoteHostClient.Connection
    {
        private readonly Workspace _workspace;

        // communication channel related to service information
        private readonly RemoteEndPoint _serviceEndPoint;

        // communication channel related to snapshot information
        private readonly ReferenceCountedDisposable<RemotableDataJsonRpc> _remoteDataRpc;

        public JsonRpcConnection(
            Workspace workspace,
            TraceSource logger,
            object? callbackTarget,
            Stream serviceStream,
            ReferenceCountedDisposable<RemotableDataJsonRpc> dataRpc)
        {
            Contract.ThrowIfNull(dataRpc);

            _workspace = workspace;
            _remoteDataRpc = dataRpc;
            _serviceEndPoint = new RemoteEndPoint(serviceStream, logger, callbackTarget);
            _serviceEndPoint.UnexpectedExceptionThrown += UnexpectedExceptionThrown;
            _serviceEndPoint.StartListening();
        }

        private void UnexpectedExceptionThrown(Exception exception)
            => RemoteHostCrashInfoBar.ShowInfoBar(_workspace, exception);

        public override Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => _serviceEndPoint.InvokeAsync(targetName, arguments, cancellationToken);

        public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => _serviceEndPoint.InvokeAsync<T>(targetName, arguments, cancellationToken);

        public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> directStreamReader, CancellationToken cancellationToken)
            => _serviceEndPoint.InvokeAsync(targetName, arguments, directStreamReader, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose service and snapshot channels
                _serviceEndPoint.UnexpectedExceptionThrown -= UnexpectedExceptionThrown;
                _serviceEndPoint.Dispose();
                _remoteDataRpc.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
