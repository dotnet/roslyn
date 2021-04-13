' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Using" keyword at the beginning of a statement.
    ''' </summary>
    Friend Class UsingKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Using", VBFeaturesResources.A_Using_block_does_three_things_colon_it_creates_and_initializes_variables_in_the_resource_list_it_runs_the_code_in_the_block_and_it_disposes_of_the_variables_before_exiting_Resources_used_in_the_Using_block_must_implement_System_IDisposable_Using_resource1_bracket_resource2_bracket_End_Using))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Return If(context.IsMultiLineStatementContext, s_keywords, ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace
