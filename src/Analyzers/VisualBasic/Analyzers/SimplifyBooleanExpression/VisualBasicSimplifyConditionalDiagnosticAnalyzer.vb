' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.SimplifyBooleanExpression

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyBooleanExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicSimplifyConditionalDiagnosticAnalyzer
        Inherits AbstractSimplifyConditionalDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            TernaryConditionalExpressionSyntax)

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides Function GetConversion(semanticModel As SemanticModel, node As ExpressionSyntax, cancellationToken As CancellationToken) As CommonConversion
            Return semanticModel.GetConversion(node, cancellationToken).ToCommonConversion()
        End Function
    End Class
End Namespace
