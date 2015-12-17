' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ContinueKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ContinueNotInMethodBody()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Continue")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ContinueInForLoop()
            VerifyRecommendationsContain(<MethodBody>
For i = 1 To 10
|
Next</MethodBody>, "Continue")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ContinueInForEachLoop()
            VerifyRecommendationsContain(<MethodBody>
For Each i In j
|
Next</MethodBody>, "Continue")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ContinueInWhileLoop()
            VerifyRecommendationsContain(<MethodBody>
While True
|
End While</MethodBody>, "Continue")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ContinueInDoWhileLoop()
            VerifyRecommendationsContain(<MethodBody>
Do While True
|
Loop</MethodBody>, "Continue")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ContinueInLoopWhileLoop()
            VerifyRecommendationsContain(<MethodBody>
Do
|
Loop While True</MethodBody>, "Continue")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ContinueInInfiniteDoWhileLoop()
            VerifyRecommendationsContain(<MethodBody>
Do
|
Loop</MethodBody>, "Continue")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ContinueNotInLambda()
            VerifyRecommendationsMissing(<MethodBody>
Do
Dim x = Function()
|
        End Function
Loop</MethodBody>, "Continue")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ContinueInClassDeclarationLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Dim _x = Function()
             Do
             |
             Loop
         End Function
</ClassDeclaration>, "Continue")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ContinueNotInSingleLineLambdaInMethodBody()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Continue")
        End Sub
    End Class
End Namespace
