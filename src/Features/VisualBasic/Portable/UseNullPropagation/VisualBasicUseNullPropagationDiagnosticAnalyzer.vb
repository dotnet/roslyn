' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseNullPropagation
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseNullPropagationDiagnosticAnalyzer
        Inherits AbstractUseNullPropagationDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            TernaryConditionalExpressionSyntax,
            BinaryExpressionSyntax,
            InvocationExpressionSyntax,
            MemberAccessExpressionSyntax,
            ConditionalAccessExpressionSyntax,
            InvocationExpressionSyntax)

        Protected Overrides Function ShouldAnalyze(options As ParseOptions) As Boolean
            Return DirectCast(options, VisualBasicParseOptions).LanguageVersion >= LanguageVersion.VisualBasic14
        End Function

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function GetSemanticFactsService() As ISemanticFactsService
            Return VisualBasicSemanticFactsService.instance
        End Function

        Protected Overrides Function GetSyntaxKindToAnalyze() As SyntaxKind
            Return SyntaxKind.TernaryConditionalExpression
        End Function

        Protected Overrides Function IsEquals(condition As BinaryExpressionSyntax) As Boolean
            Return condition.Kind() = SyntaxKind.IsExpression
        End Function

        Protected Overrides Function IsNotEquals(condition As BinaryExpressionSyntax) As Boolean
            Return condition.Kind() = SyntaxKind.IsNotExpression
        End Function

        Protected Overrides Function TryAnalyzePatternCondition(syntaxFacts As ISyntaxFactsService, conditionNode As SyntaxNode, ByRef conditionPartToCheck As SyntaxNode, ByRef isEquals As Boolean) As Boolean
            conditionPartToCheck = Nothing
            isEquals = False
            Return False
        End Function
    End Class
End Namespace
