﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class CharsetModifierKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AutoAfterDeclareTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Declare |</ClassDeclaration>, "Auto")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AnsiAfterDeclareTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Declare |</ClassDeclaration>, "Ansi")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function UnicodeAfterDeclareTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Declare |</ClassDeclaration>, "Unicode")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AutoNotAfterAnotherCharsetModifier1Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Declare Ansi |</ClassDeclaration>, "Auto")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AutoNotAfterAnotherCharsetModifier2Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Declare Auto |</ClassDeclaration>, "Auto")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AutoNotAfterAnotherCharsetModifier3Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Declare Unicode |</ClassDeclaration>, "Auto")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterColonTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Declare : |</ClassDeclaration>, "Unicode")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<ClassDeclaration>Declare 
 |</ClassDeclaration>, "Unicode")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<ClassDeclaration>Declare _
 |</ClassDeclaration>, "Unicode")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsContainAsync(
<ClassDeclaration>Declare _ ' Test
 |</ClassDeclaration>, "Unicode")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTestCommentsAfterLineContinuationt() As Task
            Await VerifyRecommendationsContainAsync(
<ClassDeclaration>Declare _
 |</ClassDeclaration>, "Unicode")
        End Function
    End Class
End Namespace
