' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend NotInheritable Class VisualBasicInvertSingleLineIfCodeRefactoringProvider
        Inherits VisualBasicInvertIfCodeRefactoringProvider(Of SingleLineIfStatementSyntax)

        Protected Overrides Function GetInvertedIfNode(
                ifNode As SingleLineIfStatementSyntax,
                negatedExpression As ExpressionSyntax) As SingleLineIfStatementSyntax

            Dim elseClause = ifNode.ElseClause

            ' If we're moving a single line if from the else body to the if body,
            ' and it is the last statement in the body, we have to introduce an extra
            ' StatementTerminator Colon and Else token.
            Dim newIfStatements = elseClause.Statements

            If newIfStatements.Count > 0 Then
                newIfStatements = newIfStatements.Replace(
                    newIfStatements.Last,
                    newIfStatements.Last.WithTrailingTrivia(elseClause.ElseKeyword.GetPreviousToken().TrailingTrivia))
            End If

            If elseClause.Statements.Count > 0 AndAlso
               elseClause.Statements.Last().Kind = SyntaxKind.SingleLineIfStatement Then

                Dim singleLineIf = DirectCast(elseClause.Statements.Last, SingleLineIfStatementSyntax)

                ' Create an Extra 'Else'
                If singleLineIf.ElseClause Is Nothing Then

                    ' Replace the last EOL of the IfPart with a :
                    Dim trailing = singleLineIf.GetTrailingTrivia()
                    If trailing.Any(SyntaxKind.EndOfLineTrivia) Then
                        Dim eol = trailing.Last(Function(t) t.Kind = SyntaxKind.EndOfLineTrivia)
                        trailing = trailing.Select(Function(t) If(t = eol, SyntaxFactory.ColonTrivia(SyntaxFacts.GetText(SyntaxKind.ColonTrivia)), t)).ToSyntaxTriviaList()
                    End If

                    Dim withElsePart = singleLineIf.WithTrailingTrivia(trailing).WithElseClause(
                        SyntaxFactory.SingleLineElseClause(SyntaxFactory.List(Of StatementSyntax)()))

                    ' Put the if statement with the else into the statement list
                    newIfStatements = elseClause.Statements.Replace(elseClause.Statements.Last, withElsePart)
                End If
            End If

            Return ifNode.WithCondition(negatedExpression) _
                         .WithStatements(newIfStatements) _
                         .WithElseClause(elseClause.WithStatements(ifNode.Statements).WithTrailingTrivia(elseClause.GetTrailingTrivia()))
        End Function


        Protected Overrides Function GetHeaderSpan(ifNode As SingleLineIfStatementSyntax) As TextSpan
            Return TextSpan.FromBounds(
                    ifNode.IfKeyword.SpanStart,
                    ifNode.Condition.Span.End)
        End Function

        Protected Overrides Function IsElseless(ifNode As SingleLineIfStatementSyntax) As Boolean
            Return ifNode.ElseClause Is Nothing
        End Function

        Protected Overrides Function CanInvert(ifNode As SingleLineIfStatementSyntax) As Boolean
            If ifNode.IsParentKind(SyntaxKind.SingleLineSubLambdaExpression) Then
                If ifNode.ElseClause Is Nothing Then
                    Return False
                End If

                If ifNode.Parent.IsParentKind(SyntaxKind.EqualsValue) AndAlso
                   ifNode.Parent.Parent.IsParentKind(SyntaxKind.VariableDeclarator) AndAlso
                   ifNode.Parent.Parent.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement) Then
                    Return DirectCast(ifNode.Parent.Parent.Parent.Parent, LocalDeclarationStatementSyntax).Declarators.Count = 1
                End If
            End If

            Return True
        End Function

        Protected Overrides Function GetCondition(ifNode As SingleLineIfStatementSyntax) As SyntaxNode
            Return ifNode.Condition
        End Function
    End Class
End Namespace
