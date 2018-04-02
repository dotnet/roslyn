' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.UseConditionalExpression
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseConditionalExpressionForAssignmentCodeRefactoringProvider
        Inherits AbstractUseConditionalExpressionForAssignmentCodeFixProvider(Of
            LocalDeclarationStatementSyntax, VariableDeclaratorSyntax, ExpressionSyntax)

        Protected Overrides Function GetMultiLineFormattingRule() As IFormattingRule
            Return MultiLineConditionalExpressionFormattingRule.Instance
        End Function

        Protected Overrides Function WithInitializer(variable As VariableDeclaratorSyntax, value As ExpressionSyntax) As VariableDeclaratorSyntax
            Return variable.WithoutTrivia().WithInitializer(SyntaxFactory.EqualsValue(value)).
                                            WithTriviaFrom(variable)
        End Function

        Protected Overrides Function GetDeclaratorSyntax(declarator As IVariableDeclaratorOperation) As VariableDeclaratorSyntax
            Return DirectCast(declarator.Syntax.Parent, VariableDeclaratorSyntax)
        End Function

        Protected Overrides Function AddSimplificationToType(statement As LocalDeclarationStatementSyntax) As LocalDeclarationStatementSyntax
            Dim declarator = statement.Declarators(0)
            Return statement.ReplaceNode(declarator, declarator.WithAdditionalAnnotations(Simplifier.Annotation))
        End Function

        Private Class MultiLineConditionalExpressionFormattingRule
            Inherits AbstractFormattingRule

            Public Shared ReadOnly Instance As New MultiLineConditionalExpressionFormattingRule()

            Private Sub New()
            End Sub

            Private Function IsCommaOfNewConditional(token As SyntaxToken) As Boolean
                If token.Kind() = SyntaxKind.CommaToken Then
                    Return token.Parent.HasAnnotation(
                        UseConditionalExpressionForAssignmentHelpers.SpecializedFormattingAnnotation)
                End If

                Return False
            End Function

            Public Overrides Function GetAdjustNewLinesOperation(
                previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, nextOperation As NextOperation(Of AdjustNewLinesOperation)) As AdjustNewLinesOperation
                If IsCommaOfNewConditional(previousToken) Then
                    ' We want to force the expressions after the commas to be put on the 
                    ' next line.
                    Return FormattingOperations.CreateAdjustNewLinesOperation(
                        1, AdjustNewLinesOption.ForceLines)
                End If

                Return nextOperation.Invoke()
            End Function

            Public Overrides Sub AddIndentBlockOperations(
                list As List(Of IndentBlockOperation), node As SyntaxNode, optionSet As OptionSet, nextOperation As NextAction(Of IndentBlockOperation))

                If node.HasAnnotation(UseConditionalExpressionForAssignmentHelpers.SpecializedFormattingAnnotation) AndAlso
                        TypeOf node Is TernaryConditionalExpressionSyntax Then

                    Dim conditional = TryCast(node, TernaryConditionalExpressionSyntax)
                    Dim statement = conditional.FirstAncestorOrSelf(Of StatementSyntax)()
                    If statement IsNot Nothing Then
                        Dim baseToken = statement.GetFirstToken()

                        ' we want to indent the true and false conditions in one level from the
                        ' containing statement.
                        list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                            baseToken, conditional.WhenTrue.GetFirstToken(), conditional.WhenTrue.GetLastToken(),
                            indentationDelta:=1, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine))
                        list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                            baseToken, conditional.WhenFalse.GetFirstToken(), conditional.WhenFalse.GetLastToken(),
                            indentationDelta:=1, IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine))
                        Return
                    End If
                End If

                nextOperation.Invoke(list)
            End Sub
        End Class
    End Class
End Namespace
