' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Public Class ParseXmlDocComments
    Inherits BasicTestBase

    <Fact()>
    Public Sub ParseOneLineText()
        ParseAndVerify(<![CDATA[
                ''' hello doc comments!
                Module m1
                End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseImmediateXmlDocText()
        ParseAndVerify(<![CDATA['''hello doc comments!
                Module m1
                End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseImmediateXmlDocElement()
        ParseAndVerify(<![CDATA['''<qqq> blah </qqq>
                Module m1
                End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseImmediateXmlDocComment()
        ParseAndVerify(<![CDATA['''<!-- qqqqq -->
                Module m1
                End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseMultilineXmlDocElement()
        ParseAndVerify(<![CDATA[
                '''
                '''<qqq> 
                '''<aaa>blah 
                '''</aaa>
                '''</qqq>

                Module m1
                End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseMultiLineText()
        Dim multiline = ParseAndVerify(<![CDATA[
                ''' hello doc comments!
                ''' hello doc comments!
                Module m1
                End Module
            ]]>).GetRoot()

        Dim comments = multiline.GetFirstToken.LeadingTrivia

        Assert.Equal(4, comments.Count)

        Dim struct = DirectCast(comments(2).GetStructure, DocumentationCommentTriviaSyntax)

        Assert.DoesNotContain(vbCr, struct.GetInteriorXml(), StringComparison.Ordinal)
        Assert.DoesNotContain(vbLf, struct.GetInteriorXml(), StringComparison.Ordinal)
        Assert.Equal(" hello doc comments! hello doc comments!", struct.GetInteriorXml)
    End Sub

    <Fact>
    Public Sub ParseOneLineTextAndMarkup()
        Dim node = ParseAndVerify(<![CDATA[
                ''' hello doc comments! <!-- qqqqq --> <qqq> blah </qqq> 
                Module m1
                End Module
            ]]>)

        Dim tk = node.GetRoot().FindToken(25)

        Dim docComment = DirectCast(tk.LeadingTrivia(2).GetStructure, DocumentationCommentTriviaSyntax)
        Dim txt = docComment.GetInteriorXml

        Assert.Equal(" hello doc comments! <!-- qqqqq --> <qqq> blah </qqq> ", txt)
    End Sub

    <Fact()>
    Public Sub ParseTwoLineTextAndMarkup()
        Dim node = ParseAndVerify(<![CDATA[
                ''' hello doc comments! <!-- qqqqq --> <qqq> blah </qqq> 
                ''' hello doc comments! <!-- qqqqq --> <qqq> blah </qqq>
                Module m1
                End Module
            ]]>)


        Dim tk = node.GetRoot().FindToken(25)

        Dim docComment = DirectCast(tk.LeadingTrivia(2).GetStructure, DocumentationCommentTriviaSyntax)
        Dim docComment1 = DirectCast(tk.LeadingTrivia(2).GetStructure, DocumentationCommentTriviaSyntax)

        Assert.Same(docComment, docComment1)

        Dim txt = docComment.GetInteriorXml

        Assert.Equal(" hello doc comments! <!-- qqqqq --> <qqq> blah </qqq> " &
                     " hello doc comments! <!-- qqqqq --> <qqq> blah </qqq>", txt)
    End Sub

    <Fact>
    Public Sub XmlDocCommentsSpanLines()
        ParseAndVerify(<![CDATA[
                ''' hello doc comments! <!-- qqqqq 
                '''--> <qqq> blah </qqq> 
                ''' hello doc comments! <!--
                ''' qqqqq --> <qqq> blah </qqq>
                Module m1
                End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub XmlDocCommentsAttrSpanLines()
        ParseAndVerify(<![CDATA[
                ''' <qqq a
                '''=
                '''
                '''"
                '''h
                ''' &lt;
                ''' "
                '''> blah </qqq>
                Module m1
                End Module
            ]]>)
    End Sub

    <WorkItem(893656, "DevDiv/Personal")>
    <WorkItem(923711, "DevDiv/Personal")>
    <Fact>
    Public Sub TickTickTickKind()
        ParseAndVerify(
            " '''<qqq> blah </qqq>" & vbCrLf &
            "Module m1" & vbCrLf &
            "End Module"
        ).
        FindNodeOrTokenByKind(SyntaxKind.XmlElementStartTag).VerifyPrecedingCommentIsTrivia()
    End Sub

    <Fact>
    Public Sub TickTickTickString()
        ParseAndVerify(
             "'''<qqq aa=""qq" & vbCrLf &
             "'''qqqqqqqqqqqqqqqqqq""> </qqq>" & vbCrLf &
             "Module m1" & vbCrLf &
             "End Module"
         ).
         FindNodeOrTokenByKind(SyntaxKind.XmlTextLiteralToken, 2).VerifyPrecedingCommentIsTrivia()
    End Sub

    <Fact>
    Public Sub TickTickTickSpaceString()
        ParseAndVerify(
             "'''<qqq aa=""qq" & vbCrLf &
             " '''qqqqqqqqqqqqqqqqqq""> </qqq>" & vbCrLf &
             "Module m1" & vbCrLf &
             "End Module"
         ).
         FindNodeOrTokenByKind(SyntaxKind.XmlTextLiteralToken, 2).VerifyPrecedingCommentIsTrivia()
    End Sub

    <Fact>
    Public Sub TickTickTickMarkup()
        ParseAndVerify(
            "'''<qqq " & vbCrLf &
            "'''aaaaaaaaaaaaaaa=""qq""> " & vbCrLf &
            "'''</qqq>" & vbCrLf &
            "Module m1" & vbCrLf &
            "End Module"
        ).
        FindNodeOrTokenByKind(SyntaxKind.XmlNameToken, 2).VerifyPrecedingCommentIsTrivia()
    End Sub

    <Fact>
    Public Sub TickTickTickSpaceMarkup()
        ParseAndVerify(
            "'''<qqq " & vbCrLf &
            " '''aaaaaaaaaaaaaaa=""qq""> " & vbCrLf &
            "'''</qqq>" & vbCrLf &
            "Module m1" & vbCrLf &
            "End Module"
        ).
        FindNodeOrTokenByKind(SyntaxKind.XmlNameToken, 2).VerifyPrecedingCommentIsTrivia()
    End Sub

    <WorkItem(900384, "DevDiv/Personal")>
    <Fact>
    Public Sub InvalidCastExceptionWithEvent()
        ParseAndVerify(<![CDATA[Class Foo
    ''' <summary>
    ''' Foo
    ''' </summary>
    Custom Event eventName As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
            ]]>)
    End Sub

    <WorkItem(904414, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMalformedDocComments()
        ParseAndVerify(<![CDATA[
Module M1
'''<doc>
'''    <></>
'''    </>
'''</doc>
'''</root>
Sub Main()
End Sub
End Module

Module M2
'''</root>
'''<!--* Missing start tag and no content -->
Sub Foo()
End Sub
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
    <errors>
        <error id="42304" warning="True"/>
        <error id="42304" warning="True"/>
        <error id="42304" warning="True"/>
        <error id="42304" warning="True"/>
    </errors>)
    End Sub

    <WorkItem(904903, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseShortEndTag()
        ParseAndVerify(<![CDATA[
                '''<qqq> 
                '''<a><b></></>
                '''</>
                Module m1
                End Module
            ]]>)
    End Sub

    <WorkItem(927580, "DevDiv/Personal")>
    <WorkItem(927696, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlDocBadNamespacePrefix()
        ParseAndVerify(<![CDATA[Module M1
'''<doc xmlns:a:b="abc"/>
Sub Main()
End Sub
End Module]]>)
        ParseAndVerify(<![CDATA[Module M1
'''<doc xmlns:a:b="abc"/>
Sub Main()
End Sub
End Module]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
        </errors>)
    End Sub

    <WorkItem(927781, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlDocCommentMalformedEndTag()
        ParseAndVerify(<![CDATA[Module M1
'''<test>
'''</test
Sub Main()
End Sub
'''<doc></doc/>
Sub Foo()
End Sub
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
        </errors>)
    End Sub

    <WorkItem(927785, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlDocCommentMalformedPI()
        ParseAndVerify(<![CDATA[Module M1
'''<doc><? ?></doc>
Sub Main()
End Sub
End Module
'''<?pi
Sub Foo()
End Sub
End Module
]]>,
        <errors>
            <error id="30622"/>
        </errors>)

        ParseAndVerify(<![CDATA[Module M1
'''<doc><? ?></doc>
Sub Main()
End Sub
End Module
'''<?pi
Sub Foo()
End Sub
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
            <error id="30622"/>
        </errors>)
    End Sub

    <WorkItem(929146, "DevDiv/Personal")>
    <WorkItem(929147, "DevDiv/Personal")>
    <WorkItem(929684, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseDTDInXmlDoc()
        ParseAndVerify(<![CDATA[Module Module1
    '''<!DOCTYPE Foo []>
    '''<summary>
    '''</summary>
    Sub Main()
    End Sub
End Module
]]>)

        ParseAndVerify(<![CDATA[Module Module1
    '''<!DOCTYPE Foo []>
    '''<summary>
    '''</summary>
    Sub Main()
    End Sub
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42304" warning="True"/>
        </errors>)
    End Sub

    <WorkItem(930282, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseIncorrectCharactersInDocComment()
        '!!!!!!!!!!!!!! BELOW TEXT CONTAINS INVISIBLE UNICODE CHARACTERS !!!!!!!!!!!!!!
        ParseAndVerify("'''<doc/>" & vbCrLf &
                       "'''<doc/>" & vbCrLf &
                       "'''<doc/>" & vbCrLf &
                       "'''<doc/>")

        ParseAndVerify("'''<doc/>" & vbCrLf &
                       "'''<doc/>" & vbCrLf &
                       "'''<doc/>" & vbCrLf &
                       "'''<doc/>", VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
        </errors>)
    End Sub

    <WorkItem(530663, "DevDiv")>
    <Fact()>
    Public Sub Bug16663()
        Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Module M
    Sub M()
        GoTo 1
        ''' <
        1: 
    End Sub
End Module
    ]]></file>
</compilation>)
        compilation.AssertNoErrors()
        compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Module M
    Sub M()
        GoTo 1
        ''' </>
        1: 
    End Sub
End Module
    ]]></file>
</compilation>)
        compilation.AssertNoErrors()
        compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Module M
    Sub M()
        GoTo 1
        ''' <?p 
        1: 
    End Sub
End Module
    ]]></file>
</compilation>)
        compilation.AssertNoErrors()
    End Sub

    <WorkItem(530663, "DevDiv")>
    <Fact()>
    Public Sub ParseXmlNameWithLeadingSpaces()
        ParseAndVerify(<![CDATA[
''' < summary/>
Module M
End Module
]]>)
        ParseAndVerify(<![CDATA[
''' < summary/>
Module M
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42304" warning="True"/>
        </errors>)
        ParseAndVerify(<![CDATA[
''' < 
''' summary/>
Module M
End Module
]]>)
        ParseAndVerify(<![CDATA[
''' < 
''' summary/>
Module M
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
            <error id="42304" warning="True"/>
        </errors>)
    End Sub

    <WorkItem(530663, "DevDiv")>
    <WorkItem(547297, "DevDiv")>
    <Fact(Skip:="547297")>
    Public Sub ParseOpenBracket()
        ParseAndVerify(<![CDATA[
''' < 
Module M
End Module
]]>,
        <errors>
            <error id="42304" warning="True"/>
        </errors>)
    End Sub

    <WorkItem(697115, "DevDiv")>
    <Fact()>
    Public Sub Bug697115()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= Function()
    ''' <summary/>
    Sub M()
    End Sub
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.None),
        <errors>
            <error id="30625"/>
            <error id="31151"/>
            <error id="36674"/>
            <error id="31159"/>
            <error id="31165"/>
            <error id="30636"/>
        </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= Function()
    ''' <summary/>
    Sub M()
    End Sub
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse),
        <errors>
            <error id="30625"/>
            <error id="31151"/>
            <error id="36674"/>
            <error id="31159"/>
            <error id="31165"/>
            <error id="30636"/>
        </errors>)
    End Sub

    <WorkItem(697269, "DevDiv")>
    <Fact()>
    Public Sub Bug697269()
        ParseAndVerify(<![CDATA[
'''<?a
Module M
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42304"/>
        </errors>)
        ParseAndVerify(<![CDATA[
'''<?a
'''b c<x/>
Module M
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42304"/>
        </errors>)
        ParseAndVerify(<![CDATA[
'''<x><?
Module M
End Module
]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42304"/>
            <error id="42304"/>
            <error id="42304"/>
            <error id="42304"/>
            <error id="42304"/>
        </errors>)
    End Sub

    <Fact>
    <Trait("Feature", "Xml Documentation Comments")>
    Public Sub TestDocumentationComment()
        Dim expected =
            "''' <summary>\r\n" &
            "''' This class provides extension methods for the <see cref=""TypeName""/> class.\r\n" &
            "''' </summary>\r\n" &
            "''' <threadsafety static=""true"" instance=""false""/>\r\n" &
            "''' <preliminary/>"

        Dim documentationComment As DocumentationCommentTriviaSyntax =
            SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("This class provides extension methods for the "),
                    SyntaxFactory.XmlSeeElement(
                        SyntaxFactory.CrefReference(
                            SyntaxFactory.ParseTypeName("TypeName"))),
                    SyntaxFactory.XmlText(" class."),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlThreadSafetyElement(),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlPreliminaryElement())

        Dim actual = documentationComment.ToFullString()

        Assert.Equal(Of String)(expected, actual)
    End Sub

    <Fact>
    <Trait("Feature", "Xml Documentation Comments")>
    Public Sub TestXmlSummaryElement()
        Dim expected =
            "''' <summary>\r\n" &
            "''' This class provides extension methods.\r\n" &
            "''' </summary>"

        Dim documentationComment As DocumentationCommentTriviaSyntax =
            SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("This class provides extension methods."),
                    SyntaxFactory.XmlNewLine("\r\n")))

        Dim actual = documentationComment.ToFullString()

        Assert.Equal(Of String)(expected, actual)
    End Sub

    <Fact>
    <Trait("Feature", "Xml Documentation Comments")>
    Public Sub TestXmlSeeElementAndXmlSeeAlsoElement()
        Dim expected =
            "''' <summary>\r\n" &
            "''' This class provides extension methods for the <see cref=""TypeName""/> class and the <seealso cref=""TypeName2""/> class.\r\n" &
            "''' </summary>"

        Dim documentationComment As DocumentationCommentTriviaSyntax =
            SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("This class provides extension methods for the "),
                    SyntaxFactory.XmlSeeElement(
                        SyntaxFactory.CrefReference(
                            SyntaxFactory.ParseTypeName("TypeName"))),
                    SyntaxFactory.XmlText(" class and the "),
                    SyntaxFactory.XmlSeeAlsoElement(
                        SyntaxFactory.CrefReference(
                            SyntaxFactory.ParseTypeName("TypeName2"))),
                    SyntaxFactory.XmlText(" class."),
                    SyntaxFactory.XmlNewLine("\r\n")))

        Dim actual = documentationComment.ToFullString()

        Assert.Equal(Of String)(expected, actual)
    End Sub

    <Fact>
    <Trait("Feature", "Xml Documentation Comments")>
    Public Sub TestXmlNewLineElement()
        Dim expected =
            "''' <summary>\r\n" &
            "''' This is a summary.\r\n" &
            "''' </summary>\r\n" &
            "''' \r\n" &
            "''' \r\n" &
            "''' <remarks>\r\n" &
            "''' \r\n" &
            "''' </remarks>"

        Dim documentationComment As DocumentationCommentTriviaSyntax =
            SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("This is a summary."),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlRemarksElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlNewLine("\r\n")))

        Dim actual = documentationComment.ToFullString()

        Assert.Equal(Of String)(expected, actual)
    End Sub

    <Fact>
    <Trait("Feature", "Xml Documentation Comments")>
    Public Sub TestXmlParamAndParamRefElement()
        Dim expected =
            "''' <summary>\r\n" &
            "''' <paramref name=""b""/>\r\n" &
            "''' </summary>\r\n" &
            "''' <param name=""a""></param>\r\n" &
            "''' <param name=""b""></param>"

        Dim documentationComment As DocumentationCommentTriviaSyntax =
            SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlParamRefElement("b"),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlParamElement("a"),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlParamElement("b"))

        Dim actual = documentationComment.ToFullString()

        Assert.Equal(Of String)(expected, actual)
    End Sub

    <Fact>
    <Trait("Feature", "Xml Documentation Comments")>
    Public Sub TestXmlReturnsElement()
        Dim expected =
            "''' <summary>\r\n" &
            "''' \r\n" &
            "''' </summary>\r\n" &
            "''' <returns>\r\n" &
            "''' Returns a value.\r\n" &
            "''' </returns>"

        Dim documentationComment As DocumentationCommentTriviaSyntax =
            SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlReturnsElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("Returns a value."),
                    SyntaxFactory.XmlNewLine("\r\n")))

        Dim actual = documentationComment.ToFullString()

        Assert.Equal(Of String)(expected, actual)
    End Sub

    <Fact>
    <Trait("Feature", "Xml Documentation Comments")>
    Public Sub TestXmlRemarksElement()
        Dim expected =
            "''' <summary>\r\n" &
            "''' \r\n" &
            "''' </summary>\r\n" &
            "''' <remarks>\r\n" &
            "''' Same as in class <see cref=""TypeName""/>.\r\n" &
            "''' </remarks>"

        Dim documentationComment As DocumentationCommentTriviaSyntax =
            SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlRemarksElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("Same as in class "),
                    SyntaxFactory.XmlSeeElement(
                        SyntaxFactory.CrefReference(
                            SyntaxFactory.ParseTypeName("TypeName"))),
                    SyntaxFactory.XmlText("."),
                    SyntaxFactory.XmlNewLine("\r\n")))

        Dim actual = documentationComment.ToFullString()

        Assert.Equal(Of String)(expected, actual)
    End Sub

    <Fact>
    <Trait("Feature", "Xml Documentation Comments")>
    Public Sub TestXmlExceptionElement()
        Dim expected =
            "''' <summary>\r\n" &
            "''' \r\n" &
            "''' </summary>\r\n" &
            "''' <exception cref=""InvalidOperationException"">This exception will be thrown if the object is in an invalid state when calling this method.</exception>"

        Dim documentationComment As DocumentationCommentTriviaSyntax =
            SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlExceptionElement(
                    SyntaxFactory.CrefReference(
                        SyntaxFactory.ParseTypeName("InvalidOperationException")),
                    SyntaxFactory.XmlText("This exception will be thrown if the object is in an invalid state when calling this method.")))

        Dim actual = documentationComment.ToFullString()

        Assert.Equal(Of String)(expected, actual)
    End Sub

    <Fact>
    <Trait("Feature", "Xml Documentation Comments")>
    Public Sub TestXmlPermissionElement()
        Dim expected =
            "''' <summary>\r\n" &
            "''' \r\n" &
            "''' </summary>\r\n" &
            "''' <permission cref=""MyPermission"">Needs MyPermission to execute.</permission>"

        Dim documentationComment As DocumentationCommentTriviaSyntax =
            SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlPermissionElement(
                    SyntaxFactory.CrefReference(
                        SyntaxFactory.ParseTypeName("MyPermission")),
                    SyntaxFactory.XmlText("Needs MyPermission to execute.")))

        Dim actual = documentationComment.ToFullString()

        Assert.Equal(Of String)(expected, actual)
    End Sub

End Class
