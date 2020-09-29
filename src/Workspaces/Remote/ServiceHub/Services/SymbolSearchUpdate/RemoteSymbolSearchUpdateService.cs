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
        internal sealed class Factory : FactoryBase<IRemoteSymbolSearchUpdateService, ISymbolSearchLogService>
        {
            protected override IRemoteSymbolSearchUpdateService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<ISymbolSearchLogService> callback)
                => new RemoteSymbolSearchUpdateService(arguments, callback);
        }

        private sealed class LogService : ISymbolSearchLogService
        {
            private readonly RemoteCallback<ISymbolSearchLogService> _callback;

            public LogService(RemoteCallback<ISymbolSearchLogService> callback)
                => _callback = callback;

            public ValueTask LogExceptionAsync(string exception, string text, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.LogExceptionAsync(exception, text, cancellationToken), cancellationToken);

            public ValueTask LogInfoAsync(string text, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.LogInfoAsync(text, cancellationToken), cancellationToken);
        }

        private readonly ISymbolSearchUpdateEngine _updateEngine;

        public RemoteSymbolSearchUpdateService(in ServiceConstructionArguments arguments, RemoteCallback<ISymbolSearchLogService> callback)
            : base(arguments)
        {
            _updateEngine = SymbolSearchUpdateEngineFactory.CreateEngineInProcess(new LogService(callback));
        }

        public ValueTask UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
                _updateEngine.UpdateContinuouslyAsync(sourceName, localSettingsDirectory, cancellationToken),
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
