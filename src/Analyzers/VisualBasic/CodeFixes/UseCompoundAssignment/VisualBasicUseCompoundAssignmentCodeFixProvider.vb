' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.UseCompoundAssignment
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCompoundAssignment

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseCompoundAssignment), [Shared]>
    Friend Class VisualBasicUseCompoundAssignmentCodeFixProvider
        Inherits AbstractUseCompoundAssignmentCodeFixProvider(Of SyntaxKind, AssignmentStatementSyntax, ExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
            MyBase.New(Kinds)
        End Sub

        Protected Overrides Function Token(kind As SyntaxKind) As SyntaxToken
            Return SyntaxFactory.Token(kind)
        End Function

        Protected Overrides Function Assignment(
            assignmentOpKind As SyntaxKind, left As ExpressionSyntax, syntaxToken As SyntaxToken, right As ExpressionSyntax) As AssignmentStatementSyntax

            Return SyntaxFactory.AssignmentStatement(assignmentOpKind, left, syntaxToken, right)
        End Function

        Protected Overrides Function Increment(left As ExpressionSyntax, postfix As Boolean) As ExpressionSyntax
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Function Decrement(left As ExpressionSyntax, postfix As Boolean) As ExpressionSyntax
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Function PrepareRightExpressionLeadingTrivia(initialTrivia As SyntaxTriviaList) As SyntaxTriviaList
            Return initialTrivia.WithoutLeadingWhitespaceOrEndOfLine()
        End Function
    End Class
End Namespace
