// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// Factory that will produce the <see cref="ISymbolSearchUpdateEngine"/>.  The default
    /// implementation produces an engine that will run in-process.  Implementations at
    /// other layers can behave differently (for example, running the engine out-of-process).
    /// </summary>
    internal interface ISymbolSearchUpdateEngineFactory : IWorkspaceService
    {
        Task<ISymbolSearchUpdateEngine> CreateEngineAsync(
            Workspace workspace, ISymbolSearchLogService logService, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(ISymbolSearchUpdateEngineFactory)), Shared]
    internal class DefaultSymbolSearchUpdateEngineFactory : ISymbolSearchUpdateEngineFactory
    {
        public Task<ISymbolSearchUpdateEngine> CreateEngineAsync(
            Workspace workspace, ISymbolSearchLogService logService, CancellationToken cancellationToken)
        {
            return Task.FromResult<ISymbolSearchUpdateEngine>(
                new SymbolSearchUpdateEngine(logService));
        }
    }
}