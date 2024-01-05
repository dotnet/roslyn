' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class GoToDestinationsRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ZeroAndOneAfterOnErrorGotoTest()
            VerifyRecommendationsAreExactly(<MethodBody>On Error Goto |</MethodBody>, "0", "-1")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>On Error Goto 
|</MethodBody>, "0", "-1")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsAreExactly(
<MethodBody>On Error Goto _
 |</MethodBody>, "0", "-1")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsAreExactly(
<MethodBody>On Error Goto _ ' Test
 |</MethodBody>, "0", "-1")
        End Sub
    End Class
End Namespace
