' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class IdentifierNameSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Private Const s_awaitText = "Await"

        Public Overrides ReadOnly Property SyntaxNodeTypes As IEnumerable(Of Type)
            Get
                Return {GetType(IdentifierNameSyntax)}
            End Get
        End Property

        Public Overrides Function ClassifyNode(syntax As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of ClassifiedSpan)
            Dim identifierName = DirectCast(syntax, IdentifierNameSyntax)
            Dim identifier = identifierName.Identifier
            If CaseInsensitiveComparison.Equals(identifier.ValueText, s_awaitText) Then
                Dim symbolInfo = semanticModel.GetSymbolInfo(identifier)
                If symbolInfo.GetAnySymbol() Is Nothing Then
                    Return SpecializedCollections.SingletonEnumerable(New ClassifiedSpan(ClassificationTypeNames.Keyword, identifier.Span))
                End If
            End If

            Return MyBase.ClassifyNode(syntax, semanticModel, cancellationToken)
        End Function
    End Class
End Namespace