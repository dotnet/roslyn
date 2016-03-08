' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class EventBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New EventBlockHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventSample2_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Custom Event|]|} Foo As EventHandler [|Implements|] IFoo.Foo
    AddHandler(value As EventHandler)
    End AddHandler
    RemoveHandler(value As EventHandler)
    End RemoveHandler
    RaiseEvent(sender As Object, e As EventArgs)
    End RaiseEvent
[|End Event|]
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventSample2_2() As Task
            Await TestAsync(<Text>
Class C
[|Public Custom Event|] Foo As EventHandler {|Cursor:[|Implements|]|} IFoo.Foo
    AddHandler(value As EventHandler)
    End AddHandler
    RemoveHandler(value As EventHandler)
    End RemoveHandler
    RaiseEvent(sender As Object, e As EventArgs)
    End RaiseEvent
[|End Event|]
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestEventSample2_3() As Task
            Await TestAsync(<Text>
Class C
[|Public Custom Event|] Foo As EventHandler [|Implements|] IFoo.Foo
    AddHandler(value As EventHandler)
    End AddHandler
    RemoveHandler(value As EventHandler)
    End RemoveHandler
    RaiseEvent(sender As Object, e As EventArgs)
    End RaiseEvent
{|Cursor:[|End Event|]|}
End Class</Text>)
        End Function
    End Class
End Namespace
