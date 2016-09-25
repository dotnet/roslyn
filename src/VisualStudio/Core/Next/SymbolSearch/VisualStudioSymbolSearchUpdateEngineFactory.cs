// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    [ExportWorkspaceService(typeof(ISymbolSearchUpdateEngineFactory)), Shared]
    internal class VisualStudioSymbolSearchUpdateEngineFactory : ISymbolSearchUpdateEngineFactory
    {
        public Task<ISymbolSearchUpdateEngine> CreateEngineAsync(
            CodeAnalysis.Workspace workspace, ISymbolSearchLogService logService, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
