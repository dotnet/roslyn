// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteSymbolSearchUpdateEngine
    {
        private class ProgressService : ISymbolSearchProgressService
        {
            private readonly RemoteSymbolSearchUpdateEngine _service;

            public ProgressService(RemoteSymbolSearchUpdateEngine service)
            {
                _service = service;
            }

            public Task OnDownloadFullDatabaseStartedAsync(string title)
                => _service.Rpc.InvokeAsync(nameof(OnDownloadFullDatabaseStartedAsync), title);

            public Task OnDownloadFullDatabaseSucceededAsync()
                => _service.Rpc.InvokeAsync(nameof(OnDownloadFullDatabaseSucceededAsync));

            public Task OnDownloadFullDatabaseCanceledAsync()
                => _service.Rpc.InvokeAsync(nameof(OnDownloadFullDatabaseCanceledAsync));

            public Task OnDownloadFullDatabaseFailedAsync(string message)
                => _service.Rpc.InvokeAsync(nameof(OnDownloadFullDatabaseFailedAsync), message);
        }
    }
}