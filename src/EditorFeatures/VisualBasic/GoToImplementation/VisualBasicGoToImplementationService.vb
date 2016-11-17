' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.GoToImplementation
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToImplementation
    <ExportLanguageService(GetType(IGoToImplementationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToImplementationService
        Inherits AbstractGoToImplementationService

        <ImportingConstructor>
        Public Sub New(<ImportMany> presenters As IEnumerable(Of Lazy(Of INavigableItemsPresenter)))
            MyBase.New(presenters)
        End Sub
    End Class
End Namespace
