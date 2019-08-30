' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class AliasKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AliasAfterLibNameInSubTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Declare Sub goo Lib "Goo" |</ClassDeclaration>, "Alias")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AliasAfterLibNameInFunctionTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Declare Function goo Lib "Goo" |</ClassDeclaration>, "Alias")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AliasNotAfterLibKeywordTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Declare Sub goo Lib |</ClassDeclaration>, Array.Empty(Of String)())
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NothingAfterBrokenAliasTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Declare Sub goo Lib "Goo" Alais |</ClassDeclaration>, Array.Empty(Of String)())
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoAliasAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<ClassDeclaration>Declare Function goo Lib "Goo" 
    |</ClassDeclaration>, "Alias")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AliasAfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(
<ClassDeclaration>Declare Function goo Lib "Goo" _
|</ClassDeclaration>, "Alias")
        End Function
        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AliasAfterExplicitLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsAreExactlyAsync(
<ClassDeclaration>Declare Function goo Lib "Goo" _ ' Test
|</ClassDeclaration>, "Alias")
        End Function
    End Class
End Namespace
