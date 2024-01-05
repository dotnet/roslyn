' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ThrowKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ThrowInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Throw")
        End Sub

        <Fact>
        Public Sub ThrowInMultiLineLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                          </ClassDeclaration>, "Throw")

        End Sub

        <Fact>
        Public Sub ThrowInSingleLineLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Throw")
        End Sub

        <Fact>
        Public Sub ThrowInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Function() |
                                               </ClassDeclaration>, "Throw")
        End Sub
    End Class
End Namespace
