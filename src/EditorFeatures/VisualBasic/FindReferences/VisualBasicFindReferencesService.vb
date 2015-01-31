' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.FindReferences
    <ExportLanguageService(GetType(IFindReferencesService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicFindReferencesService
        Inherits AbstractFindReferencesService

        <ImportingConstructor>
        Protected Sub New(<ImportMany> presenters As IEnumerable(Of IReferencedSymbolsPresenter))
            MyBase.New(presenters)
        End Sub
    End Class
End Namespace
