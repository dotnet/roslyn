' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class EndRegionDirectiveKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEndRegionNotInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#End Region")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEndRegionInFileAfterRegionTest()
            VerifyRecommendationsContain(<File>
#Region "goo"
|</File>, "#End Region")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RegionAfterHashEndEndTest()
            VerifyRecommendationsContain(<File>
#Region "goo"
#End |</File>, "Region")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotHashEndRegionAfterHashEndTest()
            VerifyRecommendationsMissing(<File>
#Region "goo"
#End |</File>, "#End Region")
        End Sub
    End Class
End Namespace
