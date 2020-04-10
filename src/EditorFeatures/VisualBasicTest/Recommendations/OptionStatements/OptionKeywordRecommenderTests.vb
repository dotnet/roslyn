' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OptionStatements
    Public Class OptionKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OptionInBlankFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>|</File>, "Option")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OptionAfterAnotherOptionStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Option Strict On
|</File>, "Option")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OptionAfterBlankLineTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Option Strict On

|</File>, "Option")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OptionNotAfterImportsTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports Goo
|</File>, "Option")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OptionNotAfterTypeTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class Goo
End Class
|</File>, "Option")
        End Function

        <WorkItem(543008, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543008")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OptionNotAfterRegionKeywordTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#Region |
</File>, "Option")
        End Function
    End Class
End Namespace
