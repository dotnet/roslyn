' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ReturnKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ReturnInMethodBodyTest()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Return")
        End Sub

        <Fact>
        Public Sub ReturnInPropertyGetTest()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsContain(<ClassDeclaration>
ReadOnly Property Goo
Get
|
End Get
End Property
</ClassDeclaration>, "Return")
        End Sub

        <Fact>
        Public Sub ReturnInPropertySetTest()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsContain(<ClassDeclaration>
WriteOnly Property Goo
Set
|
End Set
End Property
</ClassDeclaration>, "Return")
        End Sub

        <Fact>
        Public Sub ReturnInLoopInClassDeclarationLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
Do
|
Loop
End Sub
                                         </ClassDeclaration>, "Return")

        End Sub

        <Fact>
        Public Sub ReturnInClassDeclarationLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Return")
        End Sub

        <Fact>
        Public Sub ReturnInClassDeclarationSingleLineLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Return")
        End Sub

        <Fact>
        Public Sub ReturnNotInFinallyBlockTest()
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
