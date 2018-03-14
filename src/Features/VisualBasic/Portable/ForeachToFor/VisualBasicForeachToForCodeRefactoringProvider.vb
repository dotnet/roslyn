' Copyright(c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt In the project root For license information

Imports System.Composition
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.ForeachToFor
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ForeachToFor
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicForEachToForCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicForEachToForCodeRefactoringProvider
        Inherits AbstractForEachToForCodeRefactoringProvider

        Protected Overrides Function GetForEachStatement(token As SyntaxToken) As SyntaxNode
            Dim foreachBlock = token.Parent.FirstAncestorOrSelf(Of ForEachBlockSyntax)()
            If foreachBlock Is Nothing Then
                Return Nothing
            End If

            ' support refactoring only if caret Is on for each statement
            If Not foreachBlock.ForEachStatement.Span.IntersectsWith(token.Span) Then
                Return Nothing
            End If

            ' we don't support colon seperated statements
            If foreachBlock.DescendantTrivia().Any(Function(t) t.IsKind(SyntaxKind.ColonTrivia)) Then
                Return Nothing
            End If

            Return foreachBlock
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
            Dim foreachStatement = DirectCast(foreachInfo.ForEachStatement, ForEachBlockSyntax)

            ' this expression Is from user code. don't simplify this.
            Dim foreachCollectionExpression = foreachStatement.ForEachStatement.Expression.WithoutAnnotations(SimplificationHelpers.DontSimplifyAnnotation)
            Dim collectionVariableName = foreachCollectionExpression.ToString()

            ' first, see whether we need to introduce New statement to capture collection
            If foreachInfo.RequireCollectionStatement Then
                collectionVariableName = CreateUniqueName(model, foreachStatement, "list")

                Dim collectionStatement = generator.LocalDeclarationStatement(
                    collectionVariableName,
                    If(foreachInfo.RequireExplicitCast,
                        DirectCast(generator.CastExpression(foreachInfo.ExplicitCastInterface, foreachCollectionExpression), CastExpressionSyntax),
                        foreachCollectionExpression))

                collectionStatement = AddRenameAnnotation(
                    collectionStatement.WithLeadingTrivia(foreachStatement.ForEachStatement.ForKeyword.LeadingTrivia), collectionVariableName)

                editor.InsertBefore(foreachStatement, collectionStatement)
            End If

            ' create New index varialbe name
            Dim indexString = CreateUniqueName(model, foreachStatement, "i")

            ' put variable statement in body
            Dim bodyStatement = GetForLoopBody(generator, foreachInfo, collectionVariableName, indexString)

            ' create for statement from foreach statement
            Dim forStatement = SyntaxFactory.ForBlock(
                SyntaxFactory.ForStatement(
                    DirectCast(generator.IdentifierName(generator.Identifier(indexString).WithAdditionalAnnotations(RenameAnnotation.Create())), VisualBasicSyntaxNode),
                    DirectCast(generator.LiteralExpression(0), ExpressionSyntax),
                    DirectCast(generator.SubtractExpression(
                        generator.MemberAccessExpression(
                            generator.IdentifierName(collectionVariableName), foreachInfo.CountName), generator.LiteralExpression(1)), ExpressionSyntax)),
                bodyStatement,
                SyntaxFactory.NextStatement())

            ' let leading And trailing trivia set
            If Not foreachInfo.RequireCollectionStatement Then
                forStatement = forStatement.WithLeadingTrivia(foreachStatement.ForEachStatement.ForKeyword.LeadingTrivia)
            End If

            forStatement = forStatement.WithForStatement(forStatement.ForStatement.WithTrailingTrivia(foreachStatement.ForEachStatement.GetLastToken().TrailingTrivia))

            editor.ReplaceNode(foreachStatement, forStatement)
        End Sub

        Private Function GetForLoopBody(generator As SyntaxGenerator, foreachInfo As ForEachInfo, collectionVariableName As String, indexString As String) As SyntaxList(Of StatementSyntax)
            Dim foreachStatement = DirectCast(foreachInfo.ForEachStatement, ForEachBlockSyntax)
            If foreachStatement.Statements.Count = 0 Then
                Return foreachStatement.Statements
            End If

            ' use original text
            Dim foreachVariableString = foreachStatement.ForEachStatement.ControlVariable.ToString()

            ' create varialbe statement
            Dim variableStatement = generator.LocalDeclarationStatement(
                foreachVariableString,
                generator.ElementAccessExpression(
                    generator.IdentifierName(collectionVariableName), generator.IdentifierName(indexString)))

            Return foreachStatement.Statements.Insert(0, DirectCast(variableStatement, StatementSyntax))
        End Function
    End Class
End Namespace
