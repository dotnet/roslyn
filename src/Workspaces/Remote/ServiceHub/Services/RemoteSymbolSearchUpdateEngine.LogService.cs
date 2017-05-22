// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteSymbolSearchUpdateEngine
    {
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