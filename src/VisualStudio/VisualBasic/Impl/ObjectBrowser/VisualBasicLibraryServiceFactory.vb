' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    <ExportLanguageServiceFactory(GetType(ILibraryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicLibraryServiceFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicLibraryService()
        End Function
    End Class
End Namespace