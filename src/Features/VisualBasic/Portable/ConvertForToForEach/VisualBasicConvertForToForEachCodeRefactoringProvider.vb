﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertForToForEach
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertForToForEach
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertForToForEachCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicConvertForToForEachCodeRefactoringProvider
        Inherits AbstractConvertForToForEachCodeRefactoringProvider(Of
            StatementSyntax,
            ForBlockSyntax,
            ExpressionSyntax,
            MemberAccessExpressionSyntax,
            TypeSyntax,
            VariableDeclaratorSyntax)

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Convert_to_For_Each
        End Function

        Protected Overrides Function IsValidCursorPosition(forBlock As ForBlockSyntax, cursorPos As Integer) As Boolean
            ' If there isn't a selection, then we allow the refactoring in the 'for' keyword, or at the end
            ' of hte for-statement signature.

            Dim forStatement = forBlock.ForStatement
            Dim startSpan = forStatement.ForKeyword.Span
            Dim endSpan = TextSpan.FromBounds(forStatement.Span.End, forStatement.FullSpan.End)

            Return startSpan.IntersectsWith(cursorPos) OrElse endSpan.IntersectsWith(cursorPos)
        End Function

        Protected Overrides Function GetBodyStatements(forStatement As ForBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return forStatement.Statements
        End Function

        Protected Overrides Function IsValidVariableDeclarator(firstVariable As VariableDeclaratorSyntax) As Boolean
            ' we only support local declarations in vb that have a single identifier.  i.e.
            '    dim x = list(i)
            Return firstVariable.Names.Count = 1
        End Function

        Protected Overrides Function TryGetForStatementComponents(
                forBlock As ForBlockSyntax,
                ByRef iterationVariable As SyntaxToken,
                ByRef initializer As ExpressionSyntax,
                ByRef memberAccess As MemberAccessExpressionSyntax,
                ByRef stepValueExpressionOpt As ExpressionSyntax,
                cancellationToken As CancellationToken) As Boolean

            Dim forStatement As ForStatementSyntax = forBlock.ForStatement
            Dim identifierName = TryCast(forStatement.ControlVariable, IdentifierNameSyntax)

            If identifierName IsNot Nothing Then
                iterationVariable = identifierName.Identifier

                If forStatement.FromValue IsNot Nothing Then
                    initializer = forStatement.FromValue

                    Dim subtraction = TryCast(forStatement.ToValue, BinaryExpressionSyntax)
                    If subtraction?.Kind() = SyntaxKind.SubtractExpression Then
                        Dim subtractionRight = TryCast(subtraction.Right, LiteralExpressionSyntax)
                        If TypeOf subtractionRight.Token.Value Is Integer AndAlso
                           DirectCast(subtractionRight.Token.Value, Integer) = 1 Then

                            memberAccess = TryCast(subtraction.Left, MemberAccessExpressionSyntax)
                            If memberAccess IsNot Nothing Then
                                stepValueExpressionOpt = forStatement.StepClause?.StepValue
                                Return True
                            End If
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        Protected Overrides Function ConvertForNode(
                currentFor As ForBlockSyntax, typeNode As TypeSyntax,
                foreachIdentifier As SyntaxToken, collectionExpression As ExpressionSyntax, iterationVariableType As ITypeSymbol, options As OptionSet) As SyntaxNode

            Dim forStatement = currentFor.ForStatement
            Return SyntaxFactory.ForEachBlock(
                SyntaxFactory.ForEachStatement(
                    forStatement.ForKeyword,
                    SyntaxFactory.Token(SyntaxKind.EachKeyword),
                    SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(foreachIdentifier)),
                        If(typeNode IsNot Nothing, SyntaxFactory.SimpleAsClause(typeNode), Nothing),
                        Nothing),
                    SyntaxFactory.Token(SyntaxKind.InKeyword),
                    collectionExpression).WithTriviaFrom(forStatement),
                currentFor.Statements,
                currentFor.NextStatement)
        End Function
    End Class
End Namespace
