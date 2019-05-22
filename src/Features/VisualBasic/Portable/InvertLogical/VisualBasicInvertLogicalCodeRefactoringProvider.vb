' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
