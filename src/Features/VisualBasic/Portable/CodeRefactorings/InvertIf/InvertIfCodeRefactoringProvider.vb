' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    Friend MustInherit Class VisualBasicInvertIfCodeRefactoringProvider(Of TIfStatementSyntax As ExecutableStatementSyntax)
        Inherits AbstractInvertIfCodeRefactoringProvider(Of TIfStatementSyntax, SyntaxList(Of StatementSyntax))

        Protected NotOverridable Overrides Function GetTitle() As String
            Return VBFeaturesResources.Invert_If
        End Function

        Protected NotOverridable Overrides Function GetIfBodyStatementRange(ifNode As TIfStatementSyntax) As StatementRange
            Dim statements = ifNode.GetStatements()
            Return If(statements.Count = 0, Nothing, New StatementRange(statements.First(), statements.Last()))
        End Function

        Protected NotOverridable Overrides Function CanControlFlowOut(node As SyntaxNode) As Boolean
            Return TypeOf node IsNot MethodBlockBaseSyntax AndAlso
                   TypeOf node IsNot CaseBlockSyntax AndAlso
                   TypeOf node IsNot DoLoopBlockSyntax AndAlso
                   TypeOf node IsNot ForOrForEachBlockSyntax AndAlso
                   TypeOf node IsNot LambdaExpressionSyntax AndAlso
                   TypeOf node IsNot WhileBlockSyntax
        End Function

        Protected NotOverridable Overrides Function GetJumpStatementRawKind(node As SyntaxNode) As Integer
            If TypeOf node Is MethodBlockBaseSyntax OrElse
               TypeOf node Is LambdaExpressionSyntax Then
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

            Return -1
        End Function

        Protected NotOverridable Overrides Function IsStatementContainer(node As SyntaxNode) As Boolean
            Return node.IsStatementContainerNode()
        End Function

        Protected NotOverridable Overrides Function GetStatements(node As SyntaxNode) As SyntaxList(Of SyntaxNode)
            Return node.GetStatements()
        End Function

        Protected NotOverridable Overrides Function GetNextStatement(node As SyntaxNode) As SyntaxNode
            Dim parent = node.Parent
            Dim statements = parent.GetStatements
            Dim nextIndex = 1 + statements.IndexOf(DirectCast(node, StatementSyntax))
            If nextIndex < statements.Count - 1 Then
                Return statements(nextIndex)
            End If

            Return Nothing
        End Function

        Protected NotOverridable Overrides Function GetJumpStatement(rawKind As Integer) As SyntaxNode
            Select Case rawKind
                Case SyntaxKind.ReturnStatement
                    Return SyntaxFactory.ReturnStatement
                Case SyntaxKind.ExitSelectStatement
                    Return SyntaxFactory.ExitSelectStatement
                Case SyntaxKind.ContinueDoStatement
                    Return SyntaxFactory.ContinueDoStatement
                Case SyntaxKind.ContinueForStatement
                    Return SyntaxFactory.ContinueForStatement
                Case SyntaxKind.ContinueWhileStatement
                    Return SyntaxFactory.ContinueWhileStatement
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(rawKind)
            End Select
        End Function

        Protected NotOverridable Overrides Function IsNoOpSyntaxNode(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.EmptyStatement)
        End Function

        Protected NotOverridable Overrides Function IsStatement(node As SyntaxNode) As Boolean
            Return TypeOf node Is ExecutableStatementSyntax
        End Function

        Protected NotOverridable Overrides Function UnwrapBlock(ifBody As SyntaxList(Of StatementSyntax)) As IEnumerable(Of SyntaxNode)
            Return ifBody
        End Function

        Protected NotOverridable Overrides Function GetEmptyEmbeddedStatement() As SyntaxList(Of StatementSyntax)
            Return SyntaxFactory.List(Of StatementSyntax)
        End Function

        Protected NotOverridable Overrides Function AsEmbeddedStatement(originalStatement As SyntaxList(Of StatementSyntax), newStatements As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
            Return SyntaxFactory.List(newStatements)
        End Function

        Protected NotOverridable Overrides Function WithStatements(node As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return node.ReplaceStatements(SyntaxFactory.List(statements))
        End Function
    End Class
End Namespace
