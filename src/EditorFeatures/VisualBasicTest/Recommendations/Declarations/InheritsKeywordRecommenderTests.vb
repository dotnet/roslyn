' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class InheritsKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InheritsAfterClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Foo
|</File>, "Inherits")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InheritsAfterInterfaceDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Interface Foo
|</File>, "Inherits")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InheritsAfterClassDeclarationAndBlankLineTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Foo

|</File>, "Inherits")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InheritsAfterInterfaceDeclarationAndBlankLineTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Interface Foo

|</File>, "Inherits")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InheritsNotAfterImplementsTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class Foo
Implements IFooable
|</File>, "Inherits")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InheritsNotInStructureTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Structure Foo
|</File>, "Inherits")
        End Function

        <WorkItem(531257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531257")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InheritsAfterInheritsInInterfaceTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Public Interface ITest1
End Interface
Public Interface ITest2
    Inherits ITest1
    |
</File>, "Inherits")
        End Function

        <WorkItem(531257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531257")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InheritsNotAfterInheritsInClassTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Public Class Foo
    Inherits Bar
    |
</File>, "Inherits")
        End Function

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Inherits")
        End Function
    End Class
End Namespace
