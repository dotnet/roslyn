' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.UseNullPropagation
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseNullPropagation), [Shared]>
    Friend Class VisualBasicUseNullPropagationCodeFixProvider
        Inherits AbstractUseNullPropagationCodeFixProvider(Of
            SyntaxKind,
            ExpressionSyntax,
            ExecutableStatementSyntax,
            TernaryConditionalExpressionSyntax,
            BinaryExpressionSyntax,
            InvocationExpressionSyntax,
            MemberAccessExpressionSyntax,
            ConditionalAccessExpressionSyntax,
            InvocationExpressionSyntax,
            InvocationExpressionSyntax,
            MultiLineIfBlockSyntax,
            ExpressionStatementSyntax,
            ArgumentListSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function ElementBindingExpression(argumentList As ArgumentListSyntax) As InvocationExpressionSyntax
            Return SyntaxFactory.InvocationExpression(Nothing, argumentList)
        End Function

        'Protected Overrides Function RewriteInvocation(whenTrueInvocation As InvocationExpressionSyntax, memberAccessExpression As MemberAccessExpressionSyntax) As ExpressionSyntax
        '    ' convert x.Y(...) to x?.Y(...)

        '    Dim dotToken = memberAccessExpression.OperatorToken
        '    Return SyntaxFactory.ConditionalAccessExpression(
        '        memberAccessExpression.Expression,
        '        SyntaxFactory.Token(SyntaxKind.QuestionToken).WithLeadingTrivia(dotToken.LeadingTrivia),
        '        SyntaxFactory.InvocationExpression(
        '            SyntaxFactory.SimpleMemberAccessExpression(
        '                Nothing,
        '                SyntaxFactory.Token(SyntaxKind.DotToken).WithTrailingTrivia(dotToken.TrailingTrivia),
        '                memberAccessExpression.Name),
        '            whenTrueInvocation.ArgumentList))
        'End Function
    End Class
End Namespace
