// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal class DefaultSymbolNavigationService : ISymbolNavigationService
    {
        public bool TryNavigateToSymbol(ISymbol symbol, Project project, CancellationToken cancellationToken = default(CancellationToken), bool usePreviewTab = false)
        {
            return false;
        }

        public bool TrySymbolNavigationNotify(ISymbol symbol, Solution solution)
        {
            return false;
        }

        public bool WouldNavigateToSymbol(ISymbol symbol, Solution solution, out string filePath, out int lineNumber, out int charOffset)
        {
            filePath = null;
            lineNumber = 0;
            charOffset = 0;

            return false;
        }
    }
}
