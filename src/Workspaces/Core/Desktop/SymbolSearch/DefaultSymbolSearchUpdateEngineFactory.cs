// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SymbolSearch
{

    [ExportWorkspaceService(typeof(ISymbolSearchUpdateEngineFactory)), Shared]
    internal class DefaultSymbolSearchUpdateEngineFactory : ISymbolSearchUpdateEngineFactory
    {
        public Task<ISymbolSearchUpdateEngine> CreateEngineAsync(
            Workspace workspace, ISymbolSearchLogService logService)
        {
            return Task.FromResult<ISymbolSearchUpdateEngine>(
                new SymbolSearchUpdateEngine(logService));
        }
    }
}