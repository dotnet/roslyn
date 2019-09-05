' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.UseConditionalExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseConditionalExpressionForReturnCodeRefactoringProvider
        Inherits AbstractUseConditionalExpressionForReturnCodeFixProvider(Of
            StatementSyntax, MultiLineIfBlockSyntax, ExpressionSyntax, TernaryConditionalExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function IsRef(returnOperation As IReturnOperation) As Boolean
            ' VB does not have ref returns.
            Return False
        End Function

        Protected Overrides Function GetMultiLineFormattingRule() As AbstractFormattingRule
            Return MultiLineConditionalExpressionFormattingRule.Instance
        End Function

        Protected Overrides Function WrapWithBlockIfAppropriate(ifStatement As MultiLineIfBlockSyntax, statement As StatementSyntax) As StatementSyntax
            Return statement
        End Function
    End Class
End Namespace
