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
        Inherits RemoveUnnecessaryCastDiagnosticAnalyzerBase(Of SyntaxKind, ExpressionSyntax)

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
