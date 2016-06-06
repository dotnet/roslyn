' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class HandlesKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HandlesAfterMethodInClassTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Foo
Sub Foo() |
|</File>, "Handles")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HandlesAfterMethodInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Module Foo
Sub Foo() |
|</File>, "Handles")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HandlesAfterFunctionTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Module Foo
Function Foo() As Integer |
|</File>, "Handles")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HandlesNotAfterMethodInStructureTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Structure Foo
Sub Foo() |
|</File>, "Handles")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HandlesNotAfterNewLineTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class Foo
Sub Foo() 
        |
</File>, "Handles")
        End Function

        <WorkItem(577941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577941")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoHandlesAfterIteratorTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class C
    Private Iterator Function TestIterator() |
</File>, "Handles")
        End Function
    End Class
End Namespace
