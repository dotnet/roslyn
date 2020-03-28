' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class EmbeddedLanguageTokenClassifier
        Inherits AbstractEmbeddedLanguageTokenClassifier

        Public Overrides ReadOnly Property SyntaxTokenKinds As ImmutableArray(Of Integer) = ImmutableArray.Create(Of Integer)(SyntaxKind.StringLiteralToken)

        Public Sub New()
            MyBase.New(VisualBasicEmbeddedLanguageProvider.Instance)
        End Sub
    End Class
End Namespace
