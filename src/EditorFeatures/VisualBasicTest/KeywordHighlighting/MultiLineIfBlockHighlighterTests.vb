﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class MultiLineIfBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(MultiLineIfBlockHighlighter)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineIf1() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
{|Cursor:[|If|]|} a < b [|Then|]
    a = b
[|ElseIf|] DateTime.Now.Ticks Mod 2 = 0
    Throw New RandomException
[|Else|]
    If a < b Then a = b Else b = a
[|End If|]
End Sub
End Class]]></Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineIf2() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
[|If|] a < b {|Cursor:[|Then|]|}
    a = b
[|ElseIf|] DateTime.Now.Ticks Mod 2 = 0
    Throw New RandomException
[|Else|]
    If a < b Then a = b Else b = a
[|End If|]
End Sub
End Class]]></Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineIf3() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
[|If|] a < b [|Then|]
    a = b
{|Cursor:[|ElseIf|]|} DateTime.Now.Ticks Mod 2 = 0
    Throw New RandomException
[|Else|]
    If a < b Then a = b Else b = a
[|End If|]
End Sub
End Class]]></Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineIf4() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
[|If|] a < b [|Then|]
    a = b
[|ElseIf|] DateTime.Now.Ticks Mod 2 = 0
    Throw New RandomException
[|{|Cursor:Else|}|]
    If a < b Then a = b Else b = a
[|End If|]
End Sub
End Class]]></Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineIf5() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
[|If|] a < b [|Then|]
    a = b
[|ElseIf|] DateTime.Now.Ticks Mod 2 = 0
    Throw New RandomException
[|Else|]
    If a < b Then a = b Else b = a
{|Cursor:[|End If|]|}
End Sub
End Class]]></Text>)
        End Function

        <WorkItem(542614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542614")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestMultilineIf6() As Task
            Await TestAsync(<Text><![CDATA[
Imports System
Module M
    Sub C()
        Dim x As Integer = 5
        [|If|] x < 0 [|Then|]
        {|Cursor:[|Else If|]|}

        [|End If|]
    End Sub
End Module]]></Text>)
        End Function
    End Class
End Namespace
