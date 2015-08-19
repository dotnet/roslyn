' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class XmlProcessingInstructionHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New XmlProcessingInstructionHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestXmlProcessingInstruction1()
            Test(<Text><![CDATA[
Class C
Sub M()
Dim q = {|Cursor:[|<?|]|}fogbar[|?>|]
End Sub
End Class]]></Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestXmlProcessingInstruction2()
            Test(<Text><![CDATA[
Class C
Sub M()
Dim q = [|<?|]fogbar{|Cursor:[|?>|]|}
End Sub
End Class]]></Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestXmlProcessingInstruction3()
            Test(<Text><![CDATA[
Class C
Sub M()
Dim q = <?f{|Cursor:ooba|}r?>
End Sub
End Class]]></Text>)
        End Sub
    End Class
End Namespace
