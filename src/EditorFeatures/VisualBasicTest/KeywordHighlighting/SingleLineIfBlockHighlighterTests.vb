' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class SingleLineIfBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New SingleLineIfBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSinglelineIf1()
            Test(<Text><![CDATA[
Class C
Sub M()
{|Cursor:[|If|]|} a < b [|Then|] a = b [|Else|] b = a
End Sub
End Class]]></Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSinglelineIf2()
            Test(<Text><![CDATA[
Class C
Sub M()
[|If|] a < b {|Cursor:[|Then|]|} a = b [|Else|] b = a
End Sub
End Class]]></Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSinglelineIf3()
            Test(<Text><![CDATA[
Class C
Sub M()
[|If|] a < b [|Then|] a = b {|Cursor:[|Else|]|} b = a
End Sub
End Class]]></Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSinglelineIfNestedInMultilineIf1()
            Test(<Text><![CDATA[
Class C
Sub M()
If a < b Then
    a = b
ElseIf DateTime.Now.Ticks Mod 2 = 0
    Throw New RandomException
Else
    {|Cursor:[|If|]|} a < b [|Then|] a = b [|Else|] b = a
End If
End Sub
End Class]]></Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSinglelineIfNestedInMultilineIf2()
            Test(<Text><![CDATA[
Class C
Sub M()
If a < b Then
    a = b
ElseIf DateTime.Now.Ticks Mod 2 = 0
    Throw New RandomException
Else
    [|If|] a < b {|Cursor:[|Then|]|} a = b [|Else|] b = a
End If
End Sub
End Class]]></Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSinglelineIfNestedInMultilineIf3()
            Test(<Text><![CDATA[
Class C
Sub M()
If a < b Then
    a = b
ElseIf DateTime.Now.Ticks Mod 2 = 0
    Throw New RandomException
Else
    [|If|] a < b [|Then|] a = b {|Cursor:[|Else|]|} b = a
End If
End Sub
End Class]]></Text>)
        End Sub
    End Class
End Namespace
