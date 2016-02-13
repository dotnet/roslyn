' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class XmlProcessingInstructionHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New XmlProcessingInstructionHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestXmlProcessingInstruction1() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
Dim q = {|Cursor:[|<?|]|}fogbar[|?>|]
End Sub
End Class]]></Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestXmlProcessingInstruction2() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
Dim q = [|<?|]fogbar{|Cursor:[|?>|]|}
End Sub
End Class]]></Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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
