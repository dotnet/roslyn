' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "True" and "False" keywords
    ''' </summary>
    Friend Class TrueFalseKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim preselect As Boolean = ShouldPreselect(context, cancellationToken)

            If context.IsAnyExpressionContext Then
                Return {New RecommendedKeyword("True", VBFeaturesResources.TrueKeywordToolTip, shouldPreselect:=preselect),
                        New RecommendedKeyword("False", VBFeaturesResources.FalseKeywordToolTip, shouldPreselect:=preselect)}
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

        Private Function ShouldPreselect(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As Boolean
            ' The Workspace might be null in the keyword recommender tests, since we don't create one for those.
            ' This function still gets test coverage through the all-up completion tests.
            Dim document = context.Workspace?.CurrentSolution.GetDocument(context.SyntaxTree)

            If document Is Nothing Then
                Return False
            End If

            Dim typeInferenceService = document.Project.LanguageServices.GetService(Of ITypeInferenceService)()
            Dim types = typeInferenceService.InferTypes(context.SemanticModel, context.Position, cancellationToken)

            Return types.Any(Function(t) t.SpecialType = SpecialType.System_Boolean)
        End Function
    End Class
End Namespace
