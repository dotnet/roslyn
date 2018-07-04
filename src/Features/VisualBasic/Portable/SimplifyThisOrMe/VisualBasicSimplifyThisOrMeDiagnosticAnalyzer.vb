' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.SimplifyThisOrMe
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyThisOrMe
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicSimplifyThisOrMeDiagnosticAnalyzer
        Inherits AbstractSimplifyThisOrMeDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            MeExpressionSyntax,
            MemberAccessExpressionSyntax)

        Public Sub New()
            MyBase.New(ImmutableArray.Create(SyntaxKind.SimpleMemberAccessExpression))
        End Sub

        Protected Overrides Function GetLanguageName() As String
            Return LanguageNames.VisualBasic
        End Function

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function CanSimplifyTypeNameExpression(
                model As SemanticModel, memberAccess As MemberAccessExpressionSyntax,
                optionSet As OptionSet, ByRef issueSpan As TextSpan,
                cancellationToken As CancellationToken) As Boolean

            Dim replacementSyntax As ExpressionSyntax = Nothing
            Return memberAccess.TryReduceOrSimplifyExplicitName(model, replacementSyntax, issueSpan, optionSet, cancellationToken)
        End Function
    End Class
End Namespace
