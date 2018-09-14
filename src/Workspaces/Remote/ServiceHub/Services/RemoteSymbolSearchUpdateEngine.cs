// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteSymbolSearchUpdateEngine :
        ServiceHubServiceBase, IRemoteSymbolSearchUpdateEngine, ISymbolSearchLogService, ISymbolSearchProgressService
    {
        private readonly SymbolSearchUpdateEngine _updateEngine;

        public RemoteSymbolSearchUpdateEngine(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            _updateEngine = new SymbolSearchUpdateEngine(
                logService: this, progressService: this);

            Rpc.StartListening();
        }

        public Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory)
        {
            return RunServiceAsync(_ =>
            {
                return _updateEngine.UpdateContinuouslyAsync(sourceName, localSettingsDirectory);
            }, CancellationToken.None);
        }

        public Task<IList<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                var results = await _updateEngine.FindPackagesWithTypeAsync(
                    source, name, arity, token).ConfigureAwait(false);

                return (IList<PackageWithTypeResult>)results;
            }, cancellationToken);
        }

        public Task<IList<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                var results = await _updateEngine.FindPackagesWithAssemblyAsync(
                    source, assemblyName, token).ConfigureAwait(false);

                return (IList<PackageWithAssemblyResult>)results;
            }, cancellationToken);
        }

        public Task<IList<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                var results = await _updateEngine.FindReferenceAssembliesWithTypeAsync(
                    name, arity, token).ConfigureAwait(false);

                return (IList<ReferenceAssemblyWithTypeResult>)results;
            }, cancellationToken);
        }

        #region Messages to forward from here to VS

        public Task LogExceptionAsync(string exception, string text)
            => this.Rpc.InvokeAsync(nameof(LogExceptionAsync), exception, text);

        public Task LogInfoAsync(string text)
            => this.Rpc.InvokeAsync(nameof(LogInfoAsync), text);

        public Task OnDownloadFullDatabaseStartedAsync(string title)
            => this.Rpc.InvokeAsync(nameof(OnDownloadFullDatabaseStartedAsync), title);

        public Task OnDownloadFullDatabaseSucceededAsync()
            => this.Rpc.InvokeAsync(nameof(OnDownloadFullDatabaseSucceededAsync));

        public Task OnDownloadFullDatabaseCanceledAsync()
            => this.Rpc.InvokeAsync(nameof(OnDownloadFullDatabaseCanceledAsync));

        public Task OnDownloadFullDatabaseFailedAsync(string message)
            => this.Rpc.InvokeAsync(nameof(OnDownloadFullDatabaseFailedAsync), message);

        #endregion
    }
}
