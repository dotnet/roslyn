' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class XmlCDataHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New XmlCDataHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestXmlLiteralSample6_1() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
Dim q = <?xml version="1.0"?>
<contact>
    <!-- who is this guy? -->
    <name>Bill Chiles</name>
    <phone type="home">555-555-5555</phone>
    <birthyear><%= DateTime.Today.Year - 100 %></birthyear>
    {|Cursor:[|<![CDATA[|]|}Be wary of this guy![|]]>]]<![CDATA[>|]
</contact>
End Sub
End Class]]></Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestXmlLiteralSample6_2() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
Dim q = <?xml version="1.0"?>
<contact>
    <!-- who is this guy? -->
    <name>Bill Chiles</name>
    <phone type="home">555-555-5555</phone>
    <birthyear><%= DateTime.Today.Year - 100 %></birthyear>
    [|<![CDATA[|]Be wary of this guy!{|Cursor:[|]]>]]<![CDATA[>|]|}
</contact>
End Sub
End Class]]></Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestXmlLiteralSample6_3() As Task
            Await TestAsync(<Text><![CDATA[
Class C
Sub M()
Dim q = <?xml version="1.0"?>
<contact>
    <!-- who is this guy? -->
    <name>Bill Chiles</name>
    <phone type="home">555-555-5555</phone>
    <birthyear><%= DateTime.Today.Year - 100 %></birthyear>
    <![CDATA[B{|Cursor:e wary of this guy|}!]]>]]<![CDATA[>
</contact>
End Sub
End Class]]></Text>)
        End Function
    End Class
End Namespace
