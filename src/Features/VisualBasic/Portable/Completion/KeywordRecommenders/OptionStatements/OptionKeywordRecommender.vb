' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OptionStatements
    ''' <summary>
    ''' Recommends the "Option" keyword
    ''' </summary>
    Friend Class OptionKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Option", VBFeaturesResources.Introduces_a_statement_that_specifies_a_compiler_option_that_applies_to_the_entire_source_file))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, CancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsPreProcessorDirectiveContext Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            ' If we have no left token, then we're at the start of the file
            If targetToken.Kind = SyntaxKind.None Then
                Return s_keywords
            End If

            ' Show if after an earlier option statement
            If context.IsAfterStatementOfKind(SyntaxKind.OptionStatement) Then
                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
