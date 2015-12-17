' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class HandlesClauseCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New HandlesClauseCompletionProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546497)>
        Public Async Function TestSuggestMeEventInDerived() As Task
            Dim text = <text>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base

    Sub Foo() Handles Me.$$

End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Click")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546497)>
        Public Async Function TestSuggestMeEventInIndirectDerived() As Task
            Dim text = <text>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base
End Class
Public Class IndirectDerived
    Inherits Base
    Sub Foo() Handles MyClass.$$

End Class
</text>.Value

            Await VerifyItemExistsAsync(text, "Click")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

    Public Sub foo Handles $$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "handlee")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

    Public Sub foo Handles handlee.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Ev_Event")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546508)>
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

        <WorkItem(546494)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuggestFieldDerivedEvent() As Task
            Dim text = <text>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base
End Class
Class Test
    WithEvents obj As Derived
    Sub foo() Handles obj.$$
End Class
</text>.Value

            Await VerifyItemExistsAsync(text, "Click")
        End Function

        <WorkItem(546513)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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
    Sub foo() Handles $$
End Class
</text>.Value
            Await VerifyItemExistsAsync(text, "obj")
        End Function

        <WorkItem(546511)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotShowMeShadowedEvents() As Task
            Dim text = <text>Public Class Base
    Protected Event B()
End Class
Public Class Derived
    Inherits Base
    Shadows Event B()
    Sub foo() Handles Me.$$
    End Sub
End Class

</text>.Value
            Await VerifyItemExistsAsync(text, "B", "Event Derived.B()")
            Await VerifyItemIsAbsentAsync(text, "B", "Event Base.B()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

    Public Sub foo Handles '$$
End Class</text>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function
    End Class
End Namespace
