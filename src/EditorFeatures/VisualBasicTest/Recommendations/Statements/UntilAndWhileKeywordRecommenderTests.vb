' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class UntilAndWhileKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UntilAfterDoTest()
            VerifyRecommendationsContain(<MethodBody>Do |</MethodBody>, "Until")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterDoTest()
            VerifyRecommendationsContain(<MethodBody>Do |</MethodBody>, "While")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UntilAfterLoopTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop |</MethodBody>, "Until")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterLoopTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop |</MethodBody>, "While")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UntilAndWhileMissingInDoLoopTopTestBlockTest()
            VerifyRecommendationsMissing(<MethodBody>
Do Until True
Loop |</MethodBody>, "While", "Until")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UntilAndWhileMissingAfterInvalidLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
Loop |</MethodBody>, "While", "Until")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>Do 
|</MethodBody>, "Until")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>Do _
|</MethodBody>, "Until")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>Do _ ' Test
|</MethodBody>, "Until")
        End Sub

    End Class
End Namespace
