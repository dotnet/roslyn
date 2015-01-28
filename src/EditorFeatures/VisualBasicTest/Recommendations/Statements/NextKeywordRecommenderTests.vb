' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class NextKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NextNotInMethodBody()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Next")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NextNotInLambda()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Next")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NextNotAfterStatement()
            VerifyRecommendationsMissing(<MethodBody>
Dim x
|</MethodBody>, "Next")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NextAfterForStatement()
            VerifyRecommendationsContain(<MethodBody>
For i = 1 To 10
|</MethodBody>, "Next")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NextAfterForEachStatement()
            VerifyRecommendationsContain(<MethodBody>
For i = 1 To 10
|</MethodBody>, "Next")
        End Sub
    End Class
End Namespace
