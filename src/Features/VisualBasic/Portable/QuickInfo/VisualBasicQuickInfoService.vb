﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.QuickInfo

Namespace Microsoft.CodeAnalysis.VisualBasic.QuickInfo
    <ExportLanguageServiceFactory(GetType(QuickInfoService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicQuickInfoServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicQuickInfoService(languageServices.WorkspaceServices.Workspace)
        End Function

    End Class

    Friend Class VisualBasicQuickInfoService
        Inherits QuickInfoServiceWithProviders

        Public Sub New(workspace As Workspace)
            MyBase.New(workspace, LanguageNames.VisualBasic)
        End Sub
    End Class
End Namespace
