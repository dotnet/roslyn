' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageService
    <ExportLanguageServiceFactory(GetType(ISyntaxKindsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxKindsServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return VisualBasicSyntaxKindsService.Instance
        End Function

        Private NotInheritable Class VisualBasicSyntaxKindsService
            Inherits VisualBasicSyntaxKinds
            Implements ISyntaxKindsService

            Public Shared Shadows ReadOnly Instance As New VisualBasicSyntaxKindsService
        End Class
    End Class
End Namespace
