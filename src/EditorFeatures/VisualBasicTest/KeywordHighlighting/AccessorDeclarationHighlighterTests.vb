' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class AccessorDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(AccessorDeclarationHighlighter)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestPropertyAccessorsSample1_1() As Task
            Await TestAsync(<Text>
Class C
Public Property Goo As Integer Implements IGoo.Goo
    {|Cursor:[|Get|]|}
        [|Return|] 1
    [|End Get|]
    Private Set(value As Integer)
        Exit Property
    End Set
End Property
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestPropertyAccessorsSample1_2() As Task
            Await TestAsync(<Text>
Class C
Public Property Goo As Integer Implements IGoo.Goo
    [|Get|]
        {|Cursor:[|Return|]|} 1
    [|End Get|]
    Private Set(value As Integer)
        Exit Property
    End Set
End Property
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestPropertyAccessorsSample1_3() As Task
            Await TestAsync(<Text>
Class C
Public Property Goo As Integer Implements IGoo.Goo
    [|Get|]
        [|Return|] 1
    {|Cursor:[|End Get|]|}
    Private Set(value As Integer)
        Exit Property
    End Set
End Property
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestPropertyAccessorsSample2_1() As Task
            Await TestAsync(<Text>
Class C
Public Property Goo As Integer Implements IGoo.Goo
    Get
        Return 1
    End Get
    {|Cursor:[|Private Set|]|}(value As Integer)
        [|Exit Property|]
    [|End Set|]
End Property
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestPropertyAccessorsSample2_2() As Task
            Await TestAsync(<Text>
Class C
Public Property Goo As Integer Implements IGoo.Goo
    Get
        Return 1
    End Get
    [|Private Set|](value As Integer)
        {|Cursor:[|Exit Property|]|}
    [|End Set|]
End Property
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestPropertyAccessorsSample2_3() As Task
            Await TestAsync(<Text>
Class C
Public Property Goo As Integer Implements IGoo.Goo
    Get
        Return 1
    End Get
    [|Private Set|](value As Integer)
        [|Exit Property|]
    {|Cursor:[|End Set|]|}
End Property
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventAccessorsSample1_1() As Task
            Await TestAsync(<Text>
Class C
Public Custom Event Goo As EventHandler Implements IGoo.Goo
    {|Cursor:[|AddHandler|]|}(value As EventHandler)
        [|Return|]
    [|End AddHandler|]
    RemoveHandler(value As EventHandler)
    End RemoveHandler
    RaiseEvent(sender As Object, e As EventArgs)
    End RaiseEvent
End Event
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventAccessorsSample1_2() As Task
            Await TestAsync(<Text>
Class C
Public Custom Event Goo As EventHandler Implements IGoo.Goo
    [|AddHandler|](value As EventHandler)
        {|Cursor:[|Return|]|}
    [|End AddHandler|]
    RemoveHandler(value As EventHandler)
    End RemoveHandler
    RaiseEvent(sender As Object, e As EventArgs)
    End RaiseEvent
End Event
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventAccessorsSample1_3() As Task
            Await TestAsync(<Text>
Class C
Public Custom Event Goo As EventHandler Implements IGoo.Goo
    [|AddHandler|](value As EventHandler)
        [|Return|]
    {|Cursor:[|End AddHandler|]|}
    RemoveHandler(value As EventHandler)
    End RemoveHandler
    RaiseEvent(sender As Object, e As EventArgs)
    End RaiseEvent
End Event
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventAccessorsSample2_1() As Task
            Await TestAsync(<Text>
Class C
Public Custom Event Goo As EventHandler Implements IGoo.Goo
    AddHandler(value As EventHandler)
        Return
    End AddHandler
    {|Cursor:[|RemoveHandler|]|}(value As EventHandler)
    [|End RemoveHandler|]
    RaiseEvent(sender As Object, e As EventArgs)
    End RaiseEvent
End Event
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventAccessorsSample2_2() As Task
            Await TestAsync(<Text>
Class C
Public Custom Event Goo As EventHandler Implements IGoo.Goo
    AddHandler(value As EventHandler)
        Return
    End AddHandler
    [|RemoveHandler|](value As EventHandler)
    {|Cursor:[|End RemoveHandler|]|}
    RaiseEvent(sender As Object, e As EventArgs)
    End RaiseEvent
End Event
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventAccessorsSample3_1() As Task
            Await TestAsync(<Text>
Class C
Public Custom Event Goo As EventHandler Implements IGoo.Goo
    AddHandler(value As EventHandler)
        Return
    End AddHandler
    RemoveHandler(value As EventHandler)
    End RemoveHandler
    {|Cursor:[|RaiseEvent|]|}(sender As Object, e As EventArgs)
    [|End RaiseEvent|]
End Event
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventAccessorsSample3_2() As Task
            Await TestAsync(<Text>
Class C
Public Custom Event Goo As EventHandler Implements IGoo.Goo
    AddHandler(value As EventHandler)
        Return
    End AddHandler
    RemoveHandler(value As EventHandler)
    End RemoveHandler
    [|RaiseEvent|](sender As Object, e As EventArgs)
    {|Cursor:[|End RaiseEvent|]|}
End Event
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestProperty_IteratorExample5_1() As Task
            Await TestAsync(
<Text>
ReadOnly Iterator Property Goo As IEnumerable(Of Integer)
    {|Cursor:[|Get|]|}
        [|Yield|] 1
    [|End Get|]
End Property
</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestProperty_IteratorExample5_2() As Task
            Await TestAsync(
<Text>
ReadOnly Iterator Property Goo As IEnumerable(Of Integer)
    [|Get|]
        {|Cursor:[|Yield|]|} 1
    [|End Get|]
End Property
</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestProperty_IteratorExample5_3() As Task
            Await TestAsync(
<Text>
ReadOnly Iterator Property Goo As IEnumerable(Of Integer)
    [|Get|]
        [|Yield|] 1
    {|Cursor:[|End Get|]|}
End Property
</Text>)
        End Function

    End Class
End Namespace
