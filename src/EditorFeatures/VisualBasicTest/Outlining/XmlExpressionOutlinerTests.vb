' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class XmlDocumentOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of XmlNodeSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New XmlExpressionOutliner()
        End Function


        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlDocument1()
            Dim code = "
Class C
    Dim x = {|span:$$<?xml version=""1.0""?>
            <foo>
            </foo>|}
End Class
"

            Regions(code,
                Region("span", "<?xml version=""1.0""?> ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlDocument2()
            Dim code = "
Class C
    Dim x = {|span:$$<?xml version=""1.0""?><foo>
            </foo>|}
End Class
"

            Regions(code,
                Region("span", "<?xml version=""1.0""?><foo> ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlLiteral()
            Dim code = "
Class C
    Dim x = {|span:$$<foo>
            </foo>|}
End Class
"

            Regions(code,
                Region("span", "<foo> ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestNestedXmlLiteral()
            Dim code = "
Class C
    Dim x = <foo>
                {|span:$$<bar>
                </bar>|}
            </foo>
End Class
"

            Regions(code,
                Region("span", "<bar> ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlProcessingInstruction()
            Dim code = "
Class C
    Dim x = {|span:$$<?foo
              bar=""baz""?>|}
End Class
"

            Regions(code,
                Region("span", "<?foo ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlComment()
            Dim code = "
Class C
    Dim x = {|span:$$<!-- Foo
            Bar -->|}
End Class
"

            Regions(code,
                Region("span", "<!-- Foo ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlCData()
            Dim code = "
Class C
    Dim x = {|span:$$<![CDATA[
            Foo]]>|}
End Class
"

            Regions(code,
                Region("span", "<![CDATA[ ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlEmbeddedExpression()
            Dim code = "
Class C
    Dim x = <foo>
                {|span:$$<%=
                    From c in ""abc""
                %>|}
            </foo>
End Class
"

            Regions(code,
                Region("span", "<%= ...", autoCollapse:=False))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDocumentationCommentIsNotOutlined()
            Dim code = "
''' $$<summary>
''' Foo
''' </summary>
Class C
End Class
"

            NoRegions(code)
        End Sub

    End Class
End Namespace
