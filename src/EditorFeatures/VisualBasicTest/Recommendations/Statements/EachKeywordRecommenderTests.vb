' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class EachKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EachNotInMethodBody()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Each")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EachAfterForKeyword()
            VerifyRecommendationsContain(<MethodBody>For |</MethodBody>, "Each")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EachNotAfterTouchingFor()
            VerifyRecommendationsMissing(<MethodBody>For|</MethodBody>, "Each")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EachTouchingLoopIdentifier()
            VerifyRecommendationsContain(<MethodBody>For i|</MethodBody>, "Each")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<MethodBody>For 
|</MethodBody>, "Each")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>For _
|</MethodBody>, "Each")
        End Sub
    End Class
End Namespace
