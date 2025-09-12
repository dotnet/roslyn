// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

/// <summary>
/// Note: This type is exposed through IVT to dotnet-project-system.
/// </summary>
internal interface IEntryPointFinderService : ILanguageService
{
    /// <summary>
    /// Finds the types that contain entry points like the Main method in a given compilation.
    /// </summary>
    /// <param name="compilation">The compilation to search.</param>
    /// <param name="findFormsOnly">Restrict the search to only Windows Forms classes. Note that this is only implemented for VisualBasic</param>
    IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation, bool findFormsOnly);
}
