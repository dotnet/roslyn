' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageServiceFactory(GetType(ISyntaxKindsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxKindsServiceFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return VisualBasicSyntaxKindsService.Instance
        End Function
    End Class
End Namespace
