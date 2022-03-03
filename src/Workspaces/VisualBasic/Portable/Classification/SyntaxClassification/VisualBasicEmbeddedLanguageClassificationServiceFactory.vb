' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    <ExportLanguageService(GetType(IEmbeddedLanguageClassificationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEmbeddedLanguageClassificationService
        Inherits AbstractEmbeddedLanguageClassificationService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
                <ImportMany> classifiers As IEnumerable(Of Lazy(Of IEmbeddedLanguageClassifier, OrderableLanguageMetadata)))
            MyBase.New(classifiers, VisualBasicSyntaxKinds.Instance)
        End Sub

        Protected Overrides ReadOnly Property Language As String = LanguageNames.VisualBasic
    End Class
End Namespace
