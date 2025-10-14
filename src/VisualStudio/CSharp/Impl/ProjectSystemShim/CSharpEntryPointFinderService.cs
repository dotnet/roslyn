// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;

[ExportLanguageService(typeof(IEntryPointFinderService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpEntryPointFinderService() : AbstractEntryPointFinderService
{
    public override IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation, bool findFormsOnly)
        => CSharpEntryPointFinder.FindEntryPoints(compilation);
}
