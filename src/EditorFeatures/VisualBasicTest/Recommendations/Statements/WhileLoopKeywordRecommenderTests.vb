' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class WhileLoopKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileInLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterStatement()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterExitKeyword()
            VerifyRecommendationsContain(<MethodBody>
While
Exit |
Loop</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterContinueKeyword()
            VerifyRecommendationsContain(<MethodBody>
While
Continue |
Loop</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileNotAfterContinueKeywordOutsideLoop()
            VerifyRecommendationsMissing(<MethodBody>
Continue |
</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileNotAfterExitKeywordOutsideLoop()
            VerifyRecommendationsMissing(<MethodBody>
Exit |
</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWhileAfterExitInsideLambdaInsideWhileLoop()
            VerifyRecommendationsMissing(<MethodBody>
While
Dim x = Sub()
            Exit |
        End Sub
Loop
</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhileAfterExitInsideWhileLoopInsideLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
            While True
                Exit |
            Loop
        End Sub
</MethodBody>, "While")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterExitInFinallyBlock()
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
