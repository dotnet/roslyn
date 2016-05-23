// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.FindReferences;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.FindReferences
{
    [ExportLanguageService(typeof(IFindReferencesService), LanguageNames.CSharp), Shared]
    internal class CSharpFindReferencesService : AbstractFindReferencesService
    {
        [ImportingConstructor]
        public CSharpFindReferencesService(
            [ImportMany] IEnumerable<IReferencedSymbolsPresenter> referencedSymbolsPresenters,
            [ImportMany] IEnumerable<INavigableItemsPresenter> navigableItemsPresenters,
            [ImportMany] IEnumerable<IFindReferencesResultProvider> externalReferencesProviders)
            : base(referencedSymbolsPresenters, navigableItemsPresenters, externalReferencesProviders)
        {
        }
    }
}
