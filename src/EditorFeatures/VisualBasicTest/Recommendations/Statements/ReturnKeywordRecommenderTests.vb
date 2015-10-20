' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ReturnKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReturnInMethodBody()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Return")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReturnInPropertyGet()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsContain(<ClassDeclaration>
ReadOnly Property Foo
Get
|
End Get
End Property
</ClassDeclaration>, "Return")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReturnInPropertySet()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsContain(<ClassDeclaration>
WriteOnly Property Foo
Set
|
End Set
End Property
</ClassDeclaration>, "Return")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReturnInLoopInClassDeclarationLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
Do
|
Loop
End Sub
                                         </ClassDeclaration>, "Return")

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReturnInClassDeclarationLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Return")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReturnInClassDeclarationSingleLineLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Return")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ReturnNotInFinallyBlock()
            Dim code =
<MethodBody>
Try
Finally
    |
</MethodBody>

            VerifyRecommendationsMissing(code, "Return")
        End Sub

    End Class
End Namespace
