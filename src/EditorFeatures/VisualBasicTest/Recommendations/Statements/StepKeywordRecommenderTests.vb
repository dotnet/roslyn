' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class StepKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub StepInForLoopTest()
            VerifyRecommendationsContain(<MethodBody>For i = 1 To 10 |</MethodBody>, "Step")
        End Sub

        <Fact>
        Public Sub StepInForLoopAfterLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>
    For i = 1 To 10 _
_
|</MethodBody>, "Step")
        End Sub

        <Fact>
        Public Sub StepInForLoopAfterLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>
    For i = 1 To 10 _ ' Test
_
|</MethodBody>, "Step")
        End Sub
        <Fact>
        Public Sub StepInForLoopNotAfterEOLTest()
            VerifyRecommendationsMissing(
<MethodBody>
    For i = 1 To 10 
|</MethodBody>, "Step")
        End Sub

        <Fact>
        Public Sub StepInForLoopNotAfterEOLWithLineContinuationTest()
            VerifyRecommendationsMissing(
<MethodBody>
    For i = 1 To 10 _

|</MethodBody>, "Step")
        End Sub
    End Class
End Namespace
