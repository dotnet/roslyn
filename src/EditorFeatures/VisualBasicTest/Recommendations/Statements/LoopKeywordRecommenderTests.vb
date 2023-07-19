' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class LoopKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub LoopNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Loop")
        End Sub

        <Fact>
        Public Sub LoopNotInLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Loop")
        End Sub

        <Fact>
        Public Sub LoopNotAfterStatementTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x
|</MethodBody>, "Loop")
        End Sub

        <Fact>
        Public Sub LoopAfterDoStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Do
|</MethodBody>, "Loop", "Loop Until", "Loop While")
        End Sub

        <Fact>
        Public Sub LoopAfterDoUntilStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Do Until True
|</MethodBody>, "Loop")
        End Sub

        <Fact>
        Public Sub LoopUntilNotAfterDoUntilStatementTest()
            VerifyRecommendationsMissing(<MethodBody>
Do Until True
|</MethodBody>, "Loop Until", "Loop While")
        End Sub

        <Fact>
        Public Sub LoopNotInDoLoopUntilBlockTest()
            VerifyRecommendationsMissing(<MethodBody>
Do
|
Loop Until True</MethodBody>, "Loop")
        End Sub
    End Class
End Namespace
