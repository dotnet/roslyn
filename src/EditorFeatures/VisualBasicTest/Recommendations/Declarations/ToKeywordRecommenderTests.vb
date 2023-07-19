' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ToKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoToWithEmptyBoundInDimTest()
            VerifyRecommendationsMissing(<MethodBody>Dim i( |</MethodBody>, "To")
        End Sub

        <Fact>
        Public Sub ToAfterLowerBoundInDimTest()
            VerifyRecommendationsContain(<MethodBody>Dim i(0 |</MethodBody>, "To")
        End Sub

        <Fact>
        Public Sub NoToAfterUpperBoundInDimTest()
            VerifyRecommendationsMissing(<MethodBody>Dim i(0 To 4 |</MethodBody>, "To")
        End Sub

        <Fact>
        Public Sub NoToAfterCommaInDimTest()
            VerifyRecommendationsMissing(<MethodBody>Dim i(0 To 4, |</MethodBody>, "To")
        End Sub

        <Fact>
        Public Sub ToAfterSecondLowerBoundInDimTest()
            VerifyRecommendationsContain(<MethodBody>Dim i(0 To 4, 0 |</MethodBody>, "To")
        End Sub

        <Fact>
        Public Sub NoToWithEmptyBoundInReDimTest()
            VerifyRecommendationsMissing(<MethodBody>ReDim i( |</MethodBody>, "To")
        End Sub

        <Fact>
        Public Sub ToAfterLowerBoundInReDimTest()
            VerifyRecommendationsContain(<MethodBody>ReDim i(0 |</MethodBody>, "To")
        End Sub

        <Fact>
        Public Sub NoToAfterUpperBoundInReDimTest()
            VerifyRecommendationsMissing(<MethodBody>ReDim i(0 To 4 |</MethodBody>, "To")
        End Sub

        <Fact>
        Public Sub NoToAfterCommaInReDimTest()
            VerifyRecommendationsMissing(<MethodBody>ReDim i(0 To 4, |</MethodBody>, "To")
        End Sub

        <Fact>
        Public Sub ToAfterSecondLowerBoundInReDimTest()
            VerifyRecommendationsContain(<MethodBody>ReDim i(0 To 4, 0 |</MethodBody>, "To")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>Dim i(0 
|</MethodBody>, "To")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>Dim i(0 _
|</MethodBody>, "To")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>Dim i(0 _ ' Test
|</MethodBody>, "To")
        End Sub
    End Class
End Namespace
