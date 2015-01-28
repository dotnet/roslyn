' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ElseIfKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseIfNotInMethodBody()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseIfInMultiLineIf()
            VerifyRecommendationsContain(<MethodBody>If True Then
|
End If</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseIfInMultiLineElseIf1()
            VerifyRecommendationsContain(<MethodBody>If True Then
ElseIf True Then
|
End If</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseIfInMultiLineElseIf2()
            VerifyRecommendationsContain(<MethodBody>If True Then
Else If True Then
|
End If</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseIfNotInMultiLineElse()
            VerifyRecommendationsMissing(<MethodBody>If True Then
Else 
|
End If</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseIfNotInSingleLineIf1()
            VerifyRecommendationsMissing(<MethodBody>If True Then |</MethodBody>, "ElseIf")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ElseIfNotInSingleLineIf2()
            VerifyRecommendationsMissing(<MethodBody>If True Then Stop Else |</MethodBody>, "ElseIf")
        End Sub
    End Class
End Namespace
