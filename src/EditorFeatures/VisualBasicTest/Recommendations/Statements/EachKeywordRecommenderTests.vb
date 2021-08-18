' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class EachKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EachNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Each")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EachAfterForKeywordTest()
            VerifyRecommendationsContain(<MethodBody>For |</MethodBody>, "Each")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EachNotAfterTouchingForTest()
            VerifyRecommendationsMissing(<MethodBody>For|</MethodBody>, "Each")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EachTouchingLoopIdentifierTest()
            VerifyRecommendationsContain(<MethodBody>For i|</MethodBody>, "Each")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>For 
|</MethodBody>, "Each")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>For _
|</MethodBody>, "Each")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>For _ ' Test
|</MethodBody>, "Each")
        End Sub

        <WorkItem(4946, "http://github.com/dotnet/roslyn/issues/4946")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInForLoop()
            VerifyNoRecommendations(
<MethodBody>For | = 1 To 100
Next</MethodBody>)
        End Sub
    End Class
End Namespace
