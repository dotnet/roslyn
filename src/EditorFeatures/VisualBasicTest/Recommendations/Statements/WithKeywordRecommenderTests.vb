﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class WithKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "With")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithInLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "With")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "With")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithNotAfterExitKeywordTest()
            VerifyRecommendationsMissing(<MethodBody>
With
Exit |
Loop</MethodBody>, "With")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithNotAfterContinueKeywordTest()
            VerifyRecommendationsMissing(<MethodBody>
With
Continue |
Loop</MethodBody>, "With")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithNotAfterContinueKeywordOutsideLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
Continue |
</MethodBody>, "With")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithNotAfterExitKeywordOutsideLoopTest()
            VerifyRecommendationsMissing(<MethodBody>
Exit |
</MethodBody>, "With")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithNotAfterExitInsideLambdaInsideWithBlockTest()
            VerifyRecommendationsMissing(<MethodBody>
While
Dim x = Sub()
            Exit |
        End Sub
Loop
</MethodBody>, "With")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterExitInsideWhileLoopInsideLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
            With x
                Exit |
            Loop
        End Sub
</MethodBody>, "With")
        End Sub
    End Class
End Namespace
