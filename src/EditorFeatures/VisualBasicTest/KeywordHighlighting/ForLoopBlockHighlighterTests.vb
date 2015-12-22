' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class ForLoopBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New ForLoopBlockHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForLoop1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|For|]|} i = 2 [|To|] 10 [|Step|] 2
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit For|]
    Else
        [|Continue For|]
    End If
[|Next|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForLoop2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For|] i = 2 {|Cursor:[|To|]|} 10 [|Step|] 2
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit For|]
    Else
        [|Continue For|]
    End If
[|Next|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForLoop3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For|] i = 2 [|To|] 10 {|Cursor:[|Step|]|} 2
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit For|]
    Else
        [|Continue For|]
    End If
[|Next|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForLoop5() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For|] i = 2 [|To|] 10 [|Step|] 2
    If DateTime.Now.Ticks Mod 2 = 0 Then
        {|Cursor:[|Exit For|]|}
    Else
        [|Continue For|]
    End If
[|Next|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForLoop6() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For|] i = 2 [|To|] 10 [|Step|] 2
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit For|]
    Else
        {|Cursor:[|Continue For|]|}
    End If
[|Next|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForLoop4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For|] i = 2 [|To|] 10 [|Step|] 2
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit For|]
    Else
        [|Continue For|]
    End If
{|Cursor:[|Next|]|}
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForEachLoop1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|For Each|]|} x [|In|] a
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit For|]
    Else
        [|Continue For|]
    End If
[|Next|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForEachLoop2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For Each|] x {|Cursor:[|In|]|} a
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit For|]
    Else
        [|Continue For|]
    End If
[|Next|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForEachLoop3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For Each|] x [|In|] a
    If DateTime.Now.Ticks Mod 2 = 0 Then
        {|Cursor:[|Exit For|]|}
    Else
        [|Continue For|]
    End If
[|Next|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForEachLoop4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For Each|] x [|In|] a
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit For|]
    Else
        {|Cursor:[|Continue For|]|}
    End If
[|Next|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForEachLoop5() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For Each|] x [|In|] a
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit For|]
    Else
        [|Continue For|]
    End If
{|Cursor:[|Next|]|}
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(541628), WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|For|]|} i = 1 [|To|] 10
   For j = 1 To 10 Step 2
[|Next|] j, i
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
For i = 1 To 10
   {|Cursor:[|For|]|} j = 1 [|To|] 10 [|Step|] 2
[|Next|] j, i
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(541628), WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For|] i = 1 [|To|] 10
   For j = 1 To 10 Step 2
[|{|Cursor:Next|}|] j, i
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop_ForMatchesNextWithSingleElementIdentifierList() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|{|Cursor:For|}|] a = 1 [|To|] 2
[|Next|] a
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop_ForMatchesCorrectNextWithSingleElementIdentifierList() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For|] a = 1 [|To|] 2
[|{|Cursor:Next|}|] a
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop_ForMatchesNextOfCorrectSinglyNestedFor() As Task
            ' Outer for blocks closed by a Next <identifier list> must go through their children for
            ' blocks to find the one that closes it (always the last such nested for block if found
            ' at the first nested level)
            Await TestAsync(<Text>
Class C
Sub M()
[|{|Cursor:For|}|] a = 1 [|To|] 2
For b = 1 To 2
Next
For b = 1 To 2
[|Next|] b, a
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop_ForMatchesNextAtCorrectNestingLevel() As Task
            Await TestAsync(<Text>
Class C
Sub M()For a = 1 To 2
For b = 1 To 2
[|{|Cursor:For|}|] c = 1 [|To|] 2
For d = 1 To 2
For e = 1 To 2
For f = 1 To 2
Next f, e
[|Next|] d, c
Next b, a
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop_ForMatchesNextOfCorrectDoublyNestedFor() As Task
            ' Outer for blocks closed by a Next <identifier list> must go through their children,
            ' grandchildren, etc. for blocks to find the one that closes it (always the last nested
            ' block in the last nested block (... etc.) if ever found)
            Await TestAsync(<Text>
Class C
Sub M()
[|{|Cursor:For|}|] a = 1 [|To|] 2
For b = 1 To 2
Next
For b = 1 To 2
For c = 1 To 2
For d = 1 To 2
Next d, c
For c = 1 To 2
[|Next|] c, b, a
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop_ForNotMatchesNextOfIncorrectNestedFor() As Task
            ' Outer for blocks without a Next should not match the Next of a nested for block unless
            ' the next block actually closes the outer for.
            Await TestAsync(<Text>
Class C
Sub M()
[|{|Cursor:For|}|] a = 1 [|To|] 2
For b = 1 To 2
Next
For b = 1 To 2
Next b
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop_NextMatchesCorrectForIgnoringLoopIdentifierNames() As Task
            ' The choice of For loop to highlight based on a Next <identifier list> statement should
            ' be based on structure, not identifier name matches.
            Await TestAsync(<Text>
Class C
Sub M()
[|For|] a = 0 [|To|] 2
For b = 0 To 3
[|{|Cursor:Next|}|] z, y
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop_ForMatchesCorrectNextIgnoringLoopIdentifierNames() As Task
            ' The choice of Next <identifier list> to highlight statement should be based on
            ' structure, not identifier name matches.
            Await TestAsync(<Text>
Class C
Sub M()
[|For|] a = 0 [|To|] 2
For b = 0 To 3
[|{|Cursor:Next|}|] z, y
End Sub
End Class</Text>)
        End Function

        <Fact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestForNestedLoop_NextMatchesOutermostForIfNextClosesMoreForsThanExist() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|For|] a = 1 [|To|] 2
    For b = 1 To 2
[|{|Cursor:Next|}|] z, b, a
End Sub
End Class</Text>)
        End Function
    End Class
End Namespace
