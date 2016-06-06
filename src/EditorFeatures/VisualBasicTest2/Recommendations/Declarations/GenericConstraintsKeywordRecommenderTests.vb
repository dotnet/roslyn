' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class GenericConstraintsKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterAsInSingleConstraintTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Class Foo(Of T As |</File>, "Class", "Structure", "New")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterInMultipleConstraintTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Class Foo(Of T As {|</File>, "Class", "Structure", "New")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterExplicitTypeTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Class Foo(Of T As {OtherType, |</File>, "Class", "Structure", "New")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterStructureConstraintTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Class Foo(Of T As {Structure, |</File>, "Class", "Structure", "New")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ClassOnlyAfterNewTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Class Foo(Of T As {New, |</File>, "Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NewOnlyAfterClassTest() As Task
            Await VerifyRecommendationsContainAsync(<File>Class Foo(Of T As {Class, |</File>, "New")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterClassAndNewTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>Class Foo(Of T As {Class, New,|</File>, "Class", "Structure", "New")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>Class Foo(Of T As 
|</File>, "New")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>Class Foo(Of T As _
|</File>, "New")
        End Function
    End Class
End Namespace
