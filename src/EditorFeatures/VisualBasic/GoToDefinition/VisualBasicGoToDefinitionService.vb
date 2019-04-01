' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToDefinitionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToDefinitionService
        Inherits AbstractGoToDefinitionService

        <ImportingConstructor>
        Public Sub New(<Import(AllowDefault:=True)> streamingPresenterOpt As Lazy(Of IStreamingFindUsagesPresenter))
            MyBase.New(streamingPresenterOpt)
        End Sub
    End Class
End Namespace
