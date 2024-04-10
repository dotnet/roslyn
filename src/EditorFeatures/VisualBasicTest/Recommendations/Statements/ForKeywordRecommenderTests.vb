' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ForKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ForInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "For")
        End Sub

        <Fact>
        Public Sub ForInLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "For")
        End Sub

        <Fact>
        Public Sub ForAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "For")
        End Sub

        <Fact>
        Public Sub ForAfterExitKeywordTest()
            VerifyRecommendationsContain(<MethodBody>
For
Exit |
Loop</MethodBody>, "For")
        End Sub

        <Fact>
        Public Sub ForAfterContinueKeywordTest()
            VerifyRecommendationsContain(<MethodBody>
For
Continue |
Loop</MethodBody>, "For")
        End Sub

        <Fact>
        Public Sub ForNotAfterContinueKeywordOutsideLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
Continue |
</MethodBody>, "For")
        End Sub

        <Fact>
        Public Sub ForNotAfterExitKeywordOutsideLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
Exit |
</MethodBody>, "For")
        End Sub

        <Fact>
        Public Sub ForNotInSingleLineLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Sub() |</MethodBody>, "For")
        End Sub

        <Fact>
        Public Sub NoForAfterExitInsideLambdaInsideLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
For Each i In goo
    x = Sub()
            Exit |
        End Sub
Next
</MethodBody>, "For")
        End Sub

        <Fact>
        Public Sub ForAfterExitInsideForLoopInsideLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
            For Each i in bar
                Exit |
        End Function
        Next
</MethodBody>, "For")
        End Sub

        <Fact>
        Public Sub ForNotAfterExitInsideForLoopInsideFinallyBlockTest()
            Dim code =
<MethodBody>
For i = 1 to 100
    Try
    Finally
        Exit |
</MethodBody>

            VerifyRecommendationsMissing(code, "For")
        End Sub
    End Class
End Namespace
