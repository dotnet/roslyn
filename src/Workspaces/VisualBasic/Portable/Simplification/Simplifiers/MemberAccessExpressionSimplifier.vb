' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Simplification.Simplifiers
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
    Friend Class MemberAccessExpressionSimplifier
        Inherits AbstractMemberAccessExpressionSimplifier(Of
            ExpressionSyntax,
            MemberAccessExpressionSyntax,
            MeExpressionSyntax)

        Public Shared ReadOnly Instance As New MemberAccessExpressionSimplifier()

        Private Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides Function GetSpeculationAnalyzer(semanticModel As SemanticModel, memberAccessExpression As MemberAccessExpressionSyntax, cancellationToken As CancellationToken) As ISpeculationAnalyzer
            Return New SpeculationAnalyzer(memberAccessExpression, memberAccessExpression.Name, semanticModel, cancellationToken)
        End Function

        Protected Overrides Function MayCauseParseDifference(memberAccessExpression As MemberAccessExpressionSyntax) As Boolean
            Return False
        End Function
    End Class
End Namespace
