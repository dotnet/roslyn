' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LanguageServices
    <ExportLanguageServiceFactory(GetType(ISymbolDisplayService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSymbolDisplayServiceFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicSymbolDisplayService(provider)
        End Function
    End Class
End Namespace
