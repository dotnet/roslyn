' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.CodeFixes.VisualBasic
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=RoslynDiagnosticIds.DirectlyAwaitingTaskAnalyzerRuleId), [Shared]>
    Public Class BasicDirectlyAwaitingTaskFix
        Inherits DirectlyAwaitingTaskFix(Of ExpressionSyntax)

        Protected Overrides Function FixExpression(expression As ExpressionSyntax, cancellationToken As CancellationToken) As ExpressionSyntax
            Return _
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParenthesizedExpression(expression).WithAdditionalAnnotations(Simplifier.Annotation),
                        SyntaxFactory.Token(SyntaxKind.DotToken),
                        SyntaxFactory.IdentifierName("ConfigureAwait")),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(Of ArgumentSyntax)(
                            SyntaxFactory.SimpleArgument(
                                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression, SyntaxFactory.Token(SyntaxKind.FalseKeyword))))))
        End Function

        Protected Overrides ReadOnly Property FalseLiteralString As String
            Get
                Return "False"
            End Get
        End Property
    End Class
End Namespace
