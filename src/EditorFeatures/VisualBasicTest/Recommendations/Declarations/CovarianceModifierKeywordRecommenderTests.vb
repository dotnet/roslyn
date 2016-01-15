' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class CovarianceModifierKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InAfterOfInInterfaceTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Interface IFoo(Of |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OutAfterOfInInterfaceTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Interface IFoo(Of |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InNotInClassTypeParamTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Class Foo(Of |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OutNotInClassTypeParamTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Class Foo(Of |</File>, "Out")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InNotInStructureTypeParamTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Structure Foo(Of |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OutNotInStructureTypeParamTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Structure Foo(Of |</File>, "Out")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InForSecondInterfaceTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Interface IFoo(Of T, |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OutForSecondInterfaceTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Interface IFoo(Of T, |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InNotInMultipleConstraintsTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Interface IFoo(Of T As {New, |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OutNotInMultipleConstraintsTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Interface IFoo(Of T As {New, |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InAfterOfInDelegateTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Delegate Sub Foo(Of |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OutAfterOfInDelegateTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Delegate Sub Foo(Of |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InForSecondDelegateTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Delegate Sub Foo(Of |</File>, "In")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OutForSecondDelegateTypeParamTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Delegate Sub Foo(Of |</File>, "In")
        End Function

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterEolTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>Delegate Sub Foo(Of 
    |</File>, "In")
        End Function
    End Class
End Namespace
