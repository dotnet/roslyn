' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class DoLoopBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New DoLoopBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoWhileLoop1()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoWhileLoop2()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoWhileLoop3()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoWhileLoop4()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoUntilLoop1()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoUntilLoop2()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoUntilLoop3()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoUntilLoop4()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoLoopWhile1()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoLoopWhile2()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoLoopWhile3()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoLoopWhile4()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoLoopUntil1()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoLoopUntil2()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoLoopUntil3()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestDoLoopUntil4()
            Test(<Text>
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
        End Sub
    End Class
End Namespace
