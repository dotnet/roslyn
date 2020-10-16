// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal class DefaultSymbolNavigationService : ISymbolNavigationService
    {
        public Task<MetadataAsSourceFile> GetAndOpenGeneratedFileAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
            => Task.FromResult<MetadataAsSourceFile>(null);

        public bool TryNavigateToSymbol(ISymbol symbol, Project project, OptionSet options = null, CancellationToken cancellationToken = default)
            => false;

        public bool TrySymbolNavigationNotify(ISymbol symbol, Project project, CancellationToken cancellationToken)
            => false;

        public bool WouldNavigateToSymbol(
            DefinitionItem definitionItem, Solution solution, CancellationToken cancellationToken,
            out string filePath, out int lineNumber, out int charOffset)
        {
            filePath = null;
            lineNumber = 0;
            charOffset = 0;

            return false;
        }
    }
}
