' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.Fixers
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(BasicCompareSymbolsCorrectlyFix)), [Shared]>
    Public Class BasicCompareSymbolsCorrectlyFix
        Inherits CompareSymbolsCorrectlyFix

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub

        Protected Overrides Function CreateConditionalAccessExpression(expression As SyntaxNode, whenNotNull As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.ConditionalAccessExpression(
                DirectCast(expression, ExpressionSyntax),
                DirectCast(whenNotNull, ExpressionSyntax))
        End Function

        Protected Overrides Function GetExpression(invocationOperation As Operations.IInvocationOperation) As SyntaxNode
            Dim invocation = DirectCast(invocationOperation.Syntax, InvocationExpressionSyntax)
            Return invocation.Expression
        End Function
    End Class
End Namespace
