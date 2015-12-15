' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
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
            Dim matchPriority = If(ShouldPreselect(context, cancellationToken), CodeAnalysis.Completion.MatchPriority.Preselect, CodeAnalysis.Completion.MatchPriority.Default)

            If context.IsAnyExpressionContext Then
                Return {New RecommendedKeyword("True", VBFeaturesResources.TrueKeywordToolTip, matchPriority:=matchPriority),
                        New RecommendedKeyword("False", VBFeaturesResources.FalseKeywordToolTip, matchPriority:=matchPriority)}
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

        Private Function ShouldPreselect(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As Boolean
            Dim documentId = context?.Workspace?.CurrentSolution.GetDocumentIdsWithFilePath(context.SyntaxTree.FilePath).FirstOrDefault()
            If documentId Is Nothing Then
                Return False
            End If

            Dim document = context.Workspace.CurrentSolution.GetDocument(documentId)
            Dim typeInferenceService = document.Project.LanguageServices.GetService(Of ITypeInferenceService)()
            Dim types = typeInferenceService.InferTypes(context.SemanticModel, context.Position, cancellationToken)

            Return types.Any(Function(t) t.SpecialType = SpecialType.System_Boolean)
        End Function
    End Class
End Namespace
