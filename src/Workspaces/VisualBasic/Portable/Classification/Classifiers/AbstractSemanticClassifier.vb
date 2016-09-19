' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend MustInherit Class AbstractSemanticClassifier
        Implements ISemanticClassifier

        Protected Sub New()
        End Sub

        Protected Function GetClassificationForType(type As ITypeSymbol) As String
            Return type.GetClassification()
        End Function

        Public Overridable ReadOnly Property SyntaxNodeTypes As IEnumerable(Of System.Type) Implements ISemanticClassifier.SyntaxNodeTypes
            Get
                Return Nothing
            End Get
        End Property

        Public Overridable ReadOnly Property SyntaxTokenKinds As IEnumerable(Of Integer) Implements ISemanticClassifier.SyntaxTokenKinds
            Get
                Return Nothing
            End Get
        End Property

        Public Overridable Function ClassifyNode(syntax As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of ClassifiedSpan) Implements ISemanticClassifier.ClassifyNode
            Return Nothing
        End Function

        Public Overridable Function ClassifyToken(syntax As SyntaxToken, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of ClassifiedSpan) Implements ISemanticClassifier.ClassifyToken
            Return Nothing
        End Function
    End Class
End Namespace
