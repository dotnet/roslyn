' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    <Trait(Traits.Feature, Traits.Features.Classification)>
    Public Class SyntacticClassifierTests
        Inherits AbstractVisualBasicClassifierTests

        Protected Overrides Async Function GetClassificationSpansAsync(code As String, spans As ImmutableArray(Of TextSpan), parseOptions As ParseOptions, testHost As TestHost) As Task(Of ImmutableArray(Of ClassifiedSpan))
            Using workspace = CreateWorkspace(code, testHost)
                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Return Await GetSyntacticClassificationsAsync(document, spans)
            End Using
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlStartElementName1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo></goo>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlStartElementName2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlStartElementName3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlStartElementName4(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo.",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo."))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlStartElementName5(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo.b",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo.b"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlStartElementName6(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo.b>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo.b"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlStartElementName7(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo:",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlStartElementName8(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo:b",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"),
                VBXmlName("b"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlStartElementName9(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo:b>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"),
                VBXmlName("b"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmptyElementName1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo/>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter("/>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmptyElementName2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo. />",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo."),
                VBXmlDelimiter("/>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmptyElementName3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo.bar />",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo.bar"),
                VBXmlDelimiter("/>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmptyElementName4(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo: />",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"),
                VBXmlDelimiter("/>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmptyElementName5(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo:bar />",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlName(":"),
                VBXmlName("bar"),
                VBXmlDelimiter("/>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeName1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo b",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("b"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeName2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo ba",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("ba"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeName3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeValue1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=""",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeValue2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=""b",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("b" & vbCrLf))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeValue3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=""ba",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("ba" & vbCrLf))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeValue4(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=""ba""",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("ba"),
                VBXmlAttributeQuotes(""""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeValue5(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=""""",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeQuotes(""""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeValue6(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=""b""",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("b"),
                VBXmlAttributeQuotes(""""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeValue7(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=""ba""",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("ba"),
                VBXmlAttributeQuotes(""""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeValueMultiple1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=""ba"" baz="""" ",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeValueMultiple2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=""ba"" baz=""a"" ",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlElementContent1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f>&l</f>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlEntityReference("&"),
                VBXmlText("l"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlElementContent2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f>goo</f>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlText("goo"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlElementContent3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f>&#x03C0;</f>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlEntityReference("&#x03C0;"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlElementContent4(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f>goo &#x03C0;</f>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlText("goo "),
                VBXmlEntityReference("&#x03C0;"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlElementContent5(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f>goo &lt;</f>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlText("goo "),
                VBXmlEntityReference("&lt;"),
                VBXmlDelimiter("</"),
                VBXmlName("f"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlElementContent6(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f>goo &lt; bar</f>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlElementContent7(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f>goo &lt;",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlText("goo "),
                VBXmlEntityReference("&lt;"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlCData1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f><![CDATA[bar]]></f>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlCData4(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f><![CDATA[bar]]>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<![CDATA["),
                VBXmlCDataSection("bar"),
                VBXmlDelimiter("]]>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlCData5(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f><![CDATA[<>/]]>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("f"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("<![CDATA["),
                VBXmlCDataSection("<>/"),
                VBXmlDelimiter("]]>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlCData6(testHost As TestHost) As Task
            Dim code =
"<f><![CDATA[goo
baz]]></f>"

            Await TestInExpressionAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAtElementName1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<<%= ",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAtElementName2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<<%= %>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                VBXmlEmbeddedExpression("%>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAtElementName3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<<%= bar %>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAtElementName4(testHost As TestHost) As Task
            Await TestInExpressionAsync("<<%= bar.Baz() %>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                Operators.Dot,
                Identifier("Baz"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                VBXmlEmbeddedExpression("%>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAtElementName5(testHost As TestHost) As Task
            Await TestInExpressionAsync("<<%= bar.Baz() %> />",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAtElementName6(testHost As TestHost) As Task
            Await TestInExpressionAsync("<<%= bar %> />",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("/>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo <%= bar %>>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo <%= bar %>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo <%= bar %>></goo>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute4(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo <%= bar %> />",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("/>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsAttributeValue1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=<%=baz >",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("baz"),
                Operators.GreaterThan)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsAttributeValue2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=<%=baz %> >",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("bar"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("baz"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsAttributeValue3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo bar=<%=baz.Goo %> >",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsElementContent1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f><%= bar %></f>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsElementContent2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f><%= bar.Goo %></f>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsElementContent3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<f><%= bar.Goo %> jaz</f>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsElementContentNested(testHost As TestHost) As Task
            Dim code =
"Dim doc = _
    <goo>
        <%= <bug141>
                <a>hello</a>
            </bug141> %>
    </goo>"

            Await TestInMethodAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbeddedExpressionAsElementContentNestedCommentsAfterLineContinuation(testHost As TestHost) As Task
            Dim code =
"Dim doc = _ ' Test
    <goo>
        <%= <bug141>
                <a>hello</a>
            </bug141> %>
    </goo>"

            Await TestInMethodAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiteralsInLambdas(testHost As TestHost) As Task
            Dim code =
"Dim x = Function() _
                    <element val=""something""/>
Dim y = Function() <element val=""something""/>"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiteralsInLambdasCommentsAfterLineContinuation(testHost As TestHost) As Task
            Dim code =
"Dim x = Function() _ 'Test
                    <element val=""something""/>
Dim y = Function() <element val=""something""/>"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocumentPrologue(testHost As TestHost) As Task
            Await TestInExpressionAsync("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiterals1(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiterals2(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiterals3(testHost As TestHost) As Task
            Dim code =
"Dim c = <p:x xmlns:p=""abc
123""/>"

            Await TestInMethodAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiterals4(testHost As TestHost) As Task
            Dim code =
"Dim d = _
        <?xml version=""1.0""?>
        <a/>"

            Await TestInMethodAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiterals4CommentsAfterLineContinuation(testHost As TestHost) As Task
            Dim code =
"Dim d = _ ' Test
        <?xml version=""1.0""?>
        <a/>"

            Await TestInMethodAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiterals5(testHost As TestHost) As Task
            Dim code =
"Dim i = 100
        Process( _
                <Customer ID=<%= i + 1000 %> a="""">
                </Customer>)"

            Await TestInMethodAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiterals5CommentsAfterLineContinuation(testHost As TestHost) As Task
            Dim code =
"Dim i = 100
        Process( _ '    Test
                <Customer ID=<%= i + 1000 %> a="""">
                </Customer>)"

            Await TestInMethodAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiterals6(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlLiterals7(testHost As TestHost) As Task
            Dim code =
"Dim spacetest = <a b=""1"" c=""2"">
                 </a>"

            Await TestInMethodAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestOptionKeywordsInClassContext(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestOptionInferAndExplicit(testHost As TestHost) As Task
            Dim text =
"Option Infer On
Option Explicit Off"

            Await TestAsync(text,
                testHost,
                Keyword("Option"),
                Keyword("Infer"),
                Keyword("On"),
                Keyword("Option"),
                Keyword("Explicit"),
                Keyword("Off"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestOptionCompareTextBinary(testHost As TestHost) As Task
            Dim code =
"Option Compare Text ' comment
Option Compare Binary "

            Await TestAsync(code,
                testHost,
                Keyword("Option"),
                Keyword("Compare"),
                Keyword("Text"),
                Comment("' comment"),
                Keyword("Option"),
                Keyword("Compare"),
                Keyword("Binary"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestOptionInfer1(testHost As TestHost) As Task
            Await TestAsync("Option Infer",
                testHost,
                Keyword("Option"),
                Keyword("Infer"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestOptionExplicit1(testHost As TestHost) As Task
            Await TestAsync("Option Explicit",
                testHost,
                Keyword("Option"),
                Keyword("Explicit"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestOptionStrict1(testHost As TestHost) As Task
            Await TestAsync("Option Strict",
                testHost,
                Keyword("Option"),
                Keyword("Strict"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestLinqContextualKeywords(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestFromLinqExpression1(testHost As TestHost) As Task
            Await TestInExpressionAsync("From it in goo",
                testHost,
                Keyword("From"),
                Identifier("it"),
                Keyword("in"),
                Identifier("goo"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFromLinqExpression2(testHost As TestHost) As Task
            Await TestInExpressionAsync("From it in goofooo.Goo",
                testHost,
                Keyword("From"),
                Identifier("it"),
                Keyword("in"),
                Identifier("goofooo"),
                Operators.Dot,
                Identifier("Goo"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFromLinqExpression3(testHost As TestHost) As Task
            Await TestInExpressionAsync("From it ",
                testHost,
                Keyword("From"),
                Identifier("it"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFromNotInContext1(testHost As TestHost) As Task
            Dim code =
"Class From
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Class"),
                [Class]("From"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFromNotInContext2(testHost As TestHost) As Task
            Await TestInMethodAsync("Dim from = 42",
                testHost,
                Keyword("Dim"),
                Local("from"),
                Operators.Equals,
                Number("42"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestWhereLinqExpression1(testHost As TestHost) As Task
            Await TestInExpressionAsync("From it in goo Where it <> 4",
                testHost,
                Keyword("From"),
                Identifier("it"),
                Keyword("in"),
                Identifier("goo"),
                Keyword("Where"),
                Identifier("it"),
                Operators.LessThanGreaterThan,
                Number("4"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestLinqQuery1(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestLinqQuery1CommentsAfterLineContinuation(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542387")>
        Public Async Function TestFromInQuery(testHost As TestHost) As Task
            Dim code =
"Dim From = New List(Of Integer)
Dim result = From s In From Select s"

            Await TestInMethodAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestKeyKeyword1(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestKeyKeyword1CommentsAfterLineContinuation(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestKeyKeyword2(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestNamespaceDeclaration(testHost As TestHost) As Task
            Dim code =
"Namespace N1.N2
End Namespace"

            Await TestAsync(code,
                testHost,
                Keyword("Namespace"),
                [Namespace]("N1"),
                Operators.Dot,
                [Namespace]("N2"),
                Keyword("End"),
                Keyword("Namespace"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestClassDeclaration1(testHost As TestHost) As Task
            Dim code = "Class C1"

            Await TestAsync(code,
                testHost,
                Keyword("Class"),
                [Class]("C1"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestClassDeclaration2(testHost As TestHost) As Task
            Dim code =
"Class C1
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Class"),
                [Class]("C1"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestClassDeclaration3(testHost As TestHost) As Task
            Dim code = "Class C1 : End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Class"),
                [Class]("C1"),
                Punctuation.Colon,
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestStructDeclaration1(testHost As TestHost) As Task
            Dim code = "Structure S1"

            Await TestAsync(code,
                testHost,
                Keyword("Structure"),
                Struct("S1"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestStructDeclaration2(testHost As TestHost) As Task
            Dim code = "Structure S1 : End Structure"

            Await TestAsync(code,
                testHost,
                Keyword("Structure"),
                Struct("S1"),
                Punctuation.Colon,
                Keyword("End"),
                Keyword("Structure"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestStructDeclaration3(testHost As TestHost) As Task
            Dim code =
"Structure S1
End Structure"

            Await TestAsync(code,
                testHost,
                Keyword("Structure"),
                Struct("S1"),
                Keyword("End"),
                Keyword("Structure"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestInterfaceDeclaration1(testHost As TestHost) As Task
            Dim code = "Interface I1"

            Await TestAsync(code,
                testHost,
                Keyword("Interface"),
                [Interface]("I1"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestInterfaceDeclaration2(testHost As TestHost) As Task
            Dim code = "Interface I1 : End Interface"

            Await TestAsync(code,
                testHost,
                Keyword("Interface"),
                [Interface]("I1"),
                Punctuation.Colon,
                Keyword("End"),
                Keyword("Interface"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestInterfaceDeclaration3(testHost As TestHost) As Task
            Dim code =
"Interface I1
End Interface"

            Await TestAsync(code,
                testHost,
                Keyword("Interface"),
                [Interface]("I1"),
                Keyword("End"),
                Keyword("Interface"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEnumDeclaration1(testHost As TestHost) As Task
            Dim code = "Enum E1"

            Await TestAsync(code,
                testHost,
                Keyword("Enum"),
                [Enum]("E1"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEnumDeclaration2(testHost As TestHost) As Task
            Dim code = "Enum E1 : End Enum"

            Await TestAsync(code,
                testHost,
                Keyword("Enum"),
                [Enum]("E1"),
                Punctuation.Colon,
                Keyword("End"),
                Keyword("Enum"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEnumDeclaration3(testHost As TestHost) As Task
            Dim code =
"Enum E1
End Enum"

            Await TestAsync(code,
                testHost,
                Keyword("Enum"),
                [Enum]("E1"),
                Keyword("End"),
                Keyword("Enum"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestDelegateSubDeclaration1(testHost As TestHost) As Task
            Dim code = "Public Delegate Sub Goo()"

            Await TestAsync(code,
                testHost,
                Keyword("Public"),
                Keyword("Delegate"),
                Keyword("Sub"),
                [Delegate]("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestDelegateFunctionDeclaration1(testHost As TestHost) As Task
            Dim code = "Public Delegate Function Goo() As Integer"

            Await TestAsync(code,
                testHost,
                Keyword("Public"),
                Keyword("Delegate"),
                Keyword("Function"),
                [Delegate]("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("As"),
                Keyword("Integer"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestTernaryConditionalExpression(testHost As TestHost) As Task
            Dim code = "Dim i = If(True, 1, 2)"

            Await TestInMethodAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestForStatement(testHost As TestHost) As Task
            Dim code =
"For i = 0 To 10
Exit For"
            Await TestInMethodAsync(code,
                testHost,
                ControlKeyword("For"),
                Identifier("i"),
                Operators.Equals,
                Number("0"),
                ControlKeyword("To"),
                Number("10"),
                ControlKeyword("Exit"),
                ControlKeyword("For"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFloatLiteral(testHost As TestHost) As Task
            Await TestInExpressionAsync("1.0",
                testHost,
                Number("1.0"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestIntLiteral(testHost As TestHost) As Task
            Await TestInExpressionAsync("1",
                testHost,
                Number("1"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestDecimalLiteral(testHost As TestHost) As Task
            Await TestInExpressionAsync("123D",
                testHost,
                Number("123D"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestStringLiterals1(testHost As TestHost) As Task
            Await TestInExpressionAsync("""goo""",
                testHost,
                [String]("""goo"""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestCharacterLiteral(testHost As TestHost) As Task
            Await TestInExpressionAsync("""f""c",
                testHost,
                [String]("""f""c"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestRegression_DoUntil1(testHost As TestHost) As Task
            Dim code = "Do Until True"
            Await TestInMethodAsync(code,
                testHost,
                ControlKeyword("Do"),
                ControlKeyword("Until"),
                Keyword("True"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestComment1(testHost As TestHost) As Task
            Dim code = "'goo"

            Await TestAsync(code,
                testHost,
                Comment("'goo"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestComment2(testHost As TestHost) As Task
            Dim code =
"Class C1
'hello"

            Await TestAsync(code,
                testHost,
                Keyword("Class"),
                [Class]("C1"),
                Comment("'hello"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_SingleLine(testHost As TestHost) As Task
            Dim code =
"'''<summary>something</summary>
Class Bar
End Class"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_ExteriorTrivia(testHost As TestHost) As Task
            Dim code =
"''' <summary>
''' something
''' </summary>
Class Bar
End Class"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_ExteriorTriviaInsideEndTag(testHost As TestHost) As Task
            Dim code =
"''' <summary></
''' summary>
Class Bar
End Class"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_AttributesWithExteriorTrivia(testHost As TestHost) As Task
            Dim code =
"''' <summary att1=""value1""
''' att2=""value2"">
''' something
''' </summary>
Class Bar
End Class"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_EmptyElementAttributesWithExteriorTrivia(testHost As TestHost) As Task
            Dim code =
"''' <summary att1=""value1""
''' att2=""value2"" />
Class Bar
End Class"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_XmlCommentWithExteriorTrivia(testHost As TestHost) As Task
            Dim code =
"'''<summary>
'''<!--first
'''second-->
'''</summary>
Class Bar
End Class"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_CDataWithExteriorTrivia(testHost As TestHost) As Task
            Dim code =
"'''<summary>
'''<![CDATA[first
'''second]]>
'''</summary>
Class Bar
End Class"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_PreprocessingInstruction1(testHost As TestHost) As Task
            Await TestAsync("''' <?",
                testHost,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_PreprocessingInstruction2(testHost As TestHost) As Task
            Await TestAsync("''' <??>",
                testHost,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("?>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_PreprocessingInstruction3(testHost As TestHost) As Task
            Await TestAsync("''' <?xml",
                testHost,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("xml"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_PreprocessingInstruction4(testHost As TestHost) As Task
            Await TestAsync("''' <?xml version=""1.0""?>",
                testHost,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("xml"),
                XmlDoc.ProcessingInstruction(" "),
                XmlDoc.ProcessingInstruction("version=""1.0"""),
                XmlDoc.ProcessingInstruction("?>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_PreprocessingInstruction5(testHost As TestHost) As Task
            Await TestAsync("''' <?goo?>",
                testHost,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("goo"),
                XmlDoc.ProcessingInstruction("?>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlDocComment_PreprocessingInstruction6(testHost As TestHost) As Task
            Await TestAsync("''' <?goo bar?>",
                testHost,
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("goo"),
                XmlDoc.ProcessingInstruction(" "),
                XmlDoc.ProcessingInstruction("bar"),
                XmlDoc.ProcessingInstruction("?>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestIsTrue(testHost As TestHost) As Task
            Await TestInClassAsync("    Public Shared Operator IsTrue(c As C) As Boolean",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestIsFalse(testHost As TestHost) As Task
            Await TestInClassAsync("    Public Shared Operator IsFalse(c As C) As Boolean",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestDelegate1(testHost As TestHost) As Task
            Await TestAsync("Delegate Sub Goo()",
                testHost,
                Keyword("Delegate"),
                Keyword("Sub"),
                [Delegate]("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestImports1(testHost As TestHost) As Task
            Dim code =
"Imports Goo
Imports Bar"

            Await TestAsync(code,
                testHost,
                Keyword("Imports"),
                Identifier("Goo"),
                Keyword("Imports"),
                Identifier("Bar"))
        End Function

        ''' <summary>
        ''' Clear Syntax Error
        ''' </summary>
        <Theory, CombinatorialData>
        Public Async Function TestImports2(testHost As TestHost) As Task
            Dim code =
"Imports
Imports Bar"

            Await TestAsync(code,
                testHost,
                Keyword("Imports"),
                Keyword("Imports"),
                Identifier("Bar"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestImports3(testHost As TestHost) As Task
            Dim code =
"Imports Goo=Baz
Imports Bar=Quux"

            Await TestAsync(code,
                testHost,
                Keyword("Imports"),
                Identifier("Goo"),
                Operators.Equals,
                Identifier("Baz"),
                Keyword("Imports"),
                Identifier("Bar"),
                Operators.Equals,
                Identifier("Quux"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestImports4(testHost As TestHost) As Task
            Dim code = "Imports System.Text"

            Await TestAsync(code,
                testHost,
                Keyword("Imports"),
                Identifier("System"),
                Operators.Dot,
                Identifier("Text"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlElement1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo></goo>",
                testHost,
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
        <Theory, CombinatorialData>
        Public Async Function TestXmlElement3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        '''<summary>
        ''' Broken end only element should still classify
        ''' </summary>
        <Theory, CombinatorialData>
        Public Async Function TestXmlElement4(testHost As TestHost) As Task
            Await TestInExpressionAsync("</goo>",
                testHost,
                VBXmlDelimiter("</"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlElement5(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo.bar></goo.bar>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo.bar"),
                VBXmlDelimiter(">"),
                VBXmlDelimiter("</"),
                VBXmlName("goo.bar"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlElement6(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo:bar>hello</goo:bar>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlElement7(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo.bar />",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo.bar"),
                VBXmlDelimiter("/>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbedded1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo><%= bar %></goo>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbedded3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<<%= bar %>/>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("/>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbedded4(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo <%= bar %>=""42""/>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlEmbedded5(testHost As TestHost) As Task
            Await TestInExpressionAsync("<goo a1=<%= bar %>/>",
                testHost,
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlAttributeName("a1"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("bar"),
                VBXmlEmbeddedExpression("%>"),
                VBXmlDelimiter("/>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlComment1(testHost As TestHost) As Task
            Await TestInExpressionAsync("<!---->",
                testHost,
                VBXmlDelimiter("<!--"),
                VBXmlDelimiter("-->"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlComment2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<!--goo-->",
                testHost,
                VBXmlDelimiter("<!--"),
                VBXmlComment("goo"),
                VBXmlDelimiter("-->"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlComment3(testHost As TestHost) As Task
            Await TestInExpressionAsync("<a><!--goo--></a>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlPreprocessingInstruction2(testHost As TestHost) As Task
            Await TestInExpressionAsync("<a><?pi value=2?></a>",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlDescendantsMemberAccess1(testHost As TestHost) As Task
            Await TestInExpressionAsync("x...<goo>",
                testHost,
                Identifier("x"),
                VBXmlDelimiter("."),
                VBXmlDelimiter("."),
                VBXmlDelimiter("."),
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlElementMemberAccess1(testHost As TestHost) As Task
            Await TestInExpressionAsync("x.<goo>",
                testHost,
                Identifier("x"),
                VBXmlDelimiter("."),
                VBXmlDelimiter("<"),
                VBXmlName("goo"),
                VBXmlDelimiter(">"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeMemberAccess1(testHost As TestHost) As Task
            Await TestInExpressionAsync("x.@goo",
                testHost,
                Identifier("x"),
                VBXmlDelimiter("."),
                VBXmlDelimiter("@"),
                VBXmlAttributeName("goo"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestXmlAttributeMemberAccess2(testHost As TestHost) As Task
            Await TestInExpressionAsync("x.@goo:bar",
                testHost,
                Identifier("x"),
                VBXmlDelimiter("."),
                VBXmlDelimiter("@"),
                VBXmlAttributeName("goo"),
                VBXmlAttributeName(":"),
                VBXmlAttributeName("bar"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorReference(testHost As TestHost) As Task
            Await TestInNamespaceAsync("#R ""Ref""",
                testHost,
                PPKeyword("#"),
                PPKeyword("R"),
                [String]("""Ref"""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorConst1(testHost As TestHost) As Task
            Await TestInNamespaceAsync("#Const Goo = 1",
                testHost,
                PPKeyword("#"),
                PPKeyword("Const"),
                Identifier("Goo"),
                Operators.Equals,
                Number("1"))
        End Function

        Public Async Function TestPreprocessorConst2(testHost As TestHost) As Task
            Await TestInNamespaceAsync("#Const DebugCode = True",
                testHost,
                PPKeyword("#"),
                PPKeyword("Const"),
                Identifier("DebugCode"),
                Operators.Equals,
                Keyword("True"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorIfThen1(testHost As TestHost) As Task
            Await TestInNamespaceAsync("#If Goo Then",
                testHost,
                PPKeyword("#"),
                PPKeyword("If"),
                Identifier("Goo"),
                PPKeyword("Then"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorElseIf1(testHost As TestHost) As Task
            Await TestInNamespaceAsync("#ElseIf Goo Then",
                testHost,
                PPKeyword("#"),
                PPKeyword("ElseIf"),
                Identifier("Goo"),
                PPKeyword("Then"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorElse1(testHost As TestHost) As Task
            Await TestInNamespaceAsync("#Else",
                testHost,
                PPKeyword("#"),
                PPKeyword("Else"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorEndIf1(testHost As TestHost) As Task
            Await TestInNamespaceAsync("#End If",
                testHost,
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("If"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorExternalSource1(testHost As TestHost) As Task
            Await TestInNamespaceAsync("#ExternalSource(""c:\wwwroot\inetpub\test.aspx"", 30)",
                testHost,
                PPKeyword("#"),
                PPKeyword("ExternalSource"),
                Punctuation.OpenParen,
                [String]("""c:\wwwroot\inetpub\test.aspx"""),
                Punctuation.Comma,
                Number("30"),
                Punctuation.CloseParen)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorExternalChecksum1(testHost As TestHost) As Task
            Dim code =
"#ExternalChecksum(""c:\wwwroot\inetpub\test.aspx"", _
""{12345678-1234-1234-1234-123456789abc}"", _
""1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"")"

            Await TestInNamespaceAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorExternalChecksum1CommentsAfterLineContinuation(testHost As TestHost) As Task
            Dim code =
"#ExternalChecksum(""c:\wwwroot\inetpub\test.aspx"", _ ' Test
""{12345678-1234-1234-1234-123456789abc}"", _ ' Test
""1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"")"

            Await TestInNamespaceAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorExternalChecksum2(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestPreprocessorExternalChecksum2CommentsAfterLineContinuation(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestBug2641_1(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestBug2641_1CommentsAfterLineContinuation(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestBug2641_2(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestBug2640(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestBug2638(testHost As TestHost) As Task
            Dim code =
"Module M
    Sub Main()
        Dim dt = #1/1/2000#
    End Sub
End Module"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestBug2562(testHost As TestHost) As Task
            Dim code =
"Module Program
  Sub Main(args As String())
    #region ""Goo""
    #End region REM dfkjslfkdsjf
  End Sub
End Module"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestBug3004(testHost As TestHost) As Task
            Dim code =
"''' <summary>
''' &#65;
''' </summary>
Module M
End Module"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestBug3006(testHost As TestHost) As Task
            Dim code =
"#If True Then ' comment
#End If"

            Await TestAsync(code,
                testHost,
                PPKeyword("#"),
                PPKeyword("If"),
                Keyword("True"),
                PPKeyword("Then"),
                Comment("' comment"),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("If"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestBug3008(testHost As TestHost) As Task
            Dim code =
"#If #12/2/2010# = #12/2/2010# Then
#End If"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestBug927678(testHost As TestHost) As Task
            Dim code =
            "'This is not usually a " & vbCrLf &
            "'collapsible comment block" & vbCrLf &
            "x = 2"

            Await TestInMethodAsync(code,
                testHost,
                Comment("'This is not usually a "),
                         Comment("'collapsible comment block"),
                         Identifier("x"),
                         Operators.Equals,
                         Number("2"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAttribute(testHost As TestHost) As Task
            Dim code = "<Assembly: Goo()>"

            Await TestAsync(code,
                testHost,
                Punctuation.OpenAngle,
                 Keyword("Assembly"),
                 Punctuation.Colon,
                 Identifier("Goo"),
                 Punctuation.OpenParen,
                 Punctuation.CloseParen,
                 Punctuation.CloseAngle)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAngleBracketsOnGenericConstraints_Bug932262(testHost As TestHost) As Task
            Dim code =
"Class C(Of T As A(Of T))
End Class"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestIntegerAsContextualKeyword(testHost As TestHost) As Task
            Dim code =
"Sub CallMeInteger(ByVal [Integer] As Integer)
    CallMeInteger(Integer:=1)
    CallMeInteger(Integer _
                    := _
                    1)
End Sub
Dim [Class] As Integer"

            Await TestInClassAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestIntegerAsContextualKeywordCommentsAfterLineContinuation(testHost As TestHost) As Task
            Dim code =
"Sub CallMeInteger(ByVal [Integer] As Integer)
    CallMeInteger(Integer:=1)
    CallMeInteger(Integer _ ' Test 1
                    := _ ' Test 2
                    1)
End Sub
Dim [Class] As Integer"

            Await TestInClassAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestIndexStrings(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestIndexStringsCommentsAfterLineContinuation(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestMyIsIdentifierOnSyntaxLevel(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestMyIsIdentifierOnSyntaxLevelCommentsAfterLineContinuation(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestIsTrueIsFalse(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestDeclareAnsiAutoUnicode(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestDeclareAnsiAutoUnicodeCommentsAfterLineContinuation(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestUntil(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestUntilCommentsAfterLineContinuation(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestPreserve(testHost As TestHost) As Task
            Dim code =
"    Dim Preserve
    Sub TestSub()
        Dim arr As Integer() = Nothing
        ReDim Preserve arr(0)
        ReDim _
        Preserve arr(0)
    End Sub"

            Await TestInClassAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestPreserveCommentsAfterLineContinuation(testHost As TestHost) As Task
            Dim code =
"    Dim Preserve
    Sub TestSub()
        Dim arr As Integer() = Nothing
        ReDim Preserve arr(0)
        ReDim _ ' Test
        Preserve arr(0)
    End Sub"

            Await TestInClassAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestSkippedTextAsTokens(testHost As TestHost) As Task
            Dim code =
"Module Program
    Sub Test(ByVal readOnly As Boolean)
    End Sub
End Module"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538647")>
        Public Async Function TestRegression4315_VariableNamesClassifiedAsType(testHost As TestHost) As Task
            Dim code =
"Module M
    Sub S()
        Dim goo
    End Sub
End Module"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539203")>
        Public Async Function TestColonTrivia(testHost As TestHost) As Task
            Await TestInMethodAsync("    : Console.WriteLine()",
                testHost,
                Punctuation.Colon,
                Identifier("Console"),
                Operators.Dot,
                Identifier("WriteLine"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539642")>
        Public Async Function TestFromInCollectionInitializer1(testHost As TestHost) As Task
            Await TestInMethodAsync("Dim y = New Goo() From",
                testHost,
                Keyword("Dim"),
                Local("y"),
                Operators.Equals,
                Keyword("New"),
                Identifier("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("From"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539642")>
        Public Async Function TestFromInCollectionInitializer2(testHost As TestHost) As Task
            Await TestInMethodAsync("Dim y As New Goo() From",
                testHost,
                Keyword("Dim"),
                Local("y"),
                Keyword("As"),
                Keyword("New"),
                Identifier("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("From"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport1(testHost As TestHost) As Task
            Await TestAsync("Imports <x",
                testHost,
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlName("x"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport2(testHost As TestHost) As Task
            Await TestAsync("Imports <xml",
                testHost,
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlName("xml"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport3(testHost As TestHost) As Task
            Await TestAsync("Imports <xmlns",
                testHost,
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlAttributeName("xmlns"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport4(testHost As TestHost) As Task
            Await TestAsync("Imports <xmlns:",
                testHost,
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlAttributeName("xmlns"),
                VBXmlAttributeName(":"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport5(testHost As TestHost) As Task
            Await TestAsync("Imports <xmlns:ns",
                testHost,
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlAttributeName("xmlns"),
                VBXmlAttributeName(":"),
                VBXmlAttributeName("ns"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport6(testHost As TestHost) As Task
            Await TestAsync("Imports <xmlns:ns=",
                testHost,
                Keyword("Imports"),
                VBXmlDelimiter("<"),
                VBXmlAttributeName("xmlns"),
                VBXmlAttributeName(":"),
                VBXmlAttributeName("ns"),
                VBXmlDelimiter("="))
        End Function

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestPartiallyTypedXmlNamespaceImport7(testHost As TestHost) As Task
            Await TestAsync("Imports <xmlns:ns=""http://goo""",
                testHost,
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

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539779")>
        Public Async Function TestFullyTypedXmlNamespaceImport(testHost As TestHost) As Task
            Await TestAsync("Imports <xmlns:ns=""http://goo"">",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestGetXmlNamespaceExpression(testHost As TestHost) As Task
            Await TestInExpressionAsync("GetXmlNamespace(Name)",
                testHost,
                Keyword("GetXmlNamespace"),
                Punctuation.OpenParen,
                VBXmlName("Name"),
                Punctuation.CloseParen)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGetXmlNamespaceExpressionWithNoName(testHost As TestHost) As Task
            Await TestInExpressionAsync("GetXmlNamespace()",
                testHost,
                Keyword("GetXmlNamespace"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestClassifyXmlDocumentFollowingMisc(testHost As TestHost) As Task
            Await TestInExpressionAsync("<?xml ?><x></x><!--h-->",
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestXmlDeclaration(testHost As TestHost) As Task
            Await TestInExpressionAsync("<?xml version=""1.0""?>",
                testHost,
                VBXmlDelimiter("<?"),
                VBXmlName("xml"),
                VBXmlAttributeName("version"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1.0"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("?>"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestEnableWarningDirective(testHost As TestHost) As Task
            Dim code =
"Module Program
    Sub Main
#Enable Warning BC123, [bc456], SomeId
    End Sub
End Module"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestDisableWarningDirective(testHost As TestHost) As Task
            Dim code =
"Module Program
    Sub Main
#Disable Warning
    End Sub
End Module"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestBadWarningDirectives(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestInterpolatedString1(testHost As TestHost) As Task
            Dim code =
"Module Program
    Sub Main
        Dim s = $""Hello, {name,10:F}.""
    End Sub
End Module"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestInterpolatedString2(testHost As TestHost) As Task
            Dim code =
"Module Program
    Sub Main
        Dim s = $""{x}, {y}""
    End Sub
End Module"

            Await TestAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/2126")>
        Public Async Function CommentBeforeXmlAccessExpression(testHost As TestHost) As Task
            Dim code =
" ' Comment
  x.@Name = ""Text""
' Comment"

            Await TestInMethodAsync(
                className:="C",
                methodName:="M",
                code,
                testHost,
                Comment("' Comment"),
                Identifier("x"),
                VBXmlDelimiter("."),
                VBXmlDelimiter("@"),
                VBXmlAttributeName("Name"),
                Operators.Equals,
                [String]("""Text"""),
                Comment("' Comment"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/3291")>
        Public Async Function TestCommentOnCollapsedEndRegion(testHost As TestHost) As Task
            Dim code =
"#Region ""Stuff""
#End Region ' Stuff"

            Await TestAsync(
                code,
                testHost,
                PPKeyword("#"),
                PPKeyword("Region"),
                [String]("""Stuff"""),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("Region"),
                Comment("' Stuff"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestConflictMarkers1(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestConflictMarkers2(testHost As TestHost) As Task
            Dim code =
"interface I
<<<<<<< Start
    sub Goo()
||||||| Baseline
    sub Removed()
=======
    sub Bar()
>>>>>>> End
end interface"

            Await TestAsync(
                code,
                testHost,
                Keyword("interface"),
                [Interface]("I"),
                Comment("<<<<<<< Start"),
                Keyword("sub"),
                Method("Goo"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Comment("||||||| Baseline"),
                Keyword("sub"),
                Identifier("Removed"),
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

        <Theory, CombinatorialData>
        Public Async Function TestConstField(testHost As TestHost) As Task
            Dim code = "Const Number = 42"

            Await TestInClassAsync(code,
                testHost,
                Keyword("Const"),
                Constant("Number"),
                [Static]("Number"),
                Operators.Equals,
                Number("42"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestLocalConst(testHost As TestHost) As Task
            Dim code = "Const Number = 42"

            Await TestInMethodAsync(code,
                testHost,
                Keyword("Const"),
                Constant("Number"),
                Operators.Equals,
                Number("42"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestModifiedIdentifiersInLocals(testHost As TestHost) As Task
            Dim code =
"Dim x$ = ""23""
x$ = ""19"""

            Await TestInMethodAsync(code,
                testHost,
                Keyword("Dim"),
                Local("x$"),
                Operators.Equals,
                [String]("""23"""),
                Identifier("x$"),
                Operators.Equals,
                [String]("""19"""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestModifiedIdentifiersInFields(testHost As TestHost) As Task
            Dim code =
"Const x$ = ""23""
Dim y$ = x$"

            Await TestInClassAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestFunctionNamesWithTypeCharacters(testHost As TestHost) As Task
            Dim code =
"Function x%()
    x% = 42
End Function"

            Await TestInClassAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestExtensionMethod(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestSimpleEvent(testHost As TestHost) As Task
            Dim code = "
Event E(x As Integer)

Sub M()
    RaiseEvent E(42)
End Sub"

            Await TestInClassAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestOperators(testHost As TestHost) As Task
            Dim code = "
Public Shared Operator Not(t As Test) As Test
    Return New Test()
End Operator
Public Shared Operator +(t1 As Test, t2 As Test) As Integer
    Return 1
End Operator"

            Await TestInClassAsync(code,
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestLabelName(testHost As TestHost) As Task
            Dim code =
"Sub Main
E:
    GoTo E
End Sub"

            Await TestAsync(code,
                testHost,
                Keyword("Sub"),
                Method("Main"),
                Label("E"),
                Punctuation.Colon,
                ControlKeyword("GoTo"),
                Identifier("E"),
                Keyword("End"),
                Keyword("Sub"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestCatchStatement(testHost As TestHost) As Task
            Dim code =
"Try

Catch ex As Exception

End Try"

            Await TestInMethodAsync(code,
                testHost,
                ControlKeyword("Try"),
                ControlKeyword("Catch"),
                Local("ex"),
                Keyword("As"),
                Identifier("Exception"),
                ControlKeyword("End"),
                ControlKeyword("Try"))
        End Function

        <Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/61687")>
        Public Async Function TestThrow(testHost As TestHost) As Task
            Dim code = "Throw New System.NotImplementedException"
            Await TestInMethodAsync(code,
                testHost,
                ControlKeyword("Throw"),
                Keyword("New"),
                Identifier("System"),
                Operators.Dot,
                Identifier("NotImplementedException"))
        End Function
    End Class
End Namespace
