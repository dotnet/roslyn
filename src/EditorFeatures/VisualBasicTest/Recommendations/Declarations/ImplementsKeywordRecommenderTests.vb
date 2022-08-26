' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ImplementsKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterClassDeclarationTest()
            VerifyRecommendationsContain(<File>
Class Goo
|</File>, "Implements")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterClassDeclarationAndBlankLineTest()
            VerifyRecommendationsContain(<File>
Class Goo

|</File>, "Implements")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterImplementsTest()
            VerifyRecommendationsContain(<File>
Class Goo
Implements IGooable
|</File>, "Implements")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsInStructureTest()
            VerifyRecommendationsContain(<File>
Structure Goo
|</File>, "Implements")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterInheritsTest()
            VerifyRecommendationsContain(<File>
Class Goo
Inherits Base
|</File>, "Implements")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterMethodInClassImplementingInterfaceTest()
            VerifyRecommendationsContain(<File>
Class Goo
Implements IGooable
Sub Goo() |
|</File>, "Implements")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsNotAfterMethodInClassNotImplementingInterfaceTest()
            VerifyRecommendationsMissing(<File>
Class Goo
Sub Goo() |
|</File>, "Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterPropertyNameTest()
            VerifyRecommendationsContain(
<File>
Interface goo
    Property x() As Integer
End Interface
Class bar
    Implements goo
    Property x |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyOpenParenTest()
            VerifyRecommendationsMissing(
<File>
Interface goo
    Property x() As Integer
End Interface
Class bar
    Implements goo
    Property x( |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterPropertyCloseParenTest()
            VerifyRecommendationsContain(
<File>
Interface goo
    Property x() As Integer
End Interface
Class bar
    Implements goo
    Property x() |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyAsTest()
            VerifyRecommendationsMissing(
<File>
Interface goo
    Property x() As Integer
End Interface
Class bar
    Implements goo
    Property x() As |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterCompletePropertyAsClauseTest()
            VerifyRecommendationsContain(
<File>
Interface goo
    Property x() As Integer
End Interface
Class bar
    Implements goo
    Property x() As Integer |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterIncompletePropertyAsClauseInitializerTest()
            VerifyRecommendationsMissing(
<File>
Interface goo
    Property x() As Integer
End Interface
Class bar
    Implements goo
    Property x() As Integer = |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterCompletePropertyAsClauseInitializerTest()
            VerifyRecommendationsContain(
<File>
Interface goo
    Property x() As Integer
End Interface
Class bar
    Implements goo
    Property x() As Integer = 3 |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterIncompletePropertyAsNewClauseTest()
            VerifyRecommendationsMissing(
<File>
Interface goo
    Property x() As Object
End Interface
Class bar
    Implements goo
    Property x() As New |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterCompletePropertyAsNewClauseTest()
            VerifyRecommendationsContain(
<File>
Interface goo
    Property x() As Object
End Interface
Class bar
    Implements goo
    Property x() As New Object |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyAsNewClauseOpenParenTest()
            VerifyRecommendationsMissing(
<File>
Interface goo
    Property x() As Object
End Interface
Class bar
    Implements goo
    Property x() As New Object( |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterPropertyAsNewClauseCloseParenTest()
            VerifyRecommendationsContain(
<File>
Interface goo
    Property x() As Object
End Interface
Class bar
    Implements goo
    Property x() As New Object() |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyAsNamespaceDotTest()
            VerifyRecommendationsMissing(
<File>
Interface goo
    Property x() As System.Collections.Generic.List(Of T)
End Interface
Class bar
    Implements goo
    Property x() As System.|
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyAsListOfTest()
            VerifyRecommendationsMissing(
<File>
Imports System.Collections.Generic
Interface goo
    Property x() As List(Of T)
End Interface
Class bar
    Implements goo
    Property x() As List(Of |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyAsListOfTypeTest()
            VerifyRecommendationsMissing(
<File>
Imports System.Collections.Generic
Interface goo
    Property x() As List(Of T)
End Interface
Class bar
    Implements goo
    Property x() As List(Of bar |
</File>,
"Implements")
        End Sub

        <WorkItem(543635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543635")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterPropertyParameterTest()
            VerifyRecommendationsMissing(
<File>
Imports System.Collections.Generic
Interface goo
    Property x(i As Integer) As Integer
End Interface
Class bar
    Implements goo
    Property x(i As Integer |
</File>,
"Implements")
        End Sub

        <WorkItem(543811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543811")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterEventNameTest()
            VerifyRecommendationsContain(
<File>
Interface i1
    Event myevent()
End Interface
Class C1
    Implements i1
    Event myevent |
</File>,
"Implements")
        End Sub

        <WorkItem(543811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543811")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsAfterEventOpenParenTest()
            VerifyRecommendationsMissing(
<File>
Interface i1
    Event myevent()
End Interface
Class C1
    Implements i1
    Event myevent( |
</File>,
"Implements")
        End Sub

        <WorkItem(543811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543811")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterEventCloseParenTest()
            VerifyRecommendationsContain(
<File>
Interface i1
    Event myevent()
End Interface
Class C1
    Implements i1
    Event myevent() |
</File>,
"Implements")
        End Sub

        <WorkItem(546404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546404")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterAsClauseTest()
            VerifyRecommendationsContain(
<File>
Interface I1
    Function F() As Integer
End Interface
Class Bar
    Implements I1
    Function F() As Integer |
</File>,
"Implements")
        End Sub

        <WorkItem(531254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsInPartialClass1Test()
            VerifyRecommendationsContain(
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
        End Sub

        <WorkItem(531254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsInPartialClass2Test()
            VerifyRecommendationsMissing(
<File>
Public Interface ITest
End Interface
Partial Public Class Test
End Class
Partial Public Class Test
    Sub X() |
</File>,
"Implements")
        End Sub

        <WorkItem(531257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531257")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsInInterface1Test()
            VerifyRecommendationsMissing(
<File>
Public Interface ITest1
End Interface
Public Interface ITest2
    |
</File>,
"Implements")
        End Sub

        <WorkItem(531257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531257")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsInInterface2Test()
            VerifyRecommendationsMissing(
<File>
Public Interface ITest1
End Interface
Public Interface ITest2
    Inherits ITest1
    |
</File>,
"Implements")
        End Sub

        <WorkItem(531257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531257")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoImplementsInModuleTest()
            VerifyRecommendationsMissing(
<File>
Public Interface ITest1
End Interface
Public Module Test2
    |
</File>,
"Implements")
        End Sub

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Implements")
        End Sub
    End Class
End Namespace
