' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class XmlDocumentOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of XmlNodeSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New XmlExpressionOutliner()
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyNoRegionsAsync(code)
        End Function

    End Class
End Namespace
