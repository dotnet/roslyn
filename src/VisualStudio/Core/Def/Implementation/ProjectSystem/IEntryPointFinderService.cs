// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal interface IEntryPointFinderService : ILanguageService
    {
        /// <summary>
        /// Finds the types that contain entry points like the Main method in a give namespace.
        /// </summary>
        /// <param name="symbol">The namespace to search.</param>
        /// <param name="findFormsOnly">Restrict the search to only Windows Forms classes. Note that this is only implemented for VisualBasic</param>
        IEnumerable<INamedTypeSymbol> FindEntryPoints(INamespaceSymbol symbol, bool findFormsOnly);
    }
}
