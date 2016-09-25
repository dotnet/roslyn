// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal interface ISymbolSearchUpdateEngineFactory : IWorkspaceService
    {
        Task<ISymbolSearchUpdateEngine> CreateEngineAsync(
            Workspace workspace, ISymbolSearchLogService logService);
    }
}