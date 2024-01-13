' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LanguageServices
    <ExportLanguageServiceFactory(GetType(ISymbolDisplayService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSymbolDisplayServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicSymbolDisplayService(provider.LanguageServices)
        End Function
    End Class
End Namespace
