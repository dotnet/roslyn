' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.Analyzers.UseCoalesceExpression
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.UseCoalesceExpression
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseCoalesceExpressionForIfNullStatementCheckDiagnosticAnalyzer
        Inherits AbstractUseCoalesceExpressionForIfNullCheckDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            IfStatementSyntax)

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function

        Protected Overrides Function IsNullCheck(condition As ExpressionSyntax, <NotNullWhen(True)> ByRef checkedExpression As ExpressionSyntax) As Boolean
            Dim binary = TryCast(condition, BinaryExpressionSyntax)
            If binary Is Nothing Then
                Return False
            End If

            If binary.Right.Kind() <> SyntaxKind.NothingLiteralExpression Then
                Return False
            End If

            If binary.Kind() <> SyntaxKind.IsExpression AndAlso binary.Kind() <> SyntaxKind.EqualsExpression Then
                Return False
            End If

            checkedExpression = binary.Left
            Return True
        End Function

        Protected Overrides Function TryGetEmbeddedStatement(ifStatement As IfStatementSyntax, <NotNullWhen(True)> ByRef whenTrueStatement As StatementSyntax) As Boolean
            Dim ifBlock = TryCast(ifStatement.Parent, MultiLineIfBlockSyntax)
            If ifBlock Is Nothing Then
                Return False
            End If

            If ifBlock.Statements.Count <> 1 Then
                Return False
            End If

            whenTrueStatement = ifBlock.Statements(0)
            Return True
        End Function

        Protected Overrides Function HasElseBlock(ifStatement As IfStatementSyntax) As Boolean
            Dim ifBlock = TryCast(ifStatement.Parent, MultiLineIfBlockSyntax)
            If ifBlock Is Nothing Then
                Return False
            End If

            Return ifBlock.ElseBlock IsNot Nothing Or ifBlock.ElseIfBlocks.Count > 0
        End Function

        Protected Overrides Function TryGetPreviousStatement(ifStatement As IfStatementSyntax) As StatementSyntax
            Return ifStatement.GetPreviousStatement()
        End Function
    End Class
End Namespace
