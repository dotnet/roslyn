' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ToKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoToWithEmptyBoundInDimTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim i( |</MethodBody>, "To")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ToAfterLowerBoundInDimTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim i(0 |</MethodBody>, "To")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoToAfterUpperBoundInDimTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim i(0 To 4 |</MethodBody>, "To")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoToAfterCommaInDimTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim i(0 To 4, |</MethodBody>, "To")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ToAfterSecondLowerBoundInDimTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim i(0 To 4, 0 |</MethodBody>, "To")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoToWithEmptyBoundInReDimTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>ReDim i( |</MethodBody>, "To")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ToAfterLowerBoundInReDimTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ReDim i(0 |</MethodBody>, "To")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoToAfterUpperBoundInReDimTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>ReDim i(0 To 4 |</MethodBody>, "To")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoToAfterCommaInReDimTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>ReDim i(0 To 4, |</MethodBody>, "To")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ToAfterSecondLowerBoundInReDimTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ReDim i(0 To 4, 0 |</MethodBody>, "To")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<MethodBody>Dim i(0 
|</MethodBody>, "To")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>Dim i(0 _
|</MethodBody>, "To")
        End Function
    End Class
End Namespace
