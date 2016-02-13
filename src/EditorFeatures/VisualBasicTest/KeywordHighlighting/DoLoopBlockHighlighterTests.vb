' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class DoLoopBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New DoLoopBlockHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoWhileLoop1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|Do While|]|} x = 1
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        [|Continue Do|]
    End If
[|Loop|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoWhileLoop2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do While|] x = 1
    If DateTime.Now.Ticks Mod 2 = 0 Then
        {|Cursor:[|Exit Do|]|}
    Else
        [|Continue Do|]
    End If
[|Loop|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoWhileLoop3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do While|] x = 1
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        {|Cursor:[|Continue Do|]|}
    End If
[|Loop|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoWhileLoop4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do While|] x = 1
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        [|Continue Do|]
    End If
{|Cursor:[|Loop|]|}
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoUntilLoop1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|Do Until|]|} x = 1
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        [|Continue Do|]
    End If
[|Loop|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoUntilLoop2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do Until|] x = 1
    If DateTime.Now.Ticks Mod 2 = 0 Then
        {|Cursor:[|Exit Do|]|}
    Else
        [|Continue Do|]
    End If
[|Loop|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoUntilLoop3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do Until|] x = 1
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        {|Cursor:[|Continue Do|]|}
    End If
[|Loop|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoUntilLoop4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do Until|] x = 1
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        [|Continue Do|]
    End If
{|Cursor:[|Loop|]|}
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoLoopWhile1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|Do|]|}
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        [|Continue Do|]
    End If
[|Loop While|] x = 1
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoLoopWhile2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do|]
    If DateTime.Now.Ticks Mod 2 = 0 Then
        {|Cursor:[|Exit Do|]|}
    Else
        [|Continue Do|]
    End If
[|Loop While|] x = 1
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoLoopWhile3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do|]
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        {|Cursor:[|Continue Do|]|}
    End If
[|Loop While|] x = 1
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoLoopWhile4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do|]
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        [|Continue Do|]
    End If
{|Cursor:[|Loop While|]|} x = 1
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoLoopUntil1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|Do|]|}
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        [|Continue Do|]
    End If
[|Loop Until|] x = 1
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoLoopUntil2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do|]
    If DateTime.Now.Ticks Mod 2 = 0 Then
        {|Cursor:[|Exit Do|]|}
    Else
        [|Continue Do|]
    End If
[|Loop Until|] x = 1
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoLoopUntil3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do|]
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        {|Cursor:[|Continue Do|]|}
    End If
[|Loop Until|] x = 1
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestDoLoopUntil4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Do|]
    If DateTime.Now.Ticks Mod 2 = 0 Then
        [|Exit Do|]
    Else
        [|Continue Do|]
    End If
{|Cursor:[|Loop Until|]|} x = 1
End Sub
End Class</Text>)
        End Function
    End Class
End Namespace
