' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ElseIfDirectiveKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseIfNotInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#ElseIf")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseIfInFileAfterIfTest()
            VerifyRecommendationsContain(<File>
#If True Then
|</File>, "#ElseIf")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseIfInFileAfterElseIfTest()
            VerifyRecommendationsContain(<File>
#If True Then
#ElseIf True Then
|</File>, "#ElseIf")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseIfNotInFileAfterElseIf1Test()
            VerifyRecommendationsMissing(<File>
#If True Then
#Else
|</File>, "#ElseIf")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseIfNotInFileAfterElseIf2Test()
            VerifyRecommendationsMissing(<File>
#If True Then
#ElseIf True Then
#Else
|</File>, "#ElseIf")
        End Sub
    End Class
End Namespace
