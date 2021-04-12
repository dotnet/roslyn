' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class WhenKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhenAfterCatchBlockTest()
            VerifyRecommendationsContain(<MethodBody>
Try
Catch x As Exception |
End Try</MethodBody>, "When")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhenAfterCatchBlockWithoutAsTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x
Try
Catch x |
End Try</MethodBody>, "When")
        End Sub

        <WorkItem(542803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542803")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWhenAfterDimStatementTest()
            VerifyRecommendationsMissing(<MethodBody>Dim ex As Exception |</MethodBody>, "When")
        End Sub

        <WorkItem(542803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542803")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWhenAfterLambdaInExceptionFilterTest()
            VerifyRecommendationsMissing(
<MethodBody>
Try
Catch ex As Exception When (Function(e As Exception) As Boolean |
                                Return False
                            End Function).Invoke(ex)
End Try
</MethodBody>,
 "When")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>
Try
Catch x As Exception 
|
End Try</MethodBody>, "When")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>
Try
Catch x As Exception _
|
End Try</MethodBody>, "When")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>
Try
Catch x As Exception _ ' Test
|
End Try</MethodBody>, "When")
        End Sub
    End Class
End Namespace
