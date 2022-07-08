' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class InKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInForEach1Test()
            VerifyRecommendationsContain(<MethodBody>For Each x |</MethodBody>, "In")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInForEach2Test()
            VerifyRecommendationsContain(<MethodBody>For Each x As Goo |</MethodBody>, "In")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInFromQuery1Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x |</MethodBody>, "In")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInFromQuery2Test()
            VerifyRecommendationsContain(<MethodBody>Dim x = From x As Goo |</MethodBody>, "In")
        End Sub

        <WorkItem(543231, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543231")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InInFromQuery3Test()
            VerifyRecommendationsAreExactly(<MethodBody>Dim x = From x As Integer |</MethodBody>, "In")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>For Each x 
|</MethodBody>, "In")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>For Each x _
|</MethodBody>, "In")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>For Each x _ ' Test
|</MethodBody>, "In")
        End Sub
    End Class
End Namespace
