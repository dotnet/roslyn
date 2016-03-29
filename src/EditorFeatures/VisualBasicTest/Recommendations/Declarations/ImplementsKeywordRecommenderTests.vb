' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ImplementsKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Foo
|</File>, "Implements")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterClassDeclarationAndBlankLineTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Foo

|</File>, "Implements")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterImplementsTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Foo
Implements IFooable
|</File>, "Implements")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsInStructureTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Structure Foo
|</File>, "Implements")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterInheritsTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Foo
Inherits Base
|</File>, "Implements")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterMethodInClassImplementingInterfaceTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Class Foo
Implements IFooable
Sub Foo() |
|</File>, "Implements")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsNotAfterMethodInClassNotImplementingInterfaceTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class Foo
Sub Foo() |
|</File>, "Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterPropertyNameTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsAfterPropertyOpenParenTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x( |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterPropertyCloseParenTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x() |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsAfterPropertyAsTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x() As |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterCompletePropertyAsClauseTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x() As Integer |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsAfterIncompletePropertyAsClauseInitializerTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x() As Integer = |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterCompletePropertyAsClauseInitializerTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>
Interface foo
    Property x() As Integer
End Interface
Class bar
    Implements foo
    Property x() As Integer = 3 |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsAfterIncompletePropertyAsNewClauseTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Interface foo
    Property x() As Object
End Interface
Class bar
    Implements foo
    Property x() As New |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterCompletePropertyAsNewClauseTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>
Interface foo
    Property x() As Object
End Interface
Class bar
    Implements foo
    Property x() As New Object |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsAfterPropertyAsNewClauseOpenParenTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Interface foo
    Property x() As Object
End Interface
Class bar
    Implements foo
    Property x() As New Object( |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterPropertyAsNewClauseCloseParenTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>
Interface foo
    Property x() As Object
End Interface
Class bar
    Implements foo
    Property x() As New Object() |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsAfterPropertyAsNamespaceDotTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Interface foo
    Property x() As System.Collections.Generic.List(Of T)
End Interface
Class bar
    Implements foo
    Property x() As System.|
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsAfterPropertyAsListOfTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Imports System.Collections.Generic
Interface foo
    Property x() As List(Of T)
End Interface
Class bar
    Implements foo
    Property x() As List(Of |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsAfterPropertyAsListOfTypeTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Imports System.Collections.Generic
Interface foo
    Property x() As List(Of T)
End Interface
Class bar
    Implements foo
    Property x() As List(Of bar |
</File>,
"Implements")
        End Function

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsAfterPropertyParameterTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Imports System.Collections.Generic
Interface foo
    Property x(i As Integer) As Integer
End Interface
Class bar
    Implements foo
    Property x(i As Integer |
</File>,
"Implements")
        End Function

        <WorkItem(543811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543811")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterEventNameTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>
Interface i1
    Event myevent()
End Interface
Class C1
    Implements i1
    Event myevent |
</File>,
"Implements")
        End Function

        <WorkItem(543811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543811")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsAfterEventOpenParenTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Interface i1
    Event myevent()
End Interface
Class C1
    Implements i1
    Event myevent( |
</File>,
"Implements")
        End Function

        <WorkItem(543811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543811")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterEventCloseParenTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>
Interface i1
    Event myevent()
End Interface
Class C1
    Implements i1
    Event myevent() |
</File>,
"Implements")
        End Function

        <WorkItem(546404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546404")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterAsClauseTest() As Task
            Await VerifyRecommendationsContainAsync(
<File>
Interface I1
    Function F() As Integer
End Interface
Class Bar
    Implements I1
    Function F() As Integer |
</File>,
"Implements")
        End Function

        <WorkItem(531254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsInPartialClass1Test() As Task
            Await VerifyRecommendationsContainAsync(
<File>
Public Interface ITest
End Interface
Partial Public Class Test
    Implements ITest
End Class
Partial Public Class Test
    Sub X() |
</File>,
"Implements")
        End Function

        <WorkItem(531254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsInPartialClass2Test() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Public Interface ITest
End Interface
Partial Public Class Test
End Class
Partial Public Class Test
    Sub X() |
</File>,
"Implements")
        End Function

        <WorkItem(531257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531257")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsInInterface1Test() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Public Interface ITest1
End Interface
Public Interface ITest2
    |
</File>,
"Implements")
        End Function

        <WorkItem(531257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531257")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsInInterface2Test() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Public Interface ITest1
End Interface
Public Interface ITest2
    Inherits ITest1
    |
</File>,
"Implements")
        End Function

        <WorkItem(531257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531257")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoImplementsInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(
<File>
Public Interface ITest1
End Interface
Public Module Test2
    |
</File>,
"Implements")
        End Function

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Implements")
        End Function
    End Class
End Namespace
