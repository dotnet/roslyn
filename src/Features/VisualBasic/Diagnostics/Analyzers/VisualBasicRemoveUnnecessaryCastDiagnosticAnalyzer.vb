' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.RemoveUnnecessaryCast
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.RemoveUnnecessaryCast

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnnecessaryCastDiagnosticAnalyzer
        Inherits RemoveUnnecessaryCastDiagnosticAnalyzerBase(Of SyntaxKind)

        Private Shared ReadOnly s_kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.CTypeExpression,
                                                                                                          SyntaxKind.DirectCastExpression,
                                                                                                          SyntaxKind.TryCastExpression,
                                                                                                          SyntaxKind.PredefinedCastExpression)
        Public Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind)
            Get
                Return s_kindsOfInterest
            End Get
        End Property

        Protected Overrides Function IsUnnecessaryCast(model As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Select Case node.Kind
                Case SyntaxKind.CTypeExpression, SyntaxKind.DirectCastExpression, SyntaxKind.TryCastExpression
                    Return DirectCast(node, CastExpressionSyntax).IsUnnecessaryCast(model, assumeCallKeyword:=True, cancellationToken:=cancellationToken)
                Case SyntaxKind.PredefinedCastExpression
                    Return DirectCast(node, PredefinedCastExpressionSyntax).IsUnnecessaryCast(model, assumeCallKeyword:=True, cancellationToken:=cancellationToken)
                Case Else
                    Throw ExceptionUtilities.Unreachable
            End Select
        End Function

        Protected Overrides Function GetDiagnosticSpan(node As SyntaxNode) As TextSpan
            Select Case node.Kind
                Case SyntaxKind.CTypeExpression, SyntaxKind.DirectCastExpression, SyntaxKind.TryCastExpression
                    Return DirectCast(node, CastExpressionSyntax).Keyword.Span
                Case SyntaxKind.PredefinedCastExpression
                    Return DirectCast(node, PredefinedCastExpressionSyntax).Keyword.Span
                Case Else
                    Throw ExceptionUtilities.Unreachable
            End Select
        End Function
    End Class
End Namespace
