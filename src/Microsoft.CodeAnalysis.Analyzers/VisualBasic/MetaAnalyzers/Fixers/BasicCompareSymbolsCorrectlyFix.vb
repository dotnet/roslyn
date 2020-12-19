' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    End Class
End Namespace
