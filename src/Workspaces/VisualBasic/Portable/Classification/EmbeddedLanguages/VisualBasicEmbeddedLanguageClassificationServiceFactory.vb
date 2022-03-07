' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    <ExportLanguageService(GetType(IEmbeddedLanguageClassificationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEmbeddedLanguageClassificationService
        Inherits AbstractEmbeddedLanguageClassificationService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
                <ImportMany> classifiers As IEnumerable(Of Lazy(Of IEmbeddedLanguageClassifier, EmbeddedLanguageMetadata)))
            MyBase.New(classifiers, VisualBasicFallbackEmbeddedLanguageClassifier.Instance, VisualBasicSyntaxKinds.Instance, LanguageNames.VisualBasic)
        End Sub
    End Class
End Namespace
