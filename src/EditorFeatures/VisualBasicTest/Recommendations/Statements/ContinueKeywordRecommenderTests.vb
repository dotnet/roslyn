' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ContinueKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ContinueNotInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Continue")
        End Sub

        <Fact>
        Public Sub ContinueInForLoopTest()
            VerifyRecommendationsContain(<MethodBody>
For i = 1 To 10
|
Next</MethodBody>, "Continue")
        End Sub

        <Fact>
        Public Sub ContinueInForEachLoopTest()
            VerifyRecommendationsContain(<MethodBody>
For Each i In j
|
Next</MethodBody>, "Continue")
        End Sub

        <Fact>
        Public Sub ContinueInWhileLoopTest()
            VerifyRecommendationsContain(<MethodBody>
While True
|
End While</MethodBody>, "Continue")
        End Sub

        <Fact>
        Public Sub ContinueInDoWhileLoopTest()
            VerifyRecommendationsContain(<MethodBody>
Do While True
|
Loop</MethodBody>, "Continue")
        End Sub

        <Fact>
        Public Sub ContinueInLoopWhileLoopTest()
            VerifyRecommendationsContain(<MethodBody>
Do
|
Loop While True</MethodBody>, "Continue")
        End Sub

        <Fact>
        Public Sub ContinueInInfiniteDoWhileLoopTest()
            VerifyRecommendationsContain(<MethodBody>
Do
|
Loop</MethodBody>, "Continue")
        End Sub

        <Fact>
        Public Sub ContinueNotInLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Do
Dim x = Function()
|
        End Function
Loop</MethodBody>, "Continue")
        End Sub

        <Fact>
        Public Sub ContinueInClassDeclarationLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Dim _x = Function()
             Do
             |
             Loop
         End Function
</ClassDeclaration>, "Continue")
        End Sub

        <Fact>
        Public Sub ContinueNotInSingleLineLambdaInMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Continue")
        End Sub
    End Class
End Namespace
