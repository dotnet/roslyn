' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class XmlProcessingInstructionHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(XmlProcessingInstructionHighlighter)
        End Function

        <Fact>
        Public Async Function TestXmlProcessingInstruction1() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
Dim q = {|Cursor:[|<?|]|}fogbar[|?>|]
End Sub
End Class]]></Text>)
        End Function

        <Fact>
        Public Async Function TestXmlProcessingInstruction2() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
Dim q = [|<?|]fogbar{|Cursor:[|?>|]|}
End Sub
End Class]]></Text>)
        End Function

        <Fact>
        Public Async Function TestXmlProcessingInstruction3() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
Dim q = <?f{|Cursor:ooba|}r?>
End Sub
End Class]]></Text>)
        End Function
    End Class
End Namespace
