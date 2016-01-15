' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ReturnKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReturnInMethodBodyTest() As Task
            ' We can always exit a Sub/Function, so it should be there
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Return")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReturnInPropertyGetTest() As Task
            ' We can always exit a Sub/Function, so it should be there
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
ReadOnly Property Foo
Get
|
End Get
End Property
</ClassDeclaration>, "Return")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReturnInPropertySetTest() As Task
            ' We can always exit a Sub/Function, so it should be there
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
WriteOnly Property Foo
Set
|
End Set
End Property
</ClassDeclaration>, "Return")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReturnInLoopInClassDeclarationLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub()
Do
|
Loop
End Sub
                                         </ClassDeclaration>, "Return")

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReturnInClassDeclarationLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Return")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReturnInClassDeclarationSingleLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Return")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ReturnNotInFinallyBlockTest() As Task
            Dim code =
<MethodBody>
Try
Finally
    |
</MethodBody>

            Await VerifyRecommendationsMissingAsync(code, "Return")
        End Function
    End Class
End Namespace
