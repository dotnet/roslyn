' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class EmbeddedLanguagesTokenClassifier
        Inherits AbstractEmbeddedLanguagesTokenClassifier

        Public Overrides ReadOnly Property SyntaxTokenKinds As ImmutableArray(Of Integer) =
            ImmutableArray.Create(Of Integer)(SyntaxKind.StringLiteralToken, SyntaxKind.InterpolatedStringTextToken)

        Public Sub New()
            MyBase.New(VisualBasicEmbeddedLanguagesProvider.Instance)
        End Sub
    End Class
End Namespace
