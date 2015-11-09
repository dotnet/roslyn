// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using System.Threading;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal interface ISymbolNavigationService : IWorkspaceService
    {
        /// <summary>
        /// Navigate to the first source location of a given symbol.
        /// </summary>
        /// <param name="project">A project context with which to generate source for symbol
        /// if it has no source locations</param>
        /// <param name="symbol">The symbol to navigate to</param>
        /// <param name="options">A set of options. If these options are not supplied the
        /// current set of options from the project's workspace will be used.</param>
        /// <param name="cancellationToken">The token to check for cancellation</param>
        bool TryNavigateToSymbol(ISymbol symbol, Project project, OptionSet options = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <returns>True if the navigation was handled, indicating that the caller should not 
        /// perform the navigation.</returns>
        bool TrySymbolNavigationNotify(ISymbol symbol, Solution solution);

        /// <returns>True if the navigation would be handled.</returns>
        bool WouldNavigateToSymbol(ISymbol symbol, Solution solution, out string filePath, out int lineNumber, out int charOffset);
    }
}
