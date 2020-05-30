﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToDefinitionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToDefinitionService
        Inherits AbstractGoToDefinitionService

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(threadingContext As IThreadingContext,
                       streamingPresenter As Lazy(Of IStreamingFindUsagesPresenter))
            MyBase.New(threadingContext, streamingPresenter)
        End Sub
    End Class
End Namespace
