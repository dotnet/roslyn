' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.InvertLogical

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertLogical
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertLogical), [Shared]>
    Friend Class VisualBasicInvertLogicalCodeRefactoringProvider
        Inherits AbstractInvertLogicalCodeRefactoringProvider(Of SyntaxKind, ExpressionSyntax, BinaryExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetKind(rawKind As Integer) As SyntaxKind
            Return CType(rawKind, SyntaxKind)
        End Function

        Protected Overrides Function InvertedKind(binaryExprKind As SyntaxKind) As SyntaxKind
            Return If(binaryExprKind = SyntaxKind.AndAlsoExpression,
                      SyntaxKind.OrElseExpression,
                      SyntaxKind.AndAlsoExpression)
        End Function

        Protected Overrides Function GetOperatorText(binaryExprKind As SyntaxKind) As String
            Return If(binaryExprKind = SyntaxKind.AndAlsoExpression,
                      SyntaxFacts.GetText(SyntaxKind.AndAlsoKeyword),
                      SyntaxFacts.GetText(SyntaxKind.OrElseKeyword))
        End Function
    End Class
End Namespace
