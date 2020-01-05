' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
