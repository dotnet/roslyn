' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SignatureHelp

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    <ExportLanguageServiceFactory(GetType(SignatureHelpService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSignatureHelpServiceFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicSignatureHelpService()
        End Function
    End Class

    Friend Class VisualBasicSignatureHelpService
        Inherits CommonSignatureHelpService

        Public Overrides ReadOnly Property Language As String = LanguageNames.VisualBasic
    End Class
End Namespace