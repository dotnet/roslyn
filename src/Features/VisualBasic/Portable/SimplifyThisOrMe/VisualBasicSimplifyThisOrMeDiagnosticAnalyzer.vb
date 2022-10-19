' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.SimplifyThisOrMe
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyThisOrMe
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicSimplifyThisOrMeDiagnosticAnalyzer
        Inherits AbstractSimplifyThisOrMeDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            MeExpressionSyntax,
            MemberAccessExpressionSyntax)

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function

        Protected Overrides Function CanSimplifyTypeNameExpression(
                model As SemanticModel, memberAccess As MemberAccessExpressionSyntax,
                optionSet As OptionSet, ByRef issueSpan As TextSpan,
                cancellationToken As CancellationToken) As Boolean

            Dim replacementSyntax As ExpressionSyntax = Nothing
            Return ExpressionSimplifier.Instance.TrySimplify(memberAccess, model, optionSet, replacementSyntax, issueSpan, cancellationToken)
        End Function
    End Class
End Namespace
