' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.UseIsNullCheck

Namespace Microsoft.CodeAnalysis.VisualBasic.UseIsNullCheck
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseIsNullCheckForReferenceEqualsCodeFixProvider
        Inherits AbstractUseIsNullCheckForReferenceEqualsCodeFixProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetIsNullTitle() As String
            Return VBFeaturesResources.Use_Is_Nothing_check
        End Function

        Protected Overrides Function GetIsNotNullTitle() As String
            Return VBFeaturesResources.Use_IsNot_Nothing_check
        End Function

        Protected Overrides Function CreateNullCheck(argument As SyntaxNode, isUnconstrainedGeneric As Boolean) As SyntaxNode
            Return SyntaxFactory.IsExpression(
                DirectCast(argument, ExpressionSyntax).Parenthesize(),
                SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword))).Parenthesize()
        End Function

        Protected Overrides Function CreateNotNullCheck(argument As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.IsNotExpression(
                DirectCast(argument, ExpressionSyntax).Parenthesize(),
                SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword))).Parenthesize()
        End Function
    End Class
End Namespace
