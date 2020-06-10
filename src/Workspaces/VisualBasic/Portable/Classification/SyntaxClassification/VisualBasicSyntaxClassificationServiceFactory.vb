' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    <ExportLanguageServiceFactory(GetType(ISyntaxClassificationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxClassificationServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        <Obsolete(MefConstruction.FactoryMethodMessage, True)>
        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicSyntaxClassificationService(languageServices)
        End Function
    End Class
End Namespace
