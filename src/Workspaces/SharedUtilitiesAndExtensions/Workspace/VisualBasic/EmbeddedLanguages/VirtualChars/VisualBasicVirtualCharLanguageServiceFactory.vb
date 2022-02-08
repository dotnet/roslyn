' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
    <ExportLanguageServiceFactory(GetType(IVirtualCharLanguageService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicVirtualCharLanguageServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return VisualBasicVirtualCharLanguageService.Instance
        End Function

        Private NotInheritable Class VisualBasicVirtualCharLanguageService
            Inherits VisualBasicVirtualCharService
            Implements IVirtualCharLanguageService

            Public Shared Shadows ReadOnly Property Instance As New VisualBasicVirtualCharLanguageService()

            Private Sub New()
            End Sub
        End Class
    End Class
End Namespace
