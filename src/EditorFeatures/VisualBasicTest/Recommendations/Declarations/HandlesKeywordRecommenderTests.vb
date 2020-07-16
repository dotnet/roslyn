' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class HandlesKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HandlesAfterMethodInClassTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Goo
Sub Goo() |
|</File>, "Handles")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HandlesAfterMethodInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Module Goo
Sub Goo() |
|</File>, "Handles")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HandlesAfterFunctionTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Module Goo
Function Goo() As Integer |
|</File>, "Handles")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HandlesNotAfterMethodInStructureTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Structure Goo
Sub Goo() |
|</File>, "Handles")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HandlesNotAfterNewLineTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class Goo
Sub Goo() 
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
