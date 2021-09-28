﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CallstackExplorer
{
    internal class FileLineResult : ParsedLine
    {
        private readonly string _methodSignature;
        private readonly string _fileInformation;

        public FileLineResult(string methodSignature, string fileInformation)
        {
            _methodSignature = methodSignature;
            _fileInformation = fileInformation;
        }

        internal override async Task<ISymbol?> ResolveSymbolAsync(Solution solution, CancellationToken cancellationToken)
        {
            foreach (var project in solution.Projects)
            {
                var foundSymbols = await FindSymbols.DeclarationFinder.FindSourceDeclarationsWithPatternAsync(
                    project,
                    _methodSignature,
                    SymbolFilter.Member,
                    cancellationToken).ConfigureAwait(false);

                var foundSymbol = foundSymbols.Length == 1
                    ? foundSymbols[0]
                    : null;

                if (foundSymbol is not null)
                {
                    return foundSymbol;
                }
            }

            return null;
        }
    }
}
