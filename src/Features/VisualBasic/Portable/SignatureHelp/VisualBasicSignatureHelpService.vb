' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SignatureHelp

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    <ExportLanguageServiceFactory(GetType(SignatureHelpService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSignatureHelpServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicSignatureHelpService(languageServices.LanguageServices)
        End Function

    End Class

    Friend Class VisualBasicSignatureHelpService
        Inherits SignatureHelpServiceWithProviders

        Public Sub New(services As Host.LanguageServices)
            MyBase.New(services)
        End Sub
    End Class
End Namespace
