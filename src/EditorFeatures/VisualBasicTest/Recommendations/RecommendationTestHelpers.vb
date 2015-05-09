' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations
    Public Module RecommendationTestHelpers
        Private s_parts As IEnumerable(Of AbstractKeywordRecommender)

        Private Function GetRecommendedKeywords(source As String, position As Integer) As IEnumerable(Of RecommendedKeyword)
            If s_parts Is Nothing Then
                s_parts = GetType(AbstractKeywordRecommender).Assembly.
                    GetTypes().
                    Where(Function(t) t.IsSubclassOf(GetType(AbstractKeywordRecommender))).
                    Select(Function(t) Activator.CreateInstance(t)).
                    Cast(Of AbstractKeywordRecommender)().
                    ToList()
            End If

            Dim tree = DirectCast(SyntaxFactory.ParseSyntaxTree(SourceText.From(source)), VisualBasicSyntaxTree)
            Dim comp = VisualBasicCompilation.Create("Text", syntaxTrees:={tree}, references:={TestReferences.NetFx.v4_0_30319.mscorlib})
            Dim semanticModel = comp.GetSemanticModel(tree)

            Dim context = VisualBasicSyntaxContext.CreateContext_Test(semanticModel, position, CancellationToken.None)
            Return s_parts.SelectMany(Function(part) part.RecommendKeywords_Test(context))
        End Function

        Private Function GetRecommendedKeywordStrings(source As String, position As Integer) As IEnumerable(Of String)
            Return GetRecommendedKeywords(source, position).Select(Function(k) k.Keyword)
        End Function

        Friend Sub VerifyRecommendationsAreExactly(testSource As XElement, ParamArray recommendations As String())
            Dim source = ConvertTestSourceTag(testSource)
            Dim recommendedKeywords = GetRecommendedKeywordStrings(source.Replace("|", ""), source.IndexOf("|"c)) _
                                      .OrderBy(Function(recommendation) recommendation) _
                                      .ToArray()

            Assert.Equal(recommendations.OrderBy(Function(recommendation) recommendation).ToArray(), recommendedKeywords)
        End Sub

        Friend Sub VerifyRecommendationDescriptionTextIs(testSource As XElement, keyword As String, text As String)
            Dim source = ConvertTestSourceTag(testSource)
            Dim recommendedKeyword = GetRecommendedKeywords(source.Replace("|", ""), source.IndexOf("|"c)).Single(Function(r) r.Keyword = keyword)
            Dim expectedText = text.Trim()
            Assert.Equal(expectedText, recommendedKeyword.DescriptionFactory(CancellationToken.None).GetFullText())
        End Sub

        Friend Sub VerifyRecommendationsContain(testSource As XElement, ParamArray recommendations As String())
            Assert.NotEmpty(recommendations)

            Dim source = ConvertTestSourceTag(testSource)

            VerifyRecommendationsContainNothingTyped(source, recommendations)
            VerifyRecommendationsContainPartiallyTyped(source, recommendations)
        End Sub

        Private Sub VerifyRecommendationsContainNothingTyped(source As String, ParamArray recommendations As String())
            ' Test with the | removed
            Dim recommendedKeywords = GetRecommendedKeywords(source.Replace("|", ""), source.IndexOf("|"c))

            Dim recommendedKeywordStrings = recommendedKeywords.Select(Function(k) k.Keyword)
            For Each recommendation In recommendations
                Assert.Contains(recommendation, recommendedKeywordStrings)
            Next

            VerifyRecommendationsHaveDescriptionText(recommendedKeywords.Where(Function(k) recommendations.Contains(k.Keyword)))
        End Sub

        Private Sub VerifyRecommendationsHaveDescriptionText(recommendations As IEnumerable(Of RecommendedKeyword))
            For Each keyword In recommendations
                Assert.NotEmpty(keyword.DescriptionFactory(CancellationToken.None).GetFullText())
            Next
        End Sub

        Private Sub VerifyRecommendationsContainPartiallyTyped(source As String, ParamArray recommendations As String())
            ' Test with the | replaced with the first character of the keywords we expect
            For Each partiallyTypedRecommendation In recommendations.Select(Function(recommendation) recommendation(0)).Distinct()
                Dim recommendedKeywords = GetRecommendedKeywordStrings(source.Replace("|"c, partiallyTypedRecommendation), source.IndexOf("|"c) + 1).ToArray()

                For Each recommendation In recommendations
                    Assert.Contains(recommendation, recommendedKeywords)
                Next
            Next
        End Sub

        Friend Sub VerifyRecommendationsMissing(testSource As XElement, ParamArray recommendations As String())
            Assert.NotEmpty(recommendations)

            Dim source = ConvertTestSourceTag(testSource)

            Dim recommendedKeywords = GetRecommendedKeywordStrings(source.Replace("|", ""), source.IndexOf("|"c)) _
                                      .OrderBy(Function(recommendation) recommendation) _
                                      .ToArray()

            For Each recommendation In recommendations
                Assert.DoesNotContain(recommendation, recommendedKeywords)
            Next
        End Sub
    End Module
End Namespace
