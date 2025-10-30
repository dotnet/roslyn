// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal abstract class AbstractEntryPointFinderService : IEntryPointFinderService
{
    public abstract IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation, bool findFormsOnly);
}
