' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class HandlesClauseCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(HandlesClauseCompletionProvider)
        End Function

        <Fact>
        Public Async Function TestSuggestMeEvent() As Task
            Dim text = <text>Public Class Class1
        ' Declare an event. 
        Public Event Ev_Event()
        Sub CauseSomeEvent()
            ' Raise an event. 
            RaiseEvent Ev_Event()
        End Sub

        Sub Handler() Handles Me.$$ 
    End Class </text>.Value

            Await VerifyItemExistsAsync(text, "Ev_Event")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546497")>
        Public Async Function TestSuggestMeEventInDerived() As Task
            Dim text = <text>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base

    Sub Goo() Handles Me.$$

End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Click")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546497")>
        Public Async Function TestSuggestMeEventInIndirectDerived() As Task
            Dim text = <text>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base
End Class
Public Class IndirectDerived
    Inherits Base
    Sub Goo() Handles MyClass.$$

End Class
</text>.Value

            Await VerifyItemExistsAsync(text, "Click")
        End Function

        <Fact>
        Public Async Function TestSuggestMyBaseEvent() As Task
            Dim text = <text>Public Class BaseClass
    Public Event Event1()
End Class
                           
Public Class Class1
    Inherits BaseClass
    Sub Handler() Handles MyBase.$$ 
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Event1")
        End Function

        <Fact>
        Public Async Function TestSuggestMyClassEventEvent() As Task
            Dim text = <text>Public Class Class1
        ' Declare an event. 
        Public Event Ev_Event()
        Sub CauseSomeEvent()
            ' Raise an event. 
            RaiseEvent Ev_Event()
        End Sub

        Sub Handler() Handles MyClass.$$ 
    End Class </text>.Value

            Await VerifyItemExistsAsync(text, "Ev_Event")
        End Function

        <Fact>
        Public Async Function TestSuggestField() As Task
            Dim text = <text>Public Class Class1
        ' Declare an event. 
        Public Event Ev_Event()
        Sub CauseSomeEvent()
            ' Raise an event. 
            RaiseEvent Ev_Event()
        End Sub
    End Class 

Public Class Handler
    WithEvents handlee as New Class1

    Public Sub goo Handles $$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "handlee")
        End Function

        <Fact>
        Public Async Function TestSuggestFieldEvent() As Task
            Dim text = <text>Public Class Class1
        ' Declare an event. 
        Public Event Ev_Event()
        Sub CauseSomeEvent()
            ' Raise an event. 
            RaiseEvent Ev_Event()
        End Sub
    End Class 

Public Class Handler
    WithEvents handlee as New Class1

    Public Sub goo Handles handlee.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Ev_Event")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546508")>
        Public Async Function TestSuggestGenericFieldEvent() As Task
            Dim text = <text>Class A
    Event Ev_Event()
End Class

Class test(Of T As A)
    WithEvents obj As T

    Sub bar() Handles obj.$$

End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Ev_Event")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546494")>
        Public Async Function TestSuggestFieldDerivedEvent() As Task
            Dim text = <text>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base
End Class
Class Test
    WithEvents obj As Derived
    Sub goo() Handles obj.$$
End Class
</text>.Value

            Await VerifyItemExistsAsync(text, "Click")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546513")>
        Public Async Function TestInheritedFieldOfNestedType() As Task
            Dim text = <text>Class container
    'Delegate Sub MyDele(x As Integer)
    Class inner
        Event Ev As System.EventHandler
    End Class
    Protected WithEvents obj As inner
End Class
Class derived
    Inherits container
    Sub goo() Handles $$
End Class
</text>.Value
            Await VerifyItemExistsAsync(text, "obj")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546511")>
        Public Async Function TestDoNotShowMeShadowedEvents() As Task
            Dim text = <text>Public Class Base
    Protected Event B()
End Class
Public Class Derived
    Inherits Base
    Shadows Event B()
    Sub goo() Handles Me.$$
    End Sub
End Class

</text>.Value
            Await VerifyItemExistsAsync(text, "B", "Event Derived.B()")
            Await VerifyItemIsAbsentAsync(text, "B", "Event Base.B()")
        End Function

        <Fact>
        Public Async Function TestNotInTrivia() As Task
            Dim text = <text>Public Class Class1
        ' Declare an event. 
        Public Event Ev_Event()
        Sub CauseSomeEvent()
            ' Raise an event. 
            RaiseEvent Ev_Event()
        End Sub
    End Class 

Public Class Handler
    WithEvents handlee as New Class1

    Public Sub goo Handles '$$
End Class</text>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8307")>
        Public Async Function DoNotCrashOnDotAfterCompleteHandlesClause() As Task
            Dim text = "
Imports System

Class C
    Public Event E As EventHandler
End Class

Class D
    WithEvents c As New C

    Sub OnE(sender As Object, e As EventArgs) Handles c.E.$$

    End Sub
End Class"

            Await VerifyNoItemsExistAsync(text)
        End Function
    End Class
End Namespace
