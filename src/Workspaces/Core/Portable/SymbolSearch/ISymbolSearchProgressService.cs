// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface ISymbolSearchProgressService : IWorkspaceService
    {
        Task OnDownloadFullDatabaseStartedAsync(string title, CancellationToken cancellationToken);

        Task OnDownloadFullDatabaseSucceededAsync(CancellationToken cancellationToken);
        Task OnDownloadFullDatabaseCanceledAsync(CancellationToken cancellationToken);
        Task OnDownloadFullDatabaseFailedAsync(string message, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(ISymbolSearchProgressService)), Shared]
    internal class DefaultSymbolSearchProgressService : ISymbolSearchProgressService
    {
        [ImportingConstructor]
        public DefaultSymbolSearchProgressService()
        {
        }

        public Task OnDownloadFullDatabaseStartedAsync(string title, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task OnDownloadFullDatabaseSucceededAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task OnDownloadFullDatabaseCanceledAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task OnDownloadFullDatabaseFailedAsync(string message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
