' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    Friend MustInherit Class VisualBasicInvertIfCodeRefactoringProvider(Of TIfStatementSyntax As ExecutableStatementSyntax)
        Inherits AbstractInvertIfCodeRefactoringProvider(Of TIfStatementSyntax)

        Protected NotOverridable Overrides Function GetTitle() As String
            Return VBFeaturesResources.Invert_If
        End Function

        Protected NotOverridable Overrides Function IsEmptyStatementRange(statementRange As (first As SyntaxNode, last As SyntaxNode)) As Boolean
            Return statementRange.first Is Nothing OrElse statementRange.last Is Nothing
        End Function

        Protected NotOverridable Overrides Function GetIfBodyStatementRange(ifNode As TIfStatementSyntax) As (first As SyntaxNode, last As SyntaxNode)
            Dim statements = ifNode.GetStatements()
            Return (statements.FirstOrDefault(), statements.LastOrDefault())
        End Function

        Protected NotOverridable Overrides Iterator Function GetSubsequentStatementRanges(ifNode As TIfStatementSyntax) As IEnumerable(Of (first As SyntaxNode, last As SyntaxNode))
            Dim syntaxFacts = VisualBasicSyntaxFactsService.Instance

            Dim innerStatement As StatementSyntax = ifNode
            For Each node In ifNode.Ancestors
                Dim nextStatement = syntaxFacts.GetNextExecutableStatement(innerStatement)
                If nextStatement IsNot Nothing AndAlso node.IsStatementContainerNode() Then
                    Dim lastStatement = node.GetStatements().Last()
                    Debug.Assert(nextStatement.Parent IsNot Nothing)
                    Debug.Assert(nextStatement.Parent Is lastStatement.Parent)
                    Debug.Assert(nextStatement.SpanStart <= lastStatement.SpanStart)
                    Yield (nextStatement, lastStatement)
                End If

                If TypeOf node Is MethodBlockBaseSyntax OrElse
                   TypeOf node Is CaseBlockSyntax OrElse
                   TypeOf node Is DoLoopBlockSyntax OrElse
                   TypeOf node Is ForOrForEachBlockSyntax OrElse
                   TypeOf node Is WhileBlockSyntax Then
                    Exit Function
                End If

                If TypeOf node Is StatementSyntax Then
                    innerStatement = DirectCast(node, StatementSyntax)
                End If
            Next
        End Function

        Protected NotOverridable Overrides Function GetNearmostParentJumpStatementRawKind(ifNode As TIfStatementSyntax) As Integer
            For Each node In ifNode.Ancestors
                If TypeOf node Is MethodBlockBaseSyntax Then
                    Return SyntaxKind.ReturnStatement
                End If

                If TypeOf node Is CaseBlockSyntax Then
                    Return SyntaxKind.ExitSelectStatement
                End If

                If TypeOf node Is DoLoopBlockSyntax Then
                    Return SyntaxKind.ContinueDoStatement
                End If

                If TypeOf node Is ForOrForEachBlockSyntax Then
                    Return SyntaxKind.ContinueForStatement
                End If

                If TypeOf node Is WhileBlockSyntax Then
                    Return SyntaxKind.ContinueWhileStatement
                End If
            Next

            Throw ExceptionUtilities.Unreachable
        End Function

        Protected MustOverride Function GetInvertedIfNode(
            ifNode As TIfStatementSyntax,
            negatedExpression As ExpressionSyntax) As TIfStatementSyntax

        Protected NotOverridable Overrides Function GetRootWithInvertIfStatement(
            root As SyntaxNode,
            ifNode As TIfStatementSyntax,
            invertIfStyle As InvertIfStyle,
            subsequentSingleExitPointOpt As SyntaxNode,
            negatedExpression As SyntaxNode) As SyntaxNode
            Select Case invertIfStyle
                Case InvertIfStyle.Normal
                    Return root.ReplaceNode(ifNode, GetInvertedIfNode(ifNode, DirectCast(negatedExpression, ExpressionSyntax)))
                Case InvertIfStyle.SwapIfBodyWithSubsequentStatements
                    Exit Select
                Case InvertIfStyle.MoveSubsequentStatementsToIfBody
                    Exit Select
                Case InvertIfStyle.WithElseClause
                    Exit Select
                Case InvertIfStyle.MoveIfBodyToElseClause
                    Exit Select
                Case InvertIfStyle.WithSubsequentExitPointStatement
                    Exit Select
                Case InvertIfStyle.WithNearmostJumpStatement
                    Exit Select
                Case InvertIfStyle.WithNegatedCondition
                    Exit Select
            End Select

            Debug.WriteLine(invertIfStyle)

            Return root
        End Function
    End Class
End Namespace
