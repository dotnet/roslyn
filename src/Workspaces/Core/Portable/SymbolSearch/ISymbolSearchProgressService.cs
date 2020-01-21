// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface ISymbolSearchProgressService : IWorkspaceService
    {
        Task OnDownloadFullDatabaseStartedAsync(string title);

        Task OnDownloadFullDatabaseSucceededAsync();
        Task OnDownloadFullDatabaseCanceledAsync();
        Task OnDownloadFullDatabaseFailedAsync(string message);
    }

    [ExportWorkspaceService(typeof(ISymbolSearchProgressService)), Shared]
    internal class DefaultSymbolSearchProgressService : ISymbolSearchProgressService
    {
        [ImportingConstructor]
        public DefaultSymbolSearchProgressService()
        {
        }

        public Task OnDownloadFullDatabaseStartedAsync(string title) => Task.CompletedTask;
        public Task OnDownloadFullDatabaseSucceededAsync() => Task.CompletedTask;
        public Task OnDownloadFullDatabaseCanceledAsync() => Task.CompletedTask;
        public Task OnDownloadFullDatabaseFailedAsync(string message) => Task.CompletedTask;
    }
}
