' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend MustInherit Class AbstractSyntaxClassifier
        Implements ISyntaxClassifier

        Protected Sub New()
        End Sub

        Protected Function GetClassificationForType(type As ITypeSymbol) As String
            Return type.GetClassification()
        End Function

        Public Overridable ReadOnly Property SyntaxNodeTypes As IEnumerable(Of System.Type) Implements ISyntaxClassifier.SyntaxNodeTypes
            Get
                Return Nothing
            End Get
        End Property

        Public Overridable ReadOnly Property SyntaxTokenKinds As IEnumerable(Of Integer) Implements ISyntaxClassifier.SyntaxTokenKinds
            Get
                Return Nothing
            End Get
        End Property

        Public Overridable Function ClassifyNode(syntax As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of ClassifiedSpan) Implements ISyntaxClassifier.ClassifyNode
            Return Nothing
        End Function

        Public Overridable Function ClassifyToken(syntax As SyntaxToken, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of ClassifiedSpan) Implements ISyntaxClassifier.ClassifyToken
            Return Nothing
        End Function
    End Class
End Namespace
