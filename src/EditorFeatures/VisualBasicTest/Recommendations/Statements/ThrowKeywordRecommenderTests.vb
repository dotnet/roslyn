' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ThrowKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ThrowInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Throw")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ThrowInMultiLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                          </ClassDeclaration>, "Throw")

        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ThrowInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Throw")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ThrowInSingleLineFunctionLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
Private _member = Function() |
                                               </ClassDeclaration>, "Throw")
        End Function
    End Class
End Namespace
