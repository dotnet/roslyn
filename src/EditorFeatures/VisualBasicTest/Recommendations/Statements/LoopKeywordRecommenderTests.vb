' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class LoopKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopNotInMethodBody()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Loop")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopNotInLambda()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Loop")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopNotAfterStatement()
            VerifyRecommendationsMissing(<MethodBody>
Dim x
|</MethodBody>, "Loop")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopAfterDoStatement()
            VerifyRecommendationsContain(<MethodBody>
Do
|</MethodBody>, "Loop", "Loop Until", "Loop While")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopAfterDoUntilStatement()
            VerifyRecommendationsContain(<MethodBody>
Do Until True
|</MethodBody>, "Loop")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopUntilNotAfterDoUntilStatement()
            VerifyRecommendationsMissing(<MethodBody>
Do Until True
|</MethodBody>, "Loop Until", "Loop While")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopNotInDoLoopUntilBlock()
            VerifyRecommendationsMissing(<MethodBody>
Do
|
Loop Until True</MethodBody>, "Loop")
        End Sub
    End Class
End Namespace
