// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
