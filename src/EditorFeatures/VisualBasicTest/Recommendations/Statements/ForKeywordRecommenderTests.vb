' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ForKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ForInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "For")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ForInLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "For")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ForAfterStatement()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "For")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ForAfterExitKeyword()
            VerifyRecommendationsContain(<MethodBody>
For
Exit |
Loop</MethodBody>, "For")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ForAfterContinueKeyword()
            VerifyRecommendationsContain(<MethodBody>
For
Continue |
Loop</MethodBody>, "For")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ForNotAfterContinueKeywordOutsideLoop()
            VerifyRecommendationsMissing(<MethodBody>
Continue |
</MethodBody>, "For")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ForNotAfterExitKeywordOutsideLoop()
            VerifyRecommendationsMissing(<MethodBody>
Exit |
</MethodBody>, "For")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ForNotInSingleLineLambda()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Sub() |</MethodBody>, "For")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoForAfterExitInsideLambdaInsideLoop()
            VerifyRecommendationsMissing(<MethodBody>
For Each i In foo
    x = Sub()
            Exit |
        End Sub
Next
</MethodBody>, "For")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ForAfterExitInsideForLoopInsideLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
            For Each i in bar
                Exit |
            End Sub
        Next
</MethodBody>, "For")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ForNotAfterExitInsideForLoopInsideFinallyBlock()
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
