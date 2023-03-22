' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class NextKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NextAfterOnErrorResumeTest()
            VerifyRecommendationsAreExactly(<MethodBody>On Error Resume |</MethodBody>, "Next")
        End Sub

        <Fact>
        Public Sub NextAfterResumeStatementTest()
            VerifyRecommendationsAreExactly(<MethodBody>Resume |</MethodBody>, "Next")
        End Sub

        <Fact>
        Public Sub NextNotInLambdaAfterResumeTest()
            ' On Error statements are never allowed within lambdas
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
            Resume |
End Sub</MethodBody>, "Next")
        End Sub

        <Fact>
        Public Sub NextNotInLambdaAfterOnErrorResumeTest()
            ' On Error statements are never allowed within lambdas
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
            On Error Resume |
End Sub</MethodBody>, "Next")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>On Error Resume 
|</MethodBody>, "Next")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>On Error Resume _
|</MethodBody>, "Next")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>On Error Resume _ ' Test
|</MethodBody>, "Next")
        End Sub
    End Class
End Namespace
