' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class DoKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DoInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, {"Do", "Do Until", "Do While"})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DoInLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DoAfterStatement()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DoAfterExitKeyword()
            VerifyRecommendationsContain(<MethodBody>
Do
Exit |
Loop</MethodBody>, "Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DoAfterContinueKeyword()
            VerifyRecommendationsContain(<MethodBody>
Do
Continue |
Loop</MethodBody>, "Do")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DoNotAfterContinueKeywordOutsideLoop()
            VerifyRecommendationsMissing(<MethodBody>
Continue |
</MethodBody>, "Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DoNotAfterExitKeywordOutsideLoop()
            VerifyRecommendationsMissing(<MethodBody>
Exit |
</MethodBody>, "Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoDoAfterExitInsideLambdaInsideDoLoop()
            VerifyRecommendationsMissing(<MethodBody>
Do
Dim x = Sub()
            Exit |
        End Sub
Loop
</MethodBody>, "Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DoAfterExitInsideDoLoopInsideLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
            Do
                Exit |
            Loop
        End Sub
</MethodBody>, "Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DoNotInsideSingleLineLambda()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Do")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterExitInFinallyBlock()
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
