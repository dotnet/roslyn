' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class IdentifierNameSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Private Const s_awaitText = "Await"

        Public Overrides ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) = ImmutableArray.Create(GetType(IdentifierNameSyntax))

        Public Overrides Sub AddClassifications(workspace As Workspace, syntax As SyntaxNode, semanticModel As SemanticModel, result As ArrayBuilder(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Dim identifierName = DirectCast(syntax, IdentifierNameSyntax)
            Dim identifier = identifierName.Identifier
            If CaseInsensitiveComparison.Equals(identifier.ValueText, s_awaitText) Then
                Dim symbolInfo = semanticModel.GetSymbolInfo(identifier)
                If symbolInfo.GetAnySymbol() Is Nothing Then
                    result.Add(New ClassifiedSpan(ClassificationTypeNames.Keyword, identifier.Span))
                    Return
                End If
            End If
        End Sub
    End Class
End Namespace
