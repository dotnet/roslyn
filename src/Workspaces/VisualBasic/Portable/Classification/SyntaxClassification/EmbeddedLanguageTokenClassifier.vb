' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class EmbeddedLanguageTokenClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxTokenKinds As ImmutableArray(Of Integer) = ImmutableArray.Create(Of Integer)(SyntaxKind.StringLiteralToken)

        Public Overrides Sub AddClassifications(workspace As Workspace, token As SyntaxToken, semanticModel As SemanticModel, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Debug.Assert(token.Kind() = SyntaxKind.StringLiteralToken)
            For Each language In VisualBasicEmbeddedLanguageProvider.Instance.GetEmbeddedLanguages()
                Dim classifier = language.Classifier
                If classifier IsNot Nothing Then
                    classifier.AddClassifications(workspace, token, semanticModel, result, cancellationToken)
                End If
            Next
        End Sub
    End Class
End Namespace
