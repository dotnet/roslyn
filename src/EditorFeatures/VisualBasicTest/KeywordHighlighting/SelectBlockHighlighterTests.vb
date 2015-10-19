' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class SelectBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New SelectBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSelectBlock1()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSelectBlock2()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSelectBlock3()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSelectBlock4()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSelectBlock5()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSelectBlock6()
            Test(<Text>
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
        End Sub
    End Class
End Namespace
