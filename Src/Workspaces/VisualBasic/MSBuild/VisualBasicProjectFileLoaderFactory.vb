' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MSBuild
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MSB = Microsoft.Build

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageServiceFactory(GetType(IProjectFileLoader), LanguageNames.VisualBasic)>
    <ProjectFileExtension("vbproj")>
    <ProjectTypeGuid("F184B08F-C81C-45F6-A57F-5ABD9991F28F")>
    Friend Class VisualBasicProjectFileLoaderFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicProjectFileLoader(languageServices.WorkspaceServices)
        End Function
    End Class
End Namespace

