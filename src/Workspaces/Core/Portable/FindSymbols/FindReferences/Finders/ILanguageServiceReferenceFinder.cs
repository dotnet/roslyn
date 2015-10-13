// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal interface ILanguageServiceReferenceFinder : ILanguageService
    {
        Task<IEnumerable<ISymbol>> DetermineCascadedSymbolsAsync(ISymbol symbol, Project project, CancellationToken cancellationToken);
    }
}