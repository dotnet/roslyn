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
        Inherits AbstractUseCoalesceExpressionForIfNullStatementCheckDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            VariableDeclaratorSyntax,
            MultiLineIfBlockSyntax)

        Protected Overrides ReadOnly Property IfStatementKind As SyntaxKind = SyntaxKind.MultiLineIfBlock

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides Function IsSingle(declarator As VariableDeclaratorSyntax) As Boolean
            Return declarator.Names.Count = 1
        End Function

        Protected Overrides Function GetDeclarationNode(declarator As VariableDeclaratorSyntax) As SyntaxNode
            Return declarator.Names(0)
        End Function

        Protected Overrides Function GetConditionOfIfStatement(ifBlock As MultiLineIfBlockSyntax) As ExpressionSyntax
            Return ifBlock.IfStatement.Condition
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

        Protected Overrides Function TryGetEmbeddedStatement(ifBlock As MultiLineIfBlockSyntax, <NotNullWhen(True)> ByRef whenTrueStatement As StatementSyntax) As Boolean
            If ifBlock.Statements.Count <> 1 Then
                Return False
            End If

            whenTrueStatement = ifBlock.Statements(0)
            Return True
        End Function

        Protected Overrides Function HasElseBlock(ifBlock As MultiLineIfBlockSyntax) As Boolean
            Return ifBlock.ElseBlock IsNot Nothing Or ifBlock.ElseIfBlocks.Count > 0
        End Function

        Protected Overrides Function TryGetPreviousStatement(ifBlock As MultiLineIfBlockSyntax) As StatementSyntax
            Return ifBlock.GetPreviousStatement()
        End Function
    End Class
End Namespace
