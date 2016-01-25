' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
#Region "foo"
|</File>, "#End Region")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RegionAfterHashEndEndTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#Region "foo"
#End |</File>, "Region")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotHashEndRegionAfterHashEndTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Region "foo"
#End |</File>, "#End Region")
        End Function
    End Class
End Namespace
