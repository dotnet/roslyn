' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToSymbolService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToSymbolService
        Inherits AbstractGoToSymbolService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As IThreadingContext)
            MyBase.New(threadingContext)
        End Sub
    End Class
End Namespace
