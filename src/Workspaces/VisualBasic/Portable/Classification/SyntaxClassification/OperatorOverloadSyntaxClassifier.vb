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

    Friend Class OperatorOverloadSyntaxClassifier
        Inherits AbstractSyntaxClassifier

        Public Overrides ReadOnly Property SyntaxNodeTypes As ImmutableArray(Of Type) = ImmutableArray.Create(
            GetType(BinaryExpressionSyntax),
            GetType(UnaryExpressionSyntax))

        Public Overrides Sub AddClassifications(
            syntax As SyntaxNode,
            textSpan As TextSpan,
            semanticModel As SemanticModel,
            options As ClassificationOptions,
            result As SegmentedList(Of ClassifiedSpan), cancellationToken As CancellationToken)

            ' Short-circuit simple assignments to prevent calculation of symbol info as it can be expensive.
            If syntax.IsKind(SyntaxKind.SimpleAssignmentStatement) Then
                Return
            End If

            ' Short-circuit operators whose span doesn't intersect the requested span.
            Dim operatorSpan = GetOperatorTokenSpan(syntax)
            If operatorSpan.IsEmpty OrElse Not operatorSpan.IntersectsWith(textSpan) Then
                Return
            End If

            Dim symbolInfo = semanticModel.GetSymbolInfo(syntax, cancellationToken)
            If TypeOf symbolInfo.Symbol Is IMethodSymbol AndAlso
                DirectCast(symbolInfo.Symbol, IMethodSymbol).MethodKind = MethodKind.UserDefinedOperator Then

                result.Add(New ClassifiedSpan(operatorSpan, ClassificationTypeNames.OperatorOverloaded))
            End If
        End Sub

        Private Shared Function GetOperatorTokenSpan(syntax As SyntaxNode) As TextSpan
            If TypeOf syntax Is BinaryExpressionSyntax Then
                Return DirectCast(syntax, BinaryExpressionSyntax).OperatorToken.Span
            ElseIf TypeOf syntax Is UnaryExpressionSyntax Then
                Return DirectCast(syntax, UnaryExpressionSyntax).OperatorToken.Span
            End If

            Return Nothing
        End Function
    End Class
End Namespace
