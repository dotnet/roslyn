﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteSymbolSearchUpdateEngine : ServiceBase, IRemoteSymbolSearchUpdateEngine, ISymbolSearchLogService, ISymbolSearchProgressService
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
                // In non-test scenarios, we're not cancellable.  Our lifetime will simply be that
                // of the OOP process itself.  i.e. when it goes away, it will just tear down our
                // update-loop itself.  So we don't need any additional controls over it.
                return _updateEngine.UpdateContinuouslyAsync(sourceName, localSettingsDirectory, CancellationToken.None);
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
            => EndPoint.InvokeAsync(nameof(LogExceptionAsync), new object[] { exception, text }, CancellationToken.None);

        public Task LogInfoAsync(string text)
            => EndPoint.InvokeAsync(nameof(LogInfoAsync), new object[] { text }, CancellationToken.None);

        public Task OnDownloadFullDatabaseStartedAsync(string title)
            => EndPoint.InvokeAsync(nameof(OnDownloadFullDatabaseStartedAsync), new object[] { title }, CancellationToken.None);

        public Task OnDownloadFullDatabaseSucceededAsync()
            => EndPoint.InvokeAsync(nameof(OnDownloadFullDatabaseSucceededAsync), Array.Empty<object>(), CancellationToken.None);

        public Task OnDownloadFullDatabaseCanceledAsync()
            => EndPoint.InvokeAsync(nameof(OnDownloadFullDatabaseCanceledAsync), Array.Empty<object>(), CancellationToken.None);

        public Task OnDownloadFullDatabaseFailedAsync(string message)
            => EndPoint.InvokeAsync(nameof(OnDownloadFullDatabaseFailedAsync), new object[] { message }, CancellationToken.None);

        #endregion
    }
}
