' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InitializeParameter
    Friend Class InitializeParameterHelpers
        Public Shared Function IsFunctionDeclaration(node As SyntaxNode) As Boolean
            Return TypeOf node Is MethodBlockBaseSyntax OrElse
                   TypeOf node Is LambdaExpressionSyntax
        End Function

        Public Shared Function GetBody(node As SyntaxNode) As SyntaxNode
            Return node
        End Function

        Private Shared Function GetStatements(functionDeclaration As SyntaxNode) As SyntaxList(Of StatementSyntax)
            If TypeOf functionDeclaration Is MethodBlockBaseSyntax Then
                Dim methodBlock = DirectCast(functionDeclaration, MethodBlockBaseSyntax)
                Return methodBlock.Statements
            ElseIf TypeOf functionDeclaration Is MultiLineLambdaExpressionSyntax Then
                Dim multiLineLambda = DirectCast(functionDeclaration, MultiLineLambdaExpressionSyntax)
                Return multiLineLambda.Statements
            ElseIf TypeOf functionDeclaration Is SingleLineLambdaExpressionSyntax Then
                Dim singleLineLambda = DirectCast(functionDeclaration, SingleLineLambdaExpressionSyntax)
                Dim convertedStatement = If(TypeOf singleLineLambda.Body Is StatementSyntax,
                    DirectCast(singleLineLambda.Body, StatementSyntax),
                    SyntaxFactory.ReturnStatement(DirectCast(singleLineLambda.Body, ExpressionSyntax)))
                Return SyntaxFactory.List(ImmutableArray.Create(convertedStatement))
            Else
                Throw ExceptionUtilities.UnexpectedValue(functionDeclaration)
            End If
        End Function

        Public Shared Function IsImplicitConversion(compilation As Compilation, source As ITypeSymbol, destination As ITypeSymbol) As Boolean
            Return compilation.ClassifyConversion(source:=source, destination:=destination).IsWidening
        End Function

        Public Shared Function TryGetLastStatement(blockStatementOpt As IBlockOperation) As SyntaxNode
            Return GetStatements(blockStatementOpt.Syntax).LastOrDefault()
        End Function

        Public Shared Sub InsertStatement(
                editor As SyntaxEditor,
                functionDeclaration As SyntaxNode,
                statementToAddAfterOpt As SyntaxNode,
                statement As StatementSyntax)

            If statementToAddAfterOpt IsNot Nothing Then
                editor.InsertAfter(statementToAddAfterOpt, statement)
            Else
                Dim newStatements = GetStatements(functionDeclaration).Insert(0, statement)
                editor.SetStatements(functionDeclaration, newStatements)
            End If
        End Sub
    End Class
End Namespace
