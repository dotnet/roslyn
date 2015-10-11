' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class WithKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "With")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithInLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "With")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterStatement()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "With")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithNotAfterExitKeyword()
            VerifyRecommendationsMissing(<MethodBody>
With
Exit |
Loop</MethodBody>, "With")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithNotAfterContinueKeyword()
            VerifyRecommendationsMissing(<MethodBody>
With
Continue |
Loop</MethodBody>, "With")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithNotAfterContinueKeywordOutsideLoop()
            VerifyRecommendationsMissing(<MethodBody>
Continue |
</MethodBody>, "With")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithNotAfterExitKeywordOutsideLoop()
            VerifyRecommendationsMissing(<MethodBody>
Exit |
</MethodBody>, "With")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithNotAfterExitInsideLambdaInsideWithBlock()
            VerifyRecommendationsMissing(<MethodBody>
While
Dim x = Sub()
            Exit |
        End Sub
Loop
</MethodBody>, "With")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterExitInsideWhileLoopInsideLambda()
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
