' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    Public Class SyntacticClassifierTests
        Inherits AbstractVisualBasicClassifierTests

        Protected Overrides Function GetClassificationSpansAsync(code As String, span As TextSpan, parseOptions As ParseOptions) As Task(Of ImmutableArray(Of ClassifiedSpan))
            Using Workspace = TestWorkspace.CreateVisualBasic(code)
                Dim document = Workspace.CurrentSolution.Projects.First().Documents.First()

                Return GetSyntacticClassificationsAsync(document, span)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName1() As Task
            Await TestInExpressionAsync("<goo></goo>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName2() As Task
            Await TestInExpressionAsync("<goo",
                VBXmlDelimiter("<"),
                VBXmlName("goo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName3() As Task
            Await TestInExpressionAsync("<goo>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName4() As Task
            Await TestInExpressionAsync("<goo.",
                VBXmlDelimiter("<"),
                VBXmlName("goo."))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName5() As Task
            Await TestInExpressionAsync("<goo.b",
                VBXmlDelimiter("<"),
                VBXmlName("goo.b"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName6() As Task
            Await TestInExpressionAsync("<goo.b>",
                VBXmlDelimiter("<"),
                VBXmlName("goo.b"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName7() As Task
            Await TestInExpressionAsync("<goo:",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName8() As Task
            Await TestInExpressionAsync("<goo:b",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"),
                VBXmlName("b"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName9() As Task
            Await TestInExpressionAsync("<goo:b>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"),
                VBXmlName("b"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmptyElementName1() As Task
            Await TestInExpressionAsync("<goo/>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmptyElementName2() As Task
            Await TestInExpressionAsync("<goo. />",
                VBXmlDelimiter("<"),
                VBXmlName("goo."),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmptyElementName3() As Task
            Await TestInExpressionAsync("<goo.bar />",
                VBXmlDelimiter("<"),
                VBXmlName("goo.bar"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmptyElementName4() As Task
            Await TestInExpressionAsync("<goo: />",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmptyElementName5() As Task
            Await TestInExpressionAsync("<goo:bar />",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"),
                VBXmlName("bar"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeName1() As Task
            Await TestInExpressionAsync("<goo b",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("b"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeName2() As Task
            Await TestInExpressionAsync("<goo ba",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("ba"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeName3() As Task
            Await TestInExpressionAsync("<goo bar=",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue1() As Task
            Await TestInExpressionAsync("<goo bar=""",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue2() As Task
            Await TestInExpressionAsync("<goo bar=""b",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("b" & vbCrLf))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue3() As Task
            Await TestInExpressionAsync("<goo bar=""ba",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("ba" & vbCrLf))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue4() As Task
            Await TestInExpressionAsync("<goo bar=""ba""",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("ba"),
                VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue5() As Task
            Await TestInExpressionAsync("<goo bar=""""",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue6() As Task
            Await TestInExpressionAsync("<goo bar=""b""",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("b"),
                VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue7() As Task
            Await TestInExpressionAsync("<goo bar=""ba""",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("ba"),
                VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValueMultiple1() As Task
            Await TestInExpressionAsync("<goo bar=""ba"" baz="""" ",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("ba"),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeName("baz"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValueMultiple2() As Task
            Await TestInExpressionAsync("<goo bar=""ba"" baz=""a"" ",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("ba"),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeName("baz"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("a"),
                VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementContent1() As Task
            Await TestInExpressionAsync("<f>&l</f>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlEntityReference("&"),
                VBXmlText("l"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementContent2() As Task
            Await TestInExpressionAsync("<f>goo</f>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlText("goo"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementContent3() As Task
            Await TestInExpressionAsync("<f>&#x03C0;</f>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlEntityReference("&#x03C0;"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementContent4() As Task
            Await TestInExpressionAsync("<f>goo &#x03C0;</f>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlText("goo "),
                VBXmlEntityReference("&#x03C0;"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementContent5() As Task
            Await TestInExpressionAsync("<f>goo &lt;</f>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlText("goo "),
                VBXmlEntityReference("&lt;"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementContent6() As Task
            Await TestInExpressionAsync("<f>goo &lt; bar</f>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlText("goo "),
                VBXmlEntityReference("&lt;"),
                VBXmlText(" bar"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementContent7() As Task
            Await TestInExpressionAsync("<f>goo &lt;",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlText("goo "),
                VBXmlEntityReference("&lt;"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlCData1() As Task
            Await TestInExpressionAsync("<f><![CDATA[bar]]></f>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<![CDATA["),
                VBXmlCDataSection("bar"),
                VBXmlDelimiter("]]>"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlCData4() As Task
            Await TestInExpressionAsync("<f><![CDATA[bar]]>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<![CDATA["),
                VBXmlCDataSection("bar"),
                VBXmlDelimiter("]]>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlCData5() As Task
            Await TestInExpressionAsync("<f><![CDATA[<>/]]>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<![CDATA["),
                VBXmlCDataSection("<>/"),
                VBXmlDelimiter("]]>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlCData6() As Task
            Dim code =
"<f><![CDATA[goo
baz]]></f>"

            Await TestInExpressionAsync(code,
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<![CDATA["),
                VBXmlCDataSection("goo" & vbCrLf),
                VBXmlCDataSection("baz"),
                VBXmlDelimiter("]]>"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAtElementName1() As Task
            Await TestInExpressionAsync("<<%= ",
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAtElementName2() As Task
            Await TestInExpressionAsync("<<%= %>",
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                VBXmlEmbeddedExpression("%>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAtElementName3() As Task
            Await TestInExpressionAsync("<<%= bar %>",
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAtElementName4() As Task
            Await TestInExpressionAsync("<<%= bar.Baz() %>",
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                Operators.Dot,
                Identifier("Baz"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                VBXmlEmbeddedExpression("%>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAtElementName5() As Task
            Await TestInExpressionAsync("<<%= bar.Baz() %> />",
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                Operators.Dot,
                Identifier("Baz"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAtElementName6() As Task
            Await TestInExpressionAsync("<<%= bar %> />",
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute1() As Task
            Await TestInExpressionAsync("<goo <%= bar %>>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute2() As Task
            Await TestInExpressionAsync("<goo <%= bar %>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute3() As Task
            Await TestInExpressionAsync("<goo <%= bar %>></goo>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute4() As Task
            Await TestInExpressionAsync("<goo <%= bar %> />",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttributeValue1() As Task
            Await TestInExpressionAsync("<goo bar=<%=baz >",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("baz"),
                Operators.GreaterThan)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttributeValue2() As Task
            Await TestInExpressionAsync("<goo bar=<%=baz %> >",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("baz"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttributeValue3() As Task
            Await TestInExpressionAsync("<goo bar=<%=baz.Goo %> >",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("baz"),
                Operators.Dot,
                Identifier("Goo"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsElementContent1() As Task
            Await TestInExpressionAsync("<f><%= bar %></f>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsElementContent2() As Task
            Await TestInExpressionAsync("<f><%= bar.Goo %></f>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                Operators.Dot,
                Identifier("Goo"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsElementContent3() As Task
            Await TestInExpressionAsync("<f><%= bar.Goo %> jaz</f>",
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                Operators.Dot,
                Identifier("Goo"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlText(" jaz"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsElementContentNested() As Task
            Dim code =
"Dim doc = _
    <goo>
        <%= <bug141>
                <a>hello</a>
            </bug141> %>
    </goo>"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("doc"),
                Operators.Equals,
                LineContinuation,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("<%="),
                VBXmlDelimiter("<"),
                VBXmlName("bug141"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("a"),
                VBXmlDelimiter(">"),
                VBXmlText("hello"),
                VBXmlDelimiter("</"),
                VBXmlName("a"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("bug141"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("</"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsElementContentNestedCommentsAfterLineContinuation() As Task
            Dim code =
"Dim doc = _ ' Test
    <goo>
        <%= <bug141>
                <a>hello</a>
            </bug141> %>
    </goo>"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("doc"),
                Operators.Equals,
                LineContinuation,
                Comment("' Test"),
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("<%="),
                VBXmlDelimiter("<"),
                VBXmlName("bug141"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("a"),
                VBXmlDelimiter(">"),
                VBXmlText("hello"),
                VBXmlDelimiter("</"),
                VBXmlName("a"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("bug141"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("</"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiteralsInLambdas() As Task
            Dim code =
"Dim x = Function() _
                    <element val=""something""/>
Dim y = Function() <element val=""something""/>"

            Await TestAsync(code,
                Keyword("Dim"),
                Field("x"),
                Operators.Equals,
                Keyword("Function"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                LineContinuation,
                VBXmlDelimiter("<"),
                VBXmlName("element"),
                VBXmlAttributeName("val"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("something"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("/>"),
                Keyword("Dim"),
                Field("y"),
                Operators.Equals,
                Keyword("Function"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                VBXmlDelimiter("<"),
                VBXmlName("element"),
                VBXmlAttributeName("val"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("something"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiteralsInLambdasCommentsAfterLineContinuation() As Task
            Dim code =
"Dim x = Function() _ 'Test
                    <element val=""something""/>
Dim y = Function() <element val=""something""/>"

            Await TestAsync(code,
                Keyword("Dim"),
                Field("x"),
                Operators.Equals,
                Keyword("Function"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                LineContinuation,
                Comment("'Test"),
                VBXmlDelimiter("<"),
                VBXmlName("element"),
                VBXmlAttributeName("val"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("something"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("/>"),
                Keyword("Dim"),
                Field("y"),
                Operators.Equals,
                Keyword("Function"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                VBXmlDelimiter("<"),
                VBXmlName("element"),
                VBXmlAttributeName("val"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("something"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocumentPrologue() As Task
            Await TestInExpressionAsync("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>",
                VBXmlDelimiter("<?"),
                VBXmlName("xml"),
                VBXmlAttributeName("version"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1.0"),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeName("encoding"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("UTF-8"),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeName("standalone"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("yes"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("?>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiterals1() As Task
            Dim code =
"Dim a = <Customer id1=""1"" id2=""2"" id3=<%= n2 %> id4="""">
                    <!-- This is a simple Xml element with all of the node types -->
                    <Name>Me</Name>
                    <NameUsingExpression><%= n1 %></NameUsingExpression>
                    <Street>10802 177th CT NE</Street>
                    <Misc><![CDATA[Let's add some CDATA
 for fun. ]]>
                    </Misc>
                    <Empty><%= Nothing %></Empty>
                </Customer>"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("a"),
                Operators.Equals,
                VBXmlDelimiter("<"),
                VBXmlName("Customer"),
                VBXmlAttributeName("id1"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1"),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeName("id2"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("2"),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeName("id3"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("n2"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlAttributeName("id4"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<!--"),
                VBXmlComment(" This is a simple Xml element with all of the node types "),
                VBXmlDelimiter("-->"),
                VBXmlDelimiter("<"),
                VBXmlName("Name"),
                VBXmlDelimiter(">"),
                VBXmlText("Me"),
                VBXmlDelimiter("</"),
                VBXmlName("Name"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("NameUsingExpression"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("n1"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("</"),
                VBXmlName("NameUsingExpression"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("Street"),
                VBXmlDelimiter(">"),
                VBXmlText("10802 177th CT NE"),
                VBXmlDelimiter("</"),
                VBXmlName("Street"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("Misc"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<![CDATA["),
                VBXmlCDataSection("Let's add some CDATA" & Environment.NewLine),
                VBXmlCDataSection(" for fun. "),
                VBXmlDelimiter("]]>"),
                VBXmlDelimiter("</"),
                VBXmlName("Misc"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("Empty"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("<%="),
                Keyword("Nothing"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("</"),
                VBXmlName("Empty"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("Customer"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiterals2() As Task
            Dim code =
"Dim b = <?xml version=""1.0""?>
         <!-- comment before the root -->
         <?my-PI PI before the root ?>
         <p:Customer id1=""1"" id2=""2"" id3=<%= n2 %> id4="""">
             <!-- This is a simple Xml element with all of the node types -->
             <q:Name>Me</q:Name>
             <s:NameUsingExpression><%= n1 %></s:NameUsingExpression>
             <t:Street>10802 177th CT NE</t:Street>
             <p:Misc><![CDATA[Let's add some CDATA  for fun. ]]>
             </p:Misc>
             <Empty><%= Nothing %></Empty>
             <entity>hello&#x7b;world</entity>
         </p:Customer>"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("b"),
                Operators.Equals,
                VBXmlDelimiter("<?"),
                VBXmlName("xml"),
                VBXmlAttributeName("version"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1.0"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("?>"),
                VBXmlDelimiter("<!--"),
                VBXmlComment(" comment before the root "),
                VBXmlDelimiter("-->"),
                VBXmlDelimiter("<?"),
                VBXmlName("my-PI"),
                VBXmlProcessingInstruction("PI before the root "),
                VBXmlDelimiter("?>"),
                VBXmlDelimiter("<"),
                VBXmlName("p"),
                VBXmlName(":"),
                VBXmlName("Customer"),
                VBXmlAttributeName("id1"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1"),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeName("id2"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("2"),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeName("id3"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("n2"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlAttributeName("id4"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<!--"),
                VBXmlComment(" This is a simple Xml element with all of the node types "),
                VBXmlDelimiter("-->"),
                VBXmlDelimiter("<"),
                VBXmlName("q"),
                VBXmlName(":"),
                VBXmlName("Name"),
                VBXmlDelimiter(">"),
                VBXmlText("Me"),
                VBXmlDelimiter("</"),
                VBXmlName("q"),
                VBXmlName(":"),
                VBXmlName("Name"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("s"),
                VBXmlName(":"),
                VBXmlName("NameUsingExpression"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("n1"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("</"),
                VBXmlName("s"),
                VBXmlName(":"),
                VBXmlName("NameUsingExpression"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("t"),
                VBXmlName(":"),
                VBXmlName("Street"),
                VBXmlDelimiter(">"),
                VBXmlText("10802 177th CT NE"),
                VBXmlDelimiter("</"),
                VBXmlName("t"),
                VBXmlName(":"),
                VBXmlName("Street"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("p"),
                VBXmlName(":"),
                VBXmlName("Misc"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<![CDATA["),
                VBXmlCDataSection("Let's add some CDATA  for fun. "),
                VBXmlDelimiter("]]>"),
                VBXmlDelimiter("</"),
                VBXmlName("p"),
                VBXmlName(":"),
                VBXmlName("Misc"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("Empty"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("<%="),
                Keyword("Nothing"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("</"),
                VBXmlName("Empty"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("entity"),
                VBXmlDelimiter(">"),
                VBXmlText("hello"),
                VBXmlEntityReference("&#x7b;"),
                VBXmlText("world"),
                VBXmlDelimiter("</"),
                VBXmlName("entity"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("p"),
                VBXmlName(":"),
                VBXmlName("Customer"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiterals3() As Task
            Dim code =
"Dim c = <p:x xmlns:p=""abc
123""/>"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("c"),
                Operators.Equals,
                VBXmlDelimiter("<"),
                VBXmlName("p"),
                VBXmlName(":"),
                VBXmlName("x"),
                VBXmlAttributeName("xmlns"),
                VBXmlAttributeName(":"),
                VBXmlAttributeName("p"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("abc" & vbCrLf),
                VBXmlAttributeValue("123"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiterals4() As Task
            Dim code =
"Dim d = _
        <?xml version=""1.0""?>
        <a/>"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("d"),
                Operators.Equals,
                LineContinuation,
                VBXmlDelimiter("<?"),
                VBXmlName("xml"),
                VBXmlAttributeName("version"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1.0"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("?>"),
                VBXmlDelimiter("<"),
                VBXmlName("a"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiterals4CommentsAfterLineContinuation() As Task
            Dim code =
"Dim d = _ ' Test
        <?xml version=""1.0""?>
        <a/>"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("d"),
                Operators.Equals,
                LineContinuation,
                Comment("' Test"),
                VBXmlDelimiter("<?"),
                VBXmlName("xml"),
                VBXmlAttributeName("version"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1.0"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("?>"),
                VBXmlDelimiter("<"),
                VBXmlName("a"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiterals5() As Task
            Dim code =
"Dim i = 100
        Process( _
                <Customer ID=<%= i + 1000 %> a="""">
                </Customer>)"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("i"),
                Operators.Equals,
                Number("100"),
                Identifier("Process"),
                Punctuation.OpenParen,
                LineContinuation,
                VBXmlDelimiter("<"),
                VBXmlName("Customer"),
                VBXmlAttributeName("ID"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("i"),
                Operators.Plus,
                Number("1000"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlAttributeName("a"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("Customer"),
                VBXmlDelimiter(">"),
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiterals5CommentsAfterLineContinuation() As Task
            Dim code =
"Dim i = 100
        Process( _ '    Test
                <Customer ID=<%= i + 1000 %> a="""">
                </Customer>)"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("i"),
                Operators.Equals,
                Number("100"),
                Identifier("Process"),
                Punctuation.OpenParen,
                LineContinuation,
                Comment("'    Test"),
                VBXmlDelimiter("<"),
                VBXmlName("Customer"),
                VBXmlAttributeName("ID"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("i"),
                Operators.Plus,
                Number("1000"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlAttributeName("a"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("Customer"),
                VBXmlDelimiter(">"),
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiterals6() As Task
            Dim code =
"Dim xmlwithkeywords = <MODULE>
                           <CLASS>
                               <FUNCTION>
                                   <DIM i=""1""/>
                                   <FOR j=""1"" to=""i"">
                                       <NEXT/>
                                   </FOR>
                                   <END/>
                               </FUNCTION>
                           </CLASS>
                       </MODULE>"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("xmlwithkeywords"),
                Operators.Equals,
                VBXmlDelimiter("<"),
                VBXmlName("MODULE"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("CLASS"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("FUNCTION"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("DIM"),
                VBXmlAttributeName("i"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("/>"),
                VBXmlDelimiter("<"),
                VBXmlName("FOR"),
                VBXmlAttributeName("j"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1"),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeName("to"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("i"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("NEXT"),
                VBXmlDelimiter("/>"),
                VBXmlDelimiter("</"),
                VBXmlName("FOR"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<"),
                VBXmlName("END"),
                VBXmlDelimiter("/>"),
                VBXmlDelimiter("</"),
                VBXmlName("FUNCTION"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("CLASS"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("MODULE"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiterals7() As Task
            Dim code =
"Dim spacetest = <a b=""1"" c=""2"">
                 </a>"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("spacetest"),
                Operators.Equals,
                VBXmlDelimiter("<"),
                VBXmlName("a"),
                VBXmlAttributeName("b"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1"),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeName("c"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("2"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("a"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptionKeywordsInClassContext() As Task
            Dim code =
"Class OptionNoContext
    Dim Infer
    Dim Explicit
    Dim Strict
    Dim Off
    Dim Compare
    Dim Text
    Dim Binary
End Class"

            Await TestAsync(code,
                Keyword("Class"),
                [Class]("OptionNoContext"),
                Keyword("Dim"),
                Field("Infer"),
                Keyword("Dim"),
                Field("Explicit"),
                Keyword("Dim"),
                Field("Strict"),
                Keyword("Dim"),
                Field("Off"),
                Keyword("Dim"),
                Field("Compare"),
                Keyword("Dim"),
                Field("Text"),
                Keyword("Dim"),
                Field("Binary"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptionInferAndExplicit() As Task
            Dim text =
"Option Infer On
Option Explicit Off"

            Await TestAsync(text,
                Keyword("Option"),
                Keyword("Infer"),
                Keyword("On"),
                Keyword("Option"),
                Keyword("Explicit"),
                Keyword("Off"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptionCompareTextBinary() As Task
            Dim code =
"Option Compare Text ' comment
Option Compare Binary "

            Await TestAsync(code,
                Keyword("Option"),
                Keyword("Compare"),
                Keyword("Text"),
                Comment("' comment"),
                Keyword("Option"),
                Keyword("Compare"),
                Keyword("Binary"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptionInfer1() As Task
            Await TestAsync("Option Infer",
                Keyword("Option"),
                Keyword("Infer"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptionExplicit1() As Task
            Await TestAsync("Option Explicit",
                Keyword("Option"),
                Keyword("Explicit"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptionStrict1() As Task
            Await TestAsync("Option Strict",
                Keyword("Option"),
                Keyword("Strict"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestLinqContextualKeywords() As Task
            Dim code =
"Dim from = 0
Dim aggregate = 0
Dim ascending = 0
Dim descending = 0
Dim distinct = 0
Dim by = 0
Shadows equals = 0
Dim group = 0
Dim into = 0
Dim join = 0
Dim skip = 0
Dim take = 0
Dim where = 0
Dim order = 0"

            Await TestInClassAsync(code,
                Keyword("Dim"),
                Field("from"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("aggregate"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("ascending"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("descending"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("distinct"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("by"),
                Operators.Equals,
                Number("0"),
                Keyword("Shadows"),
                Field("equals"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("group"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("into"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("join"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("skip"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("take"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("where"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Field("order"),
                Operators.Equals,
                Number("0"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromLinqExpression1() As Task
            Await TestInExpressionAsync("From it in goo",
                Keyword("From"),
                Identifier("it"),
                Keyword("in"),
                Identifier("goo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromLinqExpression2() As Task
            Await TestInExpressionAsync("From it in goofooo.Goo",
                Keyword("From"),
                Identifier("it"),
                Keyword("in"),
                Identifier("goofooo"),
                Operators.Dot,
                Identifier("Goo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromLinqExpression3() As Task
            Await TestInExpressionAsync("From it ",
                Keyword("From"),
                Identifier("it"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromNotInContext1() As Task
            Dim code =
"Class From
End Class"

            Await TestAsync(code,
                Keyword("Class"),
                [Class]("From"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromNotInContext2() As Task
            Await TestInMethodAsync("Dim from = 42",
                Keyword("Dim"),
                Local("from"),
                Operators.Equals,
                Number("42"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestWhereLinqExpression1() As Task
            Await TestInExpressionAsync("From it in goo Where it <> 4",
                Keyword("From"),
                Identifier("it"),
                Keyword("in"),
                Identifier("goo"),
                Keyword("Where"),
                Identifier("it"),
                Operators.LessThanGreaterThan,
                Number("4"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestLinqQuery1() As Task
            Dim code =
"Dim src = New List(Of Boolean)
Dim var3 = 1
Dim q = From var1 In src Where var1 And True _
        Order By var1 Ascending Order By var1 Descending _
        Select var1 Distinct _
        Join var2 In src On var1 Equals var2 _
        Skip var3 Skip While var3 Take var3 Take While var3 _
        Aggregate var4 In src _
        Group var4 By var4 Into var5 = Count() _
        Group Join var6 In src On var6 Equals var5 Into var7 Into var8 = Count()"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("src"),
                Operators.Equals,
                Keyword("New"),
                Identifier("List"),
                Punctuation.OpenParen,
                Keyword("Of"),
                Keyword("Boolean"),
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("var3"),
                Operators.Equals,
                Number("1"),
                Keyword("Dim"),
                Local("q"),
                Operators.Equals,
                Keyword("From"),
                Identifier("var1"),
                Keyword("In"),
                Identifier("src"),
                Keyword("Where"),
                Identifier("var1"),
                Keyword("And"),
                Keyword("True"),
                LineContinuation,
                Keyword("Order"),
                Keyword("By"),
                Identifier("var1"),
                Keyword("Ascending"),
                Keyword("Order"),
                Keyword("By"),
                Identifier("var1"),
                Keyword("Descending"),
                LineContinuation,
                Keyword("Select"),
                Identifier("var1"),
                Keyword("Distinct"),
                LineContinuation,
                Keyword("Join"),
                Identifier("var2"),
                Keyword("In"),
                Identifier("src"),
                Keyword("On"),
                Identifier("var1"),
                Keyword("Equals"),
                Identifier("var2"),
                LineContinuation,
                Keyword("Skip"),
                Identifier("var3"),
                Keyword("Skip"),
                Keyword("While"),
                Identifier("var3"),
                Keyword("Take"),
                Identifier("var3"),
                Keyword("Take"),
                Keyword("While"),
                Identifier("var3"),
                LineContinuation,
                Keyword("Aggregate"),
                Identifier("var4"),
                Keyword("In"),
                Identifier("src"),
                LineContinuation,
                Keyword("Group"),
                Identifier("var4"),
                Keyword("By"),
                Identifier("var4"),
                Keyword("Into"),
                Identifier("var5"),
                Operators.Equals,
                Identifier("Count"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                LineContinuation,
                Keyword("Group"),
                Keyword("Join"),
                Identifier("var6"),
                Keyword("In"),
                Identifier("src"),
                Keyword("On"),
                Identifier("var6"),
                Keyword("Equals"),
                Identifier("var5"),
                Keyword("Into"),
                Identifier("var7"),
                Keyword("Into"),
                Identifier("var8"),
                Operators.Equals,
                Identifier("Count"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestLinqQuery1CommentsAfterLineContinuation() As Task
            Dim code =
"Dim src = New List(Of Boolean)
Dim var3 = 1
Dim q = From var1 In src Where var1 And True _ ' Test 1 space
        Order By var1 Ascending Order By var1 Descending _  ' Test 2 space
        Select var1 Distinct _   ' Test 3 space
        Join var2 In src On var1 Equals var2 _   ' Test 4 space
        Skip var3 Skip While var3 Take var3 Take While var3 _ ' Test 1 space
        Aggregate var4 In src _ ' Test 1 space
        Group var4 By var4 Into var5 = Count() _ ' Test 1 space
        Group Join var6 In src On var6 Equals var5 Into var7 Into var8 = Count()"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("src"),
                Operators.Equals,
                Keyword("New"),
                Identifier("List"),
                Punctuation.OpenParen,
                Keyword("Of"),
                Keyword("Boolean"),
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("var3"),
                Operators.Equals,
                Number("1"),
                Keyword("Dim"),
                Local("q"),
                Operators.Equals,
                Keyword("From"),
                Identifier("var1"),
                Keyword("In"),
                Identifier("src"),
                Keyword("Where"),
                Identifier("var1"),
                Keyword("And"),
                Keyword("True"),
                LineContinuation,
                Comment("' Test 1 space"),
                Keyword("Order"),
                Keyword("By"),
                Identifier("var1"),
                Keyword("Ascending"),
                Keyword("Order"),
                Keyword("By"),
                Identifier("var1"),
                Keyword("Descending"),
                LineContinuation,
                Comment("' Test 2 space"),
                Keyword("Select"),
                Identifier("var1"),
                Keyword("Distinct"),
                LineContinuation,
                Comment("' Test 3 space"),
                Keyword("Join"),
                Identifier("var2"),
                Keyword("In"),
                Identifier("src"),
                Keyword("On"),
                Identifier("var1"),
                Keyword("Equals"),
                Identifier("var2"),
                LineContinuation,
                Comment("' Test 4 space"),
                Keyword("Skip"),
                Identifier("var3"),
                Keyword("Skip"),
                Keyword("While"),
                Identifier("var3"),
                Keyword("Take"),
                Identifier("var3"),
                Keyword("Take"),
                Keyword("While"),
                Identifier("var3"),
                LineContinuation,
                Comment("' Test 1 space"),
                Keyword("Aggregate"),
                Identifier("var4"),
                Keyword("In"),
                Identifier("src"),
                LineContinuation,
                Comment("' Test 1 space"),
                Keyword("Group"),
                Identifier("var4"),
                Keyword("By"),
                Identifier("var4"),
                Keyword("Into"),
                Identifier("var5"),
                Operators.Equals,
                Identifier("Count"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                LineContinuation,
                Comment("' Test 1 space"),
                Keyword("Group"),
                Keyword("Join"),
                Identifier("var6"),
                Keyword("In"),
                Identifier("src"),
                Keyword("On"),
                Identifier("var6"),
                Keyword("Equals"),
                Identifier("var5"),
                Keyword("Into"),
                Identifier("var7"),
                Keyword("Into"),
                Identifier("var8"),
                Operators.Equals,
                Identifier("Count"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <WorkItem(542387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542387")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromInQuery() As Task
            Dim code =
"Dim From = New List(Of Integer)
Dim result = From s In From Select s"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("From"),
                Operators.Equals,
                Keyword("New"),
                Identifier("List"),
                Punctuation.OpenParen,
                Keyword("Of"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("result"),
                Operators.Equals,
                Keyword("From"),
                Identifier("s"),
                Keyword("In"),
                Identifier("From"),
                Keyword("Select"),
                Identifier("s"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestKeyKeyword1() As Task
            Dim code =
"Dim Value = ""Test""
Dim Key As String = Key.Length & (Key.Length)
Dim Array As String() = { Key, Key.Length }
Dim o = New With {Key Key.Length, Key .Id = 1, Key Key, Key Value, Key.Empty}
o = New With {Key _
                Key.Length, _
                Key _
                .Id = 1, _
                Key _
                Key, _
                Key _
                Value, _
                Key Key. _
                Empty _
                }"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("Value"),
                Operators.Equals,
                [String]("""Test"""),
                Keyword("Dim"),
                Local("Key"),
                Keyword("As"),
                Keyword("String"),
                Operators.Equals,
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Operators.Ampersand,
                Punctuation.OpenParen,
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("Array"),
                Keyword("As"),
                Keyword("String"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Operators.Equals,
                Punctuation.OpenCurly,
                Identifier("Key"),
                Punctuation.Comma,
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.CloseCurly,
                Keyword("Dim"),
                Local("o"),
                Operators.Equals,
                Keyword("New"),
                Keyword("With"),
                Punctuation.OpenCurly,
                Keyword("Key"),
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.Comma,
                Keyword("Key"),
                Operators.Dot,
                Identifier("Id"),
                Operators.Equals,
                Number("1"),
                Punctuation.Comma,
                Keyword("Key"),
                Identifier("Key"),
                Punctuation.Comma,
                Keyword("Key"),
                Identifier("Value"),
                Punctuation.Comma,
                Keyword("Key"),
                Operators.Dot,
                Identifier("Empty"),
                Punctuation.CloseCurly,
                Identifier("o"),
                Operators.Equals,
                Keyword("New"),
                Keyword("With"),
                Punctuation.OpenCurly,
                Keyword("Key"),
                LineContinuation,
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.Comma,
                LineContinuation,
                Keyword("Key"),
                LineContinuation,
                Operators.Dot,
                Identifier("Id"),
                Operators.Equals,
                Number("1"),
                Punctuation.Comma,
                LineContinuation,
                Keyword("Key"),
                LineContinuation,
                Identifier("Key"),
                Punctuation.Comma,
                LineContinuation,
                Keyword("Key"),
                LineContinuation,
                Identifier("Value"),
                Punctuation.Comma,
                LineContinuation,
                Keyword("Key"),
                Identifier("Key"),
                Operators.Dot,
                LineContinuation,
                Identifier("Empty"),
                LineContinuation,
                Punctuation.CloseCurly)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestKeyKeyword1CommentsAfterLineContinuation() As Task
            Dim code =
"Dim Value = ""Test""
Dim Key As String = Key.Length & (Key.Length)
Dim Array As String() = { Key, Key.Length }
Dim o = New With {Key Key.Length, Key .Id = 1, Key Key, Key Value, Key.Empty}
o = New With {Key _ ' Test
                Key.Length, _ ' Test
                Key _ ' Test
                .Id = 1, _ ' Test
                Key _ ' Test
                Key, _ ' Test
                Key _ ' Test
                Value, _ ' Test
                Key Key. _ ' Test
                Empty _ ' Test
                }"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("Value"),
                Operators.Equals,
                [String]("""Test"""),
                Keyword("Dim"),
                Local("Key"),
                Keyword("As"),
                Keyword("String"),
                Operators.Equals,
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Operators.Ampersand,
                Punctuation.OpenParen,
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("Array"),
                Keyword("As"),
                Keyword("String"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Operators.Equals,
                Punctuation.OpenCurly,
                Identifier("Key"),
                Punctuation.Comma,
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.CloseCurly,
                Keyword("Dim"),
                Local("o"),
                Operators.Equals,
                Keyword("New"),
                Keyword("With"),
                Punctuation.OpenCurly,
                Keyword("Key"),
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.Comma,
                Keyword("Key"),
                Operators.Dot,
                Identifier("Id"),
                Operators.Equals,
                Number("1"),
                Punctuation.Comma,
                Keyword("Key"),
                Identifier("Key"),
                Punctuation.Comma,
                Keyword("Key"),
                Identifier("Value"),
                Punctuation.Comma,
                Keyword("Key"),
                Operators.Dot,
                Identifier("Empty"),
                Punctuation.CloseCurly,
                Identifier("o"),
                Operators.Equals,
                Keyword("New"),
                Keyword("With"),
                Punctuation.OpenCurly,
                Keyword("Key"),
                LineContinuation,
                Comment("' Test"),
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.Comma,
                LineContinuation,
                Comment("' Test"),
                Keyword("Key"),
                LineContinuation,
                Comment("' Test"),
                Operators.Dot,
                Identifier("Id"),
                Operators.Equals,
                Number("1"),
                Punctuation.Comma,
                LineContinuation,
                Comment("' Test"),
                Keyword("Key"),
                LineContinuation,
                Comment("' Test"),
                Identifier("Key"),
                Punctuation.Comma,
                LineContinuation,
                Comment("' Test"),
                Keyword("Key"),
                LineContinuation,
                Comment("' Test"),
                Identifier("Value"),
                Punctuation.Comma,
                LineContinuation,
                Comment("' Test"),
                Keyword("Key"),
                Identifier("Key"),
                Operators.Dot,
                LineContinuation,
                Comment("' Test"),
                Identifier("Empty"),
                LineContinuation,
                Comment("' Test"),
                Punctuation.CloseCurly)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestKeyKeyword2() As Task
            Dim code =
"Dim k = 10
Dim x = New With {Key If(k > 3, 2, -2).GetTypeCode}
Dim y = New With {Key DirectCast(New Object(), Integer).GetTypeCode}
Dim z1 = New With {Key If(True, 1,2).GetTypeCode()}
Dim z2 = New With {Key CType(Nothing, Integer).GetTypeCode()}
Dim Key As Integer
If Key Or True Or Key = 1 Then Console.WriteLine()
Dim z3() = { Key Or True, Key = 1 }
Dim z4 = New List(Of Integer) From {1, 2, 3}
Dim z5 As New List(Of Integer) From {1, 2, 3}
Dim z6 = New List(Of Integer) With {.Capacity = 2}"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("k"),
                Operators.Equals,
                Number("10"),
                Keyword("Dim"),
                Local("x"),
                Operators.Equals,
                Keyword("New"),
                Keyword("With"),
                Punctuation.OpenCurly,
                Keyword("Key"),
                ControlKeyword("If"),
                Punctuation.OpenParen,
                Identifier("k"),
                Operators.GreaterThan,
                Number("3"),
                Punctuation.Comma,
                Number("2"),
                Punctuation.Comma,
                Operators.Minus,
                Number("2"),
                Punctuation.CloseParen,
                Operators.Dot,
                Identifier("GetTypeCode"),
                Punctuation.CloseCurly,
                Keyword("Dim"),
                Local("y"),
                Operators.Equals,
                Keyword("New"),
                Keyword("With"),
                Punctuation.OpenCurly,
                Keyword("Key"),
                Keyword("DirectCast"),
                Punctuation.OpenParen,
                Keyword("New"),
                Keyword("Object"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.Comma,
                Keyword("Integer"),
                Punctuation.CloseParen,
                Operators.Dot,
                Identifier("GetTypeCode"),
                Punctuation.CloseCurly,
                Keyword("Dim"),
                Local("z1"),
                Operators.Equals,
                Keyword("New"),
                Keyword("With"),
                Punctuation.OpenCurly,
                Keyword("Key"),
                ControlKeyword("If"),
                Punctuation.OpenParen,
                Keyword("True"),
                Punctuation.Comma,
                Number("1"),
                Punctuation.Comma,
                Number("2"),
                Punctuation.CloseParen,
                Operators.Dot,
                Identifier("GetTypeCode"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.CloseCurly,
                Keyword("Dim"),
                Local("z2"),
                Operators.Equals,
                Keyword("New"),
                Keyword("With"),
                Punctuation.OpenCurly,
                Keyword("Key"),
                Keyword("CType"),
                Punctuation.OpenParen,
                Keyword("Nothing"),
                Punctuation.Comma,
                Keyword("Integer"),
                Punctuation.CloseParen,
                Operators.Dot,
                Identifier("GetTypeCode"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.CloseCurly,
                Keyword("Dim"),
                Local("Key"),
                Keyword("As"),
                Keyword("Integer"),
                ControlKeyword("If"),
                Identifier("Key"),
                Keyword("Or"),
                Keyword("True"),
                Keyword("Or"),
                Identifier("Key"),
                Operators.Equals,
                Number("1"),
                ControlKeyword("Then"),
                Identifier("Console"),
                Operators.Dot,
                Identifier("WriteLine"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("z3"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Operators.Equals,
                Punctuation.OpenCurly,
                Identifier("Key"),
                Keyword("Or"),
                Keyword("True"),
                Punctuation.Comma,
                Identifier("Key"),
                Operators.Equals,
                Number("1"),
                Punctuation.CloseCurly,
                Keyword("Dim"),
                Local("z4"),
                Operators.Equals,
                Keyword("New"),
                Identifier("List"),
                Punctuation.OpenParen,
                Keyword("Of"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Keyword("From"),
                Punctuation.OpenCurly,
                Number("1"),
                Punctuation.Comma,
                Number("2"),
                Punctuation.Comma,
                Number("3"),
                Punctuation.CloseCurly,
                Keyword("Dim"),
                Local("z5"),
                Keyword("As"),
                Keyword("New"),
                Identifier("List"),
                Punctuation.OpenParen,
                Keyword("Of"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Keyword("From"),
                Punctuation.OpenCurly,
                Number("1"),
                Punctuation.Comma,
                Number("2"),
                Punctuation.Comma,
                Number("3"),
                Punctuation.CloseCurly,
                Keyword("Dim"),
                Local("z6"),
                Operators.Equals,
                Keyword("New"),
                Identifier("List"),
                Punctuation.OpenParen,
                Keyword("Of"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Keyword("With"),
                Punctuation.OpenCurly,
                Operators.Dot,
                Identifier("Capacity"),
                Operators.Equals,
                Number("2"),
                Punctuation.CloseCurly)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestNamespaceDeclaration() As Task
            Dim code =
"Namespace N1.N2
End Namespace"

            Await TestAsync(code,
                Keyword("Namespace"),
                [Namespace]("N1"),
                Operators.Dot,
                [Namespace]("N2"),
                Keyword("End"),
                Keyword("Namespace"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestClassDeclaration1() As Task
            Dim code = "Class C1"

            Await TestAsync(code,
                Keyword("Class"),
                [Class]("C1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestClassDeclaration2() As Task
            Dim code =
"Class C1
End Class"

            Await TestAsync(code,
                Keyword("Class"),
                [Class]("C1"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestClassDeclaration3() As Task
            Dim code = "Class C1 : End Class"

            Await TestAsync(code,
                Keyword("Class"),
                [Class]("C1"),
                Punctuation.Colon,
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStructDeclaration1() As Task
            Dim code = "Structure S1"

            Await TestAsync(code,
                Keyword("Structure"),
                Struct("S1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStructDeclaration2() As Task
            Dim code = "Structure S1 : End Structure"

            Await TestAsync(code,
                Keyword("Structure"),
                Struct("S1"),
                Punctuation.Colon,
                Keyword("End"),
                Keyword("Structure"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStructDeclaration3() As Task
            Dim code =
"Structure S1
End Structure"

            Await TestAsync(code,
                Keyword("Structure"),
                Struct("S1"),
                Keyword("End"),
                Keyword("Structure"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestInterfaceDeclaration1() As Task
            Dim code = "Interface I1"

            Await TestAsync(code,
                Keyword("Interface"),
                [Interface]("I1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestInterfaceDeclaration2() As Task
            Dim code = "Interface I1 : End Interface"

            Await TestAsync(code,
                Keyword("Interface"),
                [Interface]("I1"),
                Punctuation.Colon,
                Keyword("End"),
                Keyword("Interface"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestInterfaceDeclaration3() As Task
            Dim code =
"Interface I1
End Interface"

            Await TestAsync(code,
                Keyword("Interface"),
                [Interface]("I1"),
                Keyword("End"),
                Keyword("Interface"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestEnumDeclaration1() As Task
            Dim code = "Enum E1"

            Await TestAsync(code,
                Keyword("Enum"),
                [Enum]("E1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestEnumDeclaration2() As Task
            Dim code = "Enum E1 : End Enum"

            Await TestAsync(code,
                Keyword("Enum"),
                [Enum]("E1"),
                Punctuation.Colon,
                Keyword("End"),
                Keyword("Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestEnumDeclaration3() As Task
            Dim code =
"Enum E1
End Enum"

            Await TestAsync(code,
                Keyword("Enum"),
                [Enum]("E1"),
                Keyword("End"),
                Keyword("Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDelegateSubDeclaration1() As Task
            Dim code = "Public Delegate Sub Goo()"

            Await TestAsync(code,
                Keyword("Public"),
                Keyword("Delegate"),
                Keyword("Sub"),
                [Delegate]("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDelegateFunctionDeclaration1() As Task
            Dim code = "Public Delegate Function Goo() As Integer"

            Await TestAsync(code,
                Keyword("Public"),
                Keyword("Delegate"),
                Keyword("Function"),
                [Delegate]("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("As"),
                Keyword("Integer"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestTernaryConditionalExpression() As Task
            Dim code = "Dim i = If(True, 1, 2)"

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("i"),
                Operators.Equals,
                ControlKeyword("If"),
                Punctuation.OpenParen,
                Keyword("True"),
                Punctuation.Comma,
                Number("1"),
                Punctuation.Comma,
                Number("2"),
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestForStatement() As Task
            Dim code =
"For i = 0 To 10
Exit For"
            Await TestInMethodAsync(code,
                ControlKeyword("For"),
                Identifier("i"),
                Operators.Equals,
                Number("0"),
                ControlKeyword("To"),
                Number("10"),
                ControlKeyword("Exit"),
                ControlKeyword("For"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFloatLiteral() As Task
            Await TestInExpressionAsync("1.0",
                Number("1.0"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIntLiteral() As Task
            Await TestInExpressionAsync("1",
                Number("1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDecimalLiteral() As Task
            Await TestInExpressionAsync("123D",
                Number("123D"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStringLiterals1() As Task
            Await TestInExpressionAsync("""goo""",
                [String]("""goo"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestCharacterLiteral() As Task
            Await TestInExpressionAsync("""f""c",
                [String]("""f""c"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestRegression_DoUntil1() As Task
            Dim code = "Do Until True"
            Await TestInMethodAsync(code,
                ControlKeyword("Do"),
                ControlKeyword("Until"),
                Keyword("True"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestComment1() As Task
            Dim code = "'goo"

            Await TestAsync(code,
               Comment("'goo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestComment2() As Task
            Dim code =
"Class C1
'hello"

            Await TestAsync(code,
                Keyword("Class"),
                [Class]("C1"),
                Comment("'hello"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_SingleLine() As Task
            Dim code =
"'''<summary>something</summary>
Class Bar
End Class"

            Await TestAsync(code,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                XmlDoc.Text("something"),
                XmlDoc.Delimiter("</"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                Keyword("Class"),
                [Class]("Bar"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_ExteriorTrivia() As Task
            Dim code =
"''' <summary>
''' something
''' </summary>
Class Bar
End Class"

            Await TestAsync(code,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" something"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("</"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                Keyword("Class"),
                [Class]("Bar"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_ExteriorTriviaInsideEndTag() As Task
            Dim code =
"''' <summary></
''' summary>
Class Bar
End Class"

            Await TestAsync(code,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                XmlDoc.Delimiter("</"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Name(" "),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                Keyword("Class"),
                [Class]("Bar"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_AttributesWithExteriorTrivia() As Task
            Dim code =
"''' <summary att1=""value1""
''' att2=""value2"">
''' something
''' </summary>
Class Bar
End Class"

            Await TestAsync(code,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Name(" "),
                XmlDoc.AttributeName("att1"),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes(""""),
                XmlDoc.AttributeValue("value1"),
                XmlDoc.AttributeQuotes(""""),
                XmlDoc.Delimiter("'''"),
                XmlDoc.AttributeName(" "),
                XmlDoc.AttributeName("att2"),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes(""""),
                XmlDoc.AttributeValue("value2"),
                XmlDoc.AttributeQuotes(""""),
                XmlDoc.Delimiter(">"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" something"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("</"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                Keyword("Class"),
                [Class]("Bar"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_EmptyElementAttributesWithExteriorTrivia() As Task
            Dim code =
"''' <summary att1=""value1""
''' att2=""value2"" />
Class Bar
End Class"

            Await TestAsync(code,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Name(" "),
                XmlDoc.AttributeName("att1"),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes(""""),
                XmlDoc.AttributeValue("value1"),
                XmlDoc.AttributeQuotes(""""),
                XmlDoc.Delimiter("'''"),
                XmlDoc.AttributeName(" "),
                XmlDoc.AttributeName("att2"),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes(""""),
                XmlDoc.AttributeValue("value2"),
                XmlDoc.AttributeQuotes(""""),
                XmlDoc.AttributeQuotes(" "),
                XmlDoc.Delimiter("/>"),
                Keyword("Class"),
                [Class]("Bar"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_XmlCommentWithExteriorTrivia() As Task
            Dim code =
"'''<summary>
'''<!--first
'''second-->
'''</summary>
Class Bar
End Class"

            Await TestAsync(code,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Delimiter("<!--"),
                XmlDoc.Comment("first" & vbCrLf),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Comment("second"),
                XmlDoc.Delimiter("-->"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Delimiter("</"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                Keyword("Class"),
                [Class]("Bar"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_CDataWithExteriorTrivia() As Task
            Dim code =
"'''<summary>
'''<![CDATA[first
'''second]]>
'''</summary>
Class Bar
End Class"

            Await TestAsync(code,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Delimiter("<![CDATA["),
                XmlDoc.CDataSection("first" & vbCrLf),
                XmlDoc.Delimiter("'''"),
                XmlDoc.CDataSection("second"),
                XmlDoc.Delimiter("]]>"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Delimiter("</"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                Keyword("Class"),
                [Class]("Bar"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_PreprocessingInstruction1() As Task
            Await TestAsync("''' <?",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_PreprocessingInstruction2() As Task
            Await TestAsync("''' <??>",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("?>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_PreprocessingInstruction3() As Task
            Await TestAsync("''' <?xml",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("xml"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_PreprocessingInstruction4() As Task
            Await TestAsync("''' <?xml version=""1.0""?>",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("xml"),
                XmlDoc.ProcessingInstruction(" "),
                XmlDoc.ProcessingInstruction("version=""1.0"""),
                XmlDoc.ProcessingInstruction("?>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_PreprocessingInstruction5() As Task
            Await TestAsync("''' <?goo?>",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("goo"),
                XmlDoc.ProcessingInstruction("?>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_PreprocessingInstruction6() As Task
            Await TestAsync("''' <?goo bar?>",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("goo"),
                XmlDoc.ProcessingInstruction(" "),
                XmlDoc.ProcessingInstruction("bar"),
                XmlDoc.ProcessingInstruction("?>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIsTrue() As Task
            Await TestInClassAsync("    Public Shared Operator IsTrue(c As C) As Boolean",
                Keyword("Public"),
                Keyword("Shared"),
                Keyword("Operator"),
                Keyword("IsTrue"),
                Punctuation.OpenParen,
                Parameter("c"),
                Keyword("As"),
                Identifier("C"),
                Punctuation.CloseParen,
                Keyword("As"),
                Keyword("Boolean"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIsFalse() As Task
            Await TestInClassAsync("    Public Shared Operator IsFalse(c As C) As Boolean",
                Keyword("Public"),
                Keyword("Shared"),
                Keyword("Operator"),
                Keyword("IsFalse"),
                Punctuation.OpenParen,
                Parameter("c"),
                Keyword("As"),
                Identifier("C"),
                Punctuation.CloseParen,
                Keyword("As"),
                Keyword("Boolean"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDelegate1() As Task
            Await TestAsync("Delegate Sub Goo()",
                Keyword("Delegate"),
                Keyword("Sub"),
                [Delegate]("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImports1() As Task
            Dim code =
"Imports Goo
Imports Bar"

            Await TestAsync(code,
                Keyword("Imports"),
                Identifier("Goo"),
                Keyword("Imports"),
                Identifier("Bar"))
        End Function

        ''' <summary>
        ''' Clear Syntax Error
        ''' </summary>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImports2() As Task
            Dim code =
"Imports
Imports Bar"

            Await TestAsync(code,
                Keyword("Imports"),
                Keyword("Imports"),
                Identifier("Bar"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImports3() As Task
            Dim code =
"Imports Goo=Baz
Imports Bar=Quux"

            Await TestAsync(code,
                Keyword("Imports"),
                Identifier("Goo"),
                Operators.Equals,
                Identifier("Baz"),
                Keyword("Imports"),
                Identifier("Bar"),
                Operators.Equals,
                Identifier("Quux"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImports4() As Task
            Dim code = "Imports System.Text"

            Await TestAsync(code,
                Keyword("Imports"),
                Identifier("System"),
                Operators.Dot,
                Identifier("Text"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement1() As Task
            Await TestInExpressionAsync("<goo></goo>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        '''<summary>
        ''' Broken XmlElement should classify
        ''' </summary>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement3() As Task
            Await TestInExpressionAsync("<goo>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        '''<summary>
        ''' Broken end only element should still classify
        ''' </summary>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement4() As Task
            Await TestInExpressionAsync("</goo>",
                VBXmlDelimiter("</"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement5() As Task
            Await TestInExpressionAsync("<goo.bar></goo.bar>",
                VBXmlDelimiter("<"),
                VBXmlName("goo.bar"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("goo.bar"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement6() As Task
            Await TestInExpressionAsync("<goo:bar>hello</goo:bar>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"),
                VBXmlName("bar"),
                VBXmlDelimiter(">"),
                VBXmlText("hello"),
                VBXmlDelimiter("</"),
                VBXmlName("goo"),
                VBXmlName(":"),
                VBXmlName("bar"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement7() As Task
            Await TestInExpressionAsync("<goo.bar />",
                VBXmlDelimiter("<"),
                VBXmlName("goo.bar"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbedded1() As Task
            Await TestInExpressionAsync("<goo><%= bar %></goo>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("</"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbedded3() As Task
            Await TestInExpressionAsync("<<%= bar %>/>",
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbedded4() As Task
            Await TestInExpressionAsync("<goo <%= bar %>=""42""/>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("42"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbedded5() As Task
            Await TestInExpressionAsync("<goo a1=<%= bar %>/>",
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("a1"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlComment1() As Task
            Await TestInExpressionAsync("<!---->",
                VBXmlDelimiter("<!--"),
                VBXmlDelimiter("-->"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlComment2() As Task
            Await TestInExpressionAsync("<!--goo-->",
                VBXmlDelimiter("<!--"),
                VBXmlComment("goo"),
                VBXmlDelimiter("-->"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlComment3() As Task
            Await TestInExpressionAsync("<a><!--goo--></a>",
                VBXmlDelimiter("<"),
                VBXmlName("a"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<!--"),
                VBXmlComment("goo"),
                VBXmlDelimiter("-->"),
                VBXmlDelimiter("</"),
                VBXmlName("a"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlPreprocessingInstruction2() As Task
            Await TestInExpressionAsync("<a><?pi value=2?></a>",
                VBXmlDelimiter("<"),
                VBXmlName("a"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<?"),
                VBXmlName("pi"),
                VBXmlProcessingInstruction("value=2"),
                VBXmlDelimiter("?>"),
                VBXmlDelimiter("</"),
                VBXmlName("a"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDescendantsMemberAccess1() As Task
            Await TestInExpressionAsync("x...<goo>",
                Identifier("x"),
                VBXmlDelimiter("."),
                VBXmlDelimiter("."),
                VBXmlDelimiter("."),
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementMemberAccess1() As Task
            Await TestInExpressionAsync("x.<goo>",
                Identifier("x"),
                VBXmlDelimiter("."),
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeMemberAccess1() As Task
            Await TestInExpressionAsync("x.@goo",
                Identifier("x"),
                VBXmlDelimiter("."),
                VBXmlDelimiter("@"),
                VBXmlAttributeName("goo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeMemberAccess2() As Task
            Await TestInExpressionAsync("x.@goo:bar",
                Identifier("x"),
                VBXmlDelimiter("."),
                VBXmlDelimiter("@"),
                VBXmlAttributeName("goo"),
                VBXmlAttributeName(":"),
                VBXmlAttributeName("bar"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorReference() As Task
            Await TestInNamespaceAsync("#R ""Ref""",
                PPKeyword("#"),
                PPKeyword("R"),
                [String]("""Ref"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorConst1() As Task
            Await TestInNamespaceAsync("#Const Goo = 1",
                PPKeyword("#"),
                PPKeyword("Const"),
                Identifier("Goo"),
                Operators.Equals,
                Number("1"))
        End Function

        Public Async Function TestPreprocessorConst2() As Task
            Await TestInNamespaceAsync("#Const DebugCode = True",
                PPKeyword("#"),
                PPKeyword("Const"),
                Identifier("DebugCode"),
                Operators.Equals,
                Keyword("True"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorIfThen1() As Task
            Await TestInNamespaceAsync("#If Goo Then",
                PPKeyword("#"),
                PPKeyword("If"),
                Identifier("Goo"),
                PPKeyword("Then"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorElseIf1() As Task
            Await TestInNamespaceAsync("#ElseIf Goo Then",
                PPKeyword("#"),
                PPKeyword("ElseIf"),
                Identifier("Goo"),
                PPKeyword("Then"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorElse1() As Task
            Await TestInNamespaceAsync("#Else",
                PPKeyword("#"),
                PPKeyword("Else"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorEndIf1() As Task
            Await TestInNamespaceAsync("#End If",
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("If"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorExternalSource1() As Task
            Await TestInNamespaceAsync("#ExternalSource(""c:\wwwroot\inetpub\test.aspx"", 30)",
                PPKeyword("#"),
                PPKeyword("ExternalSource"),
                Punctuation.OpenParen,
                [String]("""c:\wwwroot\inetpub\test.aspx"""),
                Punctuation.Comma,
                Number("30"),
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorExternalChecksum1() As Task
            Dim code =
"#ExternalChecksum(""c:\wwwroot\inetpub\test.aspx"", _
""{12345678-1234-1234-1234-123456789abc}"", _
""1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"")"

            Await TestInNamespaceAsync(code,
                PPKeyword("#"),
                PPKeyword("ExternalChecksum"),
                Punctuation.OpenParen,
                [String]("""c:\wwwroot\inetpub\test.aspx"""),
                Punctuation.Comma,
                LineContinuation,
                [String]("""{12345678-1234-1234-1234-123456789abc}"""),
                Punctuation.Comma,
                LineContinuation,
                [String]("""1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"""),
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorExternalChecksum1CommentsAfterLineContinuation() As Task
            Dim code =
"#ExternalChecksum(""c:\wwwroot\inetpub\test.aspx"", _ ' Test
""{12345678-1234-1234-1234-123456789abc}"", _ ' Test
""1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"")"

            Await TestInNamespaceAsync(code,
                PPKeyword("#"),
                PPKeyword("ExternalChecksum"),
                Punctuation.OpenParen,
                [String]("""c:\wwwroot\inetpub\test.aspx"""),
                Punctuation.Comma,
                LineContinuation,
                Comment("' Test"),
                [String]("""{12345678-1234-1234-1234-123456789abc}"""),
                Punctuation.Comma,
                LineContinuation,
                Comment("' Test"),
                [String]("""1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"""),
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorExternalChecksum2() As Task
            Dim code =
"#ExternalChecksum(""c:\wwwroot\inetpub\test.aspx"", _
""{12345678-1234-1234-1234-123456789abc}"", _
""1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"")
Module Test
    Sub Main()
#ExternalSource(""c:\wwwroot\inetpub\test.aspx"", 30)
        Console.WriteLine(""In test.aspx"")
#End ExternalSource
    End Sub
End Module"

            Await TestInNamespaceAsync(code,
                PPKeyword("#"),
                PPKeyword("ExternalChecksum"),
                Punctuation.OpenParen,
                [String]("""c:\wwwroot\inetpub\test.aspx"""),
                Punctuation.Comma,
                LineContinuation,
                [String]("""{12345678-1234-1234-1234-123456789abc}"""),
                Punctuation.Comma,
                LineContinuation,
                [String]("""1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"""),
                Punctuation.CloseParen,
                Keyword("Module"),
                [Module]("Test"),
                Keyword("Sub"),
                Method("Main"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("ExternalSource"),
                Punctuation.OpenParen,
                [String]("""c:\wwwroot\inetpub\test.aspx"""),
                Punctuation.Comma,
                Number("30"),
                Punctuation.CloseParen,
                Identifier("Console"),
                Operators.Dot,
                Identifier("WriteLine"),
                Punctuation.OpenParen,
                [String]("""In test.aspx"""),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("ExternalSource"),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorExternalChecksum2CommentsAfterLineContinuation() As Task
            Dim code =
"#ExternalChecksum(""c:\wwwroot\inetpub\test.aspx"", _ ' Test 1
""{12345678-1234-1234-1234-123456789abc}"", _ ' Test 2
""1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"")
Module Test
    Sub Main()
#ExternalSource(""c:\wwwroot\inetpub\test.aspx"", 30)
        Console.WriteLine(""In test.aspx"")
#End ExternalSource
    End Sub
End Module"

            Await TestInNamespaceAsync(code,
                PPKeyword("#"),
                PPKeyword("ExternalChecksum"),
                Punctuation.OpenParen,
                [String]("""c:\wwwroot\inetpub\test.aspx"""),
                Punctuation.Comma,
                LineContinuation,
                Comment("' Test 1"),
                [String]("""{12345678-1234-1234-1234-123456789abc}"""),
                Punctuation.Comma,
                LineContinuation,
                Comment("' Test 2"),
                [String]("""1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"""),
                Punctuation.CloseParen,
                Keyword("Module"),
                [Module]("Test"),
                Keyword("Sub"),
                Method("Main"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("ExternalSource"),
                Punctuation.OpenParen,
                [String]("""c:\wwwroot\inetpub\test.aspx"""),
                Punctuation.Comma,
                Number("30"),
                Punctuation.CloseParen,
                Identifier("Console"),
                Operators.Dot,
                Identifier("WriteLine"),
                Punctuation.OpenParen,
                [String]("""In test.aspx"""),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("ExternalSource"),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug2641_1() As Task
            Dim code =
"Class PreprocessorNoContext
Dim Region
Dim ExternalSource
End Class
#Region ""Test""
#End Region
#Region ""Test"" ' comment
#End Region ' comment
#Region ""Test"" REM comment
#End Region REM comment
# _
Region ""Test""
# _
End Region
# _
Region _
""Test""
# _
End _
Region"

            Await TestAsync(code,
                Keyword("Class"),
                [Class]("PreprocessorNoContext"),
                Keyword("Dim"),
                Field("Region"),
                Keyword("Dim"),
                Field("ExternalSource"),
                Keyword("End"),
                Keyword("Class"),
                PPKeyword("#"),
                PPKeyword("Region"),
                [String]("""Test"""),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("Region"),
                PPKeyword("#"),
                PPKeyword("Region"),
                [String]("""Test"""),
                Comment("' comment"),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("Region"),
                Comment("' comment"),
                PPKeyword("#"),
                PPKeyword("Region"),
                [String]("""Test"""),
                Comment("REM comment"),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("Region"),
                Comment("REM comment"),
                PPKeyword("#"),
                LineContinuation,
                PPKeyword("Region"),
                [String]("""Test"""),
                PPKeyword("#"),
                LineContinuation,
                PPKeyword("End"),
                PPKeyword("Region"),
                PPKeyword("#"),
                LineContinuation,
                PPKeyword("Region"),
                LineContinuation,
                [String]("""Test"""),
                PPKeyword("#"),
                LineContinuation,
                PPKeyword("End"),
                LineContinuation,
                PPKeyword("Region"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug2641_1CommentsAfterLineContinuation() As Task
            Dim code =
"Class PreprocessorNoContext
Dim Region
Dim ExternalSource
End Class
#Region ""Test""
#End Region
#Region ""Test"" ' comment
#End Region ' comment
#Region ""Test"" REM comment
#End Region REM comment
# _ ' Test 1
Region ""Test""
# _ ' Test 2
End Region
# _ ' Test 3
Region _ ' Test 4
""Test""
# _ ' Test 5
End _ ' Test 6
Region"

            Await TestAsync(code,
                Keyword("Class"),
                [Class]("PreprocessorNoContext"),
                Keyword("Dim"),
                Field("Region"),
                Keyword("Dim"),
                Field("ExternalSource"),
                Keyword("End"),
                Keyword("Class"),
                PPKeyword("#"),
                PPKeyword("Region"),
                [String]("""Test"""),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("Region"),
                PPKeyword("#"),
                PPKeyword("Region"),
                [String]("""Test"""),
                Comment("' comment"),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("Region"),
                Comment("' comment"),
                PPKeyword("#"),
                PPKeyword("Region"),
                [String]("""Test"""),
                Comment("REM comment"),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("Region"),
                Comment("REM comment"),
                PPKeyword("#"),
                LineContinuation,
                Comment("' Test 1"),
                PPKeyword("Region"),
                [String]("""Test"""),
                PPKeyword("#"),
                LineContinuation,
                 Comment("' Test 2"),
               PPKeyword("End"),
                PPKeyword("Region"),
                PPKeyword("#"),
                LineContinuation,
                Comment("' Test 3"),
                PPKeyword("Region"),
                LineContinuation,
                Comment("' Test 4"),
                [String]("""Test"""),
                PPKeyword("#"),
                LineContinuation,
                Comment("' Test 5"),
                PPKeyword("End"),
                LineContinuation,
                Comment("' Test 6"),
                PPKeyword("Region"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug2641_2() As Task
            Dim code =
"#ExternalSource(""Test.vb"", 123)
#End ExternalSource
#ExternalSource(""Test.vb"", 123) ' comment
#End ExternalSource REM comment
# _
ExternalSource _
( _
""Test.vb"" _
, _
123)
# _
End _
ExternalSource"

            Await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("ExternalSource"),
                Punctuation.OpenParen,
                [String]("""Test.vb"""),
                Punctuation.Comma,
                Number("123"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("ExternalSource"),
                PPKeyword("#"),
                PPKeyword("ExternalSource"),
                Punctuation.OpenParen,
                [String]("""Test.vb"""),
                Punctuation.Comma,
                Number("123"),
                Punctuation.CloseParen,
                Comment("' comment"),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("ExternalSource"),
                Comment("REM comment"),
                PPKeyword("#"),
                LineContinuation,
                PPKeyword("ExternalSource"),
                LineContinuation,
                Punctuation.OpenParen,
                LineContinuation,
                [String]("""Test.vb"""),
                LineContinuation,
                Punctuation.Comma,
                LineContinuation,
                Number("123"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                LineContinuation,
                PPKeyword("End"),
                LineContinuation,
                PPKeyword("ExternalSource"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug2640() As Task
            Dim code =
"# _
Region ""Test""
# _
End Region
# _
Region _
""Test""
# _
End _
Region"

            Await TestAsync(code,
                PPKeyword("#"),
                LineContinuation,
                PPKeyword("Region"),
                [String]("""Test"""),
                PPKeyword("#"),
                LineContinuation,
                PPKeyword("End"),
                PPKeyword("Region"),
                PPKeyword("#"),
                LineContinuation,
                PPKeyword("Region"),
                LineContinuation,
                [String]("""Test"""),
                PPKeyword("#"),
                LineContinuation,
                PPKeyword("End"),
                LineContinuation,
                PPKeyword("Region"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug2638() As Task
            Dim code =
"Module M
    Sub Main()
        Dim dt = #1/1/2000#
    End Sub
End Module"

            Await TestAsync(code,
                Keyword("Module"),
                [Module]("M"),
                Keyword("Sub"),
                Method("Main"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("dt"),
                Operators.Equals,
                Number("#1/1/2000#"),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug2562() As Task
            Dim code =
"Module Program
  Sub Main(args As String())
    #region ""Goo""
    #End region REM dfkjslfkdsjf
  End Sub
End Module"

            Await TestAsync(code,
                Keyword("Module"),
                [Module]("Program"),
                Keyword("Sub"),
                Method("Main"),
                Punctuation.OpenParen,
                Parameter("args"),
                Keyword("As"),
                Keyword("String"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("region"),
                [String]("""Goo"""),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("region"),
                Comment("REM dfkjslfkdsjf"),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug3004() As Task
            Dim code =
"''' <summary>
''' &#65;
''' </summary>
Module M
End Module"

            Await TestAsync(code,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.EntityReference("&#65;"),
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("</"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                Keyword("Module"),
                [Module]("M"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug3006() As Task
            Dim code =
"#If True Then ' comment
#End If"

            Await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("If"),
                Keyword("True"),
                PPKeyword("Then"),
                Comment("' comment"),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("If"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug3008() As Task
            Dim code =
"#If #12/2/2010# = #12/2/2010# Then
#End If"

            Await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("If"),
                Number("#12/2/2010#"),
                Operators.Equals,
                Number("#12/2/2010#"),
                PPKeyword("Then"),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("If"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug927678() As Task
            Dim code =
            "'This is not usually a " & vbCrLf &
            "'collapsible comment block" & vbCrLf &
            "x = 2"

            Await TestInMethodAsync(code,
                         Comment("'This is not usually a "),
                         Comment("'collapsible comment block"),
                         Identifier("x"),
                         Operators.Equals,
                         Number("2"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAttribute() As Task
            Dim code = "<Assembly: Goo()>"

            Await TestAsync(code,
                 Punctuation.OpenAngle,
                 Keyword("Assembly"),
                 Punctuation.Colon,
                 Identifier("Goo"),
                 Punctuation.OpenParen,
                 Punctuation.CloseParen,
                 Punctuation.CloseAngle)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAngleBracketsOnGenericConstraints_Bug932262() As Task
            Dim code =
"Class C(Of T As A(Of T))
End Class"

            Await TestAsync(code,
                Keyword("Class"),
                [Class]("C"),
                Punctuation.OpenParen,
                Keyword("Of"),
                TypeParameter("T"),
                Keyword("As"),
                Identifier("A"),
                Punctuation.OpenParen,
                Keyword("Of"),
                Identifier("T"),
                Punctuation.CloseParen,
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIntegerAsContextualKeyword() As Task
            Dim code =
"Sub CallMeInteger(ByVal [Integer] As Integer)
    CallMeInteger(Integer:=1)
    CallMeInteger(Integer _
                    := _
                    1)
End Sub
Dim [Class] As Integer"

            Await TestInClassAsync(code,
                Keyword("Sub"),
                Method("CallMeInteger"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Parameter("[Integer]"),
                Keyword("As"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Identifier("CallMeInteger"),
                Punctuation.OpenParen,
                Identifier("Integer"),
                Operators.ColonEquals,
                Number("1"),
                Punctuation.CloseParen,
                Identifier("CallMeInteger"),
                Punctuation.OpenParen,
                Identifier("Integer"),
                LineContinuation,
                Operators.ColonEquals,
                LineContinuation,
                Number("1"),
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Sub"),
                Keyword("Dim"),
                Field("[Class]"),
                Keyword("As"),
                Keyword("Integer"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIntegerAsContextualKeywordCommentsAfterLineContinuation() As Task
            Dim code =
"Sub CallMeInteger(ByVal [Integer] As Integer)
    CallMeInteger(Integer:=1)
    CallMeInteger(Integer _ ' Test 1
                    := _ ' Test 2
                    1)
End Sub
Dim [Class] As Integer"

            Await TestInClassAsync(code,
                Keyword("Sub"),
                Method("CallMeInteger"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Parameter("[Integer]"),
                Keyword("As"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Identifier("CallMeInteger"),
                Punctuation.OpenParen,
                Identifier("Integer"),
                Operators.ColonEquals,
                Number("1"),
                Punctuation.CloseParen,
                Identifier("CallMeInteger"),
                Punctuation.OpenParen,
                Identifier("Integer"),
                LineContinuation,
                Comment("' Test 1"),
                Operators.ColonEquals,
                LineContinuation,
                 Comment("' Test 2"),
               Number("1"),
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Sub"),
                Keyword("Dim"),
                Field("[Class]"),
                Keyword("As"),
                Keyword("Integer"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIndexStrings() As Task
            Dim code =
"Default ReadOnly Property IndexMe(ByVal arg As String) As Integer
    Get
        With Me
            Dim t = !String
            t = ! _
                String
            t = .Class
            t = . _
                Class
        End With
    End Get
End Property"

            Await TestAsync(code,
                Keyword("Default"),
                Keyword("ReadOnly"),
                Keyword("Property"),
                [Property]("IndexMe"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Parameter("arg"),
                Keyword("As"),
                Keyword("String"),
                Punctuation.CloseParen,
                Keyword("As"),
                Keyword("Integer"),
                Keyword("Get"),
                Keyword("With"),
                Keyword("Me"),
                Keyword("Dim"),
                Local("t"),
                Operators.Equals,
                Operators.Exclamation,
                Identifier("String"),
                Identifier("t"),
                Operators.Equals,
                Operators.Exclamation,
                LineContinuation,
                Identifier("String"),
                Identifier("t"),
                Operators.Equals,
                Operators.Dot,
                Identifier("Class"),
                Identifier("t"),
                Operators.Equals,
                Operators.Dot,
                LineContinuation,
                Identifier("Class"),
                Keyword("End"),
                Keyword("With"),
                Keyword("End"),
                Keyword("Get"),
                Keyword("End"),
                Keyword("Property"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIndexStringsCommentsAfterLineContinuation() As Task
            Dim code =
"Default ReadOnly Property IndexMe(ByVal arg As String) As Integer
    Get
        With Me
            Dim t = !String
            t = ! _ ' Test 1
                String
            t = .Class
            t = . _ ' Test 2
                Class
        End With
    End Get
End Property"

            Await TestAsync(code,
                Keyword("Default"),
                Keyword("ReadOnly"),
                Keyword("Property"),
                [Property]("IndexMe"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Parameter("arg"),
                Keyword("As"),
                Keyword("String"),
                Punctuation.CloseParen,
                Keyword("As"),
                Keyword("Integer"),
                Keyword("Get"),
                Keyword("With"),
                Keyword("Me"),
                Keyword("Dim"),
                Local("t"),
                Operators.Equals,
                Operators.Exclamation,
                Identifier("String"),
                Identifier("t"),
                Operators.Equals,
                Operators.Exclamation,
                LineContinuation,
                Comment("' Test 1"),
                Identifier("String"),
                Identifier("t"),
                Operators.Equals,
                Operators.Dot,
                Identifier("Class"),
                Identifier("t"),
                Operators.Equals,
                Operators.Dot,
                LineContinuation,
                Comment("' Test 2"),
                Identifier("Class"),
                Keyword("End"),
                Keyword("With"),
                Keyword("End"),
                Keyword("Get"),
                Keyword("End"),
                Keyword("Property"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestMyIsIdentifierOnSyntaxLevel() As Task
            Dim code =
"Dim My
Dim var = My.Application.GetEnvironmentVariable(""test"")
Sub CallMeMy(ByVal My As Integer)
    CallMeMy(My:=1)
    CallMeMy(My _
                := _
                1)
    My.ToString()
    With Me
        .My = 1
        . _
        My _
        = 1
        !My = Nothing
        ! _
        My _
        = Nothing
    End With
    Me.My.ToString()
    Me. _
    My.ToString()
    Me.My = 1
    Me. _
    My = 1
End Sub"

            Await TestInClassAsync(code,
                Keyword("Dim"),
                Field("My"),
                Keyword("Dim"),
                Field("var"),
                Operators.Equals,
                Identifier("My"),
                Operators.Dot,
                Identifier("Application"),
                Operators.Dot,
                Identifier("GetEnvironmentVariable"),
                Punctuation.OpenParen,
                [String]("""test"""),
                Punctuation.CloseParen,
                Keyword("Sub"),
                Method("CallMeMy"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Parameter("My"),
                Keyword("As"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Identifier("CallMeMy"),
                Punctuation.OpenParen,
                Identifier("My"),
                Operators.ColonEquals,
                Number("1"),
                Punctuation.CloseParen,
                Identifier("CallMeMy"),
                Punctuation.OpenParen,
                Identifier("My"),
                LineContinuation,
                Operators.ColonEquals,
                LineContinuation,
                Number("1"),
                Punctuation.CloseParen,
                Identifier("My"),
                Operators.Dot,
                Identifier("ToString"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("With"),
                Keyword("Me"),
                Operators.Dot,
                Identifier("My"),
                Operators.Equals,
                Number("1"),
                Operators.Dot,
                LineContinuation,
                Identifier("My"),
                LineContinuation,
                Operators.Equals,
                Number("1"),
                Operators.Exclamation,
                Identifier("My"),
                Operators.Equals,
                Keyword("Nothing"),
                Operators.Exclamation,
                LineContinuation,
                Identifier("My"),
                LineContinuation,
                Operators.Equals,
                Keyword("Nothing"),
                Keyword("End"),
                Keyword("With"),
                Keyword("Me"),
                Operators.Dot,
                Identifier("My"),
                Operators.Dot,
                Identifier("ToString"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Me"),
                Operators.Dot,
                LineContinuation,
                Identifier("My"),
                Operators.Dot,
                Identifier("ToString"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Me"),
                Operators.Dot,
                Identifier("My"),
                Operators.Equals,
                Number("1"),
                Keyword("Me"),
                Operators.Dot,
                LineContinuation,
                Identifier("My"),
                Operators.Equals,
                Number("1"),
                Keyword("End"),
                Keyword("Sub"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestMyIsIdentifierOnSyntaxLevelCommentsAfterLineContinuation() As Task
            Dim code =
"Dim My
Dim var = My.Application.GetEnvironmentVariable(""test"")
Sub CallMeMy(ByVal My As Integer)
    CallMeMy(My:=1)
    CallMeMy(My _ ' Test 1
                := _ ' Test 2
                1)
    My.ToString()
    With Me
        .My = 1
        . _ ' Test 3
        My _ ' Test 4
        = 1
        !My = Nothing
        ! _ ' Test 5
        My _ ' Test 6
        = Nothing
    End With
    Me.My.ToString()
    Me. _ ' Test 7
    My.ToString()
    Me.My = 1
    Me. _ ' Test 8
    My = 1
End Sub"

            Await TestInClassAsync(code,
                Keyword("Dim"),
                Field("My"),
                Keyword("Dim"),
                Field("var"),
                Operators.Equals,
                Identifier("My"),
                Operators.Dot,
                Identifier("Application"),
                Operators.Dot,
                Identifier("GetEnvironmentVariable"),
                Punctuation.OpenParen,
                [String]("""test"""),
                Punctuation.CloseParen,
                Keyword("Sub"),
                Method("CallMeMy"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Parameter("My"),
                Keyword("As"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Identifier("CallMeMy"),
                Punctuation.OpenParen,
                Identifier("My"),
                Operators.ColonEquals,
                Number("1"),
                Punctuation.CloseParen,
                Identifier("CallMeMy"),
                Punctuation.OpenParen,
                Identifier("My"),
                LineContinuation,
                Comment("' Test 1"),
                Operators.ColonEquals,
                LineContinuation,
                Comment("' Test 2"),
                Number("1"),
                Punctuation.CloseParen,
                Identifier("My"),
                Operators.Dot,
                Identifier("ToString"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("With"),
                Keyword("Me"),
                Operators.Dot,
                Identifier("My"),
                Operators.Equals,
                Number("1"),
                Operators.Dot,
                LineContinuation,
                Comment("' Test 3"),
                Identifier("My"),
                LineContinuation,
                Comment("' Test 4"),
                Operators.Equals,
                Number("1"),
                Operators.Exclamation,
                Identifier("My"),
                Operators.Equals,
                Keyword("Nothing"),
                Operators.Exclamation,
                LineContinuation,
                Comment("' Test 5"),
                Identifier("My"),
                LineContinuation,
                Comment("' Test 6"),
                Operators.Equals,
                Keyword("Nothing"),
                Keyword("End"),
                Keyword("With"),
                Keyword("Me"),
                Operators.Dot,
                Identifier("My"),
                Operators.Dot,
                Identifier("ToString"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Me"),
                Operators.Dot,
                LineContinuation,
                Comment("' Test 7"),
                Identifier("My"),
                Operators.Dot,
                Identifier("ToString"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Me"),
                Operators.Dot,
                Identifier("My"),
                Operators.Equals,
                Number("1"),
                Keyword("Me"),
                Operators.Dot,
                LineContinuation,
                Comment("' Test 8"),
                Identifier("My"),
                Operators.Equals,
                Number("1"),
                Keyword("End"),
                Keyword("Sub"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIsTrueIsFalse() As Task
            Dim code =
"Class IsTrueIsFalseTests
    Dim IsTrue
    Dim IsFalse
    Shared Operator IsTrue(ByVal x As IsTrueIsFalseTests) As Boolean
    End Operator
    Shared Operator IsFalse(ByVal x As IsTrueIsFalseTests) As Boolean
    End Operator
End Class"

            Await TestInClassAsync(code,
                Keyword("Class"),
                [Class]("IsTrueIsFalseTests"),
                Keyword("Dim"),
                Field("IsTrue"),
                Keyword("Dim"),
                Field("IsFalse"),
                Keyword("Shared"),
                Keyword("Operator"),
                Keyword("IsTrue"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Parameter("x"),
                Keyword("As"),
                Identifier("IsTrueIsFalseTests"),
                Punctuation.CloseParen,
                Keyword("As"),
                Keyword("Boolean"),
                Keyword("End"),
                Keyword("Operator"),
                Keyword("Shared"),
                Keyword("Operator"),
                Keyword("IsFalse"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Parameter("x"),
                Keyword("As"),
                Identifier("IsTrueIsFalseTests"),
                Punctuation.CloseParen,
                Keyword("As"),
                Keyword("Boolean"),
                Keyword("End"),
                Keyword("Operator"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDeclareAnsiAutoUnicode() As Task
            Dim code =
"    Dim Ansi
    Dim Unicode
    Dim Auto
    Declare Ansi Sub AnsiTest Lib ""Test.dll"" ()
    Declare Auto Sub AutoTest Lib ""Test.dll"" ()
    Declare Unicode Sub UnicodeTest Lib ""Test.dll"" ()
    Declare _
        Ansi Sub AnsiTest2 Lib ""Test.dll"" ()
    Declare _
        Auto Sub AutoTest2 Lib ""Test.dll"" ()
    Declare _
        Unicode Sub UnicodeTest2 Lib ""Test.dll"" ()"

            Await TestInClassAsync(code,
                Keyword("Dim"),
                Field("Ansi"),
                Keyword("Dim"),
                Field("Unicode"),
                Keyword("Dim"),
                Field("Auto"),
                Keyword("Declare"),
                Keyword("Ansi"),
                Keyword("Sub"),
                Method("AnsiTest"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                Keyword("Auto"),
                Keyword("Sub"),
                Method("AutoTest"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                Keyword("Unicode"),
                Keyword("Sub"),
                Method("UnicodeTest"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                LineContinuation,
                Keyword("Ansi"),
                Keyword("Sub"),
                Method("AnsiTest2"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                LineContinuation,
                Keyword("Auto"),
                Keyword("Sub"),
                Method("AutoTest2"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                LineContinuation,
                Keyword("Unicode"),
                Keyword("Sub"),
                Method("UnicodeTest2"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDeclareAnsiAutoUnicodeCommentsAfterLineContinuation() As Task
            Dim code =
"    Dim Ansi
    Dim Unicode
    Dim Auto
    Declare Ansi Sub AnsiTest Lib ""Test.dll"" ()
    Declare Auto Sub AutoTest Lib ""Test.dll"" ()
    Declare Unicode Sub UnicodeTest Lib ""Test.dll"" ()
    Declare _ ' Test 1
        Ansi Sub AnsiTest2 Lib ""Test.dll"" ()
    Declare _ ' Test 2
        Auto Sub AutoTest2 Lib ""Test.dll"" ()
    Declare _ ' Test 3
        Unicode Sub UnicodeTest2 Lib ""Test.dll"" ()"

            Await TestInClassAsync(code,
                Keyword("Dim"),
                Field("Ansi"),
                Keyword("Dim"),
                Field("Unicode"),
                Keyword("Dim"),
                Field("Auto"),
                Keyword("Declare"),
                Keyword("Ansi"),
                Keyword("Sub"),
                Method("AnsiTest"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                Keyword("Auto"),
                Keyword("Sub"),
                Method("AutoTest"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                Keyword("Unicode"),
                Keyword("Sub"),
                Method("UnicodeTest"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                LineContinuation,
                Comment("' Test 1"),
                Keyword("Ansi"),
                Keyword("Sub"),
                Method("AnsiTest2"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                LineContinuation,
                Comment("' Test 2"),
                Keyword("Auto"),
                Keyword("Sub"),
                Method("AutoTest2"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                LineContinuation,
                Comment("' Test 3"),
                Keyword("Unicode"),
                Keyword("Sub"),
                Method("UnicodeTest2"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestUntil() As Task
            Dim code =
"    Dim Until
    Sub TestSub()
        Do
        Loop Until True
        Do
        Loop _
        Until True
        Do Until True
        Loop
        Do _
        Until True
        Loop
    End Sub"

            Await TestInClassAsync(code,
                Keyword("Dim"),
                Field("Until"),
                Keyword("Sub"),
                Method("TestSub"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                ControlKeyword("Do"),
                ControlKeyword("Loop"),
                ControlKeyword("Until"),
                Keyword("True"),
                ControlKeyword("Do"),
                ControlKeyword("Loop"),
                LineContinuation,
                ControlKeyword("Until"),
                Keyword("True"),
                ControlKeyword("Do"),
                ControlKeyword("Until"),
                Keyword("True"),
                ControlKeyword("Loop"),
                ControlKeyword("Do"),
                LineContinuation,
                ControlKeyword("Until"),
                Keyword("True"),
                ControlKeyword("Loop"),
                Keyword("End"),
                Keyword("Sub"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestUntilCommentsAfterLineContinuation() As Task
            Dim code =
"    Dim Until
    Sub TestSub()
        Do
        Loop Until True
        Do
        Loop _ ' Test 1
        Until True
        Do Until True
        Loop
        Do _ ' Test 2
        Until True
        Loop
    End Sub"

            Await TestInClassAsync(code,
                Keyword("Dim"),
                Field("Until"),
                Keyword("Sub"),
                Method("TestSub"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                ControlKeyword("Do"),
                ControlKeyword("Loop"),
                ControlKeyword("Until"),
                Keyword("True"),
                ControlKeyword("Do"),
                ControlKeyword("Loop"),
                LineContinuation,
                Comment("' Test 1"),
                ControlKeyword("Until"),
                Keyword("True"),
                ControlKeyword("Do"),
                ControlKeyword("Until"),
                Keyword("True"),
                ControlKeyword("Loop"),
                ControlKeyword("Do"),
                LineContinuation,
                Comment("' Test 2"),
                ControlKeyword("Until"),
                Keyword("True"),
                ControlKeyword("Loop"),
                Keyword("End"),
                Keyword("Sub"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreserve() As Task
            Dim code =
"    Dim Preserve
    Sub TestSub()
        Dim arr As Integer() = Nothing
        ReDim Preserve arr(0)
        ReDim _
        Preserve arr(0)
    End Sub"

            Await TestInClassAsync(code,
                Keyword("Dim"),
                Field("Preserve"),
                Keyword("Sub"),
                Method("TestSub"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("arr"),
                Keyword("As"),
                Keyword("Integer"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Operators.Equals,
                Keyword("Nothing"),
                Keyword("ReDim"),
                Keyword("Preserve"),
                Identifier("arr"),
                Punctuation.OpenParen,
                Number("0"),
                Punctuation.CloseParen,
                Keyword("ReDim"),
                LineContinuation,
                Keyword("Preserve"),
                Identifier("arr"),
                Punctuation.OpenParen,
                Number("0"),
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Sub"))
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreserveCommentsAfterLineContinuation() As Task
            Dim code =
"    Dim Preserve
    Sub TestSub()
        Dim arr As Integer() = Nothing
        ReDim Preserve arr(0)
        ReDim _ ' Test
        Preserve arr(0)
    End Sub"

            Await TestInClassAsync(code,
                Keyword("Dim"),
                Field("Preserve"),
                Keyword("Sub"),
                Method("TestSub"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("arr"),
                Keyword("As"),
                Keyword("Integer"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Operators.Equals,
                Keyword("Nothing"),
                Keyword("ReDim"),
                Keyword("Preserve"),
                Identifier("arr"),
                Punctuation.OpenParen,
                Number("0"),
                Punctuation.CloseParen,
                Keyword("ReDim"),
                LineContinuation,
                Comment("' Test"),
                Keyword("Preserve"),
                Identifier("arr"),
                Punctuation.OpenParen,
                Number("0"),
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Sub"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestSkippedTextAsTokens() As Task
            Dim code =
"Module Program
    Sub Test(ByVal readOnly As Boolean)
    End Sub
End Module"

            Await TestAsync(code,
                Keyword("Module"),
                [Module]("Program"),
                Keyword("Sub"),
                Method("Test"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Keyword("readOnly"),
                Parameter("As"),
                Keyword("Boolean"),
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(538647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538647")>
        Public Async Function TestRegression4315_VariableNamesClassifiedAsType() As Task
            Dim code =
"Module M
    Sub S()
        Dim goo
    End Sub
End Module"

            Await TestAsync(code,
                Keyword("Module"),
                [Module]("M"),
                Keyword("Sub"),
                Method("S"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("goo"),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539203, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539203")>
        Public Async Function TestColonTrivia() As Task
            Await TestInMethodAsync("    : Console.WriteLine()",
                Punctuation.Colon,
                Identifier("Console"),
                Operators.Dot,
                Identifier("WriteLine"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539642")>
        Public Async Function TestFromInCollectionInitializer1() As Task
            Await TestInMethodAsync("Dim y = New Goo() From",
                Keyword("Dim"),
                Local("y"),
                Operators.Equals,
                Keyword("New"),
                Identifier("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("From"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539642")>
        Public Async Function TestFromInCollectionInitializer2() As Task
            Await TestInMethodAsync("Dim y As New Goo() From",
                Keyword("Dim"),
                Local("y"),
                Keyword("As"),
                Keyword("New"),
                Identifier("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("From"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport1() As Task
            Await TestAsync("Imports <x",
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlName("x"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport2() As Task
            Await TestAsync("Imports <xml",
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlName("xml"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport3() As Task
            Await TestAsync("Imports <xmlns",
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlAttributeName("xmlns"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport4() As Task
            Await TestAsync("Imports <xmlns:",
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlAttributeName("xmlns"),
                VBXmlAttributeName(":"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport5() As Task
            Await TestAsync("Imports <xmlns:ns",
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlAttributeName("xmlns"),
                VBXmlAttributeName(":"),
                VBXmlAttributeName("ns"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport6() As Task
            Await TestAsync("Imports <xmlns:ns=",
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlAttributeName("xmlns"),
                VBXmlAttributeName(":"),
                VBXmlAttributeName("ns"),
                VBXmlDelimiter("="))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport7() As Task
            Await TestAsync("Imports <xmlns:ns=""http://goo""",
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlAttributeName("xmlns"),
                VBXmlAttributeName(":"),
                VBXmlAttributeName("ns"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("http://goo"),
                VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestFullyTypedXmlNamespaceImport() As Task
            Await TestAsync("Imports <xmlns:ns=""http://goo"">",
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlAttributeName("xmlns"),
                VBXmlAttributeName(":"),
                VBXmlAttributeName("ns"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("http://goo"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestGetXmlNamespaceExpression() As Task
            Await TestInExpressionAsync("GetXmlNamespace(Name)",
                Keyword("GetXmlNamespace"),
                Punctuation.OpenParen,
                VBXmlName("Name"),
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestGetXmlNamespaceExpressionWithNoName() As Task
            Await TestInExpressionAsync("GetXmlNamespace()",
                Keyword("GetXmlNamespace"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestClassifyXmlDocumentFollowingMisc() As Task
            Await TestInExpressionAsync("<?xml ?><x></x><!--h-->",
                VBXmlDelimiter("<?"),
                VBXmlName("xml"),
                VBXmlDelimiter("?>"),
                VBXmlDelimiter("<"),
                VBXmlName("x"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("x"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<!--"),
                VBXmlComment("h"),
                VBXmlDelimiter("-->"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDeclaration() As Task
            Await TestInExpressionAsync("<?xml version=""1.0""?>",
                VBXmlDelimiter("<?"),
                VBXmlName("xml"),
                VBXmlAttributeName("version"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1.0"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("?>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestEnableWarningDirective() As Task
            Dim code =
"Module Program
    Sub Main
#Enable Warning BC123, [bc456], SomeId
    End Sub
End Module"

            Await TestAsync(code,
                Keyword("Module"),
                [Module]("Program"),
                Keyword("Sub"),
                Method("Main"),
                PPKeyword("#"),
                PPKeyword("Enable"),
                PPKeyword("Warning"),
                Identifier("BC123"),
                Punctuation.Comma,
                Identifier("[bc456]"),
                Punctuation.Comma,
                Identifier("SomeId"),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDisableWarningDirective() As Task
            Dim code =
"Module Program
    Sub Main
#Disable Warning
    End Sub
End Module"

            Await TestAsync(code,
                Keyword("Module"),
                [Module]("Program"),
                Keyword("Sub"),
                Method("Main"),
                PPKeyword("#"),
                PPKeyword("Disable"),
                PPKeyword("Warning"),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBadWarningDirectives() As Task
            Dim code =
"Module Program
    Sub Main
#warning
    End Sub
#Enable blah Warning
End Module
#Disable bc123 Warning
#Enable
#Disable Warning blah"

            Await TestAsync(code,
                Keyword("Module"),
                [Module]("Program"),
                Keyword("Sub"),
                Method("Main"),
                PPKeyword("#"),
                Identifier("warning"),
                Keyword("End"),
                Keyword("Sub"),
                PPKeyword("#"),
                PPKeyword("Enable"),
                Identifier("blah"),
                Identifier("Warning"),
                Keyword("End"),
                Keyword("Module"),
                PPKeyword("#"),
                PPKeyword("Disable"),
                Identifier("bc123"),
                Identifier("Warning"),
                PPKeyword("#"),
                PPKeyword("Enable"),
                PPKeyword("#"),
                PPKeyword("Disable"),
                PPKeyword("Warning"),
                Identifier("blah"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestInterpolatedString1() As Task
            Dim code =
"Module Program
    Sub Main
        Dim s = $""Hello, {name,10:F}.""
    End Sub
End Module"

            Await TestAsync(code,
                Keyword("Module"),
                [Module]("Program"),
                Keyword("Sub"),
                Method("Main"),
                Keyword("Dim"),
                Local("s"),
                Operators.Equals,
                [String]("$"""),
                [String]("Hello, "),
                Punctuation.OpenCurly,
                Identifier("name"),
                Punctuation.Comma,
                Number("10"),
                Punctuation.Colon,
                [String]("F"),
                Punctuation.CloseCurly,
                [String]("."),
                [String](""""),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestInterpolatedString2() As Task
            Dim code =
"Module Program
    Sub Main
        Dim s = $""{x}, {y}""
    End Sub
End Module"

            Await TestAsync(code,
                Keyword("Module"),
                [Module]("Program"),
                Keyword("Sub"),
                Method("Main"),
                Keyword("Dim"),
                Local("s"),
                Operators.Equals,
                [String]("$"""),
                Punctuation.OpenCurly,
                Identifier("x"),
                Punctuation.CloseCurly,
                [String](", "),
                Punctuation.OpenCurly,
                Identifier("y"),
                Punctuation.CloseCurly,
                [String](""""),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(2126, "https://github.com/dotnet/roslyn/issues/2126")>
        Public Async Function CommentBeforeXmlAccessExpression() As Task
            Dim code =
" ' Comment
  x.@Name = ""Text""
' Comment"

            Await TestInMethodAsync(
                className:="C",
                methodName:="M",
                code,
                Comment("' Comment"),
                Identifier("x"),
                VBXmlDelimiter("."),
                VBXmlDelimiter("@"),
                VBXmlAttributeName("Name"),
                Operators.Equals,
                [String]("""Text"""),
                Comment("' Comment"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(3291, "https://github.com/dotnet/roslyn/issues/3291")>
        Public Async Function TestCommentOnCollapsedEndRegion() As Task
            Dim code =
"#Region ""Stuff""
#End Region ' Stuff"

            Await TestAsync(
                code,
                PPKeyword("#"),
                PPKeyword("Region"),
                [String]("""Stuff"""),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("Region"),
                Comment("' Stuff"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConflictMarkers1() As Task
            Dim code =
"interface I
<<<<<<< Start
    sub Goo()
=======
    sub Bar()
>>>>>>> End
end interface"

            Await TestAsync(
                code,
                Keyword("interface"),
                [Interface]("I"),
                Comment("<<<<<<< Start"),
                Keyword("sub"),
                Method("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Comment("======="),
                Keyword("sub"),
                Identifier("Bar"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Comment(">>>>>>> End"),
                Keyword("end"),
                Keyword("interface"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstField() As Task
            Dim code = "Const Number = 42"

            Await TestInClassAsync(code,
                Keyword("Const"),
                Constant("Number"),
                [Static]("Number"),
                Operators.Equals,
                Number("42"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestLocalConst() As Task
            Dim code = "Const Number = 42"

            Await TestInMethodAsync(code,
                Keyword("Const"),
                Constant("Number"),
                Operators.Equals,
                Number("42"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestModifiedIdentifiersInLocals() As Task
            Dim code =
"Dim x$ = ""23""
x$ = ""19"""

            Await TestInMethodAsync(code,
                Keyword("Dim"),
                Local("x$"),
                Operators.Equals,
                [String]("""23"""),
                Identifier("x$"),
                Operators.Equals,
                [String]("""19"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestModifiedIdentifiersInFields() As Task
            Dim code =
"Const x$ = ""23""
Dim y$ = x$"

            Await TestInClassAsync(code,
                Keyword("Const"),
                Constant("x$"),
                [Static]("x$"),
                Operators.Equals,
                [String]("""23"""),
                Keyword("Dim"),
                Field("y$"),
                Operators.Equals,
                Identifier("x$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFunctionNamesWithTypeCharacters() As Task
            Dim code =
"Function x%()
    x% = 42
End Function"

            Await TestInClassAsync(code,
                Keyword("Function"),
                Method("x%"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Identifier("x%"),
                Operators.Equals,
                Number("42"),
                Keyword("End"),
                Keyword("Function"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestExtensionMethod() As Task
            Dim code = "
Imports System.Runtime.CompilerServices

Module M
    <Extension>
    Sub Square(ByRef x As Integer)
        x = x * x
    End Sub
End Module

Class C
    Sub Test()
        Dim x = 42
        x.Square()
        M.Square(x)
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("Imports"),
                Identifier("System"),
                Operators.Dot,
                Identifier("Runtime"),
                Operators.Dot,
                Identifier("CompilerServices"),
                Keyword("Module"),
                [Module]("M"),
                Punctuation.OpenAngle,
                Identifier("Extension"),
                Punctuation.CloseAngle,
                Keyword("Sub"),
                Method("Square"),
                Punctuation.OpenParen,
                Keyword("ByRef"),
                Parameter("x"),
                Keyword("As"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Identifier("x"),
                Operators.Equals,
                Identifier("x"),
                Operators.Asterisk,
                Identifier("x"),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"),
                Keyword("Class"),
                [Class]("C"),
                Keyword("Sub"),
                Method("Test"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Dim"),
                Local("x"),
                Operators.Equals,
                Number("42"),
                Identifier("x"),
                Operators.Dot,
                Identifier("Square"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Identifier("M"),
                Operators.Dot,
                Identifier("Square"),
                Punctuation.OpenParen,
                Identifier("x"),
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestSimpleEvent() As Task
            Dim code = "
Event E(x As Integer)

Sub M()
    RaiseEvent E(42)
End Sub"

            Await TestInClassAsync(code,
                Keyword("Event"),
                [Event]("E"),
                Punctuation.OpenParen,
                Parameter("x"),
                Keyword("As"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Keyword("Sub"),
                Method("M"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("RaiseEvent"),
                Identifier("E"),
                Punctuation.OpenParen,
                Number("42"),
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Sub"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOperators() As Task
            Dim code = "
Public Shared Operator Not(t As Test) As Test
    Return New Test()
End Operator
Public Shared Operator +(t1 As Test, t2 As Test) As Integer
    Return 1
End Operator"

            Await TestInClassAsync(code,
                Keyword("Public"),
                Keyword("Shared"),
                Keyword("Operator"),
                Keyword("Not"),
                Punctuation.OpenParen,
                Parameter("t"),
                Keyword("As"),
                Identifier("Test"),
                Punctuation.CloseParen,
                Keyword("As"),
                Identifier("Test"),
                ControlKeyword("Return"),
                Keyword("New"),
                Identifier("Test"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Operator"),
                Keyword("Public"),
                Keyword("Shared"),
                Keyword("Operator"),
                Operators.Plus,
                Punctuation.OpenParen,
                Parameter("t1"),
                Keyword("As"),
                Identifier("Test"),
                Punctuation.Comma,
                Parameter("t2"),
                Keyword("As"),
                Identifier("Test"),
                Punctuation.CloseParen,
                Keyword("As"),
                Keyword("Integer"),
                ControlKeyword("Return"),
                Number("1"),
                Keyword("End"),
                Keyword("Operator"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestLabelName() As Task
            Dim code =
"Sub Main
E:
    GoTo E
End Sub"

            Await TestAsync(code,
                Keyword("Sub"),
                Method("Main"),
                Label("E"),
                Punctuation.Colon,
                ControlKeyword("GoTo"),
                Identifier("E"),
                Keyword("End"),
                Keyword("Sub"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestCatchStatement() As Task
            Dim code =
"Try

Catch ex As Exception

End Try"

            Await TestInMethodAsync(code,
                ControlKeyword("Try"),
                ControlKeyword("Catch"),
                Local("ex"),
                Keyword("As"),
                Identifier("Exception"),
                ControlKeyword("End"),
                ControlKeyword("Try"))
        End Function
    End Class
End Namespace
