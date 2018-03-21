' Copyright(c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt In the project root For license information

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.ForeachToFor
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ForeachToFor
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicForEachToForCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicForEachToForCodeRefactoringProvider
        Inherits AbstractForEachToForCodeRefactoringProvider

        Protected Overrides Function GetForEachStatement(selection As TextSpan, token As SyntaxToken) As SyntaxNode
            Dim foreachBlock = token.Parent.FirstAncestorOrSelf(Of ForEachBlockSyntax)()
            If foreachBlock Is Nothing Then
                Return Nothing
            End If

            ' support refactoring only if caret Is on for each statement
            Dim scope = foreachBlock.ForEachStatement.Span
            If Not scope.IntersectsWith(selection) Then
                Return Nothing
            End If

            ' we don't support colon seperated statements
            If foreachBlock.DescendantTrivia().Any(Function(t) t.IsKind(SyntaxKind.ColonTrivia)) Then
                Return Nothing
            End If

            ' check whether there Is any comments or line continuation within foreach statement
            ' if they do, we don't support conversion.
            For Each trivia In foreachBlock.ForEachStatement.DescendantTrivia()
                If trivia.Span.End <= scope.Start OrElse
                   scope.End <= trivia.Span.Start Then
                    Continue For
                End If

                If trivia.Kind() <> SyntaxKind.WhitespaceTrivia AndAlso
                   trivia.Kind() <> SyntaxKind.EndOfLineTrivia AndAlso
                   trivia.Kind() <> SyntaxKind.LineContinuationTrivia Then
                    ' we don't know what to do with these
                    Return Nothing
                End If
            Next

            Return foreachBlock
        End Function

        Protected Overrides Function ValidLocation(foreachInfo As ForEachInfo) As Boolean
            ' all places where for each can appear is valid location for vb
            Return True
        End Function

        Protected Overrides Function GetForEachBody(node As SyntaxNode) As (start As SyntaxNode, [end] As SyntaxNode)
            Dim foreachBlock = DirectCast(node, ForEachBlockSyntax)
            If foreachBlock.Statements.Count = 0 Then
                Return Nothing
            End If

            Return (foreachBlock.Statements(0), foreachBlock.Statements(foreachBlock.Statements.Count - 1))
        End Function

        Protected Overrides Sub ConvertToForStatement(model As SemanticModel, foreachInfo As ForEachInfo, editor As SyntaxEditor, cancellationToken As CancellationToken)
            cancellationToken.ThrowIfCancellationRequested()

            Dim generator = editor.Generator
            Dim forEachBlock = DirectCast(foreachInfo.ForEachStatement, ForEachBlockSyntax)

            ' trailing triva of expression will be attached to for statement below
            Dim foreachCollectionExpression = forEachBlock.ForEachStatement.Expression
            Dim collectionVariableName = GetCollectionVariableName(model, foreachInfo, foreachCollectionExpression)

            ' make sure we get rid of all comments from expression since that will be re-attached to for statement
            Dim expression = foreachCollectionExpression.WithTrailingTrivia(
                    foreachCollectionExpression.GetTrailingTrivia().Where(Function(t) t.IsWhitespaceOrEndOfLine()))

            ' and remove all trailing trivia if it is used for cast
            If foreachInfo.RequireExplicitCast Then
                expression = expression.WithoutTrailingTrivia()
            End If

            ' first, see whether we need to introduce New statement to capture collection
            IntroduceCollectionStatement(model, foreachInfo, editor, expression, collectionVariableName)

            ' create New index varialbe name
            Dim indexString = If(forEachBlock.Statements.Count = 0, "i", CreateUniqueName(model, forEachBlock.Statements(0), "i"))

            ' put variable statement in body
            Dim bodyStatement = GetForLoopBody(generator, foreachInfo, collectionVariableName, indexString)

            Dim nextStatement = forEachBlock.NextStatement

            If nextStatement.ControlVariables.Count > 0 Then
                Contract.Requires(nextStatement.ControlVariables.Count = 1)

                Dim controlVariable As SyntaxNode = nextStatement.ControlVariables(0)
                controlVariable = generator.IdentifierName(
                    generator.Identifier(indexString) _
                        .WithLeadingTrivia(controlVariable.GetFirstToken().LeadingTrivia) _
                        .WithTrailingTrivia(controlVariable.GetLastToken().TrailingTrivia))

                nextStatement = nextStatement.WithControlVariables(
                    SyntaxFactory.SingletonSeparatedList(controlVariable))
            End If

            ' create for statement from foreach statement
            Dim forStatement = SyntaxFactory.ForBlock(
                SyntaxFactory.ForStatement(
                    DirectCast(generator.IdentifierName(generator.Identifier(indexString).WithAdditionalAnnotations(RenameAnnotation.Create())), VisualBasicSyntaxNode),
                    DirectCast(generator.LiteralExpression(0), ExpressionSyntax),
                    DirectCast(generator.SubtractExpression(
                        generator.MemberAccessExpression(
                            generator.IdentifierName(collectionVariableName), foreachInfo.CountName), generator.LiteralExpression(1)), ExpressionSyntax)),
                bodyStatement,
                nextStatement)

            ' let leading And trailing trivia set
            If foreachInfo.RequireCollectionStatement Then
                forStatement = forStatement.WithLeadingTrivia(SyntaxFactory.TriviaList())
            Else
                forStatement = forStatement.WithLeadingTrivia(forEachBlock.ForEachStatement.ForKeyword.LeadingTrivia)
            End If

            forStatement = forStatement.WithForStatement(forStatement.ForStatement.WithTrailingTrivia(forEachBlock.ForEachStatement.GetLastToken().TrailingTrivia))

            editor.ReplaceNode(forEachBlock, forStatement)
        End Sub

        Private Function GetForLoopBody(generator As SyntaxGenerator, foreachInfo As ForEachInfo, collectionVariableName As String, indexString As String) As SyntaxList(Of StatementSyntax)
            Dim foreachStatement = DirectCast(foreachInfo.ForEachStatement, ForEachBlockSyntax)
            If foreachStatement.Statements.Count = 0 Then
                Return foreachStatement.Statements
            End If

            ' use original text
            Dim foreachVariableString = foreachStatement.ForEachStatement.ControlVariable.ToString()

            ' create varialbe statement
            Dim variableStatement = AddItemVariableDeclaration(generator, foreachVariableString, collectionVariableName, indexString)

            Return foreachStatement.Statements.Insert(0, DirectCast(variableStatement, StatementSyntax))
        End Function
    End Class
End Namespace
