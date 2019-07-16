' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicIntroduceLocalForExpressionCodeRefactoringProvider
        Inherits AbstractIntroduceLocalForExpressionCodeRefactoringProvider(Of
            ExpressionSyntax,
            StatementSyntax,
            ExpressionStatementSyntax,
            LocalDeclarationStatementSyntax)

        Protected Overrides Function IsValid(expressionStatement As ExpressionStatementSyntax, span As TextSpan) As Boolean
            ' Expression is likely too simple to want to offer to generate a local for.
            ' This leads to too many false cases where this is offered.
            If span.IsEmpty AndAlso expressionStatement.Expression.IsKind(SyntaxKind.IdentifierName) Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Async Function CreateLocalDeclarationAsync(
            document As Document, expressionStatement As ExpressionStatementSyntax, cancellationToken As CancellationToken) As Task(Of LocalDeclarationStatementSyntax)

            Dim expression = expressionStatement.Expression

            Dim uniqueName = Await GenerateUniqueNameAsync(document, expression, cancellationToken).configureawait(False)
            Dim declarator =
                SyntaxFactory.VariableDeclarator(SyntaxFactory.ModifiedIdentifier(uniqueName)).
                              WithInitializer(SyntaxFactory.EqualsValue(expression.WithoutLeadingTrivia()))

            Dim localDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.DimKeyword)),
                SyntaxFactory.SingletonSeparatedList(declarator)).WithLeadingTrivia(expression.GetLeadingTrivia())

            Return localDeclaration
        End Function
    End Class
End Namespace
