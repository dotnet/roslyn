' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class EndRegionDirectiveKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEndRegionNotInFile()
            VerifyRecommendationsMissing(<File>|</File>, "#End Region")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEndRegionInFileAfterRegion()
            VerifyRecommendationsContain(<File>
#Region "foo"
|</File>, "#End Region")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RegionAfterHashEndEnd()
            VerifyRecommendationsContain(<File>
#Region "foo"
#End |</File>, "Region")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotHashEndRegionAfterHashEnd()
            VerifyRecommendationsMissing(<File>
#Region "foo"
#End |</File>, "#End Region")
        End Sub
    End Class
End Namespace
