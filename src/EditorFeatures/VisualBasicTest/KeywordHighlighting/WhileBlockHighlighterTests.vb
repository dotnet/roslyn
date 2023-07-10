' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class WhileBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(WhileBlockHighlighter)
        End Function

        <Fact>
        Public Async Function TestWhileBlock1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|While|]|} True
    If x Then
        [|Exit While|]
    Else
        [|Continue While|]
    End If
[|End While|]
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestWhileBlock2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|While|] True
    If x Then
        {|Cursor:[|Exit While|]|}
    Else
        [|Continue While|]
    End If
[|End While|]
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestWhileBlock3() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|While|] True
    If x Then
        [|Exit While|]
    Else
        {|Cursor:[|Continue While|]|}
    End If
[|End While|]
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestWhileBlock4() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|While|] True
    If x Then
        [|Exit While|]
    Else
        [|Continue While|]
    End If
{|Cursor:[|End While|]|}
End Sub
End Class</Text>)
        End Function
    End Class
End Namespace
