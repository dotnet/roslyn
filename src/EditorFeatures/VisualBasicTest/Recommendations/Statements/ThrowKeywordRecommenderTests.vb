' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ThrowKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ThrowInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Throw")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ThrowInMultiLineLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                          </ClassDeclaration>, "Throw")

        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ThrowInSingleLineLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Throw")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ThrowInSingleLineFunctionLambda()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Function() |
                                               </ClassDeclaration>, "Throw")
        End Sub
    End Class
End Namespace
