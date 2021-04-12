' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseNullPropagation
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
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

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function

        Protected Overrides Function IsInExpressionTree(semanticModel As SemanticModel, node As SyntaxNode, expressionTypeOpt As INamedTypeSymbol, cancellationToken As CancellationToken) As Boolean
            Return node.IsInExpressionTree(semanticModel, expressionTypeOpt, cancellationToken)
        End Function

        Protected Overrides Function TryAnalyzePatternCondition(syntaxFacts As ISyntaxFacts, conditionNode As SyntaxNode, ByRef conditionPartToCheck As SyntaxNode, ByRef isEquals As Boolean) As Boolean
            conditionPartToCheck = Nothing
            isEquals = False
            Return False
        End Function
    End Class
End Namespace
