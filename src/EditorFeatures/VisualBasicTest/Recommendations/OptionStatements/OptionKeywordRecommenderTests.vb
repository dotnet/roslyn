' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OptionStatements
    Public Class OptionKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionInBlankFileTest()
            VerifyRecommendationsContain(<File>|</File>, "Option")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionAfterAnotherOptionStatementTest()
            VerifyRecommendationsContain(<File>
Option Strict On
|</File>, "Option")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionAfterBlankLineTest()
            VerifyRecommendationsContain(<File>
Option Strict On

|</File>, "Option")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionNotAfterImportsTest()
            VerifyRecommendationsMissing(<File>
Imports Goo
|</File>, "Option")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionNotAfterTypeTest()
            VerifyRecommendationsMissing(<File>
Class Goo
End Class
|</File>, "Option")
        End Sub

        <WorkItem(543008, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543008")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionNotAfterRegionKeywordTest()
            VerifyRecommendationsMissing(<File>
#Region |
</File>, "Option")
        End Sub
    End Class
End Namespace
