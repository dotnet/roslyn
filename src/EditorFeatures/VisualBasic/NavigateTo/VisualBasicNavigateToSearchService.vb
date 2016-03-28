' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.NavigateTo
    <ExportLanguageService(GetType(INavigateToSearchService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicNavigateToSearchService
        Inherits AbstractNavigateToSearchService

        <ImportingConstructor>
        Sub New(<ImportMany> resultProviders As IEnumerable(Of INavigateToSearchResultProvider))
            MyBase.New(resultProviders)
        End Sub
    End Class
End Namespace
