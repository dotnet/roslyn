' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification.Classifiers
    Friend Class IdentifierNameSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Private Const s_awaitText = "Await"

        Public Overrides ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) = ImmutableArray.Create(GetType(IdentifierNameSyntax))

        Public Overrides Sub AddClassifications(syntax As SyntaxNode, textSpan As TextSpan, semanticModel As SemanticModel, options As ClassificationOptions, result As SegmentedList(Of ClassifiedSpan), cancellationToken As CancellationToken)
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
