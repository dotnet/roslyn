' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.FindReferences
    <ExportLanguageService(GetType(IFindReferencesService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicFindReferencesService
        Inherits AbstractFindReferencesService

        <ImportingConstructor>
        Protected Sub New(<ImportMany> referencedSymbolsPresenters As IEnumerable(Of IReferencedSymbolsPresenter),
                          <ImportMany> navigableItemsPresenters As IEnumerable(Of INavigableItemsPresenter),
                          <ImportMany> externalReferencesProviders As IEnumerable(Of IFindReferencesResultProvider))
            MyBase.New(referencedSymbolsPresenters, navigableItemsPresenters, externalReferencesProviders)
        End Sub
    End Class
End Namespace
