' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class WhileBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New WhileBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestWhileBlock1()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestWhileBlock2()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestWhileBlock3()
            Test(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestWhileBlock4()
            Test(<Text>
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
        End Sub
    End Class
End Namespace
