' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    Public Class SyntacticClassifierTests
        Inherits AbstractVisualBasicClassifierTests

        Friend Overrides Async Function GetClassificationSpansAsync(code As String, textSpan As TextSpan) As Tasks.Task(Of IEnumerable(Of ClassifiedSpan))
            Using Workspace = Await TestWorkspaceFactory.CreateVisualBasicWorkspaceAsync(code)
                Dim document = Workspace.CurrentSolution.Projects.First().Documents.First()
                Dim tree = Await document.GetSyntaxTreeAsync()

                Dim service = document.GetLanguageService(Of IClassificationService)()
                Dim result = New List(Of ClassifiedSpan)
                service.AddSyntacticClassifications(tree, textSpan, result, CancellationToken.None)

                Return result
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName1() As Task
            Await TestInExpressionAsync("<foo></foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName2() As Task
            Await TestInExpressionAsync("<foo",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName3() As Task
            Await TestInExpressionAsync("<foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName4() As Task
            Await TestInExpressionAsync("<foo.",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo."))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName5() As Task
            Await TestInExpressionAsync("<foo.b",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo.b"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName6() As Task
            Await TestInExpressionAsync("<foo.b>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo.b"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName7() As Task
            Await TestInExpressionAsync("<foo:",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName8() As Task
            Await TestInExpressionAsync("<foo:b",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"),
                             VBXmlName("b"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlStartElementName9() As Task
            Await TestInExpressionAsync("<foo:b>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"),
                             VBXmlName("b"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmptyElementName1() As Task
            Await TestInExpressionAsync("<foo/>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmptyElementName2() As Task
            Await TestInExpressionAsync("<foo. />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo."),
                             VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmptyElementName3() As Task
            Await TestInExpressionAsync("<foo.bar />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo.bar"),
                             VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmptyElementName4() As Task
            Await TestInExpressionAsync("<foo: />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"),
                             VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmptyElementName5() As Task
            Await TestInExpressionAsync("<foo:bar />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"),
                             VBXmlName("bar"),
                             VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeName1() As Task
            Await TestInExpressionAsync("<foo b",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("b"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeName2() As Task
            Await TestInExpressionAsync("<foo ba",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("ba"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeName3() As Task
            Await TestInExpressionAsync("<foo bar=",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue1() As Task
            Await TestInExpressionAsync("<foo bar=""",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue2() As Task
            Await TestInExpressionAsync("<foo bar=""b",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeValue("b" & vbCrLf))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue3() As Task
            Await TestInExpressionAsync("<foo bar=""ba",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeValue("ba" & vbCrLf))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue4() As Task
            Await TestInExpressionAsync("<foo bar=""ba""",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeValue("ba"),
                             VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue5() As Task
            Await TestInExpressionAsync("<foo bar=""""",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue6() As Task
            Await TestInExpressionAsync("<foo bar=""b""",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeValue("b"),
                             VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValue7() As Task
            Await TestInExpressionAsync("<foo bar=""ba""",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeValue("ba"),
                             VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeValueMultiple1() As Task
            Await TestInExpressionAsync("<foo bar=""ba"" baz="""" ",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
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
            Await TestInExpressionAsync("<foo bar=""ba"" baz=""a"" ",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
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
            Await TestInExpressionAsync("<f>foo</f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlText("foo"),
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
            Await TestInExpressionAsync("<f>foo &#x03C0;</f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlText("foo "),
                             VBXmlEntityReference("&#x03C0;"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementContent5() As Task
            Await TestInExpressionAsync("<f>foo &lt;</f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlText("foo "),
                             VBXmlEntityReference("&lt;"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementContent6() As Task
            Await TestInExpressionAsync("<f>foo &lt; bar</f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlText("foo "),
                             VBXmlEntityReference("&lt;"),
                             VBXmlText(" bar"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementContent7() As Task
            Await TestInExpressionAsync("<f>foo &lt;",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlText("foo "),
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
            Dim expr = StringFromLines(
                "<f><![CDATA[foo",
                "baz]]></f>")
            Await TestInExpressionAsync(expr,
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("<![CDATA["),
                             VBXmlCDataSection("foo" & vbCrLf),
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
            Await TestInExpressionAsync("<foo <%= bar %>>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute2() As Task
            Await TestInExpressionAsync("<foo <%= bar %>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute3() As Task
            Await TestInExpressionAsync("<foo <%= bar %>></foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttribute4() As Task
            Await TestInExpressionAsync("<foo <%= bar %> />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttributeValue1() As Task
            Dim exprText = "<foo bar=<%=baz >"
            Await TestInExpressionAsync(exprText,
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("baz"),
                             Operators.GreaterThan)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttributeValue2() As Task
            Dim exprText = "<foo bar=<%=baz %> >"
            Await TestInExpressionAsync(exprText,
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("baz"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsAttributeValue3() As Task
            Dim exprText = "<foo bar=<%=baz.Foo %> >"
            Await TestInExpressionAsync(exprText,
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("baz"),
                             Operators.Dot,
                             Identifier("Foo"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsElementContent1() As Task
            Dim exprText = "<f><%= bar %></f>"
            Await TestInExpressionAsync(exprText,
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
            Dim exprText = "<f><%= bar.Foo %></f>"
            Await TestInExpressionAsync(exprText,
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             Operators.Dot,
                             Identifier("Foo"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsElementContent3() As Task
            Dim exprText = "<f><%= bar.Foo %> jaz</f>"
            Await TestInExpressionAsync(exprText,
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             Operators.Dot,
                             Identifier("Foo"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlText(" jaz"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbeddedExpressionAsElementContentNested() As Task
            Dim text = StringFromLines(
                "Dim doc = _",
                "    <foo>",
                "        <%= <bug141>",
                "                <a>hello</a>",
                "            </bug141> %>",
                "    </foo>")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("doc"),
                Operators.Equals,
                Punctuation.Text("_"),
                VBXmlDelimiter("<"),
                VBXmlName("foo"),
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
                VBXmlName("foo"),
                VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlLiteralsInLambdas() As Task
            Dim text = StringFromLines(
                "Dim x = Function() _",
                "                    <element val=""something""/>",
                "        Dim y = Function() <element val=""something""/>")
            Await TestAsync(text,
                Keyword("Dim"),
                Identifier("x"),
                Operators.Equals,
                Keyword("Function"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.Text("_"),
                VBXmlDelimiter("<"),
                VBXmlName("element"),
                VBXmlAttributeName("val"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("something"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("/>"),
                Keyword("Dim"),
                Identifier("y"),
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
            Dim exprText = "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>"
            Await TestInExpressionAsync(exprText,
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
            Dim text = StringFromLines(
                "Dim a = <Customer id1=""1"" id2=""2"" id3=<%= n2 %> id4="""">",
                "                    <!-- This is a simple Xml element with all of the node types -->",
                "                    <Name>Me</Name>",
                "                    <NameUsingExpression><%= n1 %></NameUsingExpression>",
                "                    <Street>10802 177th CT NE</Street>",
                "                    <Misc><![CDATA[Let's add some CDATA",
                " for fun. ]]>",
                "                    </Misc>",
                "                    <Empty><%= Nothing %></Empty>",
                "                </Customer>")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("a"),
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
            Dim text = StringFromLines(
                "Dim b = <?xml version=""1.0""?>",
                "                <!-- comment before the root -->",
                "                <?my-PI PI before the root ?>",
                "                <p:Customer id1=""1"" id2=""2"" id3=<%= n2 %> id4="""">",
                "                    <!-- This is a simple Xml element with all of the node types -->",
                "                    <q:Name>Me</q:Name>",
                "                    <s:NameUsingExpression><%= n1 %></s:NameUsingExpression>",
                "                    <t:Street>10802 177th CT NE</t:Street>",
                "                    <p:Misc><![CDATA[Let's add some CDATA  for fun. ]]>",
                "                    </p:Misc>",
                "                    <Empty><%= Nothing %></Empty>",
                "                    <entity>hello&#x7b;world</entity>",
                "                </p:Customer>")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("b"),
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
            Dim text = StringFromLines(
                "Dim c = <p:x xmlns:p=""abc",
                "123""/>")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("c"),
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
            Dim text = StringFromLines(
                "Dim d = _",
                "        <?xml version=""1.0""?>",
                "        <a/>")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("d"),
                Operators.Equals,
                Punctuation.Text("_"),
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
            Dim text = StringFromLines(
                "Dim i = 100",
                "        Process( _",
                "                <Customer ID=<%= i + 1000 %> a="""">",
                "                </Customer>)")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("i"),
                Operators.Equals,
                Number("100"),
                Identifier("Process"),
                Punctuation.OpenParen,
                Punctuation.Text("_"),
                VBXmlDelimiter("<"),
                VBXmlName("Customer"),
                VBXmlAttributeName("ID"),
                VBXmlDelimiter("="),
                VBXmlEmbeddedExpression("<%="),
                Identifier("i"),
                Operators.Text("+"),
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
            Dim text = StringFromLines(
                "Dim xmlwithkeywords = <MODULE>",
                "                                  <CLASS>",
                "                                      <FUNCTION>",
                "                                          <DIM i=""1""/>",
                "                                          <FOR j=""1"" to=""i"">",
                "                                              <NEXT/>",
                "                                          </FOR>",
                "                                          <END/>",
                "                                      </FUNCTION>",
                "                                  </CLASS>",
                "                              </MODULE>")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("xmlwithkeywords"),
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
            Dim text = StringFromLines(
                "Dim spacetest = <a b=""1"" c=""2"">",
                "                        </a>")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("spacetest"),
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
            Dim text = StringFromLines(
                "Class OptionNoContext",
                "    Dim Infer",
                "    Dim Explicit",
                "    Dim Strict",
                "    Dim Off",
                "    Dim Compare",
                "    Dim Text",
                "    Dim Binary",
                "End Class")
            Await TestAsync(text,
                Keyword("Class"),
                [Class]("OptionNoContext"),
                Keyword("Dim"),
                Identifier("Infer"),
                Keyword("Dim"),
                Identifier("Explicit"),
                Keyword("Dim"),
                Identifier("Strict"),
                Keyword("Dim"),
                Identifier("Off"),
                Keyword("Dim"),
                Identifier("Compare"),
                Keyword("Dim"),
                Identifier("Text"),
                Keyword("Dim"),
                Identifier("Binary"),
                Keyword("End"),
                Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptionInferAndExplicit() As Task
            Dim text = StringFromLines(
                "Option Infer On",
                "Option Explicit Off")
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
            Dim text = StringFromLines(
                "Option Compare Text ' comment",
                "Option Compare Binary ")
            Await TestAsync(text,
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
            Dim text = StringFromLines(
                "Dim from = 0",
                "Dim aggregate = 0",
                "Dim ascending = 0",
                "Dim descending = 0",
                "Dim distinct = 0",
                "Dim by = 0",
                "Shadows equals = 0",
                "Dim group = 0",
                "Dim into = 0",
                "Dim join = 0",
                "Dim skip = 0",
                "Dim take = 0",
                "Dim where = 0",
                "Dim order = 0")
            Await TestInClassAsync(text,
                Keyword("Dim"),
                Identifier("from"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("aggregate"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("ascending"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("descending"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("distinct"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("by"),
                Operators.Equals,
                Number("0"),
                Keyword("Shadows"),
                Identifier("equals"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("group"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("into"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("join"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("skip"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("take"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("where"),
                Operators.Equals,
                Number("0"),
                Keyword("Dim"),
                Identifier("order"),
                Operators.Equals,
                Number("0"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromLinqExpression1() As Task
            Await TestInExpressionAsync("From it in foo",
                 Keyword("From"),
                 Identifier("it"),
                 Keyword("in"),
                 Identifier("foo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromLinqExpression2() As Task
            Await TestInExpressionAsync("From it in foofooo.Foo",
                 Keyword("From"),
                 Identifier("it"),
                 Keyword("in"),
                 Identifier("foofooo"),
                 Operators.Dot,
                 Identifier("Foo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromLinqExpression3() As Task
            Await TestInExpressionAsync("From it ",
                 Keyword("From"),
                 Identifier("it"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromNotInContext1() As Task
            Dim code = StringFromLines(
                "Class From",
                "End Class")
            Await TestAsync(code,
                 Keyword("Class"),
                 [Class]("From"),
                 Keyword("End"),
                 Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromNotInContext2() As Task
            Dim val = "Dim from = 42"
            Await TestInMethodAsync(val,
                         Keyword("Dim"),
                         Identifier("from"),
                         Operators.Equals,
                         Number("42"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestWhereLinqExpression1() As Task
            Dim exprTest = "From it in foo Where it <> 4"
            Await TestInExpressionAsync(exprTest,
                 Keyword("From"),
                 Identifier("it"),
                 Keyword("in"),
                 Identifier("foo"),
                 Keyword("Where"),
                 Identifier("it"),
                 Operators.LessThanGreaterThan,
                 Number("4"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestLinqQuery1() As Task
            Dim text = StringFromLines(
                "            Dim src = New List(Of Boolean)",
                "            Dim var3 = 1",
                "            Dim q = From var1 In src Where var1 And True _",
                "                    Order By var1 Ascending Order By var1 Descending _",
                "                    Select var1 Distinct _",
                "                    Join var2 In src On var1 Equals var2 _",
                "                    Skip var3 Skip While var3 Take var3 Take While var3 _",
                "                    Aggregate var4 In src _",
                "                    Group var4 By var4 Into var5 = Count() _",
                "                    Group Join var6 In src On var6 Equals var5 Into var7 Into var8 = Count()")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("src"),
                Operators.Equals,
                Keyword("New"),
                Identifier("List"),
                Punctuation.OpenParen,
                Keyword("Of"),
                Keyword("Boolean"),
                Punctuation.CloseParen,
                Keyword("Dim"),
                Identifier("var3"),
                Operators.Equals,
                Number("1"),
                Keyword("Dim"),
                Identifier("q"),
                Operators.Equals,
                Keyword("From"),
                Identifier("var1"),
                Keyword("In"),
                Identifier("src"),
                Keyword("Where"),
                Identifier("var1"),
                Keyword("And"),
                Keyword("True"),
                Punctuation.Text("_"),
                Keyword("Order"),
                Keyword("By"),
                Identifier("var1"),
                Keyword("Ascending"),
                Keyword("Order"),
                Keyword("By"),
                Identifier("var1"),
                Keyword("Descending"),
                Punctuation.Text("_"),
                Keyword("Select"),
                Identifier("var1"),
                Keyword("Distinct"),
                Punctuation.Text("_"),
                Keyword("Join"),
                Identifier("var2"),
                Keyword("In"),
                Identifier("src"),
                Keyword("On"),
                Identifier("var1"),
                Keyword("Equals"),
                Identifier("var2"),
                Punctuation.Text("_"),
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
                Punctuation.Text("_"),
                Keyword("Aggregate"),
                Identifier("var4"),
                Keyword("In"),
                Identifier("src"),
                Punctuation.Text("_"),
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
                Punctuation.Text("_"),
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

        <WorkItem(542387)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFromInQuery() As Task
            Dim text = StringFromLines(
                "Dim From = New List(Of Integer)",
                "Dim result = From s In From Select s")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("From"),
                Operators.Equals,
                Keyword("New"),
                Identifier("List"),
                Punctuation.OpenParen,
                Keyword("Of"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Keyword("Dim"),
                Identifier("result"),
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
            Dim text = StringFromLines(
                "Dim Value = ""Test""",
                "            Dim Key As String = Key.Length & (Key.Length)",
                "            Dim Array As String() = { Key, Key.Length }",
                "            Dim o = New With {Key Key.Length, Key .Id = 1, Key Key, Key Value, Key.Empty}",
                "            o = New With {Key _",
                "                          Key.Length, _",
                "                          Key _",
                "                          .Id = 1, _",
                "                          Key _",
                "                          Key, _",
                "                          Key _",
                "                          Value, _",
                "                          Key Key. _",
                "                          Empty _",
                "                          }")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("Value"),
                Operators.Equals,
                [String]("""Test"""),
                Keyword("Dim"),
                Identifier("Key"),
                Keyword("As"),
                Keyword("String"),
                Operators.Equals,
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Operators.Text("&"),
                Punctuation.OpenParen,
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.CloseParen,
                Keyword("Dim"),
                Identifier("Array"),
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
                Identifier("o"),
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
                Punctuation.Text("_"),
                Identifier("Key"),
                Operators.Dot,
                Identifier("Length"),
                Punctuation.Comma,
                Punctuation.Text("_"),
                Keyword("Key"),
                Punctuation.Text("_"),
                Operators.Dot,
                Identifier("Id"),
                Operators.Equals,
                Number("1"),
                Punctuation.Comma,
                Punctuation.Text("_"),
                Keyword("Key"),
                Punctuation.Text("_"),
                Identifier("Key"),
                Punctuation.Comma,
                Punctuation.Text("_"),
                Keyword("Key"),
                Punctuation.Text("_"),
                Identifier("Value"),
                Punctuation.Comma,
                Punctuation.Text("_"),
                Keyword("Key"),
                Identifier("Key"),
                Operators.Dot,
                Punctuation.Text("_"),
                Identifier("Empty"),
                Punctuation.Text("_"),
                Punctuation.CloseCurly)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestKeyKeyword2() As Task
            Dim text = StringFromLines(
                "Dim k = 10",
                "Dim x = New With {Key If(k > 3, 2, -2).GetTypeCode}",
                "Dim y = New With {Key DirectCast(New Object(), Integer).GetTypeCode}",
                "Dim z1 = New With {Key If(True, 1,2).GetTypeCode()}",
                "Dim z2 = New With {Key CType(Nothing, Integer).GetTypeCode()}",
                "Dim Key As Integer",
                "If Key Or True Or Key = 1 Then Console.WriteLine()",
                "Dim z3() = { Key Or True, Key = 1 }",
                "Dim z4 = New List(Of Integer) From {1, 2, 3}",
                "Dim z5 As New List(Of Integer) From {1, 2, 3}",
                "Dim z6 = New List(Of Integer) With {.Capacity = 2}")
            Await TestInMethodAsync(text,
                Keyword("Dim"),
                Identifier("k"),
                Operators.Equals,
                Number("10"),
                Keyword("Dim"),
                Identifier("x"),
                Operators.Equals,
                Keyword("New"),
                Keyword("With"),
                Punctuation.OpenCurly,
                Keyword("Key"),
                Keyword("If"),
                Punctuation.OpenParen,
                Identifier("k"),
                Operators.GreaterThan,
                Number("3"),
                Punctuation.Comma,
                Number("2"),
                Punctuation.Comma,
                Operators.Text("-"),
                Number("2"),
                Punctuation.CloseParen,
                Operators.Dot,
                Identifier("GetTypeCode"),
                Punctuation.CloseCurly,
                Keyword("Dim"),
                Identifier("y"),
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
                Identifier("z1"),
                Operators.Equals,
                Keyword("New"),
                Keyword("With"),
                Punctuation.OpenCurly,
                Keyword("Key"),
                Keyword("If"),
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
                Identifier("z2"),
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
                Identifier("Key"),
                Keyword("As"),
                Keyword("Integer"),
                Keyword("If"),
                Identifier("Key"),
                Keyword("Or"),
                Keyword("True"),
                Keyword("Or"),
                Identifier("Key"),
                Operators.Equals,
                Number("1"),
                Keyword("Then"),
                Identifier("Console"),
                Operators.Dot,
                Identifier("WriteLine"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Dim"),
                Identifier("z3"),
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
                Identifier("z4"),
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
                Identifier("z5"),
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
                Identifier("z6"),
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
        Public Async Function TestClassDeclaration1() As Task
            Dim val = "Class C1"
            Await TestAsync(val,
                 Keyword("Class"),
                 [Class]("C1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestClassDeclaration2() As Task
            Dim val = StringFromLines(
                "Class C1",
                "End Class")
            Await TestAsync(val,
                 Keyword("Class"),
                 [Class]("C1"),
                 Keyword("End"),
                 Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestClassDeclaration3() As Task
            Dim val = "Class C1 : End Class"
            Await TestAsync(val,
                 Keyword("Class"),
                 [Class]("C1"),
                 Punctuation.Colon,
                 Keyword("End"),
                 Keyword("Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStructDeclaration1() As Task
            Dim val = "Structure S1"
            Await TestAsync(val,
                 Keyword("Structure"),
                 Struct("S1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStructDeclaration2() As Task
            Dim val = "Structure S1 : End Structure"
            Await TestAsync(val,
                 Keyword("Structure"),
                 Struct("S1"),
                 Punctuation.Colon,
                 Keyword("End"),
                 Keyword("Structure"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStructDeclaration3() As Task
            Dim val = StringFromLines(
                "Structure S1",
                "End Structure")
            Await TestAsync(val,
                 Keyword("Structure"),
                 Struct("S1"),
                 Keyword("End"),
                 Keyword("Structure"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestInterfaceDeclaration1() As Task
            Dim val = "Interface I1"
            Await TestAsync(val,
                 Keyword("Interface"),
                 [Interface]("I1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestInterfaceDeclaration2() As Task
            Dim val = "Interface I1 : End Interface"
            Await TestAsync(val,
                 Keyword("Interface"),
                 [Interface]("I1"),
                 Punctuation.Colon,
                 Keyword("End"),
                 Keyword("Interface"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestInterfaceDeclaration3() As Task
            Dim val = StringFromLines(
                "Interface I1",
                "End Interface")
            Await TestAsync(val,
                 Keyword("Interface"),
                 [Interface]("I1"),
                 Keyword("End"),
                 Keyword("Interface"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestEnumDeclaration1() As Task
            Dim val = "Enum E1"
            Await TestAsync(val,
                 Keyword("Enum"),
                 [Enum]("E1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestEnumDeclaration2() As Task
            Dim val = "Enum E1 : End Enum"
            Await TestAsync(val,
                 Keyword("Enum"),
                 [Enum]("E1"),
                 Punctuation.Colon,
                 Keyword("End"),
                 Keyword("Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestEnumDeclaration3() As Task
            Dim val = StringFromLines(
                "Enum E1",
                "End Enum")
            Await TestAsync(val,
                 Keyword("Enum"),
                 [Enum]("E1"),
                 Keyword("End"),
                 Keyword("Enum"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDelegateSubDeclaration1() As Task
            Dim val = StringFromLines("Public Delegate Sub Foo()")
            Await TestAsync(val,
                 Keyword("Public"),
                 Keyword("Delegate"),
                 Keyword("Sub"),
                 [Delegate]("Foo"),
                 Punctuation.OpenParen,
                 Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDelegateFunctionDeclaration1() As Task
            Dim val = StringFromLines("Public Delegate Function Foo() As Integer")
            Await TestAsync(val,
                 Keyword("Public"),
                 Keyword("Delegate"),
                 Keyword("Function"),
                 [Delegate]("Foo"),
                 Punctuation.OpenParen,
                 Punctuation.CloseParen,
                 Keyword("As"),
                 Keyword("Integer"))
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
            Dim exprText = """foo"""
            Await TestInExpressionAsync(exprText,
                             [String]("""foo"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestCharacterLiteral() As Task
            Dim exprText = """f""c"
            Await TestInExpressionAsync(exprText,
                             [String]("""f""c"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestRegression_DoUntil1() As Task
            Dim val = "Do Until True"
            Await TestInMethodAsync(val,
                         Keyword("Do"),
                         Keyword("Until"),
                         Keyword("True"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestComment1() As Task
            Dim code = "'foo"
            Await TestAsync(code,
                 Comment("'foo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestComment2() As Task
            Dim val = StringFromLines(
                "Class C1",
                "'hello")
            Await TestAsync(val,
                 Keyword("Class"),
                 [Class]("C1"),
                 Comment("'hello"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_SingleLine() As Task
            Dim val = StringFromLines(
                "'''<summary>something</summary>",
                "Class Bar",
                "End Class")
            Await TestAsync(val,
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
            Dim val = StringFromLines(
                "''' <summary>",
                "''' something",
                "''' </summary>",
                "Class Bar",
                "End Class")
            Await TestAsync(val,
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
            Dim val = StringFromLines(
                "''' <summary></",
                "''' summary>",
                "Class Bar",
                "End Class")
            Await TestAsync(val,
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
            Dim val = StringFromLines(
                "''' <summary att1=""value1""",
                "''' att2=""value2"">",
                "''' something",
                "''' </summary>",
                "Class Bar",
                "End Class")
            Await TestAsync(val,
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
            Dim val = StringFromLines(
                "''' <summary att1=""value1""",
                "''' att2=""value2"" />",
                "Class Bar",
                "End Class")
            Await TestAsync(val,
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
            Dim val = StringFromLines(
                "'''<summary>",
                "'''<!--first",
                "'''second-->",
                "'''</summary>",
                "Class Bar",
                "End Class")
            Await TestAsync(val,
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
            Dim val = StringFromLines(
                "'''<summary>",
                "'''<![CDATA[first",
                "'''second]]>",
                "'''</summary>",
                "Class Bar",
                "End Class")
            Await TestAsync(val,
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
            Await TestAsync("''' <?foo?>",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("foo"),
                XmlDoc.ProcessingInstruction("?>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlDocComment_PreprocessingInstruction6() As Task
            Await TestAsync("''' <?foo bar?>",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("foo"),
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
                        Identifier("c"),
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
                        Identifier("c"),
                        Keyword("As"),
                        Identifier("C"),
                        Punctuation.CloseParen,
                        Keyword("As"),
                        Keyword("Boolean"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDelegate1() As Task
            Await TestAsync("Delegate Sub Foo()",
                 Keyword("Delegate"),
                 Keyword("Sub"),
                 [Delegate]("Foo"),
                 Punctuation.OpenParen,
                 Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImports1() As Task
            Dim code = StringFromLines(
            "Imports Foo",
            "Imports Bar")
            Await TestAsync(code,
                 Keyword("Imports"),
                 Identifier("Foo"),
                 Keyword("Imports"),
                 Identifier("Bar"))
        End Function

        ''' <summary>
        ''' Clear Syntax Error
        ''' </summary>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImports2() As Task
            Dim code = StringFromLines(
            "Imports",
            "Imports Bar")
            Await TestAsync(code,
                 Keyword("Imports"),
                 Keyword("Imports"),
                 Identifier("Bar"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImports3() As Task
            Dim code = StringFromLines(
            "Imports Foo=Baz",
            "Imports Bar=Quux")
            Await TestAsync(code,
                 Keyword("Imports"),
                 Identifier("Foo"),
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
            Await TestInExpressionAsync("<foo></foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Function

        '''<summary>
        ''' Broken XmlElement should classify
        ''' </summary>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement3() As Task
            Await TestInExpressionAsync("<foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Function

        '''<summary>
        ''' Broken end only element should still classify
        ''' </summary>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement4() As Task
            Await TestInExpressionAsync("</foo>",
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement5() As Task
            Await TestInExpressionAsync("<foo.bar></foo.bar>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo.bar"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo.bar"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement6() As Task
            Await TestInExpressionAsync("<foo:bar>hello</foo:bar>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"),
                             VBXmlName("bar"),
                             VBXmlDelimiter(">"),
                             VBXmlText("hello"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
                             VBXmlName(":"),
                             VBXmlName("bar"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElement7() As Task
            Await TestInExpressionAsync("<foo.bar />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo.bar"),
                             VBXmlDelimiter("/>"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlEmbedded1() As Task
            Await TestInExpressionAsync("<foo><%= bar %></foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
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
            Await TestInExpressionAsync("<foo <%= bar %>=""42""/>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
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
            Await TestInExpressionAsync("<foo a1=<%= bar %>/>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
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
            Await TestInExpressionAsync("<!--foo-->",
                             VBXmlDelimiter("<!--"),
                             VBXmlComment("foo"),
                             VBXmlDelimiter("-->"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlComment3() As Task
            Dim tree = ParseExpression("<a><!--foo--></a>")
            Await TestInExpressionAsync("<a><!--foo--></a>",
                             VBXmlDelimiter("<"),
                             VBXmlName("a"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("<!--"),
                             VBXmlComment("foo"),
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
            Await TestInExpressionAsync("x...<foo>",
                             Identifier("x"),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlElementMemberAccess1() As Task
            Await TestInExpressionAsync("x.<foo>",
                             Identifier("x"),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeMemberAccess1() As Task
            Await TestInExpressionAsync("x.@foo",
                             Identifier("x"),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("@"),
                             VBXmlAttributeName("foo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestXmlAttributeMemberAccess2() As Task
            Await TestInExpressionAsync("x.@foo:bar",
                             Identifier("x"),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("@"),
                             VBXmlAttributeName("foo"),
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
            Await TestInNamespaceAsync("#Const Foo = 1",
                            PPKeyword("#"),
                            PPKeyword("Const"),
                            Identifier("Foo"),
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
            Await TestInNamespaceAsync("#If Foo Then",
                            PPKeyword("#"),
                            PPKeyword("If"),
                            Identifier("Foo"),
                            PPKeyword("Then"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreprocessorElseIf1() As Task
            Await TestInNamespaceAsync("#ElseIf Foo Then",
                            PPKeyword("#"),
                            PPKeyword("ElseIf"),
                            Identifier("Foo"),
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
            Dim val = StringFromLines("#ExternalChecksum(""c:\wwwroot\inetpub\test.aspx"", _",
                                      """{12345678-1234-1234-1234-123456789abc}"", _",
                                      """1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"")")
            Await TestInNamespaceAsync(val,
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
        Public Async Function TestPreprocessorExternalChecksum2() As Task
            Dim val = StringFromLines("#ExternalChecksum(""c:\wwwroot\inetpub\test.aspx"", _",
                                      """{12345678-1234-1234-1234-123456789abc}"", _",
                                      """1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"")",
                                      "Module Test",
                                      "    Sub Main()",
                                      "#ExternalSource(""c:\wwwroot\inetpub\test.aspx"", 30)",
                                      "        Console.WriteLine(""In test.aspx"")",
                                      "#End ExternalSource",
                                      "    End Sub",
                                      "End Module")
            Await TestInNamespaceAsync(val,
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
                            Identifier("Main"),
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
            Dim text = StringFromLines(
                "Class PreprocessorNoContext",
                "Dim Region",
                "Dim ExternalSource",
                "End Class",
                "#Region ""Test""",
                "#End Region",
                "#Region ""Test"" ' comment",
                "#End Region ' comment",
                "#Region ""Test"" REM comment",
                "#End Region REM comment",
                "# _",
                "Region ""Test""",
                "# _",
                "End Region",
                "# _",
                "Region _",
                """Test""",
                "# _",
                "End _",
                "Region")
            Await TestAsync(text,
                Keyword("Class"),
                [Class]("PreprocessorNoContext"),
                Keyword("Dim"),
                Identifier("Region"),
                Keyword("Dim"),
                Identifier("ExternalSource"),
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
                Punctuation.Text("_"),
                PPKeyword("Region"),
                [String]("""Test"""),
                PPKeyword("#"),
                Punctuation.Text("_"),
                PPKeyword("End"),
                PPKeyword("Region"),
                PPKeyword("#"),
                Punctuation.Text("_"),
                PPKeyword("Region"),
                Punctuation.Text("_"),
                [String]("""Test"""),
                PPKeyword("#"),
                Punctuation.Text("_"),
                PPKeyword("End"),
                Punctuation.Text("_"),
                PPKeyword("Region"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug2641_2() As Task
            Dim text = StringFromLines(
                "#ExternalSource(""Test.vb"", 123)",
                "#End ExternalSource",
                "#ExternalSource(""Test.vb"", 123) ' comment",
                "#End ExternalSource REM comment",
                "# _",
                "ExternalSource _",
                "( _",
                """Test.vb"" _",
                ", _",
                "123)",
                "# _",
                "End _",
                "ExternalSource")
            Await TestAsync(text,
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
                Punctuation.Text("_"),
                PPKeyword("ExternalSource"),
                Punctuation.Text("_"),
                Punctuation.OpenParen,
                Punctuation.Text("_"),
                [String]("""Test.vb"""),
                Punctuation.Text("_"),
                Punctuation.Comma,
                Punctuation.Text("_"),
                Number("123"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                Punctuation.Text("_"),
                PPKeyword("End"),
                Punctuation.Text("_"),
                PPKeyword("ExternalSource"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug2640() As Task
            Dim text = StringFromLines(
                "# _",
                "Region ""Test""",
                "# _",
                "End Region",
                "# _",
                "Region _",
                """Test""",
                "# _",
                "End _",
                "Region")
            Await TestAsync(text,
                PPKeyword("#"),
                Punctuation.Text("_"),
                PPKeyword("Region"),
                [String]("""Test"""),
                PPKeyword("#"),
                Punctuation.Text("_"),
                PPKeyword("End"),
                PPKeyword("Region"),
                PPKeyword("#"),
                Punctuation.Text("_"),
                PPKeyword("Region"),
                Punctuation.Text("_"),
                [String]("""Test"""),
                PPKeyword("#"),
                Punctuation.Text("_"),
                PPKeyword("End"),
                Punctuation.Text("_"),
                PPKeyword("Region"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug2638() As Task
            Dim text = StringFromLines(
                "Module M",
                "    Sub Main()",
                "        Dim dt = #1/1/2000#",
                "    End Sub",
                "End Module")
            Await TestAsync(text,
                Keyword("Module"),
                [Module]("M"),
                Keyword("Sub"),
                Identifier("Main"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Dim"),
                Identifier("dt"),
                Operators.Equals,
                Number("#1/1/2000#"),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestBug2562() As Task
            Dim text = StringFromLines(
                "Module Program",
                "  Sub Main(args As String())",
                "    #region ""Foo""",
                "    #End region REM dfkjslfkdsjf",
                "  End Sub",
                "End Module")
            Await TestAsync(text,
                Keyword("Module"),
                [Module]("Program"),
                Keyword("Sub"),
                Identifier("Main"),
                Punctuation.OpenParen,
                Identifier("args"),
                Keyword("As"),
                Keyword("String"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("region"),
                [String]("""Foo"""),
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
            Dim text = StringFromLines(
                "''' <summary>",
                "''' &#65;",
                "''' </summary>",
                "Module M",
                "End Module")
            Await TestAsync(text,
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
            Dim text = StringFromLines(
                "#If True Then ' comment",
                "#End If")
            Await TestAsync(text,
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
            Dim text = StringFromLines(
                "#If #12/2/2010# = #12/2/2010# Then",
                "#End If")
            Await TestAsync(text,
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
            Dim val = StringFromLines(
                "'This is not usually a ",
                "'collapsible comment block",
                "x = 2")

            Await TestInMethodAsync(val,
                         Comment("'This is not usually a "),
                         Comment("'collapsible comment block"),
                         Identifier("x"),
                         Operators.Equals,
                         Number("2"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAttribute() As Task
            Dim code = "<Assembly: Foo()>"
            Await TestAsync(code,
                 Punctuation.OpenAngle,
                 Keyword("Assembly"),
                 Punctuation.Colon,
                 Identifier("Foo"),
                 Punctuation.OpenParen,
                 Punctuation.CloseParen,
                 Punctuation.CloseAngle)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAngleBracketsOnGenericConstraints_Bug932262() As Task
            Await TestAsync(StringFromLines("Class C(Of T As A(Of T))",
                                 "End Class"),
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
            Dim text = StringFromLines(
                "    Sub CallMeInteger(ByVal [Integer] As Integer)",
                "        CallMeInteger(Integer:=1)",
                "        CallMeInteger(Integer _",
                "                      := _",
                "                      1)",
                "    End Sub",
                "    Dim [Class] As Integer")
            Await TestInClassAsync(text,
                Keyword("Sub"),
                Identifier("CallMeInteger"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Identifier("[Integer]"),
                Keyword("As"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Identifier("CallMeInteger"),
                Punctuation.OpenParen,
                Identifier("Integer"),
                Operators.Text(":="),
                Number("1"),
                Punctuation.CloseParen,
                Identifier("CallMeInteger"),
                Punctuation.OpenParen,
                Identifier("Integer"),
                Punctuation.Text("_"),
                Operators.Text(":="),
                Punctuation.Text("_"),
                Number("1"),
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Sub"),
                Keyword("Dim"),
                Identifier("[Class]"),
                Keyword("As"),
                Keyword("Integer"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIndexStrings() As Task
            Dim text = StringFromLines(
                "Default ReadOnly Property IndexMe(ByVal arg As String) As Integer",
                "        Get",
                "            With Me",
                "                Dim t = !String",
                "                t = ! _",
                "                    String",
                "                t = .Class",
                "                t = . _",
                "                    Class",
                "            End With",
                "        End Get",
                "    End Property")
            Await TestAsync(text,
                Keyword("Default"),
                Keyword("ReadOnly"),
                Keyword("Property"),
                Identifier("IndexMe"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Identifier("arg"),
                Keyword("As"),
                Keyword("String"),
                Punctuation.CloseParen,
                Keyword("As"),
                Keyword("Integer"),
                Keyword("Get"),
                Keyword("With"),
                Keyword("Me"),
                Keyword("Dim"),
                Identifier("t"),
                Operators.Equals,
                Operators.Exclamation,
                Identifier("String"),
                Identifier("t"),
                Operators.Equals,
                Operators.Exclamation,
                Punctuation.Text("_"),
                Identifier("String"),
                Identifier("t"),
                Operators.Equals,
                Operators.Dot,
                Identifier("Class"),
                Identifier("t"),
                Operators.Equals,
                Operators.Dot,
                Punctuation.Text("_"),
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
            Dim text = StringFromLines(
                "Dim My",
                "    Dim var = My.Application.GetEnvironmentVariable(""test"")",
                "    Sub CallMeMy(ByVal My As Integer)",
                "        CallMeMy(My:=1)",
                "        CallMeMy(My _",
                "                 := _",
                "                 1)",
                "        My.ToString()",
                "        With Me",
                "            .My = 1",
                "            . _",
                "            My _",
                "            = 1",
                "            !My = Nothing",
                "            ! _",
                "            My _",
                "            = Nothing",
                "        End With",
                "        Me.My.ToString()",
                "        Me. _",
                "        My.ToString()",
                "        Me.My = 1",
                "        Me. _",
                "        My = 1",
                "    End Sub")
            Await TestInClassAsync(text,
                Keyword("Dim"),
                Identifier("My"),
                Keyword("Dim"),
                Identifier("var"),
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
                Identifier("CallMeMy"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Identifier("My"),
                Keyword("As"),
                Keyword("Integer"),
                Punctuation.CloseParen,
                Identifier("CallMeMy"),
                Punctuation.OpenParen,
                Identifier("My"),
                Operators.Text(":="),
                Number("1"),
                Punctuation.CloseParen,
                Identifier("CallMeMy"),
                Punctuation.OpenParen,
                Identifier("My"),
                Punctuation.Text("_"),
                Operators.Text(":="),
                Punctuation.Text("_"),
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
                Punctuation.Text("_"),
                Identifier("My"),
                Punctuation.Text("_"),
                Operators.Equals,
                Number("1"),
                Operators.Exclamation,
                Identifier("My"),
                Operators.Equals,
                Keyword("Nothing"),
                Operators.Exclamation,
                Punctuation.Text("_"),
                Identifier("My"),
                Punctuation.Text("_"),
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
                Punctuation.Text("_"),
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
                Punctuation.Text("_"),
                Identifier("My"),
                Operators.Equals,
                Number("1"),
                Keyword("End"),
                Keyword("Sub"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIsTrueIsFalse() As Task
            Dim text = StringFromLines(
                "Class IsTrueIsFalseTests",
                "    Dim IsTrue",
                "    Dim IsFalse",
                "    Shared Operator IsTrue(ByVal x As IsTrueIsFalseTests) As Boolean",
                "    End Operator",
                "    Shared Operator IsFalse(ByVal x As IsTrueIsFalseTests) As Boolean",
                "    End Operator",
                "End Class")
            Await TestInClassAsync(text,
                Keyword("Class"),
                [Class]("IsTrueIsFalseTests"),
                Keyword("Dim"),
                Identifier("IsTrue"),
                Keyword("Dim"),
                Identifier("IsFalse"),
                Keyword("Shared"),
                Keyword("Operator"),
                Keyword("IsTrue"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Identifier("x"),
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
                Identifier("x"),
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
            Dim text = StringFromLines(
                "    Dim Ansi",
                "    Dim Unicode",
                "    Dim Auto",
                "    Declare Ansi Sub AnsiTest Lib ""Test.dll"" ()",
                "    Declare Auto Sub AutoTest Lib ""Test.dll"" ()",
                "    Declare Unicode Sub UnicodeTest Lib ""Test.dll"" ()",
                "    Declare _",
                "        Ansi Sub AnsiTest2 Lib ""Test.dll"" ()",
                "    Declare _",
                "        Auto Sub AutoTest2 Lib ""Test.dll"" ()",
                "    Declare _",
                "        Unicode Sub UnicodeTest2 Lib ""Test.dll"" ()")
            Await TestInClassAsync(text,
                Keyword("Dim"),
                Identifier("Ansi"),
                Keyword("Dim"),
                Identifier("Unicode"),
                Keyword("Dim"),
                Identifier("Auto"),
                Keyword("Declare"),
                Keyword("Ansi"),
                Keyword("Sub"),
                Identifier("AnsiTest"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                Keyword("Auto"),
                Keyword("Sub"),
                Identifier("AutoTest"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                Keyword("Unicode"),
                Keyword("Sub"),
                Identifier("UnicodeTest"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                Punctuation.Text("_"),
                Keyword("Ansi"),
                Keyword("Sub"),
                Identifier("AnsiTest2"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                Punctuation.Text("_"),
                Keyword("Auto"),
                Keyword("Sub"),
                Identifier("AutoTest2"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Declare"),
                Punctuation.Text("_"),
                Keyword("Unicode"),
                Keyword("Sub"),
                Identifier("UnicodeTest2"),
                Keyword("Lib"),
                [String]("""Test.dll"""),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestUntil() As Task
            Dim text = StringFromLines(
                "    Dim Until",
                "    Sub TestSub()",
                "        Do",
                "        Loop Until True",
                "        Do",
                "        Loop _",
                "        Until True",
                "        Do Until True",
                "        Loop",
                "        Do _",
                "        Until True",
                "        Loop",
                "    End Sub")
            Await TestInClassAsync(text,
                Keyword("Dim"),
                Identifier("Until"),
                Keyword("Sub"),
                Identifier("TestSub"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Do"),
                Keyword("Loop"),
                Keyword("Until"),
                Keyword("True"),
                Keyword("Do"),
                Keyword("Loop"),
                Punctuation.Text("_"),
                Keyword("Until"),
                Keyword("True"),
                Keyword("Do"),
                Keyword("Until"),
                Keyword("True"),
                Keyword("Loop"),
                Keyword("Do"),
                Punctuation.Text("_"),
                Keyword("Until"),
                Keyword("True"),
                Keyword("Loop"),
                Keyword("End"),
                Keyword("Sub"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestPreserve() As Task
            Dim text = StringFromLines(
                "    Dim Preserve",
                "    Sub TestSub()",
                "        Dim arr As Integer() = Nothing",
                "        ReDim Preserve arr(0)",
                "        ReDim _",
                "        Preserve arr(0)",
                "    End Sub")
            Await TestInClassAsync(text,
                Keyword("Dim"),
                Identifier("Preserve"),
                Keyword("Sub"),
                Identifier("TestSub"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Dim"),
                Identifier("arr"),
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
                Punctuation.Text("_"),
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
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Test(ByVal readOnly As Boolean)",
                "    End Sub",
                "End Module")
            Await TestAsync(text,
                Keyword("Module"),
                [Module]("Program"),
                Keyword("Sub"),
                Identifier("Test"),
                Punctuation.OpenParen,
                Keyword("ByVal"),
                Keyword("readOnly"),
                Identifier("As"),
                Keyword("Boolean"),
                Punctuation.CloseParen,
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(538647)>
        Public Async Function TestRegression4315_VariableNamesClassifiedAsType() As Task
            Dim text = StringFromLines(
                "Module M",
                "    Sub S()",
                "        Dim foo",
                "    End Sub",
                "End Module")
            Await TestAsync(text,
                Keyword("Module"),
                [Module]("M"),
                Keyword("Sub"),
                Identifier("S"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Keyword("Dim"),
                Identifier("foo"),
                Keyword("End"),
                Keyword("Sub"),
                Keyword("End"),
                Keyword("Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539203)>
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
        <WorkItem(539642)>
        Public Async Function TestFromInCollectionInitializer1() As Task
            Await TestInMethodAsync("Dim y = New Foo() From",
                         Keyword("Dim"),
                         Identifier("y"),
                         Operators.Equals,
                         Keyword("New"),
                         Identifier("Foo"),
                         Punctuation.OpenParen,
                         Punctuation.CloseParen,
                         Keyword("From"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539642)>
        Public Async Function TestFromInCollectionInitializer2() As Task
            Await TestInMethodAsync("Dim y As New Foo() From",
                         Keyword("Dim"),
                         Identifier("y"),
                         Keyword("As"),
                         Keyword("New"),
                         Identifier("Foo"),
                         Punctuation.OpenParen,
                         Punctuation.CloseParen,
                         Keyword("From"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Async Function TestPartiallyTypedXmlNamespaceImport1() As Task
            Await TestAsync("Imports <x",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlName("x"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Async Function TestPartiallyTypedXmlNamespaceImport2() As Task
            Await TestAsync("Imports <xml",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlName("xml"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Async Function TestPartiallyTypedXmlNamespaceImport3() As Task
            Await TestAsync("Imports <xmlns",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlAttributeName("xmlns"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Async Function TestPartiallyTypedXmlNamespaceImport4() As Task
            Await TestAsync("Imports <xmlns:",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlAttributeName("xmlns"),
                 VBXmlAttributeName(":"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Async Function TestPartiallyTypedXmlNamespaceImport5() As Task
            Await TestAsync("Imports <xmlns:ns",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlAttributeName("xmlns"),
                 VBXmlAttributeName(":"),
                 VBXmlAttributeName("ns"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
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
        <WorkItem(539779)>
        Public Async Function TestPartiallyTypedXmlNamespaceImport7() As Task
            Await TestAsync("Imports <xmlns:ns=""http://foo""",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlAttributeName("xmlns"),
                 VBXmlAttributeName(":"),
                 VBXmlAttributeName("ns"),
                 VBXmlDelimiter("="),
                 VBXmlAttributeQuotes(""""),
                 VBXmlAttributeValue("http://foo"),
                 VBXmlAttributeQuotes(""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Async Function TestFullyTypedXmlNamespaceImport() As Task
            Await TestAsync("Imports <xmlns:ns=""http://foo"">",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlAttributeName("xmlns"),
                 VBXmlAttributeName(":"),
                 VBXmlAttributeName("ns"),
                 VBXmlDelimiter("="),
                 VBXmlAttributeQuotes(""""),
                 VBXmlAttributeValue("http://foo"),
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
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Main",
                "#Enable Warning BC123, [bc456], SomeId",
                "    End Sub",
                "End Module")
            Await TestAsync(text,
                 Keyword("Module"),
                 [Module]("Program"),
                 Keyword("Sub"),
                 Identifier("Main"),
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
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Main",
                "#Disable Warning",
                "    End Sub",
                "End Module")
            Await TestAsync(text,
                 Keyword("Module"),
                 [Module]("Program"),
                 Keyword("Sub"),
                 Identifier("Main"),
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
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Main",
                "#warning",
                "    End Sub",
                "#Enable blah Warning",
                "End Module",
                "#Disable bc123 Warning",
                "#Enable",
                "#Disable Warning blah")
            Await TestAsync(text,
                 Keyword("Module"),
                 [Module]("Program"),
                 Keyword("Sub"),
                 Identifier("Main"),
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
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Main",
                "        Dim s = $""Hello, {name,10:F}.""",
                "    End Sub",
                "End Module")
            Await TestAsync(text,
                 Keyword("Module"),
                 [Module]("Program"),
                 Keyword("Sub"),
                 Identifier("Main"),
                 Keyword("Dim"),
                 Identifier("s"),
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
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Main",
                "        Dim s = $""{x}, {y}""",
                "    End Sub",
                "End Module")
            Await TestAsync(text,
                 Keyword("Module"),
                 [Module]("Program"),
                 Keyword("Sub"),
                 Identifier("Main"),
                 Keyword("Dim"),
                 Identifier("s"),
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
            Await TestInMethodAsync("C",
                         "M",
                         " ' Comment
                           x.@Name = ""Text""",
                         "' Comment",
                         Comment("' Comment"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(3291, "https://github.com/dotnet/roslyn/issues/3291")>
        Public Async Function TestCommentOnCollapsedEndRegion() As Task
            Await TestAsync(
"#Region ""Stuff""
End Region ' Stuff",
TextSpan.FromBounds(28, 36),
Comment("' Stuff"))
        End Function
    End Class
End Namespace
