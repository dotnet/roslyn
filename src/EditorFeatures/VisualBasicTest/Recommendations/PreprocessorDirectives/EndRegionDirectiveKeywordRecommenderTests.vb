' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class EndRegionDirectiveKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashEndRegionNotInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>|</File>, "#End Region")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashEndRegionInFileAfterRegionTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#Region "goo"
|</File>, "#End Region")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RegionAfterHashEndEndTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#Region "goo"
#End |</File>, "Region")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotHashEndRegionAfterHashEndTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Region "goo"
#End |</File>, "#End Region")
        End Function
    End Class
End Namespace
