' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ToKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoToWithEmptyBoundInDim()
            VerifyRecommendationsMissing(<MethodBody>Dim i( |</MethodBody>, "To")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ToAfterLowerBoundInDim()
            VerifyRecommendationsContain(<MethodBody>Dim i(0 |</MethodBody>, "To")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoToAfterUpperBoundInDim()
            VerifyRecommendationsMissing(<MethodBody>Dim i(0 To 4 |</MethodBody>, "To")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoToAfterCommaInDim()
            VerifyRecommendationsMissing(<MethodBody>Dim i(0 To 4, |</MethodBody>, "To")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ToAfterSecondLowerBoundInDim()
            VerifyRecommendationsContain(<MethodBody>Dim i(0 To 4, 0 |</MethodBody>, "To")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoToWithEmptyBoundInReDim()
            VerifyRecommendationsMissing(<MethodBody>ReDim i( |</MethodBody>, "To")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ToAfterLowerBoundInReDim()
            VerifyRecommendationsContain(<MethodBody>ReDim i(0 |</MethodBody>, "To")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoToAfterUpperBoundInReDim()
            VerifyRecommendationsMissing(<MethodBody>ReDim i(0 To 4 |</MethodBody>, "To")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoToAfterCommaInReDim()
            VerifyRecommendationsMissing(<MethodBody>ReDim i(0 To 4, |</MethodBody>, "To")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ToAfterSecondLowerBoundInReDim()
            VerifyRecommendationsContain(<MethodBody>ReDim i(0 To 4, 0 |</MethodBody>, "To")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<MethodBody>Dim i(0 
|</MethodBody>, "To")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>Dim i(0 _
|</MethodBody>, "To")
        End Sub
    End Class
End Namespace
