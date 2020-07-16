// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultSymbolSearchProgressService()
        {
        }

        public Task OnDownloadFullDatabaseStartedAsync(string title) => Task.CompletedTask;
        public Task OnDownloadFullDatabaseSucceededAsync() => Task.CompletedTask;
        public Task OnDownloadFullDatabaseCanceledAsync() => Task.CompletedTask;
        public Task OnDownloadFullDatabaseFailedAsync(string message) => Task.CompletedTask;
    }
}
