' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Function" and "Sub" keywords in external method declarations.
    ''' </summary>
    Friend Class ExternalSubFunctionKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) = ImmutableArray.Create(
            New RecommendedKeyword("Function", VBFeaturesResources.Specifies_that_the_external_procedure_being_referenced_in_the_Declare_statement_is_a_Function),
            New RecommendedKeyword("Sub", VBFeaturesResources.Specifies_that_the_external_procedure_being_referenced_in_the_Declare_statement_is_a_Sub))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken
            Return If(targetToken.IsKind(SyntaxKind.DeclareKeyword, SyntaxKind.AnsiKeyword, SyntaxKind.UnicodeKeyword, SyntaxKind.AutoKeyword) AndAlso targetToken.GetAncestor(Of DeclareStatementSyntax)() IsNot Nothing,
                s_keywords,
                ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace
