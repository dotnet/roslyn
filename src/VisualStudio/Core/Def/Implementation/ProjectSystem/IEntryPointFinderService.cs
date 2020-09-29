// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
