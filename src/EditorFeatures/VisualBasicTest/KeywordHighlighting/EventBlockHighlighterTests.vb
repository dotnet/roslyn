' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class EventBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(EventBlockHighlighter)
        End Function

        <Fact>
        Public Async Function TestEventSample2_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Custom Event|]|} Goo As EventHandler [|Implements|] IGoo.Goo
    AddHandler(value As EventHandler)
    End AddHandler
    RemoveHandler(value As EventHandler)
    End RemoveHandler
    RaiseEvent(sender As Object, e As EventArgs)
    End RaiseEvent
[|End Event|]
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestEventSample2_2() As Task
            Await TestAsync(<Text>
Class C
[|Public Custom Event|] Goo As EventHandler {|Cursor:[|Implements|]|} IGoo.Goo
    AddHandler(value As EventHandler)
    End AddHandler
    RemoveHandler(value As EventHandler)
    End RemoveHandler
    RaiseEvent(sender As Object, e As EventArgs)
    End RaiseEvent
[|End Event|]
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestEventSample2_3() As Task
            Await TestAsync(<Text>
Class C
[|Public Custom Event|] Goo As EventHandler [|Implements|] IGoo.Goo
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
