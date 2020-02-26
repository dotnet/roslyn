' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryCast
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryCast

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnnecessaryCastDiagnosticAnalyzer
        Inherits AbstractRemoveUnnecessaryCastDiagnosticAnalyzer(Of SyntaxKind, ExpressionSyntax)

        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) =
            ImmutableArray.Create(SyntaxKind.CTypeExpression,
                                  SyntaxKind.DirectCastExpression,
                                  SyntaxKind.TryCastExpression,
                                  SyntaxKind.PredefinedCastExpression)

        Protected Overrides Function IsUnnecessaryCast(model As SemanticModel, node As ExpressionSyntax, cancellationToken As CancellationToken) As Boolean
            Select Case node.Kind
                Case SyntaxKind.CTypeExpression, SyntaxKind.DirectCastExpression, SyntaxKind.TryCastExpression
                    Return DirectCast(node, CastExpressionSyntax).IsUnnecessaryCast(model, assumeCallKeyword:=True, cancellationToken:=cancellationToken)
                Case SyntaxKind.PredefinedCastExpression
                    Return DirectCast(node, PredefinedCastExpressionSyntax).IsUnnecessaryCast(model, assumeCallKeyword:=True, cancellationToken:=cancellationToken)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

        Protected Overrides Function GetFadeSpan(node As ExpressionSyntax) As TextSpan
            Return node.GetFirstToken().Span
        End Function
    End Class
End Namespace
