' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    Public Class SyntacticClassifierTests
        Inherits AbstractVisualBasicClassifierTests

        Friend Overrides Function GetClassificationSpans(code As String, textSpan As TextSpan) As IEnumerable(Of ClassifiedSpan)
            Using Workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(code)
                Dim document = Workspace.CurrentSolution.Projects.First().Documents.First()
                Dim tree = document.GetSyntaxTreeAsync().Result

                Dim service = document.GetLanguageService(Of IClassificationService)()
                Dim result = New List(Of ClassifiedSpan)
                service.AddSyntacticClassifications(tree, textSpan, result, CancellationToken.None)

                Return result
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlStartElementName1()
            TestInExpression("<foo></foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlStartElementName2()
            TestInExpression("<foo",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlStartElementName3()
            TestInExpression("<foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlStartElementName4()
            TestInExpression("<foo.",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo."))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlStartElementName5()
            TestInExpression("<foo.b",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo.b"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlStartElementName6()
            TestInExpression("<foo.b>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo.b"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlStartElementName7()
            TestInExpression("<foo:",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlStartElementName8()
            TestInExpression("<foo:b",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"),
                             VBXmlName("b"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlStartElementName9()
            TestInExpression("<foo:b>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"),
                             VBXmlName("b"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmptyElementName1()
            TestInExpression("<foo/>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmptyElementName2()
            TestInExpression("<foo. />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo."),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmptyElementName3()
            TestInExpression("<foo.bar />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo.bar"),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmptyElementName4()
            TestInExpression("<foo: />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmptyElementName5()
            TestInExpression("<foo:bar />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlName(":"),
                             VBXmlName("bar"),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeName1()
            TestInExpression("<foo b",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("b"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeName2()
            TestInExpression("<foo ba",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("ba"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeName3()
            TestInExpression("<foo bar=",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeValue1()
            TestInExpression("<foo bar=""",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeValue2()
            TestInExpression("<foo bar=""b",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeValue("b" & vbCrLf))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeValue3()
            TestInExpression("<foo bar=""ba",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeValue("ba" & vbCrLf))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeValue4()
            TestInExpression("<foo bar=""ba""",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeValue("ba"),
                             VBXmlAttributeQuotes(""""))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeValue5()
            TestInExpression("<foo bar=""""",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeQuotes(""""))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeValue6()
            TestInExpression("<foo bar=""b""",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeValue("b"),
                             VBXmlAttributeQuotes(""""))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeValue7()
            TestInExpression("<foo bar=""ba""",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlAttributeQuotes(""""),
                             VBXmlAttributeValue("ba"),
                             VBXmlAttributeQuotes(""""))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeValueMultiple1()
            TestInExpression("<foo bar=""ba"" baz="""" ",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeValueMultiple2()
            TestInExpression("<foo bar=""ba"" baz=""a"" ",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElementContent1()
            TestInExpression("<f>&l</f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlEntityReference("&"),
                             VBXmlText("l"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElementContent2()
            TestInExpression("<f>foo</f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlText("foo"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElementContent3()
            TestInExpression("<f>&#x03C0;</f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlEntityReference("&#x03C0;"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElementContent4()
            TestInExpression("<f>foo &#x03C0;</f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlText("foo "),
                             VBXmlEntityReference("&#x03C0;"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElementContent5()
            TestInExpression("<f>foo &lt;</f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlText("foo "),
                             VBXmlEntityReference("&lt;"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElementContent6()
            TestInExpression("<f>foo &lt; bar</f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlText("foo "),
                             VBXmlEntityReference("&lt;"),
                             VBXmlText(" bar"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElementContent7()
            TestInExpression("<f>foo &lt;",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlText("foo "),
                             VBXmlEntityReference("&lt;"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlCData1()
            TestInExpression("<f><![CDATA[bar]]></f>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("<![CDATA["),
                             VBXmlCDataSection("bar"),
                             VBXmlDelimiter("]]>"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlCData4()
            TestInExpression("<f><![CDATA[bar]]>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("<![CDATA["),
                             VBXmlCDataSection("bar"),
                             VBXmlDelimiter("]]>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlCData5()
            TestInExpression("<f><![CDATA[<>/]]>",
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("<![CDATA["),
                             VBXmlCDataSection("<>/"),
                             VBXmlDelimiter("]]>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlCData6()
            Dim expr = StringFromLines(
                "<f><![CDATA[foo",
                "baz]]></f>")
            TestInExpression(expr,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAtElementName1()
            TestInExpression("<<%= ",
                             VBXmlDelimiter("<"),
                             VBXmlEmbeddedExpression("<%="))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAtElementName2()
            TestInExpression("<<%= %>",
                             VBXmlDelimiter("<"),
                             VBXmlEmbeddedExpression("<%="),
                             VBXmlEmbeddedExpression("%>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAtElementName3()
            TestInExpression("<<%= bar %>",
                             VBXmlDelimiter("<"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAtElementName4()
            TestInExpression("<<%= bar.Baz() %>",
                             VBXmlDelimiter("<"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             Operators.Dot,
                             Identifier("Baz"),
                             Punctuation.OpenParen,
                             Punctuation.CloseParen,
                             VBXmlEmbeddedExpression("%>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAtElementName5()
            TestInExpression("<<%= bar.Baz() %> />",
                             VBXmlDelimiter("<"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             Operators.Dot,
                             Identifier("Baz"),
                             Punctuation.OpenParen,
                             Punctuation.CloseParen,
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAtElementName6()
            TestInExpression("<<%= bar %> />",
                             VBXmlDelimiter("<"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsAttribute1()
            TestInExpression("<foo <%= bar %>>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsAttribute2()
            TestInExpression("<foo <%= bar %>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsAttribute3()
            TestInExpression("<foo <%= bar %>></foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsAttribute4()
            TestInExpression("<foo <%= bar %> />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsAttributeValue1()
            Dim exprText = "<foo bar=<%=baz >"
            TestInExpression(exprText,
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("baz"),
                             Operators.GreaterThan)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsAttributeValue2()
            Dim exprText = "<foo bar=<%=baz %> >"
            TestInExpression(exprText,
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("bar"),
                             VBXmlDelimiter("="),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("baz"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsAttributeValue3()
            Dim exprText = "<foo bar=<%=baz.Foo %> >"
            TestInExpression(exprText,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsElementContent1()
            Dim exprText = "<f><%= bar %></f>"
            TestInExpression(exprText,
                             VBXmlDelimiter("<"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter("</"),
                             VBXmlName("f"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsElementContent2()
            Dim exprText = "<f><%= bar.Foo %></f>"
            TestInExpression(exprText,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsElementContent3()
            Dim exprText = "<f><%= bar.Foo %> jaz</f>"
            TestInExpression(exprText,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbeddedExpressionAsElementContentNested()
            Dim text = StringFromLines(
                "Dim doc = _",
                "    <foo>",
                "        <%= <bug141>",
                "                <a>hello</a>",
                "            </bug141> %>",
                "    </foo>")
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlLiteralsInLambdas()
            Dim text = StringFromLines(
                "Dim x = Function() _",
                "                    <element val=""something""/>",
                "        Dim y = Function() <element val=""something""/>")
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocumentPrologue()
            Dim exprText = "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>"
            TestInExpression(exprText,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlLiterals1()
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
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlLiterals2()
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
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlLiterals3()
            Dim text = StringFromLines(
                "Dim c = <p:x xmlns:p=""abc",
                "123""/>")
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlLiterals4()
            Dim text = StringFromLines(
                "Dim d = _",
                "        <?xml version=""1.0""?>",
                "        <a/>")
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlLiterals5()
            Dim text = StringFromLines(
                "Dim i = 100",
                "        Process( _",
                "                <Customer ID=<%= i + 1000 %> a="""">",
                "                </Customer>)")
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlLiterals6()
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
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlLiterals7()
            Dim text = StringFromLines(
                "Dim spacetest = <a b=""1"" c=""2"">",
                "                        </a>")
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub OptionKeywordsInClassContext()
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
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub OptionInferAndExplicit()
            Dim text = StringFromLines(
                "Option Infer On",
                "Option Explicit Off")
            Test(text,
                Keyword("Option"),
                Keyword("Infer"),
                Keyword("On"),
                Keyword("Option"),
                Keyword("Explicit"),
                Keyword("Off"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub OptionCompareTextBinary()
            Dim text = StringFromLines(
                "Option Compare Text ' comment",
                "Option Compare Binary ")
            Test(text,
                Keyword("Option"),
                Keyword("Compare"),
                Keyword("Text"),
                Comment("' comment"),
                Keyword("Option"),
                Keyword("Compare"),
                Keyword("Binary"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub OptionInfer1()
            Test("Option Infer",
                 Keyword("Option"),
                 Keyword("Infer"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub OptionExplicit1()
            Test("Option Explicit",
                 Keyword("Option"),
                 Keyword("Explicit"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub OptionStrict1()
            Test("Option Strict",
                 Keyword("Option"),
                 Keyword("Strict"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub LinqContextualKeywords()
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
            TestInClass(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub FromLinqExpression1()
            TestInExpression("From it in foo",
                 Keyword("From"),
                 Identifier("it"),
                 Keyword("in"),
                 Identifier("foo"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub FromLinqExpression2()
            TestInExpression("From it in foofooo.Foo",
                 Keyword("From"),
                 Identifier("it"),
                 Keyword("in"),
                 Identifier("foofooo"),
                 Operators.Dot,
                 Identifier("Foo"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub FromLinqExpression3()
            TestInExpression("From it ",
                 Keyword("From"),
                 Identifier("it"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub FromNotInContext1()
            Dim code = StringFromLines(
                "Class From",
                "End Class")
            Test(code,
                 Keyword("Class"),
                 [Class]("From"),
                 Keyword("End"),
                 Keyword("Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub FromNotInContext2()
            Dim val = "Dim from = 42"
            TestInMethod(val,
                         Keyword("Dim"),
                         Identifier("from"),
                         Operators.Equals,
                         Number("42"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub WhereLinqExpression1()
            Dim exprTest = "From it in foo Where it <> 4"
            TestInExpression(exprTest,
                 Keyword("From"),
                 Identifier("it"),
                 Keyword("in"),
                 Identifier("foo"),
                 Keyword("Where"),
                 Identifier("it"),
                 Operators.LessThanGreaterThan,
                 Number("4"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub LinqQuery1()
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
            TestInMethod(text,
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
        End Sub

        <WorkItem(542387)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestFromInQuery()
            Dim text = StringFromLines(
                "Dim From = New List(Of Integer)",
                "Dim result = From s In From Select s")
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub KeyKeyword1()
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
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub KeyKeyword2()
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
            TestInMethod(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub ClassDeclaration1()
            Dim val = "Class C1"
            Test(val,
                 Keyword("Class"),
                 [Class]("C1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub ClassDeclaration2()
            Dim val = StringFromLines(
                "Class C1",
                "End Class")
            Test(val,
                 Keyword("Class"),
                 [Class]("C1"),
                 Keyword("End"),
                 Keyword("Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub ClassDeclaration3()
            Dim val = "Class C1 : End Class"
            Test(val,
                 Keyword("Class"),
                 [Class]("C1"),
                 Punctuation.Colon,
                 Keyword("End"),
                 Keyword("Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub StructDeclaration1()
            Dim val = "Structure S1"
            Test(val,
                 Keyword("Structure"),
                 Struct("S1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub StructDeclaration2()
            Dim val = "Structure S1 : End Structure"
            Test(val,
                 Keyword("Structure"),
                 Struct("S1"),
                 Punctuation.Colon,
                 Keyword("End"),
                 Keyword("Structure"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub StructDeclaration3()
            Dim val = StringFromLines(
                "Structure S1",
                "End Structure")
            Test(val,
                 Keyword("Structure"),
                 Struct("S1"),
                 Keyword("End"),
                 Keyword("Structure"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub InterfaceDeclaration1()
            Dim val = "Interface I1"
            Test(val,
                 Keyword("Interface"),
                 [Interface]("I1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub InterfaceDeclaration2()
            Dim val = "Interface I1 : End Interface"
            Test(val,
                 Keyword("Interface"),
                 [Interface]("I1"),
                 Punctuation.Colon,
                 Keyword("End"),
                 Keyword("Interface"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub InterfaceDeclaration3()
            Dim val = StringFromLines(
                "Interface I1",
                "End Interface")
            Test(val,
                 Keyword("Interface"),
                 [Interface]("I1"),
                 Keyword("End"),
                 Keyword("Interface"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub EnumDeclaration1()
            Dim val = "Enum E1"
            Test(val,
                 Keyword("Enum"),
                 [Enum]("E1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub EnumDeclaration2()
            Dim val = "Enum E1 : End Enum"
            Test(val,
                 Keyword("Enum"),
                 [Enum]("E1"),
                 Punctuation.Colon,
                 Keyword("End"),
                 Keyword("Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub EnumDeclaration3()
            Dim val = StringFromLines(
                "Enum E1",
                "End Enum")
            Test(val,
                 Keyword("Enum"),
                 [Enum]("E1"),
                 Keyword("End"),
                 Keyword("Enum"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub DelegateSubDeclaration1()
            Dim val = StringFromLines("Public Delegate Sub Foo()")
            Test(val,
                 Keyword("Public"),
                 Keyword("Delegate"),
                 Keyword("Sub"),
                 [Delegate]("Foo"),
                 Punctuation.OpenParen,
                 Punctuation.CloseParen)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub DelegateFunctionDeclaration1()
            Dim val = StringFromLines("Public Delegate Function Foo() As Integer")
            Test(val,
                 Keyword("Public"),
                 Keyword("Delegate"),
                 Keyword("Function"),
                 [Delegate]("Foo"),
                 Punctuation.OpenParen,
                 Punctuation.CloseParen,
                 Keyword("As"),
                 Keyword("Integer"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub FloatLiteral()
            TestInExpression("1.0",
                Number("1.0"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IntLiteral()
            TestInExpression("1",
                Number("1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub DecimalLiteral()
            TestInExpression("123D",
                Number("123D"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub StringLiterals1()
            Dim exprText = """foo"""
            TestInExpression(exprText,
                             [String]("""foo"""))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub CharacterLiteral()
            Dim exprText = """f""c"
            TestInExpression(exprText,
                             [String]("""f""c"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Regression_DoUntil1()
            Dim val = "Do Until True"
            TestInMethod(val,
                         Keyword("Do"),
                         Keyword("Until"),
                         Keyword("True"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Comment1()
            Dim code = "'foo"
            Test(code,
                 Comment("'foo"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Comment2()
            Dim val = StringFromLines(
                "Class C1",
                "'hello")
            Test(val,
                 Keyword("Class"),
                 [Class]("C1"),
                 Comment("'hello"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_SingleLine()
            Dim val = StringFromLines(
                "'''<summary>something</summary>",
                "Class Bar",
                "End Class")
            Test(val,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_ExteriorTrivia()
            Dim val = StringFromLines(
                "''' <summary>",
                "''' something",
                "''' </summary>",
                "Class Bar",
                "End Class")
            Test(val,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_ExteriorTriviaInsideEndTag()
            Dim val = StringFromLines(
                "''' <summary></",
                "''' summary>",
                "Class Bar",
                "End Class")
            Test(val,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_AttributesWithExteriorTrivia()
            Dim val = StringFromLines(
                "''' <summary att1=""value1""",
                "''' att2=""value2"">",
                "''' something",
                "''' </summary>",
                "Class Bar",
                "End Class")
            Test(val,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_EmptyElementAttributesWithExteriorTrivia()
            Dim val = StringFromLines(
                "''' <summary att1=""value1""",
                "''' att2=""value2"" />",
                "Class Bar",
                "End Class")
            Test(val,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_XmlCommentWithExteriorTrivia()
            Dim val = StringFromLines(
                "'''<summary>",
                "'''<!--first",
                "'''second-->",
                "'''</summary>",
                "Class Bar",
                "End Class")
            Test(val,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_CDataWithExteriorTrivia()
            Dim val = StringFromLines(
                "'''<summary>",
                "'''<![CDATA[first",
                "'''second]]>",
                "'''</summary>",
                "Class Bar",
                "End Class")
            Test(val,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_PreprocessingInstruction1()
            Test("''' <?",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_PreprocessingInstruction2()
            Test("''' <??>",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("?>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_PreprocessingInstruction3()
            Test("''' <?xml",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("xml"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_PreprocessingInstruction4()
            Test("''' <?xml version=""1.0""?>",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("xml"),
                XmlDoc.ProcessingInstruction(" "),
                XmlDoc.ProcessingInstruction("version=""1.0"""),
                XmlDoc.ProcessingInstruction("?>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_PreprocessingInstruction5()
            Test("''' <?foo?>",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("foo"),
                XmlDoc.ProcessingInstruction("?>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDocComment_PreprocessingInstruction6()
            Test("''' <?foo bar?>",
                XmlDoc.Delimiter("'''"),
                XmlDoc.Text(" "),
                XmlDoc.ProcessingInstruction("<?"),
                XmlDoc.ProcessingInstruction("foo"),
                XmlDoc.ProcessingInstruction(" "),
                XmlDoc.ProcessingInstruction("bar"),
                XmlDoc.ProcessingInstruction("?>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IsTrue()
            TestInClass("    Public Shared Operator IsTrue(c As C) As Boolean",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IsFalse()
            TestInClass("    Public Shared Operator IsFalse(c As C) As Boolean",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Delegate1()
            Test("Delegate Sub Foo()",
                 Keyword("Delegate"),
                 Keyword("Sub"),
                 [Delegate]("Foo"),
                 Punctuation.OpenParen,
                 Punctuation.CloseParen)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Imports1()
            Dim code = StringFromLines(
            "Imports Foo",
            "Imports Bar")
            Test(code,
                 Keyword("Imports"),
                 Identifier("Foo"),
                 Keyword("Imports"),
                 Identifier("Bar"))
        End Sub

        ''' <summary>
        ''' Clear Syntax Error
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Imports2()
            Dim code = StringFromLines(
            "Imports",
            "Imports Bar")
            Test(code,
                 Keyword("Imports"),
                 Keyword("Imports"),
                 Identifier("Bar"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Imports3()
            Dim code = StringFromLines(
            "Imports Foo=Baz",
            "Imports Bar=Quux")
            Test(code,
                 Keyword("Imports"),
                 Identifier("Foo"),
                 Operators.Equals,
                 Identifier("Baz"),
                 Keyword("Imports"),
                 Identifier("Bar"),
                 Operators.Equals,
                 Identifier("Quux"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Imports4()
            Dim code = "Imports System.Text"
            Test(code,
                 Keyword("Imports"),
                 Identifier("System"),
                 Operators.Dot,
                 Identifier("Text"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElement1()
            TestInExpression("<foo></foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Sub

        '''<summary>
        ''' Broken XmlElement should classify
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElement3()
            TestInExpression("<foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Sub

        '''<summary>
        ''' Broken end only element should still classify
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElement4()
            TestInExpression("</foo>",
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElement5()
            TestInExpression("<foo.bar></foo.bar>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo.bar"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo.bar"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElement6()
            TestInExpression("<foo:bar>hello</foo:bar>",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElement7()
            TestInExpression("<foo.bar />",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo.bar"),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbedded1()
            TestInExpression("<foo><%= bar %></foo>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter("</"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbedded3()
            TestInExpression("<<%= bar %>/>",
                             VBXmlDelimiter("<"),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbedded4()
            TestInExpression("<foo <%= bar %>=""42""/>",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlEmbedded5()
            TestInExpression("<foo a1=<%= bar %>/>",
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlAttributeName("a1"),
                             VBXmlDelimiter("="),
                             VBXmlEmbeddedExpression("<%="),
                             Identifier("bar"),
                             VBXmlEmbeddedExpression("%>"),
                             VBXmlDelimiter("/>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlComment1()
            TestInExpression("<!---->",
                             VBXmlDelimiter("<!--"),
                             VBXmlDelimiter("-->"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlComment2()
            TestInExpression("<!--foo-->",
                             VBXmlDelimiter("<!--"),
                             VBXmlComment("foo"),
                             VBXmlDelimiter("-->"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlComment3()
            Dim tree = ParseExpression("<a><!--foo--></a>")
            TestInExpression("<a><!--foo--></a>",
                             VBXmlDelimiter("<"),
                             VBXmlName("a"),
                             VBXmlDelimiter(">"),
                             VBXmlDelimiter("<!--"),
                             VBXmlComment("foo"),
                             VBXmlDelimiter("-->"),
                             VBXmlDelimiter("</"),
                             VBXmlName("a"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlPreprocessingInstruction2()
            TestInExpression("<a><?pi value=2?></a>",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlDescendantsMemberAccess1()
            TestInExpression("x...<foo>",
                             Identifier("x"),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlElementMemberAccess1()
            TestInExpression("x.<foo>",
                             Identifier("x"),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("<"),
                             VBXmlName("foo"),
                             VBXmlDelimiter(">"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeMemberAccess1()
            TestInExpression("x.@foo",
                             Identifier("x"),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("@"),
                             VBXmlAttributeName("foo"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub XmlAttributeMemberAccess2()
            TestInExpression("x.@foo:bar",
                             Identifier("x"),
                             VBXmlDelimiter("."),
                             VBXmlDelimiter("@"),
                             VBXmlAttributeName("foo"),
                             VBXmlAttributeName(":"),
                             VBXmlAttributeName("bar"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub PreprocessorConst1()
            TestInNamespace("#Const Foo = 1",
                            PPKeyword("#"),
                            PPKeyword("Const"),
                            Identifier("Foo"),
                            Operators.Equals,
                            Number("1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub PreprocessorConst2()
            TestInNamespace("#Const DebugCode = True",
                            PPKeyword("#"),
                            PPKeyword("Const"),
                            Identifier("DebugCode"),
                            Operators.Equals,
                            Keyword("True"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub PreprocessorIfThen1()
            TestInNamespace("#If Foo Then",
                            PPKeyword("#"),
                            PPKeyword("If"),
                            Identifier("Foo"),
                            PPKeyword("Then"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub PreprocessorElseIf1()
            TestInNamespace("#ElseIf Foo Then",
                            PPKeyword("#"),
                            PPKeyword("ElseIf"),
                            Identifier("Foo"),
                            PPKeyword("Then"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub PreprocessorElse1()
            TestInNamespace("#Else",
                            PPKeyword("#"),
                            PPKeyword("Else"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub PreprocessorEndIf1()
            TestInNamespace("#End If",
                            PPKeyword("#"),
                            PPKeyword("End"),
                            PPKeyword("If"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub PreprocessorExternalSource1()
            TestInNamespace("#ExternalSource(""c:\wwwroot\inetpub\test.aspx"", 30)",
                            PPKeyword("#"),
                            PPKeyword("ExternalSource"),
                            Punctuation.OpenParen,
                            [String]("""c:\wwwroot\inetpub\test.aspx"""),
                            Punctuation.Comma,
                            Number("30"),
                            Punctuation.CloseParen)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub PreprocessorExternalChecksum1()
            Dim val = StringFromLines("#ExternalChecksum(""c:\wwwroot\inetpub\test.aspx"", _",
                                      """{12345678-1234-1234-1234-123456789abc}"", _",
                                      """1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484"")")
            TestInNamespace(val,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub PreprocessorExternalChecksum2()
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
            TestInNamespace(val,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Bug2641_1()
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
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Bug2641_2()
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
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Bug2640()
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
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Bug2638()
            Dim text = StringFromLines(
                "Module M",
                "    Sub Main()",
                "        Dim dt = #1/1/2000#",
                "    End Sub",
                "End Module")
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Bug2562()
            Dim text = StringFromLines(
                "Module Program",
                "  Sub Main(args As String())",
                "    #region ""Foo""",
                "    #End region REM dfkjslfkdsjf",
                "  End Sub",
                "End Module")
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Bug3004()
            Dim text = StringFromLines(
                "''' <summary>",
                "''' &#65;",
                "''' </summary>",
                "Module M",
                "End Module")
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Bug3006()
            Dim text = StringFromLines(
                "#If True Then ' comment",
                "#End If")
            Test(text,
                PPKeyword("#"),
                PPKeyword("If"),
                Keyword("True"),
                PPKeyword("Then"),
                Comment("' comment"),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("If"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Bug3008()
            Dim text = StringFromLines(
                "#If #12/2/2010# = #12/2/2010# Then",
                "#End If")
            Test(text,
                PPKeyword("#"),
                PPKeyword("If"),
                Number("#12/2/2010#"),
                Operators.Equals,
                Number("#12/2/2010#"),
                PPKeyword("Then"),
                PPKeyword("#"),
                PPKeyword("End"),
                PPKeyword("If"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestBug927678()
            Dim val = StringFromLines(
                "'This is not usually a ",
                "'collapsible comment block",
                "x = 2")

            TestInMethod(val,
                         Comment("'This is not usually a "),
                         Comment("'collapsible comment block"),
                         Identifier("x"),
                         Operators.Equals,
                         Number("2"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Attribute()
            Dim code = "<Assembly: Foo()>"
            Test(code,
                 Punctuation.OpenAngle,
                 Keyword("Assembly"),
                 Punctuation.Colon,
                 Identifier("Foo"),
                 Punctuation.OpenParen,
                 Punctuation.CloseParen,
                 Punctuation.CloseAngle)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestAngleBracketsOnGenericConstraints_Bug932262()
            Test(StringFromLines("Class C(Of T As A(Of T))",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IntegerAsContextualKeyword()
            Dim text = StringFromLines(
                "    Sub CallMeInteger(ByVal [Integer] As Integer)",
                "        CallMeInteger(Integer:=1)",
                "        CallMeInteger(Integer _",
                "                      := _",
                "                      1)",
                "    End Sub",
                "    Dim [Class] As Integer")
            TestInClass(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IndexStrings()
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
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub MyIsIdentifierOnSyntaxLevel()
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
            TestInClass(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IsTrueIsFalse()
            Dim text = StringFromLines(
                "Class IsTrueIsFalseTests",
                "    Dim IsTrue",
                "    Dim IsFalse",
                "    Shared Operator IsTrue(ByVal x As IsTrueIsFalseTests) As Boolean",
                "    End Operator",
                "    Shared Operator IsFalse(ByVal x As IsTrueIsFalseTests) As Boolean",
                "    End Operator",
                "End Class")
            TestInClass(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub DeclareAnsiAutoUnicode()
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
            TestInClass(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Until()
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
            TestInClass(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub Preserve()
            Dim text = StringFromLines(
                "    Dim Preserve",
                "    Sub TestSub()",
                "        Dim arr As Integer() = Nothing",
                "        ReDim Preserve arr(0)",
                "        ReDim _",
                "        Preserve arr(0)",
                "    End Sub")
            TestInClass(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub SkippedTextAsTokens()
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Test(ByVal readOnly As Boolean)",
                "    End Sub",
                "End Module")
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(538647)>
        Public Sub Regression4315_VariableNamesClassifiedAsType()
            Dim text = StringFromLines(
                "Module M",
                "    Sub S()",
                "        Dim foo",
                "    End Sub",
                "End Module")
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539203)>
        Public Sub ColonTrivia()
            TestInMethod("    : Console.WriteLine()",
                         Punctuation.Colon,
                         Identifier("Console"),
                         Operators.Dot,
                         Identifier("WriteLine"),
                         Punctuation.OpenParen,
                         Punctuation.CloseParen)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539642)>
        Public Sub FromInCollectionInitializer1()
            TestInMethod("Dim y = New Foo() From",
                         Keyword("Dim"),
                         Identifier("y"),
                         Operators.Equals,
                         Keyword("New"),
                         Identifier("Foo"),
                         Punctuation.OpenParen,
                         Punctuation.CloseParen,
                         Keyword("From"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539642)>
        Public Sub FromInCollectionInitializer2()
            TestInMethod("Dim y As New Foo() From",
                         Keyword("Dim"),
                         Identifier("y"),
                         Keyword("As"),
                         Keyword("New"),
                         Identifier("Foo"),
                         Punctuation.OpenParen,
                         Punctuation.CloseParen,
                         Keyword("From"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Sub TestPartiallyTypedXmlNamespaceImport1()
            Test("Imports <x",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlName("x"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Sub TestPartiallyTypedXmlNamespaceImport2()
            Test("Imports <xml",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlName("xml"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Sub TestPartiallyTypedXmlNamespaceImport3()
            Test("Imports <xmlns",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlAttributeName("xmlns"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Sub TestPartiallyTypedXmlNamespaceImport4()
            Test("Imports <xmlns:",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlAttributeName("xmlns"),
                 VBXmlAttributeName(":"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Sub TestPartiallyTypedXmlNamespaceImport5()
            Test("Imports <xmlns:ns",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlAttributeName("xmlns"),
                 VBXmlAttributeName(":"),
                 VBXmlAttributeName("ns"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Sub TestPartiallyTypedXmlNamespaceImport6()
            Test("Imports <xmlns:ns=",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlAttributeName("xmlns"),
                 VBXmlAttributeName(":"),
                 VBXmlAttributeName("ns"),
                 VBXmlDelimiter("="))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Sub TestPartiallyTypedXmlNamespaceImport7()
            Test("Imports <xmlns:ns=""http://foo""",
                 Keyword("Imports"),
                 VBXmlDelimiter("<"),
                 VBXmlAttributeName("xmlns"),
                 VBXmlAttributeName(":"),
                 VBXmlAttributeName("ns"),
                 VBXmlDelimiter("="),
                 VBXmlAttributeQuotes(""""),
                 VBXmlAttributeValue("http://foo"),
                 VBXmlAttributeQuotes(""""))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(539779)>
        Public Sub TestFullyTypedXmlNamespaceImport()
            Test("Imports <xmlns:ns=""http://foo"">",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestGetXmlNamespaceExpression()
            TestInExpression("GetXmlNamespace(Name)",
                Keyword("GetXmlNamespace"),
                Punctuation.OpenParen,
                VBXmlName("Name"),
                Punctuation.CloseParen)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestGetXmlNamespaceExpressionWithNoName()
            TestInExpression("GetXmlNamespace()",
                Keyword("GetXmlNamespace"),
                Punctuation.OpenParen,
                Punctuation.CloseParen)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestClassifyXmlDocumentFollowingMisc()
            TestInExpression("<?xml ?><x></x><!--h-->",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestXmlDeclaration()
            TestInExpression("<?xml version=""1.0""?>",
                VBXmlDelimiter("<?"),
                VBXmlName("xml"),
                VBXmlAttributeName("version"),
                VBXmlDelimiter("="),
                VBXmlAttributeQuotes(""""),
                VBXmlAttributeValue("1.0"),
                VBXmlAttributeQuotes(""""),
                VBXmlDelimiter("?>"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestEnableWarningDirective()
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Main",
                "#Enable Warning BC123, [bc456], SomeId",
                "    End Sub",
                "End Module")
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestDisableWarningDirective()
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Main",
                "#Disable Warning",
                "    End Sub",
                "End Module")
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestBadWarningDirectives()
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
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestInterpolatedString1()
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Main",
                "        Dim s = $""Hello, {name,10:F}.""",
                "    End Sub",
                "End Module")
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestInterpolatedString2()
            Dim text = StringFromLines(
                "Module Program",
                "    Sub Main",
                "        Dim s = $""{x}, {y}""",
                "    End Sub",
                "End Module")
            Test(text,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(2126, "https://github.com/dotnet/roslyn/issues/2126")>
        Public Sub CommentBeforeXmlAccessExpression()
            TestInMethod("C",
                         "M",
                         " ' Comment
                           x.@Name = ""Text""",
                         "' Comment",
                         Comment("' Comment"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(3291, "https://github.com/dotnet/roslyn/issues/3291")>
        Public Sub TestCommentOnCollapsedEndRegion()
            Test(
"#Region ""Stuff""
End Region ' Stuff",
TextSpan.FromBounds(28, 36),
Comment("' Stuff"))
        End Sub
    End Class
End Namespace
