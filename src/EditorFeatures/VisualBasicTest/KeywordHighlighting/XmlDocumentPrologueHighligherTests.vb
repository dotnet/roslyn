' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class XmlDocumentPrologueHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New XmlDeclarationHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestXmlLiteralSample1_1() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
Dim q = {|Cursor:[|<?|]|}xml version="1.0"[|?>|]
<contact>
    <!-- who is this guy? -->
    <name>Bill Chiles</name>
    <phone type="home">555-555-5555</phone>
    <birthyear><%= DateTime.Today.Year - 100 %></birthyear>
    <![CDATA[Be wary of this guy!]]>]]<![CDATA[>
</contact>
End Sub
End Class]]></Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestXmlLiteralSample1_2() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
Dim q = [|<?|]xml version="1.0"{|Cursor:[|?>|]|}
<contact>
    <!-- who is this guy? -->
    <name>Bill Chiles</name>
    <phone type="home">555-555-5555</phone>
    <birthyear><%= DateTime.Today.Year - 100 %></birthyear>
    <![CDATA[Be wary of this guy!]]>]]<![CDATA[>
</contact>
End Sub
End Class]]></Text>)
        End Function
    End Class
End Namespace
