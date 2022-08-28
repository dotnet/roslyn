// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    [ExportLanguageService(typeof(IEntryPointFinderService), LanguageNames.CSharp), Shared]
    internal class CSharpEntryPointFinderService : IEntryPointFinderService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEntryPointFinderService()
        {
        }

        [Obsolete("FindEntryPoints on a INamespaceSymbol is deprecated, please pass in the Compilation instead.")]
        public IEnumerable<INamedTypeSymbol> FindEntryPoints(INamespaceSymbol symbol, bool findFormsOnly)
            => EntryPointFinder.FindEntryPoints(symbol);

        public IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation, bool findFormsOnly)
            => EntryPointFinder.FindEntryPoints(compilation);
    }
}
