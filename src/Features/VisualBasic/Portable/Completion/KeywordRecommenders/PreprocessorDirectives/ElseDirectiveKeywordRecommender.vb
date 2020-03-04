' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#Else" preprocessor directive
    ''' </summary>
    Friend Class ElseDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsPreprocessorStartContext OrElse context.IsWithinPreprocessorContext Then
                Dim innermostKind = context.SyntaxTree.GetInnermostIfPreprocessorKind(context.Position, cancellationToken)

                If innermostKind.HasValue AndAlso innermostKind.Value <> SyntaxKind.ElseDirectiveTrivia Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("#Else", VBFeaturesResources.Introduces_a_group_of_statements_in_an_SharpIf_statement_that_is_compiled_if_no_previous_condition_evaluates_to_True))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
