' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class DoKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub DoInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, {"Do", "Do Until", "Do While"})
        End Sub

        <Fact>
        Public Sub DoInLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Do")
        End Sub

        <Fact>
        Public Sub DoAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "Do")
        End Sub

        <Fact>
        Public Sub DoAfterExitKeywordTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Exit |
Loop</MethodBody>, "Do")
        End Sub

        <Fact>
        Public Sub DoAfterContinueKeywordTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Continue |
Loop</MethodBody>, "Do")
        End Sub

        <Fact>
        Public Sub DoNotAfterContinueKeywordOutsideLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
Continue |
</MethodBody>, "Do")
        End Sub

        <Fact>
        Public Sub DoNotAfterExitKeywordOutsideLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
Exit |
</MethodBody>, "Do")
        End Sub

        <Fact>
        Public Sub NoDoAfterExitInsideLambdaInsideDoLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
Do
Dim x = Sub()
            Exit |
        End Sub
Loop
</MethodBody>, "Do")
        End Sub

        <Fact>
        Public Sub DoAfterExitInsideDoLoopInsideLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
            Do
                Exit |
            Loop
        End Sub
</MethodBody>, "Do")
        End Sub

        <Fact>
        Public Sub DoNotInsideSingleLineLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Do")
        End Sub

        <Fact>
        Public Sub NotAfterExitInFinallyBlockTest()
            Dim code =
<MethodBody>
Do
    Try
    Finally
        Exit |
</MethodBody>

            VerifyRecommendationsMissing(code, "Do")
        End Sub
    End Class
End Namespace
