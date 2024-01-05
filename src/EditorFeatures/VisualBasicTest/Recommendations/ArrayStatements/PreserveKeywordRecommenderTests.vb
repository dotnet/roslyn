' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ArrayStatements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class PreserveKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub PreserveNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Preserve")
        End Sub

        <Fact>
        Public Sub PreserveAfterReDimStatementTest()
            VerifyRecommendationsContain(<MethodBody>ReDim | </MethodBody>, "Preserve")
        End Sub

        <Fact>
        Public Sub PreserveNotAfterReDimPreserveTest()
            VerifyRecommendationsMissing(<ClassDeclaration>ReDim Preserve |</ClassDeclaration>, "Preserve")
        End Sub

        <Fact>
        Public Sub PreserveNotAfterWeirdBrokenReDimTest()
            VerifyRecommendationsMissing(<MethodBody>ReDim x, ReDim |</MethodBody>, "Preserve")
        End Sub

        <Fact>
        Public Sub PreserveInSingleLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() ReDim |</MethodBody>, "Preserve")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>ReDim 
| </MethodBody>, "Preserve")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>ReDim _
| </MethodBody>, "Preserve")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>ReDim _ ' Test
| </MethodBody>, "Preserve")
        End Sub
    End Class
End Namespace
