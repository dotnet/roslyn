' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class SelectBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New SelectBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestSelectBlock1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|Select Case|]|} x
    [|Case|] 5
        Console.WriteLine("x = 5")
    [|Case|] 10
        [|Exit Select|]
    [|Case Else|]
        Console.WriteLine("Otherwise")
[|End Select|]
End Sub
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestSelectBlock2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Select Case|] x
    {|Cursor:[|Case|]|} 5
        Console.WriteLine("x = 5")
    [|Case|] 10
        [|Exit Select|]
    [|Case Else|]
        Console.WriteLine("Otherwise")
[|End Select|]
End Sub
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestSelectBlock3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Select Case|] x
    [|Case|] 5
        Console.WriteLine("x = 5")
    {|Cursor:[|Case|]|} 10
        [|Exit Select|]
    [|Case Else|]
        Console.WriteLine("Otherwise")
[|End Select|]
End Sub
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestSelectBlock4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Select Case|] x
    [|Case|] 5
        Console.WriteLine("x = 5")
    [|Case|] 10
        {|Cursor:[|Exit Select|]|}
    [|Case Else|]
        Console.WriteLine("Otherwise")
[|End Select|]
End Sub
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestSelectBlock5() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Select Case|] x
    [|Case|] 5
        Console.WriteLine("x = 5")
    [|Case|] 10
        [|Exit Select|]
    {|Cursor:[|Case Else|]|}
        Console.WriteLine("Otherwise")
[|End Select|]
End Sub
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestSelectBlock6() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Select Case|] x
    [|Case|] 5
        Console.WriteLine("x = 5")
    [|Case|] 10
        [|Exit Select|]
    [|Case Else|]
        Console.WriteLine("Otherwise")
{|Cursor:[|End Select|]|}
End Sub
End Class</Text>)
        End Function
    End Class
End Namespace
