' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.InvertIf
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertIf
    Friend MustInherit Class VisualBasicInvertIfCodeRefactoringProvider(Of TIfStatementSyntax As ExecutableStatementSyntax)
        Inherits AbstractInvertIfCodeRefactoringProvider(Of TIfStatementSyntax, StatementSyntax, SyntaxList(Of StatementSyntax))

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

        Protected NotOverridable Overrides Function GetStatements(node As SyntaxNode) As SyntaxList(Of StatementSyntax)
            Return node.GetStatements()
        End Function

        Protected NotOverridable Overrides Function GetNextStatement(node As StatementSyntax) As StatementSyntax
            Dim parent = node.Parent
            Dim statements = parent.GetStatements
            Dim nextIndex = 1 + statements.IndexOf(node)
            If nextIndex < statements.Count Then
                Return statements(nextIndex)
            End If

            Return Nothing
        End Function

        Protected NotOverridable Overrides Function GetJumpStatement(rawKind As Integer) As StatementSyntax
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

        Protected NotOverridable Overrides Function IsExecutableStatement(node As SyntaxNode) As Boolean
            Return TypeOf node Is ExecutableStatementSyntax
        End Function

        Protected NotOverridable Overrides Function UnwrapBlock(ifBody As SyntaxList(Of StatementSyntax)) As IEnumerable(Of StatementSyntax)
            Return ifBody
        End Function

        Protected NotOverridable Overrides Function GetEmptyEmbeddedStatement() As SyntaxList(Of StatementSyntax)
            Return SyntaxFactory.List(Of StatementSyntax)
        End Function

        Protected NotOverridable Overrides Function AsEmbeddedStatement(statements As IEnumerable(Of StatementSyntax), original As SyntaxList(Of StatementSyntax)) As SyntaxList(Of StatementSyntax)
            Return SyntaxFactory.List(statements)
        End Function

        Protected NotOverridable Overrides Function WithStatements(node As SyntaxNode, statements As IEnumerable(Of StatementSyntax)) As SyntaxNode
            Return node.ReplaceStatements(SyntaxFactory.List(statements))
        End Function

        Protected NotOverridable Overrides Function IsSingleStatementStatementRange(statementRange As StatementRange) As Boolean
            Return Not statementRange.IsEmpty AndAlso statementRange.FirstStatement Is statementRange.LastStatement
        End Function
    End Class
End Namespace
