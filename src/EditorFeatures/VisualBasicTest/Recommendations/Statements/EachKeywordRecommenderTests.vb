' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class EachKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub EachNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Each")
        End Sub

        <Fact>
        Public Sub EachAfterForKeywordTest()
            VerifyRecommendationsContain(<MethodBody>For |</MethodBody>, "Each")
        End Sub

        <Fact>
        Public Sub EachNotAfterTouchingForTest()
            VerifyRecommendationsMissing(<MethodBody>For|</MethodBody>, "Each")
        End Sub

        <Fact>
        Public Sub EachTouchingLoopIdentifierTest()
            VerifyRecommendationsContain(<MethodBody>For i|</MethodBody>, "Each")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>For 
|</MethodBody>, "Each")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>For _
|</MethodBody>, "Each")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>For _ ' Test
|</MethodBody>, "Each")
        End Sub

        <Fact, WorkItem("http://github.com/dotnet/roslyn/issues/4946")>
        Public Sub NotInForLoop()
            VerifyNoRecommendations(
<MethodBody>For | = 1 To 100
Next</MethodBody>)
        End Sub
    End Class
End Namespace
