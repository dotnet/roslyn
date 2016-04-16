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

        Private Async Function GetRecommendedKeywordsAsync(source As String, position As Integer) As Tasks.Task(Of IEnumerable(Of RecommendedKeyword))
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

            Dim context = Await VisualBasicSyntaxContext.CreateContextAsync_Test(semanticModel, position, CancellationToken.None)
            Return s_parts.SelectMany(Function(part) part.RecommendKeywords_Test(context))
        End Function

        Private Async Function GetRecommendedKeywordStringsAsync(source As String, position As Integer) As Tasks.Task(Of IEnumerable(Of String))
            Dim keywords = Await GetRecommendedKeywordsAsync(source, position).ConfigureAwait(False)
            Return keywords.Select(Function(k) k.Keyword)
        End Function

        Friend Async Function VerifyRecommendationsAreExactlyAsync(testSource As XElement, ParamArray recommendations As String()) As Task
            Dim source = ConvertTestSourceTag(testSource)
            Dim recommendedKeywords = (Await GetRecommendedKeywordStringsAsync(source.Replace("|", ""), source.IndexOf("|"c))) _
                                      .OrderBy(Function(recommendation) recommendation) _
                                      .ToArray()

            Assert.Equal(recommendations.OrderBy(Function(recommendation) recommendation).ToArray(), recommendedKeywords)
        End Function

        Friend Async Function VerifyRecommendationDescriptionTextIsAsync(testSource As XElement, keyword As String, text As String) As Task
            Dim source = ConvertTestSourceTag(testSource)
            Dim recommendedKeyword = (Await GetRecommendedKeywordsAsync(source.Replace("|", ""), source.IndexOf("|"c))).Single(Function(r) r.Keyword = keyword)
            Dim expectedText = text.Trim()
            Assert.Equal(expectedText, recommendedKeyword.DescriptionFactory(CancellationToken.None).GetFullText())
        End Function

        Friend Async Function VerifyRecommendationsContainAsync(testSource As XElement, ParamArray recommendations As String()) As Task
            Assert.NotEmpty(recommendations)

            Dim source = ConvertTestSourceTag(testSource)

            Await VerifyRecommendationsContainNothingTypedAsync(source, recommendations)
            Await VerifyRecommendationsContainPartiallyTypedAsync(source, recommendations)
        End Function

        Private Async Function VerifyRecommendationsContainNothingTypedAsync(source As String, ParamArray recommendations As String()) As Task
            ' Test with the | removed
            Dim recommendedKeywords = Await GetRecommendedKeywordsAsync(source.Replace("|", ""), source.IndexOf("|"c))

            Dim recommendedKeywordStrings = recommendedKeywords.Select(Function(k) k.Keyword)
            For Each recommendation In recommendations
                Assert.Contains(recommendation, recommendedKeywordStrings)
            Next

            VerifyRecommendationsHaveDescriptionText(recommendedKeywords.Where(Function(k) recommendations.Contains(k.Keyword)))
        End Function

        Private Sub VerifyRecommendationsHaveDescriptionText(recommendations As IEnumerable(Of RecommendedKeyword))
            For Each keyword In recommendations
                Assert.NotEmpty(keyword.DescriptionFactory(CancellationToken.None).GetFullText())
            Next
        End Sub

        Private Async Function VerifyRecommendationsContainPartiallyTypedAsync(source As String, ParamArray recommendations As String()) As Task
            ' Test with the | replaced with the first character of the keywords we expect
            For Each partiallyTypedRecommendation In recommendations.Select(Function(recommendation) recommendation(0)).Distinct()
                Dim recommendedKeywords = (Await GetRecommendedKeywordStringsAsync(source.Replace("|"c, partiallyTypedRecommendation), source.IndexOf("|"c) + 1)).ToArray()

                For Each recommendation In recommendations
                    Assert.Contains(recommendation, recommendedKeywords)
                Next
            Next
        End Function

        Friend Async Function VerifyRecommendationsMissingAsync(testSource As XElement, ParamArray recommendations As String()) As Task
            Assert.NotEmpty(recommendations)

            Dim source = ConvertTestSourceTag(testSource)

            Dim recommendedKeywords = (Await GetRecommendedKeywordStringsAsync(source.Replace("|", ""), source.IndexOf("|"c))) _
                                      .OrderBy(Function(recommendation) recommendation) _
                                      .ToArray()

            For Each recommendation In recommendations
                Assert.DoesNotContain(recommendation, recommendedKeywords)
            Next
        End Function

        Friend Async Function VerifyNoRecommendationsAsync(testSource As XElement) As Task
            Dim source = ConvertTestSourceTag(testSource)

            Dim recommendedKeywords = (Await GetRecommendedKeywordStringsAsync(source.Replace("|", ""), source.IndexOf("|"c))) _
                                      .OrderBy(Function(recommendation) recommendation) _
                                      .ToArray()

            Assert.Equal(0, recommendedKeywords.Length)
        End Function
    End Module
End Namespace
