' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Public Class XmlInternalSyntax

    <Fact>
    Public Sub ScannerXml_SimpleComment()
        Dim str = SourceText.From(" <!-- hello there --> ")

        ' First validate that we correctly detect comment in the element context
        Using s As InternalSyntax.Scanner = New InternalSyntax.Scanner(str, TestOptions.Regular)
            Dim tkBeginComment = s.ScanXmlElement()
            Assert.Equal(SyntaxKind.LessThanExclamationMinusMinusToken, tkBeginComment.Kind)
            Assert.Equal(" <!--", tkBeginComment.ToFullString())
        End Using
        ' Now validate that we can scan the entire comment
        Using s = New InternalSyntax.Scanner(str, TestOptions.Regular)
            s.GetNextTokenInState(InternalSyntax.ScannerState.VB)
            Dim tkBeginComment = s.GetCurrentToken
            Assert.Equal(SyntaxKind.LessThanExclamationMinusMinusToken, tkBeginComment.Kind)
            Assert.Equal(" <!-- ", tkBeginComment.ToFullString())

            Dim tkComment = s.ScanXmlComment
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, tkComment.Kind)
            Assert.Equal("hello there ", tkComment.ToFullString())

            Dim tkEndComment = s.ScanXmlComment
            Assert.Equal(SyntaxKind.MinusMinusGreaterThanToken, tkEndComment.Kind)
            Assert.Equal("-->", tkEndComment.ToFullString())
        End Using
    End Sub

    <Fact>
    Public Sub ScannerXml_SimpleCData()
        Dim str = SourceText.From(" <![CDATA[some data / > < % @ here ]]> ")

        ' First validate that we correctly detect CData in the element context
        Using s = New InternalSyntax.Scanner(str, TestOptions.Regular)
            Dim tkBeginComment = s.ScanXmlElement()
            Assert.Equal(SyntaxKind.BeginCDataToken, tkBeginComment.Kind)
            Assert.Equal(" <![CDATA[", tkBeginComment.ToFullString())
        End Using

        ' Now validate that we can scan the entire CData
        Using s = New InternalSyntax.Scanner(str, TestOptions.Regular)
            s.GetNextTokenInState(InternalSyntax.ScannerState.VB)
            Dim tkBeginComment = s.GetCurrentToken
            Assert.Equal(SyntaxKind.BeginCDataToken, tkBeginComment.Kind)
            Assert.Equal(" <![CDATA[", tkBeginComment.ToFullString())

            Dim tkComment = s.ScanXmlCData
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, tkComment.Kind)
            Assert.Equal("some data / > < % @ here ", tkComment.ToFullString())

            Dim tkEndComment = s.ScanXmlCData
            Assert.Equal(SyntaxKind.EndCDataToken, tkEndComment.Kind)
            Assert.Equal("]]>", tkEndComment.ToFullString())
        End Using
    End Sub

    <Fact>
    Public Sub ScannerXml_SimpleElement()

        Dim str = SourceText.From(" <E1 : E2 A1 = 'q q' BB= "" w&apos; &#x2F blah" & ChrW(8216) & ChrW(8217) & ChrW(8220) & ChrW(8221) & """ > Ha &lt; </E1 > ")

        Using s As New InternalSyntax.Scanner(str, TestOptions.Regular)
            s.GetNextTokenInState(InternalSyntax.ScannerState.VB)
            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.LessThanToken, tk.Kind)
            Assert.Equal(" <", tk.ToFullString())

            ' Got <, switch to element
            Dim tkMarkup0 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.XmlNameToken, tkMarkup0.Kind)
            Assert.Equal("E1 ", tkMarkup0.ToFullString())

            Dim tkElement0 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.ColonToken, tkElement0.Kind)
            Assert.Equal(": ", tkElement0.ToFullString())

            ' got : , read more names
            Dim tkName0 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.XmlNameToken, tkName0.Kind)
            Assert.Equal("E2 ", tkName0.ToFullString())

            Dim tkName1 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.XmlNameToken, tkName1.Kind)
            Assert.Equal("A1 ", tkName1.ToFullString())

            ' Name was not separated by :, so switch back to element

            Dim tkElement1 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.EqualsToken, tkElement1.Kind)
            Assert.Equal("= ", tkElement1.ToFullString())

            Dim tkAttrValue0 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.SingleQuoteToken, tkAttrValue0.Kind)
            Assert.Equal("'", tkAttrValue0.ToString())

            Dim tkStrVal0 = s.ScanXmlStringSingle()
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, tkStrVal0.Kind)
            Assert.Equal("q q", tkStrVal0.ToString())

            Dim tkStrVal1 = s.ScanXmlStringSingle()
            Assert.Equal(SyntaxKind.SingleQuoteToken, tkStrVal1.Kind)
            Assert.Equal("' ", tkStrVal1.ToFullString())

            ' back in the element

            Dim tkElement2 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.XmlNameToken, tkElement2.Kind)
            Assert.Equal("BB", tkElement2.ToFullString())

            Dim tkElement3 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.EqualsToken, tkElement3.Kind)
            Assert.Equal("= ", tkElement3.ToFullString())

            ' Got equals, switch to attribute value

            Dim tkAttrValue1 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.DoubleQuoteToken, tkAttrValue1.Kind)
            Assert.Equal("""", tkAttrValue1.ToString())

            Dim tkStrVal2 = s.ScanXmlStringDouble()
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, tkStrVal2.Kind)
            Assert.Equal(" w", tkStrVal2.ToString())

            Dim tkStrVal3 = s.ScanXmlStringDouble()
            Assert.Equal(SyntaxKind.XmlEntityLiteralToken, tkStrVal3.Kind)
            Assert.Equal("&apos;", tkStrVal3.ToString())
            Assert.Equal("'", tkStrVal3.ValueText)

            Dim tkStrVal3a = s.ScanXmlStringDouble()
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, tkStrVal3a.Kind)
            Assert.Equal(" ", tkStrVal3a.ToString())

            Dim tkStrVal4 = s.ScanXmlStringDouble()
            Assert.Equal(SyntaxKind.XmlEntityLiteralToken, tkStrVal4.Kind)
            Assert.Equal("&#x2F", tkStrVal4.ToString())
            Assert.Equal("/", tkStrVal4.ValueText)

            Dim tkStrVal5 = s.ScanXmlStringDouble()
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, tkStrVal5.Kind)
            Assert.Equal(" blah" & ChrW(8216) & ChrW(8217) & ChrW(8220) & ChrW(8221), tkStrVal5.ToString())

            Dim tkStrVal6 = s.ScanXmlStringDouble()
            Assert.Equal(SyntaxKind.DoubleQuoteToken, tkStrVal6.Kind)
            Assert.Equal(""" ", tkStrVal6.ToFullString())

            ' back in the element
            Dim tkElement4 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.GreaterThanToken, tkElement4.Kind)
            Assert.Equal(">", tkElement4.ToFullString())

            Dim tkContent0 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, tkContent0.Kind)
            Assert.Equal(" Ha ", tkContent0.ToFullString())

            Dim tkContent1 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.XmlEntityLiteralToken, tkContent1.Kind)
            Assert.Equal("&lt;", tkContent1.ToFullString())
            Assert.Equal("<", tkContent1.ValueText)

            Dim tkContent2 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, tkContent2.Kind)
            Assert.Equal(" ", tkContent2.ToFullString())

            Dim tkContent3 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.LessThanSlashToken, tkContent3.Kind)
            Assert.Equal("</", tkContent3.ToFullString())

            ' Got </, switch to name

            Dim tkMarkup2 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.XmlNameToken, tkMarkup2.Kind)
            Assert.Equal("E1 ", tkMarkup2.ToFullString())

            Dim tkMarkup3 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.GreaterThanToken, tkMarkup3.Kind)
            Assert.Equal(">", tkMarkup3.ToFullString())
        End Using
    End Sub

    <Fact>
    Public Sub ScannerXml_EmptyElement()

        Dim str = SourceText.From(" <E1 /> ")

        Using s As New InternalSyntax.Scanner(str, TestOptions.Regular)
            s.GetNextTokenInState(InternalSyntax.ScannerState.VB)
            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.LessThanToken, tk.Kind)
            Assert.Equal(" <", tk.ToFullString())

            ' Got <, switch to element
            Dim tkMarkup0 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.XmlNameToken, tkMarkup0.Kind)
            Assert.Equal("E1 ", tkMarkup0.ToFullString())

            Dim tkElement0 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.SlashGreaterThanToken, tkElement0.Kind)
            Assert.Equal("/>", tkElement0.ToFullString())
        End Using
    End Sub

    <Fact>
    Public Sub ScannerXml_WhiteSpaceElement()
        Dim str = SourceText.From(" <E1> q <a/>  <b/>  &lt; " & vbCrLf & "  </E1>")

        Using s As New InternalSyntax.Scanner(str, TestOptions.Regular)
            s.GetNextTokenInState(InternalSyntax.ScannerState.VB)
            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.LessThanToken, tk.Kind)
            Assert.Equal(" <", tk.ToFullString())

            ' Got <, switch to element
            Dim tkMarkup0 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.XmlNameToken, tkMarkup0.Kind)
            Assert.Equal("E1", tkMarkup0.ToFullString())

            Dim tkElement0 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.GreaterThanToken, tkElement0.Kind)
            Assert.Equal(">", tkElement0.ToFullString())

            Dim content0 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, content0.Kind)
            Assert.Equal(" q ", content0.ToFullString())

            Dim content1 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.LessThanToken, content1.Kind)
            Assert.Equal("<", content1.ToFullString())

            ' Got <, switch to element
            Dim tkMarkup1 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.XmlNameToken, tkMarkup1.Kind)
            Assert.Equal("a", tkMarkup1.ToFullString())

            Dim tkElement1 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.SlashGreaterThanToken, tkElement1.Kind)
            Assert.Equal("/>", tkElement1.ToFullString())

            Dim content2 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.LessThanToken, content2.Kind)
            Assert.Equal("  <", content2.ToFullString())

            ' Got <, switch to element
            Dim tkMarkup2 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.XmlNameToken, tkMarkup2.Kind)
            Assert.Equal("b", tkMarkup2.ToFullString())

            Dim tkElement2 = s.ScanXmlElement
            Assert.Equal(SyntaxKind.SlashGreaterThanToken, tkElement2.Kind)
            Assert.Equal("/>", tkElement2.ToFullString())

            Dim content3 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, content3.Kind)
            Assert.Equal("  ", content3.ToFullString())

            Dim content4 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.XmlEntityLiteralToken, content4.Kind)
            Assert.Equal("&lt;", content4.ToFullString())
            Assert.Equal("<", content4.ValueText)

            Dim content5 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, content5.Kind)
            Assert.Equal(" " & vbCrLf & "  ", content5.ToFullString())
            Assert.Equal(" " & vbLf & "  ", content5.ValueText)

            Dim content6 = s.ScanXmlContent
            Assert.Equal(SyntaxKind.LessThanSlashToken, content6.Kind)
            Assert.Equal("</", content6.ToFullString())
        End Using
    End Sub

    <WorkItem(538550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538550")>
    <WorkItem(538551, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538551")>
    <Fact>
    Public Sub ScannerXml_SmartStrings()
        ' smart strings
        Dim valueText = "some text"
        Dim str = SourceText.From(ChrW(8216) & valueText & ChrW(8217))
        Using s As New InternalSyntax.Scanner(str, TestOptions.Regular)
            s.GetNextTokenInState(InternalSyntax.ScannerState.Element)
            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.SingleQuoteToken, tk.Kind)
            Assert.Equal(ChrW(8216), tk.ToFullString())

            tk = s.ScanXmlStringSmartSingle()
            Assert.Equal(valueText, tk.ToString())

            tk = s.ScanXmlStringSmartSingle()
            Assert.Equal(SyntaxKind.SingleQuoteToken, tk.Kind)
            Assert.Equal(ChrW(8217), tk.ToFullString())

            str = SourceText.From(ChrW(8220) & valueText & ChrW(8221))
        End Using

        Using s = New InternalSyntax.Scanner(str, TestOptions.Regular)
            s.GetNextTokenInState(InternalSyntax.ScannerState.Element)
            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.DoubleQuoteToken, tk.Kind)
            Assert.Equal(ChrW(8220), tk.ToFullString())

            tk = s.ScanXmlStringSmartDouble()
            Assert.Equal(valueText, tk.ToString())

            tk = s.ScanXmlStringSmartDouble()
            Assert.Equal(SyntaxKind.DoubleQuoteToken, tk.Kind)
            Assert.Equal(ChrW(8221), tk.ToFullString())
        End Using
    End Sub


    <Fact>
    Public Sub ScannerXml_CharEntity()

        Using s As New InternalSyntax.Scanner(SourceText.From("&#x03C0;"), TestOptions.Regular)
            s.GetNextTokenInState(InternalSyntax.ScannerState.Content)
            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.XmlEntityLiteralToken, tk.Kind)
            Assert.Equal("&#x03C0;", tk.ToFullString())
        End Using

        Using s = New InternalSyntax.Scanner(SourceText.From("&#x03C0"), TestOptions.Regular)
            s.GetNextTokenInState(InternalSyntax.ScannerState.Content)
            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.XmlEntityLiteralToken, tk.Kind)
            Assert.Equal("&#x03C0", tk.ToFullString())
            Assert.Equal(True, tk.ContainsDiagnostics)
        End Using
    End Sub

    <WorkItem(897814, "DevDiv/Personal")>
    <Fact>
    Public Sub ScannerXml_SurrogateCharEntity()
        Using s As New InternalSyntax.Scanner(SourceText.From("&#x103fe;"), TestOptions.Regular)

            Dim tk = s.ScanXmlContent()
            Dim value As String = tk.ValueText
            Assert.Equal(2, value.Length)
            Assert.Equal(ChrW(&HD800), value(0))
            Assert.Equal(ChrW(&HDFFE), value(1))
        End Using
    End Sub

    <WorkItem(541284, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541284")>
    <Fact>
    Public Sub ParseWithChrw0()
        Dim code = <![CDATA[
        Sub SUB0113 ()
        I<

        ]]>.Value

        code = code & ChrW(0)
        VisualBasicSyntaxTree.ParseText(code)
    End Sub

    <Fact(), WorkItem(825859, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825859")>
    Public Sub NbspFollowedByXmlDocComment()
        Dim tree = SyntaxFactory.ParseSyntaxTree("")

        Dim nbsp = ChrW(&HA0)
        Dim text = nbsp & nbsp & "''' <param name=""g"">a</param>"

        tree = tree.WithChangedText(SourceText.From(text))

        Dim eof = tree.GetRoot().ChildTokens.Single()
        Dim trivia = eof.LeadingTrivia

        Assert.Equal(2, trivia.Count)
        Assert.Equal(SyntaxKind.WhitespaceTrivia, trivia.Item(0).Kind())
        Assert.Equal(SyntaxKind.DocumentationCommentTrivia, trivia.Item(1).Kind())
    End Sub

End Class
