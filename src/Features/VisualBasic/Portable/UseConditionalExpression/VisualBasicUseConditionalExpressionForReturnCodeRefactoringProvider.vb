' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.UseConditionalExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseConditionalExpressionForReturnCodeRefactoringProvider
        Inherits AbstractUseConditionalExpressionForReturnCodeFixProvider(Of TernaryConditionalExpressionSyntax)

        Protected Overrides Function GetMultiLineFormattingRule() As IFormattingRule
            Return MultiLineConditionalExpressionFormattingRule.Instance
        End Function
    End Class
End Namespace
