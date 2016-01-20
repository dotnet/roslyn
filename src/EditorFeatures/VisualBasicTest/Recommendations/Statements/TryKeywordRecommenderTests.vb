' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class TryKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TryInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Try")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TryInMultiLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Try")

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TryInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Try")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TryInSingleLineFunctionLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
Private _member = Function() |
                                         </ClassDeclaration>, "Try")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExitInTryBlockTest() As Task
            Dim code =
<MethodBody>
Try
    Exit |
</MethodBody>

            Await VerifyRecommendationsContainAsync(code, "Try")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterExitInFinallyBlockTest() As Task
            Dim code =
<MethodBody>
Try
Finally
    Exit |
</MethodBody>

            Await VerifyRecommendationsMissingAsync(code, "Try")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExitInCatchBlockTest() As Task
            Dim code =
<MethodBody>
Try
Catch
    Exit |
</MethodBody>

            Await VerifyRecommendationsContainAsync(code, "Try")
        End Function
    End Class
End Namespace
