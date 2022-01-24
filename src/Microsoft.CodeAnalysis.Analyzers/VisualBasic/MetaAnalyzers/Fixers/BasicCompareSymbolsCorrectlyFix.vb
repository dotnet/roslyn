' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.Fixers
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(BasicCompareSymbolsCorrectlyFix)), [Shared]>
    Public Class BasicCompareSymbolsCorrectlyFix
        Inherits CompareSymbolsCorrectlyFix

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
