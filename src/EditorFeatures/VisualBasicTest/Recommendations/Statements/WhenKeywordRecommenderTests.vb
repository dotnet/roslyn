﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class WhenKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhenAfterCatchBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Try
Catch x As Exception |
End Try</MethodBody>, "When")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WhenAfterCatchBlockWithoutAsTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x
Try
Catch x |
End Try</MethodBody>, "When")
        End Function

        <WorkItem(542803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542803")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoWhenAfterDimStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim ex As Exception |</MethodBody>, "When")
        End Function

        <WorkItem(542803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542803")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoWhenAfterLambdaInExceptionFilterTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>
Try
Catch ex As Exception When (Function(e As Exception) As Boolean |
                                Return False
                            End Function).Invoke(ex)
End Try
</MethodBody>,
 "When")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>
Try
Catch x As Exception 
|
End Try</MethodBody>, "When")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
Try
Catch x As Exception _
|
End Try</MethodBody>, "When")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
Try
Catch x As Exception _ ' Test
|
End Try</MethodBody>, "When")
        End Function
    End Class
End Namespace
