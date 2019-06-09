// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    [ExportLanguageService(typeof(IEntryPointFinderService), LanguageNames.CSharp), Shared]
    internal class CSharpEntryPointFinderService : IEntryPointFinderService
    {
        [ImportingConstructor]
        public CSharpEntryPointFinderService()
        {
        }

        public IEnumerable<INamedTypeSymbol> FindEntryPoints(INamespaceSymbol symbol, bool findFormsOnly)
        {
            return EntryPointFinder.FindEntryPoints(symbol);
        }
    }
}
