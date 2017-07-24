' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.UseIsNullCheck

Namespace Microsoft.CodeAnalysis.VisualBasic.UseIsNullCheck
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseIsNullCheckCodeFixProvider
        Inherits AbstractUseIsNullCheckCodeFixProvider

        Protected Overrides Function GetIsNullTitle() As String
            Return VBFeaturesResources.use_Is_Nothing_check
        End Function

        Protected Overrides Function GetIsNotNullTitle() As String
            Return VBFeaturesResources.use_IsNot_Nothing_check
        End Function

        Protected Overrides Function CreateIsNullCheck(argument As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.IsExpression(
                DirectCast(argument, ExpressionSyntax).Parenthesize(),
                SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword))).Parenthesize()
        End Function

        Protected Overrides Function CreateIsNotNullCheck(notExpression As SyntaxNode, argument As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.IsNotExpression(
                DirectCast(argument, ExpressionSyntax).Parenthesize(),
                SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword))).Parenthesize()
        End Function
    End Class
End Namespace
