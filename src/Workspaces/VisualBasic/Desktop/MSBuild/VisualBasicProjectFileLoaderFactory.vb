' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MSBuild

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageServiceFactory(GetType(IProjectFileLoader), LanguageNames.VisualBasic), [Shared]>
    <ProjectFileExtension("vbproj")>
    <ProjectTypeGuid("F184B08F-C81C-45F6-A57F-5ABD9991F28F")>
    Friend Class VisualBasicProjectFileLoaderFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicProjectFileLoader(languageServices.WorkspaceServices)
        End Function
    End Class
End Namespace

