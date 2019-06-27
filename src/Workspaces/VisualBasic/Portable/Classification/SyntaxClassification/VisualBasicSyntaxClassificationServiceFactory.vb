' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
