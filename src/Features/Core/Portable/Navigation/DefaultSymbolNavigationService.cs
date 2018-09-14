// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal class DefaultSymbolNavigationService : ISymbolNavigationService
    {
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
