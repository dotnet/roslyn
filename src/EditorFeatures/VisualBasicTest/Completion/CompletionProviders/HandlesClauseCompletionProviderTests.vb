' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Public Sub SuggestMeEvent()
            Dim text = <text>Public Class Class1
        ' Declare an event. 
        Public Event Ev_Event()
        Sub CauseSomeEvent()
            ' Raise an event. 
            RaiseEvent Ev_Event()
        End Sub

        Sub Handler() Handles Me.$$ 
    End Class </text>.Value

            VerifyItemExists(text, "Ev_Event")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546497)>
        Public Sub SuggestMeEventInDerived()
            Dim text = <text>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base

    Sub Foo() Handles Me.$$

End Class</text>.Value

            VerifyItemExists(text, "Click")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546497)>
        Public Sub SuggestMeEventInIndirectDerived()
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

            VerifyItemExists(text, "Click")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SuggestMyBaseEvent()
            Dim text = <text>Public Class BaseClass
    Public Event Event1()
End Class
                           
Public Class Class1
    Inherits BaseClass
    Sub Handler() Handles MyBase.$$ 
End Class</text>.Value

            VerifyItemExists(text, "Event1")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SuggestMyClassEventEvent()
            Dim text = <text>Public Class Class1
        ' Declare an event. 
        Public Event Ev_Event()
        Sub CauseSomeEvent()
            ' Raise an event. 
            RaiseEvent Ev_Event()
        End Sub

        Sub Handler() Handles MyClass.$$ 
    End Class </text>.Value

            VerifyItemExists(text, "Ev_Event")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SuggestField()
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

            VerifyItemExists(text, "handlee")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SuggestFieldEvent()
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

            VerifyItemExists(text, "Ev_Event")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546508)>
        Public Sub SuggestGenericFieldEvent()
            Dim text = <text>Class A
    Event Ev_Event()
End Class

Class test(Of T As A)
    WithEvents obj As T

    Sub bar() Handles obj.$$

End Class</text>.Value

            VerifyItemExists(text, "Ev_Event")
        End Sub

        <WorkItem(546494)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SuggestFieldDerivedEvent()
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

            VerifyItemExists(text, "Click")
        End Sub

        <WorkItem(546513)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InheritedFieldOfNestedType()
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
            VerifyItemExists(text, "obj")
        End Sub

        <WorkItem(546511)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoNotShowMeShadowedEvents()
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
            VerifyItemExists(text, "B", "Event Derived.B()")
            VerifyItemIsAbsent(text, "B", "Event Base.B()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInTrivia()
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

            VerifyNoItemsExist(text)
        End Sub
    End Class
End Namespace
