' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseObjectInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseObjectInitializerDiagnosticAnalyzer
        Inherits AbstractUseObjectInitializerDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            AssignmentStatementSyntax,
            VariableDeclaratorSyntax)

        Protected Overrides ReadOnly Property FadeOutOperatorToken As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function AreObjectInitializersSupported(context As SyntaxNodeAnalysisContext) As Boolean
            'Object Initializers are supported in all the versions of Visual Basic we support
            Return True
        End Function

        Protected Overrides Function GetObjectCreationSyntaxKind() As SyntaxKind
            Return SyntaxKind.ObjectCreationExpression
        End Function

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function GetNewObjectCreation(
                objectCreation As ObjectCreationExpressionSyntax,
                matches As ImmutableArray(Of Match)) As ObjectCreationExpressionSyntax

            Dim initializer = SyntaxFactory.ObjectMemberInitializer(
                CreateFieldInitializers(matches))

            Return objectCreation.WithoutTrailingTrivia().
                                  WithInitializer(initializer).
                                  WithTrailingTrivia(objectCreation.GetTrailingTrivia())
        End Function

        Private Function CreateFieldInitializers(
                matches As ImmutableArray(Of Match)) As SeparatedSyntaxList(Of FieldInitializerSyntax)
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
                    name:=DirectCast(match.MemberAccessExpression.Name, IdentifierNameSyntax),
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