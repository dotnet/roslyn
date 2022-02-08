' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class SelectBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(SelectBlockHighlighter)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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
