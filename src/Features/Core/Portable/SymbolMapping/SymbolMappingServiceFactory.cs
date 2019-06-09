// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SymbolMapping
{
    [ExportWorkspaceService(typeof(ISymbolMappingService), ServiceLayer.Default), Shared]
    internal class DefaultSymbolMappingService : ISymbolMappingService
    {
        [ImportingConstructor]
        public DefaultSymbolMappingService()
        {
        }

        public async Task<SymbolMappingResult> MapSymbolAsync(Document document, SymbolKey symbolId, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbol = symbolId.Resolve(compilation, cancellationToken: cancellationToken).Symbol;
            if (symbol != null)
            {
                return new SymbolMappingResult(document.Project, symbol);
            }

            return null;
        }

        public Task<SymbolMappingResult> MapSymbolAsync(Document document, ISymbol symbol, CancellationToken cancellationToken)
            => Task.FromResult(new SymbolMappingResult(document.Project, symbol));
    }
}
