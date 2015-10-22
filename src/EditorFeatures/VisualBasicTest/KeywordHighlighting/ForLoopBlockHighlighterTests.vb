' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class ForLoopBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New ForLoopBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForLoop1()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForLoop2()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForLoop3()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForLoop5()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForLoop6()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForLoop4()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForEachLoop1()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForEachLoop2()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForEachLoop3()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForEachLoop4()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForEachLoop5()
            Test(<Text>
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
        End Sub

        <WpfFact, WorkItem(541628), WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop1()
            Test(<Text>
Class C
Sub M()
{|Cursor:[|For|]|} i = 1 [|To|] 10
   For j = 1 To 10 Step 2
[|Next|] j, i
End Sub
End Class</Text>)
        End Sub

        <WpfFact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop2()
            Test(<Text>
Class C
Sub M()
For i = 1 To 10
   {|Cursor:[|For|]|} j = 1 [|To|] 10 [|Step|] 2
[|Next|] j, i
End Sub
End Class</Text>)
        End Sub

        <WpfFact, WorkItem(541628), WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop3()
            Test(<Text>
Class C
Sub M()
[|For|] i = 1 [|To|] 10
   For j = 1 To 10 Step 2
[|{|Cursor:Next|}|] j, i
End Sub
End Class</Text>)
        End Sub

        <WpfFact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop_ForMatchesNextWithSingleElementIdentifierList()
            Test(<Text>
Class C
Sub M()
[|{|Cursor:For|}|] a = 1 [|To|] 2
[|Next|] a
End Sub
End Class</Text>)
        End Sub

        <WpfFact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop_ForMatchesCorrectNextWithSingleElementIdentifierList()
            Test(<Text>
Class C
Sub M()
[|For|] a = 1 [|To|] 2
[|{|Cursor:Next|}|] a
End Sub
End Class</Text>)
        End Sub

        <WpfFact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop_ForMatchesNextOfCorrectSinglyNestedFor()
            ' Outer for blocks closed by a Next <identifier list> must go through their children for
            ' blocks to find the one that closes it (always the last such nested for block if found
            ' at the first nested level)
            Test(<Text>
Class C
Sub M()
[|{|Cursor:For|}|] a = 1 [|To|] 2
For b = 1 To 2
Next
For b = 1 To 2
[|Next|] b, a
End Sub
End Class</Text>)
        End Sub

        <WpfFact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop_ForMatchesNextAtCorrectNestingLevel()
            Test(<Text>
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
        End Sub

        <WpfFact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop_ForMatchesNextOfCorrectDoublyNestedFor()
            ' Outer for blocks closed by a Next <identifier list> must go through their children,
            ' grandchildren, etc. for blocks to find the one that closes it (always the last nested
            ' block in the last nested block (... etc.) if ever found)
            Test(<Text>
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
        End Sub

        <WpfFact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop_ForNotMatchesNextOfIncorrectNestedFor()
            ' Outer for blocks without a Next should not match the Next of a nested for block unless
            ' the next block actually closes the outer for.
            Test(<Text>
Class C
Sub M()
[|{|Cursor:For|}|] a = 1 [|To|] 2
For b = 1 To 2
Next
For b = 1 To 2
Next b
End Sub
End Class</Text>)
        End Sub

        <WpfFact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop_NextMatchesCorrectForIgnoringLoopIdentifierNames()
            ' The choice of For loop to highlight based on a Next <identifier list> statement should
            ' be based on structure, not identifier name matches.
            Test(<Text>
Class C
Sub M()
[|For|] a = 0 [|To|] 2
For b = 0 To 3
[|{|Cursor:Next|}|] z, y
End Sub
End Class</Text>)
        End Sub

        <WpfFact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop_ForMatchesCorrectNextIgnoringLoopIdentifierNames()
            ' The choice of Next <identifier list> to highlight statement should be based on
            ' structure, not identifier name matches.
            Test(<Text>
Class C
Sub M()
[|For|] a = 0 [|To|] 2
For b = 0 To 3
[|{|Cursor:Next|}|] z, y
End Sub
End Class</Text>)
        End Sub

        <WpfFact, WorkItem(544961), Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestForNestedLoop_NextMatchesOutermostForIfNextClosesMoreForsThanExist()
            Test(<Text>
Class C
Sub M()
[|For|] a = 1 [|To|] 2
    For b = 1 To 2
[|{|Cursor:Next|}|] z, b, a
End Sub
End Class</Text>)
        End Sub
    End Class
End Namespace
