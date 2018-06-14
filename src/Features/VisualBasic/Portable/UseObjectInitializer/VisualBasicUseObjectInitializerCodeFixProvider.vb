' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.UseObjectInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseObjectInitializer), [Shared]>
    Friend Class VisualBasicUseObjectInitializerCodeFixProvider
        Inherits AbstractUseObjectInitializerCodeFixProvider(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            AssignmentStatementSyntax,
            VariableDeclaratorSyntax)

        Protected Overrides Function GetNewStatement(
                statement As StatementSyntax, objectCreation As ObjectCreationExpressionSyntax,
                matches As ImmutableArray(Of Match(Of ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, AssignmentStatementSyntax))) As StatementSyntax
            Dim newStatement = statement.ReplaceNode(
                objectCreation,
                GetNewObjectCreation(objectCreation, matches))

            Dim totalTrivia = ArrayBuilder(Of SyntaxTrivia).GetInstance()
            totalTrivia.AddRange(statement.GetLeadingTrivia())
            totalTrivia.Add(SyntaxFactory.ElasticMarker)

            For Each match In matches
                For Each trivia In match.Statement.GetLeadingTrivia()
                    If trivia.Kind = SyntaxKind.CommentTrivia Then
                        totalTrivia.Add(trivia)
                        totalTrivia.Add(SyntaxFactory.ElasticMarker)
                    End If
                Next
            Next

            Return newStatement.WithLeadingTrivia(totalTrivia)
        End Function

        Private Function GetNewObjectCreation(
                objectCreation As ObjectCreationExpressionSyntax,
                matches As ImmutableArray(Of Match(Of ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, AssignmentStatementSyntax))) As ObjectCreationExpressionSyntax

            Return UseInitializerHelpers.GetNewObjectCreation(
                objectCreation,
                SyntaxFactory.ObjectMemberInitializer(
                    CreateFieldInitializers(matches)))
        End Function

        Private Function CreateFieldInitializers(
                matches As ImmutableArray(Of Match(Of ExpressionSyntax, StatementSyntax, MemberAccessExpressionSyntax, AssignmentStatementSyntax))) As SeparatedSyntaxList(Of FieldInitializerSyntax)
            Dim nodesAndTokens = New List(Of SyntaxNodeOrToken)

            For i = 0 To matches.Length - 1
                Dim match = matches(i)

                Dim rightValue = match.Initializer
                If i < matches.Count - 1 Then
                    rightValue = rightValue.WithoutTrailingTrivia()
                End If

                Dim initializer = SyntaxFactory.NamedFieldInitializer(
                    keyKeyword:=Nothing,
                    dotToken:=match.MemberAccessExpression.OperatorToken,
                    name:=SyntaxFactory.IdentifierName(match.MemberName),
                    equalsToken:=match.Statement.OperatorToken,
                    expression:=rightValue).WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker)

                nodesAndTokens.Add(initializer)
                If i < matches.Length - 1 Then
                    Dim comma = SyntaxFactory.Token(SyntaxKind.CommaToken).
                                              WithTrailingTrivia(match.Initializer.GetTrailingTrivia())
                    nodesAndTokens.Add(comma)
                End If
            Next

            Return SyntaxFactory.SeparatedList(Of FieldInitializerSyntax)(nodesAndTokens)
        End Function
    End Class
End Namespace
