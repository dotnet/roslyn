' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class LibKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LibAfterNameInSubTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Declare Sub goo |</ClassDeclaration>, "Lib")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LibAfterNameInFunctionTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Declare Function goo |</ClassDeclaration>, "Lib")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function LibNotAfterLibKeywordTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Declare Sub goo Lib |</ClassDeclaration>, "Lib")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<ClassDeclaration>Declare Sub goo 
|</ClassDeclaration>, "Lib")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<ClassDeclaration>Declare Sub goo _
|</ClassDeclaration>, "Lib")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsContainAsync(
<ClassDeclaration>Declare Sub goo _ ' Test
|</ClassDeclaration>, "Lib")
        End Function
    End Class
End Namespace
