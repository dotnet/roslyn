' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
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

        Protected Overrides Function FixupLocalDeclaration(expressionStatement As ExpressionStatementSyntax, localDeclaration As LocalDeclarationStatementSyntax) As LocalDeclarationStatementSyntax
            ' Don't want an 'as clause' in our local decl. it's idiomatic already to
            ' just do `dim x = new DateTime()`
            Return localDeclaration.RemoveNode(
                localDeclaration.Declarators(0).AsClause,
                SyntaxRemoveOptions.KeepUnbalancedDirectives Or SyntaxRemoveOptions.AddElasticMarker)
        End Function
    End Class
End Namespace
