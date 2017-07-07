﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteSymbolSearchUpdateEngine : ServiceHubServiceBase, IRemoteSymbolSearchUpdateEngine
    {
        private readonly SymbolSearchUpdateEngine _updateEngine;

        public RemoteSymbolSearchUpdateEngine(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            _updateEngine = new SymbolSearchUpdateEngine(
                new LogService(this), updateCancellationToken: ShutdownCancellationToken);

            Rpc.StartListening();
        }

        public Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory)
        {
            return _updateEngine.UpdateContinuouslyAsync(sourceName, localSettingsDirectory);
        }

        public async Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
        {
            var results = await _updateEngine.FindPackagesWithTypeAsync(
                source, name, arity, cancellationToken).ConfigureAwait(false);

            return results;
        }

        public async Task<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
        {
            var results = await _updateEngine.FindPackagesWithAssemblyAsync(
                source, assemblyName, cancellationToken).ConfigureAwait(false);

            return results;
        }

        public async Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
        {
            var results = await _updateEngine.FindReferenceAssembliesWithTypeAsync(
                name, arity, cancellationToken).ConfigureAwait(false);

            return results;
        }

        private class LogService : ISymbolSearchLogService
        {
            private readonly RemoteSymbolSearchUpdateEngine _service;

            public LogService(RemoteSymbolSearchUpdateEngine service)
            {
                _service = service;
            }

            public Task LogExceptionAsync(string exception, string text)
                => _service.Rpc.InvokeAsync(nameof(LogExceptionAsync), exception, text);

            public Task LogInfoAsync(string text)
                => _service.Rpc.InvokeAsync(nameof(LogInfoAsync), text);
        }
    }
}
