// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteSymbolSearchUpdateService : BrokeredServiceBase, IRemoteSymbolSearchUpdateService
    {
        internal sealed class Factory : FactoryBase<IRemoteSymbolSearchUpdateService, IRemoteSymbolSearchUpdateService.ICallback>
        {
            protected override IRemoteSymbolSearchUpdateService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteSymbolSearchUpdateService.ICallback> callback)
                => new RemoteSymbolSearchUpdateService(arguments, callback);
        }

        private sealed class LogService : ISymbolSearchLogService
        {
            private readonly RemoteCallback<IRemoteSymbolSearchUpdateService.ICallback> _callback;
            private readonly RemoteServiceCallbackId _callbackId;

            public LogService(RemoteCallback<IRemoteSymbolSearchUpdateService.ICallback> callback, RemoteServiceCallbackId callbackId)
            {
                _callback = callback;
                _callbackId = callbackId;
            }

            public ValueTask LogExceptionAsync(string exception, string text, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.LogExceptionAsync(_callbackId, exception, text, cancellationToken), cancellationToken);

            public ValueTask LogInfoAsync(string text, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.LogInfoAsync(_callbackId, text, cancellationToken), cancellationToken);
        }

        private readonly ISymbolSearchUpdateEngine _updateEngine;
        private readonly RemoteCallback<IRemoteSymbolSearchUpdateService.ICallback> _callback;

        public RemoteSymbolSearchUpdateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteSymbolSearchUpdateService.ICallback> callback)
            : base(arguments)
        {
            _updateEngine = SymbolSearchUpdateEngineFactory.CreateEngineInProcess();
            _callback = callback;
        }

        public ValueTask UpdateContinuouslyAsync(RemoteServiceCallbackId callbackId, string sourceName, string localSettingsDirectory, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
                _updateEngine.UpdateContinuouslyAsync(sourceName, localSettingsDirectory, new LogService(_callback, callbackId), cancellationToken),
                cancellationToken);
        }

        public ValueTask<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
                _updateEngine.FindPackagesWithTypeAsync(source, name, arity, cancellationToken),
                cancellationToken);
        }

        public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancallationToken =>
                _updateEngine.FindPackagesWithAssemblyAsync(source, assemblyName, cancellationToken),
                cancellationToken);
        }

        public ValueTask<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancallationToken =>
                _updateEngine.FindReferenceAssembliesWithTypeAsync(name, arity, cancellationToken),
                cancellationToken);
        }
    }
}
