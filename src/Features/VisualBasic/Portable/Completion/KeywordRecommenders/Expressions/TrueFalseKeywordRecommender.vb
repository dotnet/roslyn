' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "True" and "False" keywords
    ''' </summary>
    Friend Class TrueFalseKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Dim matchPriority = If(ShouldPreselect(context, cancellationToken), CodeAnalysis.Completion.MatchPriority.Preselect, CodeAnalysis.Completion.MatchPriority.Default)

            If context.IsAnyExpressionContext OrElse
               context.IsPreProcessorExpressionContext Then
                Return ImmutableArray.Create(
                    New RecommendedKeyword("True", VBFeaturesResources.Represents_a_Boolean_value_that_passes_a_conditional_test, matchPriority:=matchPriority),
                    New RecommendedKeyword("False", VBFeaturesResources.Represents_a_Boolean_value_that_fails_a_conditional_test, matchPriority:=matchPriority))
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function

        Private Shared Function ShouldPreselect(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As Boolean
            Dim typeInferenceService = context.Document.GetLanguageService(Of ITypeInferenceService)()
            Contract.ThrowIfNull(typeInferenceService, NameOf(typeInferenceService))

            Dim types = typeInferenceService.InferTypes(context.SemanticModel, context.Position, cancellationToken)

            Return types.Any(Function(t) t.SpecialType = SpecialType.System_Boolean)
        End Function
    End Class
End Namespace
