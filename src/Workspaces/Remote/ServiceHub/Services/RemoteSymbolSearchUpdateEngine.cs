// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        public RemoteSymbolSearchUpdateEngine(
            Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            _updateEngine = new SymbolSearchUpdateEngine(
                logService: this, progressService: this);

            StartService();
        }

        public Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory)
        {
            return RunServiceAsync(() =>
            {
                return _updateEngine.UpdateContinuouslyAsync(sourceName, localSettingsDirectory);
            }, CancellationToken.None);
        }

        public Task<IList<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var results = await _updateEngine.FindPackagesWithTypeAsync(
                    source, name, arity, cancellationToken).ConfigureAwait(false);

                return (IList<PackageWithTypeResult>)results;
            }, cancellationToken);
        }

        public Task<IList<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var results = await _updateEngine.FindPackagesWithAssemblyAsync(
                    source, assemblyName, cancellationToken).ConfigureAwait(false);

                return (IList<PackageWithAssemblyResult>)results;
            }, cancellationToken);
        }

        public Task<IList<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var results = await _updateEngine.FindReferenceAssembliesWithTypeAsync(
                    name, arity, cancellationToken).ConfigureAwait(false);

                return (IList<ReferenceAssemblyWithTypeResult>)results;
            }, cancellationToken);
        }

        #region Messages to forward from here to VS

        public Task LogExceptionAsync(string exception, string text)
            => this.InvokeAsync(nameof(LogExceptionAsync), new object[] { exception, text }, CancellationToken.None);

        public Task LogInfoAsync(string text)
            => this.InvokeAsync(nameof(LogInfoAsync), new object[] { text }, CancellationToken.None);

        public Task OnDownloadFullDatabaseStartedAsync(string title)
            => this.InvokeAsync(nameof(OnDownloadFullDatabaseStartedAsync), new object[] { title }, CancellationToken.None);

        public Task OnDownloadFullDatabaseSucceededAsync()
            => this.InvokeAsync(nameof(OnDownloadFullDatabaseSucceededAsync), CancellationToken.None);

        public Task OnDownloadFullDatabaseCanceledAsync()
            => this.InvokeAsync(nameof(OnDownloadFullDatabaseCanceledAsync), CancellationToken.None);

        public Task OnDownloadFullDatabaseFailedAsync(string message)
            => this.InvokeAsync(nameof(OnDownloadFullDatabaseFailedAsync), new object[] { message }, CancellationToken.None);

        #endregion
    }
}
