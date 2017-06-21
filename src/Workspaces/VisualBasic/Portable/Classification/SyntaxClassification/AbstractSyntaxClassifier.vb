' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend MustInherit Class AbstractSyntaxClassifier
        Implements ISyntaxClassifier

        Protected Sub New()
        End Sub

        Protected Function GetClassificationForType(type As ITypeSymbol) As String
            Return type.GetClassification()
        End Function

        Public Overridable ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) Implements ISyntaxClassifier.SyntaxNodeTypes
            Get
                Return ImmutableArray(Of Type).Empty
            End Get
        End Property

        Public Overridable ReadOnly Property SyntaxTokenKinds As ImmutableArray(Of Integer) Implements ISyntaxClassifier.SyntaxTokenKinds
            Get
                Return ImmutableArray(Of Integer).Empty
            End Get
        End Property

        Public Overridable Sub AddClassifications(syntax As SyntaxNode, semanticModel As SemanticModel, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken) Implements ISyntaxClassifier.AddClassifications
        End Sub

        Public Overridable Sub AddClassifications(syntax As SyntaxToken, semanticModel As SemanticModel, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken) Implements ISyntaxClassifier.AddClassifications
        End Sub
    End Class
End Namespace
