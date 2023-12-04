' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class XmlDocumentStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of XmlNodeSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New XmlExpressionStructureProvider()
        End Function

        <Fact>
        Public Async Function TestXmlDocument1() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<?xml version=""1.0""?>
            <goo>
            </goo>|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<?xml version=""1.0""?> ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestXmlDocument2() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<?xml version=""1.0""?><goo>
            </goo>|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<?xml version=""1.0""?><goo> ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestXmlLiteral() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<goo>
            </goo>|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<goo> ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestNestedXmlLiteral() As Task
            Dim code = "
Class C
    Dim x = <goo>
                {|span:$$<bar>
                </bar>|}
            </goo>
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<bar> ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestXmlProcessingInstruction() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<?goo
              bar=""baz""?>|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<?goo ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestXmlComment() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<!-- Goo
            Bar -->|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<!-- Goo ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestXmlCData() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<![CDATA[
            Goo]]>|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<![CDATA[ ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestXmlEmbeddedExpression() As Task
            Dim code = "
Class C
    Dim x = <goo>
                {|span:$$<%=
                    From c in ""abc""
                %>|}
            </goo>
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<%= ...", autoCollapse:=False))
        End Function

        <Fact>
        Public Async Function TestDocumentationCommentIsNotOutlined() As Task
            Dim code = "
''' $$<summary>
''' Goo
''' </summary>
Class C
End Class
"

            Await VerifyNoBlockSpansAsync(code)
        End Function
    End Class
End Namespace
