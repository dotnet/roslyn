' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    <ExportLanguageService(GetType(IProjectExistsUIContextProviderLanguageService), LanguageNames.VisualBasic), [Shared]>
    Public Class VisualBasicProjectExistsUIContextProviderLanguageService
        Implements IProjectExistsUIContextProviderLanguageService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function GetUIContext() As UIContext Implements IProjectExistsUIContextProviderLanguageService.GetUIContext
            Return UIContext.FromUIContextGuid(Guids.VisualBasicProjectExistsInWorkspaceUIContext)
        End Function
    End Class
End Namespace
