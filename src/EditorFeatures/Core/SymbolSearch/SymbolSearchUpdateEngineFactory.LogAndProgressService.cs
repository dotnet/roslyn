// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal static partial class SymbolSearchUpdateEngineFactory
    {
        private class LogAndProgressService : ISymbolSearchLogService, ISymbolSearchProgressService
        {
            private readonly ISymbolSearchLogService _logService;
            private readonly ISymbolSearchProgressService _progressService;

            public LogAndProgressService(ISymbolSearchLogService logService, ISymbolSearchProgressService progressService)
            {
                this._logService = logService;
                this._progressService = progressService;
            }

            public Task LogExceptionAsync(string exception, string text)
                => _logService.LogExceptionAsync(exception, text);

            public Task LogInfoAsync(string text)
                => _logService.LogInfoAsync(text);

            public Task OnDownloadFullDatabaseStartedAsync(string title)
                => _progressService.OnDownloadFullDatabaseStartedAsync(title);

            public Task OnDownloadFullDatabaseSucceededAsync()
                => _progressService.OnDownloadFullDatabaseSucceededAsync();

            public Task OnDownloadFullDatabaseCanceledAsync()
                => _progressService.OnDownloadFullDatabaseCanceledAsync();

            public Task OnDownloadFullDatabaseFailedAsync(string message)
                => _progressService.OnDownloadFullDatabaseFailedAsync(message);
        }
    }
}