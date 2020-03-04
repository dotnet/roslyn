' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToDefinitionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToDefinitionService
        Inherits AbstractGoToDefinitionService

        <ImportingConstructor>
        Public Sub New(streamingPresenter As Lazy(Of IStreamingFindUsagesPresenter))
            MyBase.New(streamingPresenter)
        End Sub
    End Class
End Namespace
