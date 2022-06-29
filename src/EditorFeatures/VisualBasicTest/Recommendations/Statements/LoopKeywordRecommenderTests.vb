' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class LoopKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Loop")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopNotInLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Loop")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopNotAfterStatementTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x
|</MethodBody>, "Loop")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopAfterDoStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Do
|</MethodBody>, "Loop", "Loop Until", "Loop While")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopAfterDoUntilStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Do Until True
|</MethodBody>, "Loop")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopUntilNotAfterDoUntilStatementTest()
            VerifyRecommendationsMissing(<MethodBody>
Do Until True
|</MethodBody>, "Loop Until", "Loop While")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub LoopNotInDoLoopUntilBlockTest()
            VerifyRecommendationsMissing(<MethodBody>
Do
|
Loop Until True</MethodBody>, "Loop")
        End Sub
    End Class
End Namespace
