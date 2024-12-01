' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <ExportLanguageServiceFactory(GetType(ICodeGenerationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCodeGenerationServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicCodeGenerationService(provider.LanguageServices)
        End Function
    End Class
End Namespace
