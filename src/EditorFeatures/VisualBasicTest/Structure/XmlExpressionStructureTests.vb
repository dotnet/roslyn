' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class XmlDocumentStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of XmlNodeSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New XmlExpressionStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestXmlDocument1() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<?xml version=""1.0""?>
            <foo>
            </foo>|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<?xml version=""1.0""?> ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestXmlDocument2() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<?xml version=""1.0""?><foo>
            </foo>|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<?xml version=""1.0""?><foo> ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestXmlLiteral() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<foo>
            </foo>|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<foo> ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestNestedXmlLiteral() As Task
            Dim code = "
Class C
    Dim x = <foo>
                {|span:$$<bar>
                </bar>|}
            </foo>
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<bar> ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestXmlProcessingInstruction() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<?foo
              bar=""baz""?>|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<?foo ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestXmlComment() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<!-- Foo
            Bar -->|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<!-- Foo ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestXmlCData() As Task
            Dim code = "
Class C
    Dim x = {|span:$$<![CDATA[
            Foo]]>|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<![CDATA[ ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestXmlEmbeddedExpression() As Task
            Dim code = "
Class C
    Dim x = <foo>
                {|span:$$<%=
                    From c in ""abc""
                %>|}
            </foo>
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "<%= ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestDocumentationCommentIsNotOutlined() As Task
            Dim code = "
''' $$<summary>
''' Foo
''' </summary>
Class C
End Class
"

            Await VerifyNoBlockSpansAsync(code)
        End Function
    End Class
End Namespace