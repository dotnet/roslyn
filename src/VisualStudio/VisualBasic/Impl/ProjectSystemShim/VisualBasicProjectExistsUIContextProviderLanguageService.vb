' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    <ExportLanguageService(GetType(IProjectExistsUIContextProviderLanguageService), LanguageNames.VisualBasic), [Shared]>
    Public Class VisualBasicProjectExistsUIContextProviderLanguageService
        Implements IProjectExistsUIContextProviderLanguageService

        Public Function GetUIContext() As UIContext Implements IProjectExistsUIContextProviderLanguageService.GetUIContext
            Return UIContext.FromUIContextGuid(Guids.VisualBasicProjectExistsInWorkspaceUIContext)
        End Function
    End Class
End Namespace
