' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class WhileLoopKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileInLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterExitKeywordTest()
            VerifyRecommendationsContain(<MethodBody>
While
Exit |
Loop</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterContinueKeywordTest()
            VerifyRecommendationsContain(<MethodBody>
While
Continue |
Loop</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileNotAfterContinueKeywordOutsideLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
Continue |
</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileNotAfterExitKeywordOutsideLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
Exit |
</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWhileAfterExitInsideLambdaInsideWhileLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
While
Dim x = Sub()
            Exit |
        End Sub
Loop
</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterExitInsideWhileLoopInsideLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
            While True
                Exit |
            Loop
        End Sub
</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterExitInFinallyBlockTest()
            Dim code =
<MethodBody>
While True
    Try
    Finally
        Exit |
</MethodBody>

            VerifyRecommendationsMissing(code, "While")
        End Sub
    End Class
End Namespace
