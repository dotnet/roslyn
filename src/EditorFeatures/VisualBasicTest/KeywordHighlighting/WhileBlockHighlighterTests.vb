' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class WhileBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New WhileBlockHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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
