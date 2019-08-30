' Copyright(c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt In the project root For license information

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.ConvertForEachToFor
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Operations

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertForEachToFor
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertForEachToForCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicConvertForEachToForCodeRefactoringProvider
        Inherits AbstractConvertForEachToForCodeRefactoringProvider(Of ForEachBlockSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property Title As String = VBFeaturesResources.Convert_to_For

        Protected Overrides Function IsValid(foreachNode As ForEachBlockSyntax) As Boolean
            ' we don't support colon separated statements
            Return Not foreachNode.DescendantTrivia().Any(Function(t) t.IsKind(SyntaxKind.ColonTrivia))
        End Function

        Protected Overrides Function ValidLocation(foreachInfo As ForEachInfo) As Boolean
            ' all places where for each can appear is valid location for vb
            Return True
        End Function

        Protected Overrides Function GetForEachBody(foreachBlock As ForEachBlockSyntax) As (start As SyntaxNode, [end] As SyntaxNode)
            If foreachBlock.Statements.Count = 0 Then
                Return Nothing
            End If

            Return (foreachBlock.Statements(0), foreachBlock.Statements(foreachBlock.Statements.Count - 1))
        End Function

        Protected Overrides Sub ConvertToForStatement(model As SemanticModel, foreachInfo As ForEachInfo, editor As SyntaxEditor, cancellationToken As CancellationToken)
            cancellationToken.ThrowIfCancellationRequested()

            Dim generator = editor.Generator
            Dim forEachBlock = foreachInfo.ForEachStatement

            ' trailing triva of expression will be attached to for statement below
            Dim foreachCollectionExpression = forEachBlock.ForEachStatement.Expression
            Dim collectionVariable = GetCollectionVariableName(
                model, generator, foreachInfo, foreachCollectionExpression, cancellationToken)

            ' make sure we get rid of all comments from expression since that will be re-attached to for statement
            Dim expression = foreachCollectionExpression.WithTrailingTrivia(
                    foreachCollectionExpression.GetTrailingTrivia().Where(Function(t) t.IsWhitespaceOrEndOfLine()))

            ' and remove all trailing trivia if it is used for cast
            If foreachInfo.RequireExplicitCastInterface Then
                expression = expression.WithoutTrailingTrivia()
            End If

            ' first, see whether we need to introduce New statement to capture collection
            IntroduceCollectionStatement(model, foreachInfo, editor, type:=Nothing, expression, collectionVariable)

            ' create New index varialbe name
            Dim indexVariable = If(
                forEachBlock.Statements.Count = 0,
                generator.Identifier("i"),
                CreateUniqueName(foreachInfo.SemanticFacts, model, forEachBlock.Statements(0), "i", cancellationToken))

            ' put variable statement in body
            Dim bodyStatement = GetForLoopBody(generator, foreachInfo, collectionVariable, indexVariable)

            Dim nextStatement = forEachBlock.NextStatement

            If nextStatement.ControlVariables.Count > 0 Then
                Debug.Assert(nextStatement.ControlVariables.Count = 1)

                Dim controlVariable As SyntaxNode = nextStatement.ControlVariables(0)
                controlVariable = generator.IdentifierName(
                    indexVariable _
                        .WithLeadingTrivia(controlVariable.GetFirstToken().LeadingTrivia) _
                        .WithTrailingTrivia(controlVariable.GetLastToken().TrailingTrivia))

                nextStatement = nextStatement.WithControlVariables(
                    SyntaxFactory.SingletonSeparatedList(controlVariable))
            End If

            ' create for statement from foreach statement
            Dim forBlock = SyntaxFactory.ForBlock(
                SyntaxFactory.ForStatement(
                    DirectCast(generator.IdentifierName(indexVariable.WithAdditionalAnnotations(RenameAnnotation.Create())), VisualBasicSyntaxNode),
                    DirectCast(generator.LiteralExpression(0), ExpressionSyntax),
                    DirectCast(generator.SubtractExpression(
                        generator.MemberAccessExpression(
                            collectionVariable, foreachInfo.CountName), generator.LiteralExpression(1)), ExpressionSyntax)),
                bodyStatement,
                nextStatement)

            If foreachInfo.RequireCollectionStatement Then
                ' this is to remove blank line between newly added collection statement with "For" keyword.
                ' default VB formatting rule around elastic trivia and end of line trivia is converting elastic trivia
                ' to end of line trivia causing there to be 2 line breaks. this fix that issue. not changing the default
                ' rule since it will affect other ones that having opposite desire.
                forBlock = forBlock.WithLeadingTrivia(SyntaxFactory.TriviaList())
            Else
                ' transfer comment on "For Each" to "For"
                forBlock = forBlock.WithLeadingTrivia(forEachBlock.GetLeadingTrivia())
            End If

            ' transfer comment at then end of "For Each" to "For"
            forBlock = forBlock.WithForStatement(forBlock.ForStatement.WithTrailingTrivia(forEachBlock.ForEachStatement.GetLastToken().TrailingTrivia))

            editor.ReplaceNode(forEachBlock, forBlock)
        End Sub

        Private Function GetForLoopBody(
            generator As SyntaxGenerator, foreachInfo As ForEachInfo,
            collectionVariableName As SyntaxNode, indexVariable As SyntaxToken) As SyntaxList(Of StatementSyntax)

            Dim forEachBlock = foreachInfo.ForEachStatement
            If forEachBlock.Statements.Count = 0 Then
                Return forEachBlock.Statements
            End If

            Dim foreachVariable As SyntaxNode = Nothing
            Dim type As SyntaxNode = Nothing
            GetVariableNameAndType(forEachBlock.ForEachStatement, foreachVariable, type)

            ' use original text
            Dim foreachVariableToken = generator.Identifier(foreachVariable.ToString())

            ' create variable statement
            Dim variableStatement = AddItemVariableDeclaration(
                generator, type, foreachVariableToken, foreachInfo.ForEachElementType, collectionVariableName, indexVariable)

            ' Nested loops might not have a Next statement
            If IsForEachVariableWrittenInside Then
                variableStatement = variableStatement.WithAdditionalAnnotations(CreateWarningAnnotation())
            End If

            Return forEachBlock.Statements.Insert(0, DirectCast(variableStatement, StatementSyntax))
        End Function

        Private Sub GetVariableNameAndType(
            forEachStatement As ForEachStatementSyntax, ByRef foreachVariable As SyntaxNode, ByRef type As SyntaxNode)

            Dim controlVariable = forEachStatement.ControlVariable

            Dim declarator = TryCast(controlVariable, VariableDeclaratorSyntax)
            If declarator IsNot Nothing Then
                foreachVariable = declarator.Names(0)
                type = declarator.AsClause.Type
            Else
                foreachVariable = controlVariable
                type = Nothing
            End If
        End Sub

        Protected Overrides Function IsSupported(foreachVariable As ILocalSymbol, foreachOperation As IForEachLoopOperation, foreachStatement As ForEachBlockSyntax) As Boolean
            ' VB can have Next variable. but we only support
            ' simple 1 variable case.
            If foreachOperation.NextVariables.Length > 1 Then
                Return False
            End If

            If foreachOperation.NextVariables.IsEmpty AndAlso foreachStatement.NextStatement Is Nothing Then
                Return False
            End If

            ' It is okay to omit variable in next, but if it presents, it must be same as one in the loop
            If Not foreachOperation.NextVariables.IsEmpty Then
                Dim nextVariable = TryCast(foreachOperation.NextVariables(0), ILocalReferenceOperation)
                If nextVariable Is Nothing OrElse nextVariable.Local?.Equals(foreachVariable) = False Then
                    ' We do not support anything else than local reference for next variable
                    ' operation
                    Return False
                End If
            End If

            Return True
        End Function
    End Class
End Namespace
