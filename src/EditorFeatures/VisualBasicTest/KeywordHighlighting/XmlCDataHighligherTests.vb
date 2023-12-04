' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class XmlCDataHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(XmlCDataHighlighter)
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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
