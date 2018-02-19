' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToDefinitionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToDefinitionService
        Inherits AbstractGoToDefinitionService

        <ImportingConstructor>
        Public Sub New(<ImportMany> streamingPresenters As IEnumerable(Of Lazy(Of IStreamingFindUsagesPresenter)))
            MyBase.New(streamingPresenters)
        End Sub
    End Class
End Namespace
