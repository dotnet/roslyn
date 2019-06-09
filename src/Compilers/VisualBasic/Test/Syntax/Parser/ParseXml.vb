' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Roslyn.Test.Utilities

Public Class ParseXml
    Inherits BasicTestBase

    <Fact>
    Public Sub ParseElement()
        ' Basic xml literal test
        ParseAndVerify(<![CDATA[
                Module m1
                    dim x  = <a> 
                                <b>
                                    aa
                                </b> 
                             </a>
                    Dim x = <a b="1" c=<%= 2 %>>hello<!-- comment --><?pi target ?></a>
                End Module
            ]]>)
        'Dim x = <a b="1" c=<%= 2 %>></a>
    End Sub

    <Fact>
    Public Sub ParseCDATA()
        ' Basic xml literal test

        ParseAndVerify("Module m1" & vbCrLf &
            "Dim x = <a><![CDATA[abcde]]></a>" & vbCrLf &
            "End Module")

    End Sub

    <Fact>
    Public Sub ParseEmbeddedExpression()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
                Module m1
                    dim y  = <a><%= 1 %></a>
                End Module
            ]]>)

    End Sub

    <Fact(), WorkItem(545537, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545537")>
    Public Sub ParseNameWhitespace()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        Dim b = <x/>.@<xml:
        x>
    End Sub
End Module

            ]]>,
            Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, Environment.NewLine),
            Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, "        "),
            Diagnostic(ERRID.ERR_ExpectedXmlName, "x"))

    End Sub

    <Fact(), WorkItem(529879, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529879")>
    Public Sub ParseFWPercent()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x y=<%= 1 ]]>.Value & ChrW(65285) & <![CDATA[>/>
End Module

            ]]>.Value)

    End Sub

    <Fact>
    Public Sub ParseAccessorSpaceDisallowed()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x/>.@ _
    x

    Dim y = <y/>.@
    y
End Module

            ]]>,
            <errors>
                <error id="31146"/>
                <error id="31146"/>
                <error id="30188"/>
            </errors>)

    End Sub

    <Fact(), WorkItem(546401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546401")>
    Public Sub ParseAccessorSpaceDisallowed01()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
Module Program
    Dim x = <x/>.<
             x>

    Dim y = <y/>.<x >
End Module
VB

            ]]>,
        Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, Environment.NewLine),
        Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, "             "),
        Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
        Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "VB"))

    End Sub

    <WorkItem(531396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531396")>
    <Fact()>
    Public Sub ParseEmbeddedExpressionAttributeNoSpace()
        ParseAndVerify(<![CDATA[
Module M
    Private x = <x<%= Nothing %>/>
End Module
]]>)
        ' Dev11 does not allow this case.
        ParseAndVerify(<![CDATA[
Module M
    Private x = <x<%="a"%>="b"/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Private x = <x a="b"c="d"/>
End Module
]]>,
            Diagnostic(ERRID.ERR_ExpectedXmlWhiteSpace, "c"))
        ParseAndVerify(<![CDATA[
Module M
    Private x = <x a="b"<%= Nothing %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Private x = <x <%= Nothing %>a="b"/>
End Module
]]>,
            Diagnostic(ERRID.ERR_ExpectedXmlWhiteSpace, "a"))
        ParseAndVerify(<![CDATA[
Module M
    Private x = <x <%= Nothing %><%= Nothing %>/>
End Module
]]>)
        ' Dev11 does not allow this case.
        ParseAndVerify(<![CDATA[
Module M
    Private x = <x <%="a"%>="b"<%="c"%>="d"/>
End Module
]]>)
    End Sub

    <Fact>
    Public Sub ParseEmbeddedExpressionAttributeSpace()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
Module M1
    Dim x = <x <%= "a" %>=""/>
End Module
            ]]>)

    End Sub

    <Fact>
    Public Sub BC31169ERR_IllegalXmlStartNameChar_ParseMissingGT()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
                Module m1
                    dim y1  = <a  / >
                    dim y2 = <a    </a>
                    dim y3 = <a ? ? ?</a>
                End Module
            ]]>, <errors>
                     <error id="31177"/>
                     <error id="30636"/>
                     <error id="31169"/>
                     <error id="30035"/>
                     <error id="31169"/>
                     <error id="31169"/>
                 </errors>)

    End Sub

    <WorkItem(641680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/641680")>
    <Fact>
    Public Sub ParseDocumentationComment()
        ParseAndVerify(<![CDATA[
''' <summary?
Class C
End Class
            ]]>)

        ParseAndVerify(<![CDATA[
''' <summary?
Class C
End Class
            ]]>,
            VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
            <errors>
                <error id="42304"/>
                <error id="42304"/>
                <error id="42304"/>
                <error id="42304"/>
            </errors>)
    End Sub

    <WorkItem(641680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/641680")>
    <Fact>
    Public Sub ParseDocumentationComment2()
        ParseAndVerify(<![CDATA[
''' <summary/>
Class C
End Class
            ]]>)
    End Sub

    <WorkItem(551848, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551848")>
    <Fact()>
    Public Sub KeywordAndColonInXmlAttributeAccess()
        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As Object)
        x = x.@Sub:a
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As Object)
        x = x.@p:Sub
    End Sub
End Module
]]>)
    End Sub

    <Fact>
    Public Sub Regress12668_NoEscapingOfAttrAxis()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As Object)
        x = x.@[Sub]
    End Sub
End Module
]]>,
            <errors>
                <error id="31146"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As Object)
        x = x.@[Sub]:a
    End Sub
End Module
]]>,
            <errors>
                <error id="31146"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As Object)
        x = x.@p:[Sub]
    End Sub
End Module
]]>,
            <errors>
                <error id="31146"/>
            </errors>)

    End Sub

    <Fact>
    Public Sub Regress12664_IllegalXmlNameChars()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a/>.@豈
    Dim y = <a/>.@a豈
End Module

            ]]>, <errors>
                     <error id="31169"/>
                     <error id="31170"/>
                 </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a/>.@ｘml:y
    Dim y = <a/>.@xｍl:y
End Module

            ]]>, <errors>
                     <error id="31169"/>
                     <error id="31170"/>
                 </errors>)

    End Sub

    <Fact>
    Public Sub BC31159ERR_ExpectedXmlEndEmbedded_ParseMissingEmbbedErrorRecovery()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
          module m1
            sub s
            dim x = <a a1=<%=1 <!-- comment --> </a>
            end sub
            end module
            ]]>, <errors>
                     <error id="31159"/>
                 </errors>)

    End Sub


    <Fact>
    Public Sub BC30636ERR_ExpectedGreater_ParseMissingGreaterTokenInEndElement()
        ' Basic xml literal test

        ParseAndVerify(<![CDATA[
            module m1
                dim x = <a b="1" c =<%= 2 + 3 %>></a
            end module
            ]]>, <errors>
                     <error id="30636"/>
                 </errors>)

    End Sub


    <Fact>
    Public Sub ParseAttributeMemberAccess()
        ' Test attribute member syntax

        ParseAndVerify(<![CDATA[
            module m1
            dim a1=p.@a:b

            dim a1=p.
                @a:b
            end module
            ]]>)

    End Sub

    <Fact>
    Public Sub ParseElementMemberAccess()
        ' Test attribute member syntax

        ParseAndVerify(<![CDATA[
            module m1
            dim a1=p.<a:b>

            dim a2=p.
                    <a:b>
            end module
            ]]>)

    End Sub

    <Fact>
    Public Sub ParseDescendantMemberAccess()
        ' Test attribute member syntax

        ParseAndVerify(<![CDATA[
            module m1
            dim a1=p...<a:b>

            dim a2=p...
                <a:b>
            end module
            ]]>)

    End Sub

    <WorkItem(875151, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseEmptyCDATA()
        ParseAndVerify("Module m1" & vbCrLf &
            "Dim x = <![CDATA[]]>" & vbCrLf &
            "End Module")
    End Sub

    <WorkItem(875156, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseEmptyPI()
        ParseAndVerify("Module m1" & vbCrLf &
            "Dim x = <?pi ?>" & vbCrLf &
            "Dim y = <?pi?>" & vbCrLf &
            "Dim z = <?pi abcde?>" & vbCrLf &
            "End Module")
    End Sub

    <WorkItem(874435, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseSignificantWhitespace()
        ParseAndVerify(<![CDATA[
module m1
    dim x =<ns:e>
 a <ns:e>  &lt; 
</ns:e> 
  </ns:e>
end module
        ]]>).
        VerifyOccurrenceCount(SyntaxKind.XmlTextLiteralToken, 3)
    End Sub

    <Fact>
    Public Sub ParseXmlNamespace()
        ParseAndVerify(<![CDATA[
            module m1

                    Dim x = GetXmlNamespace(p)

            end module
        ]]>)
    End Sub

    <Fact>
    Public Sub BC30203ERR_ExpectedIdentifier_ParseDescendantMemberAccessWithEOLError()
        ' Test attribute member syntax

        ParseAndVerify(<![CDATA[
            module m1
                sub s
                    dim a1=p..
                        .<a:b>
                end sub
            end module
            ]]>,
            Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
            Diagnostic(ERRID.ERR_ExpectedIdentifier, ""))

    End Sub

    <WorkItem(539502, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539502")>
    <Fact>
    Public Sub ParseAttributeWithLeftDoubleQuotationMark()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <tag attr=“"/>“/>
End Module
        ]]>)
    End Sub

    <WorkItem(539502, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539502")>
    <Fact>
    Public Sub ParseAttributeWithRegularDoubleQuotationMark()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <tag attr="abc/>"/>
End Module
        ]]>)
    End Sub

    <WorkItem(878042, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseAttributeValueSpecialCharacters()
        ParseAndVerify(<![CDATA[
            module module1
                sub main()
				    dim x1 = <goo attr1="&amp; &lt; &gt; &apos; &quot;"></goo>
                end sub
            end module
        ]]>)
    End Sub

    <WorkItem(879417, "DevDiv/Personal")>
    <Fact>
    Public Sub ParsePrologue()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = <?xml version="1.0" encoding="utf-8"?>
                            <root/>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(879562, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseAttributeAccessExpression()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = <a b="goo" />
                    Dim y = x.@b
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(879678, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31163ERR_ExpectedSQuote_ParseAttribute()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    dim x1 = <goo attr1='qqq"></>
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="31163"/>
        </errors>)
    End Sub

    <WorkItem(880383, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMultilineCDATA()
        ParseAndVerify(
            "Module Module1" & vbCrLf &
            "    Sub Main()" & vbCrLf &
            "        Dim x = <![CDATA[" & vbCrLf &
            "                ]]>" & vbCrLf &
            "    End Sub" & vbCrLf &
            "End Module")
    End Sub

    <Fact>
    Public Sub ParseMultilineCDATAVariousEOL()
        ParseAndVerify(
            "Module Module1" & vbCrLf &
            "    Sub Main()" & vbCrLf &
            "        Dim x = <![CDATA[" & vbCrLf &
            "abcdefghihjklmn" & vbCr &
            "               " & vbLf &
            "                ]]>" & vbCrLf &
            "    End Sub" & vbCrLf &
            "End Module")
    End Sub

    <WorkItem(880401, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMultilineXComment()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    dim x1 = <!--
                             -->
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(880793, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlEmbeddedExpressionWithExplicitLineContinuation()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    dim x1 = <outer><%= _
                             <otherXml></otherXml>
                             %></outer>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(880798, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlProcessingInstructionAfterDocument()
        ParseAndVerify(<![CDATA[
            Public Module Module1
                Public Sub Main()
                    Dim x = <?xml version="1.0"?>
                                <a><?PI target2?></a>
                                <?PI target?>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881535, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlCDATAContainingImports()
        ParseAndVerify(
            "Module Module1" & vbCrLf &
            "   Sub Main()" & vbCrLf &
            "       scenario = <scenario><![CDATA[" & vbCrLf &
            "Imports Goo" & vbCrLf &
            "       ]]></scenario>" & vbCrLf &
            "    End Sub" & vbCrLf &
            "End Module")
    End Sub

    <WorkItem(881819, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31146ERR_ExpectedXmlName_ParseXmlQuestionMar()
        ParseAndVerify(<![CDATA[
            Public Module Module1
                Public Sub Main()
                    Dim x =<?></?>
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="31146"/>
        </errors>)
    End Sub

    <WorkItem(881822, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlUnterminatedXElementStartWithComment()
        ParseAndVerify(<![CDATA[
            Public Module Module1
                Public Sub Main()
                    Dim x = <   
                    '
                    Dim y = <
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30636"/>
            <error id="31165"/>
            <error id="31151"/>
            <error id="30636"/>
            <error id="31146"/>
            <error id="30636"/>
            <error id="31165"/>
            <error id="31151"/>
            <error id="30636"/>
            <error id="31146"/>
            <error id="31177"/>
        </errors>)
    End Sub

    <WorkItem(881823, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31175ERR_DTDNotSupported()
        ParseAndVerify(<![CDATA[
            Public Module Module1
                Public Sub Main()
                    Dim a2 = <?xml version="1.0" encoding="UTF-8" ?>
                             <!DOCTYPE greeting [
                                 <!ELEMENT greeting (#PCDATA)>
                             ]>
                             <greeting>Hello, world!</greeting> 
                End Sub
            End Module      
        ]]>,
        <errors>
            <error id="31175"/>
        </errors>)
    End Sub

    <WorkItem(881824, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31172ERR_EmbeddedExpression_Prologue()
        ParseAndVerify(<![CDATA[
            Public Module Module1
                Public Sub Main()
                    Dim x = <?xml version="1.0" encoding=<%= encoding %>?><element/>
                End Sub
            End Module      
        ]]>,
        <errors>
            <error id="31172"/>
        </errors>
        )
    End Sub

    <WorkItem(881825, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlAttributeEmbeddedExpressionImplicitLineContinuation()
        ParseAndVerify(<![CDATA[
            Public Module Module1
                Public Sub Main()
                    Dim x = <subnode att=
                            <%=
                                42
                            %>
                            />
                End Sub
            End Module 
        ]]>)
    End Sub

    <WorkItem(881828, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlNamespaceImports()
        ParseAndVerify(<![CDATA[
            Imports <xmlns:ns="http://microsoft.com">
        ]]>)
    End Sub

    <WorkItem(881829, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlEmbeddedExpressionLambdaImplicitLineContinuation()
        ParseAndVerify(<![CDATA[
            Public Module Module1
                Public Sub Main()
                    Dim k = <xml><%= Function()
                                         Return {1
                                         }
                                     End Function %>
                            </xml>
                End Sub
            End Module 
        ]]>)
    End Sub

    <WorkItem(881820, "DevDiv/Personal")>
    <WorkItem(882380, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlElementContentEntity()
        ParseAndVerify(<![CDATA[
            Public Module Module1
                Public Sub Main()
                    Dim x = <element>&lt;</>
                End Sub
            End Module
        ]]>)

        ParseAndVerify(<![CDATA[
            Public Module Module1
                Public Sub Main()
                    Dim buildx = <xml>&lt;&gt;</xml>
                End Sub
            End Module 
        ]]>)
    End Sub

    <WorkItem(882421, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseAttributeAccessorBracketed()
        ParseAndVerify(<![CDATA[
            Imports <xmlns:ns = "goo">
            Imports <xmlns:n-s- = "goo2">
            Module Module1
                Sub Main()
                    Dim ele3 = <ns:e/>
                    ele3.@<n-s-:A-A> = <e><%= "hello" %></e>.Value.ToString()
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(882460, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlAttributeAccessorInWith()
        ParseAndVerify(<![CDATA[
            Module Module1
                Class Customer : Inherits XElement
                   Sub New()
                        MyBase.New(<customer/>)
                   End Sub
                End Class
                Sub Main()
                    Dim oldCust = <customer name="Sally"/>
                    With oldCust
                        Dim newCust As New Customer With {.Name = .@name}
                    End With
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact()>
    Public Sub BC31178ERR_ExpectedSColon()
        Dim tree = Parse(<![CDATA[
Imports <xmlns:p="&#x30">
Module M
    Private F = <x>&lt;&#x5a</x>
End Module
                    ]]>)
        tree.AssertTheseDiagnostics(<errors><![CDATA[
BC31178: Expected closing ';' for XML entity.
Imports <xmlns:p="&#x30">
                  ~~~~~
BC31178: Expected closing ';' for XML entity.
    Private F = <x>&lt;&#x5a</x>
                       ~~~~~
        ]]></errors>)
    End Sub

    <WorkItem(882874, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31178ERR_ExpectedSColon_ParseXmlEntity()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    dim z = <goo attr1="&amp"></goo>
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="31178"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC31166ERR_StartAttributeValue_ParseXmlAttributeUnquoted()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    dim z = <goo attr1=before&amp;after></goo>
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="31166"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC31155ERR_QuotedEmbeddedExpression_ParseXmlAttribute()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    dim z = <goo attr1="<%= %>"></goo>
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="31155"/>
        </errors>)
    End Sub

    <WorkItem(882898, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlEmbeddedExpressionXmlAccessorLineContinuation()
        ParseAndVerify(<![CDATA[
            Imports <xmlns:ns="lower">
            Module Module1
                Sub Main()
                    Dim A1 = <ns:book>
                                 <title name="Debug Applications">
                                     <award>
                                         <ns:award ns:year="1998" name="MS award"/>
                                         <ns:award ns:year="1998" name="Peer Recognition"/>
                                     </award>
                                 </title>
                             </ns:book>
                    Dim frag = <fragment>
                                   <%= From i In A1.<ns:book> _
                                       .<title> _
                                       Select i.@name 'qqq
                                   %>
                               </fragment>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(883277, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlAttributeEmbeddedExpressionLineContinuation()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = <e a=
                               <%= 3 %>></e>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(883619, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlEmbeddedExpressionArrayInitializer()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = New Object() {<?xml version="1.0"?>
                                          <%= <e/> %>, _
                                          <?xml version="1.0"?>
                                          <%= <e/> %> _
                                         }
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(883620, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlEntityNumericCharacterReference()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim el3 = <element>&#1234;   &#60; &#70;</element>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(883626, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31153ERR_MissingVersionInXmlDecl_ParseXmlEmbeddedExpressionInPrologue()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = <?xml <%= New XAttribute("some", "1.0") %> version="1.0" ?><e/>
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="31146"/>
            <error id="31172"/>
            <error id="30249"/>
            <error id="31153"/>
        </errors>)
    End Sub

    <WorkItem(883628, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlNameRem()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim A1 As XElement = <Rem />
                    Dim x = A1.<Rem>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(883651, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlEmbeddedExpressionContainsQuery()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()      
                    Dim i3 = <xml>
                                 <%=
                                    From el
                                    In {1, 2, 3}
                                    Select (<node><%= el %></node>)
                                 %>
                             </xml>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(883734, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31160ERR_ExpectedXmlEndPI_ParseXmlPrologueInQuery()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x31 = From i In {}
                              Select <?xml version="1.0">
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="31146"/>
            <error id="31160"/>
            <error id="31165"/>
        </errors>)
    End Sub

    <WorkItem(887785, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlImplicitExplicitLineContinuation()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim xml = 
             _
            <xml></xml>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(887792, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlElementCharDataSibling()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim b = <fragment>
                                <element></element>
                                Sometext
                            </fragment>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(887798, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlGetXmlNamespaceRem()
        ParseAndVerify(<![CDATA[
            Imports <xmlns:Rem = "http://testcase">
            Module Module1
                Sub Main()
                    Dim y = GetXmlNamespace(Rem) + "localname"
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(888542, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlDocumentStopsParsingXml()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Goo()
                Dim x_XML = <?xml version="1.0" encoding="utf-8"?>
                    <contacts>
                        <contact>
                            <name><Prefix>Mr</Prefix>Adam Braden</name>
                            <phone>215 123456</phone>
                        </contact>
                    </contacts>
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(894127, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30001ERR_ParseXmlDocumentWithExpressionBody_NoParseError()
        ParseAndVerify(<![CDATA[
            Dim b = <?xml version="1.0"?>
            <%= <e><%= j.e & i.e %></e> %>
        ]]>)
    End Sub

    <WorkItem(893969, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlPrecededByExplicitLineContinuation()
        ParseAndVerify(<![CDATA[
            Namespace DynLateSetLHS010
                Friend Module DynLateSetLHS010mod
                    Sub DynLateSetLHS010()
                        Dim el = _
                            <name1>
        ]]>,
        <errors>
            <error id="30636"/>
            <error id="31165"/>
            <error id="31151"/>
            <error id="30026"/>
            <error id="30625"/>
            <error id="30626"/>
        </errors>)
    End Sub

    <WorkItem(893973, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlPrecededByExplicitLineContinuationLine()
        ParseAndVerify(<![CDATA[
            Namespace DynLateSetLHS010
Friend Module DynLateSetLHS010mod
Sub DynLateSetLHS010()
Dim el = 
_
<name1>
        ]]>,
            Diagnostic(ERRID.ERR_ExpectedEndNamespace, "Namespace DynLateSetLHS010"),
            Diagnostic(ERRID.ERR_ExpectedEndModule, "Friend Module DynLateSetLHS010mod"),
            Diagnostic(ERRID.ERR_EndSubExpected, "Sub DynLateSetLHS010()"),
            Diagnostic(ERRID.ERR_ExpectedExpression, ""),
            Diagnostic(ERRID.ERR_LineContWithCommentOrNoPrecSpace, "_"),
            Diagnostic(ERRID.ERR_StandaloneAttribute, ""),
            Diagnostic(ERRID.ERR_LocalsCannotHaveAttributes, "<name1>"),
            Diagnostic(ERRID.ERR_ExpectedIdentifier, ""))
    End Sub

    <WorkItem(897813, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlExplicitLineContinuationLineFollowedByLessThan()
        ParseAndVerify(<![CDATA[
' This isn't really xml
Dim el = 
_
<        ]]>,
<errors>
    <error id="30201"/>
    <error id="30999"/>
    <error id="30203"/>
    <error id="30636"/>
</errors>)
    End Sub

    <WorkItem(898451, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31146ERR_ExpectedXmlName_ParseXmlErrorBeginningWithLessThanGreaterThan()
        ParseAndVerify(<![CDATA[
Module TestModule

    Sub Main()

        Dim A = <>
                    <ns:book collection="library">
                        <%= <>
                                <ns:award ns:year="1998" name="Booker Award"/>
                            </> %>
                    </ns:book>
                    <%= returnXml(<args></args>, str) %>
                </>
    End Sub

End Module
        ]]>,
        <errors>
            <error id="31146"/>
            <error id="31146"/>
        </errors>)
    End Sub

    <WorkItem(885888, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlErrorNameStartingWithExclamation()
        ParseAndVerify(<root>
            Class Class1
               Sub Main()
                 Dim y2 = &lt;! [CDATA[]]&gt;
                    '&lt;/&gt; 'Required for error recovery
                End Sub
            End Class
            </root>.Value,
            <errors>
                <error id="31146"/>
                <error id="31169"/>
                <error id="30636"/>
                <error id="31169"/>
                <error id="31170"/>
            </errors>)
    End Sub

    <WorkItem(889091, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31151ERR_MissingXmlEndTag_ParseXmlEmbeddedExpressionMissingPercentGreaterThanToken()
        ParseAndVerify(<![CDATA[
            Class C1
　             Sub S1()
　　　             Dim x = <abc def=<%=baz >
                End Sub
            End Class
            ]]>,
            <errors>
                <error id="31151"/>
                <error id="30201"/>
                <error id="31159"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
    End Sub

    <Fact>
    Public Sub BC31151ERR_MissingXmlEndTag_ParseXmlEmbeddedExpressionMissingExpression()
        ParseAndVerify(<![CDATA[
            Class C1
　             Sub S1()
　　　             Dim x = <%=
                End Sub
            End Class
            ]]>,
            Diagnostic(ERRID.ERR_EmbeddedExpression, "<%="),
            Diagnostic(ERRID.ERR_ExpectedExpression, ""),
            Diagnostic(ERRID.ERR_ExpectedXmlEndEmbedded, ""))
    End Sub

    <WorkItem(889091, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC31151ERR_MissingXmlEndTag_ParseXmlEmbeddedExpressionMissingPercentGreaterThanTokenWithColon()
        ParseAndVerify(<![CDATA[
            Class C1
　             Sub S1()
　　　             Dim x = <abc bar=<%=baz >:
                End Sub
            End Class
            ]]>,
            <errors>
                <error id="31151"/>
                <error id="30201"/>
                <error id="31159"/>
                <error id="30035"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
    End Sub

    <WorkItem(899741, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseIncompleteProcessingInstruction()
        ParseAndVerify(<![CDATA[
            Dim y As Object() = New Object() {<goo/>, <?pi 
            ]]>,
            <errors>
                <error id="30370"/>
                <error id="31160"/>
            </errors>)
    End Sub

    <WorkItem(899919, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseIncompleteXmlDoc()
        ParseAndVerify(<![CDATA[
            Dim rss = <?xml vers]]>,
            <errors>
                <error id="31154"/>
                <error id="30249"/>
                <error id="31153"/>
                <error id="31160"/>
                <error id="31165"/>
            </errors>)
    End Sub

    <WorkItem(900238, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseGetXmlNamespace()
        ParseAndVerify(<![CDATA[
                Dim ns = GetXmlNamespace(
        ]]>,
        <errors>
            <error id="30198"/>
        </errors>)
    End Sub

    <WorkItem(900250, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseExprHoleInXMLDoc()
        ParseAndVerify(<![CDATA[
                Sub New()
MyBase.New(<?xml version="1.0" encoding=<%=
        ]]>,
        <errors>
            <error id="30026"/>
            <error id="31172"/>
            <error id="31160"/>
            <error id="31165"/>
            <error id="30198"/>
        </errors>)
    End Sub

    <WorkItem(903139, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseErrXmlDoc()
        ParseAndVerify(<![CDATA[Dim x1 = <?xml q ?>]]>,
                       <errors>
                           <error id="31154"/>
                           <error id="30249"/>
                           <error id="31153"/>
                           <error id="31165"/>
                       </errors>)
    End Sub

    <WorkItem(903556, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31198ERR_XmlEndCDataNotAllowedInContent_ParseCDataCloseTagInContent()
        'Could not use CDATA since code involves CDATA close tag
        Dim code = "Module M1" & vbCrLf &
            "Dim x = <doc>]]></doc>" & vbCrLf &
            "End Module"
        ParseAndVerify(code,
                       <errors>
                           <error id="31198"/>
                       </errors>)
    End Sub


    <WorkItem(903557, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseBadXmlDocument()
        ParseAndVerify(<![CDATA[
Module M1
Dim x = <?xml name value ?>
<test/>
End Module
]]>, <errors>
         <error id="31154"/>
         <error id="30249"/>
         <error id="31153"/>
     </errors>)
    End Sub

    <WorkItem(903564, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31171ERR_IllegalXmlCommentChar()
        ParseAndVerify(<![CDATA[
Module M1
Dim x = <!-- a -- a -->
End Module
]]>, <errors>
         <error id="31171"/>
     </errors>)
    End Sub

    <WorkItem(903592, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31173ERR_ExpectedXmlWhiteSpace_ParseAttributeSpace()
        ParseAndVerify(<![CDATA[
Module M1
Dim d = 
<a b="c"d="e"/>
End Module
]]>, <errors>
         <error id="31173"/>
     </errors>)
    End Sub

    <WorkItem(903586, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31174ERR_IllegalProcessingInstructionName()
        ParseAndVerify(<![CDATA[
Module M1
    Dim f = <?xmL?>
End Module
]]>, <errors>
         <error id="31174"/>
     </errors>)
    End Sub

    <WorkItem(903938, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseDeclaration_ERR_ExpectedEQ()
        ParseAndVerify(<![CDATA[
Module M1
Dim f = 
<?xml version eq '1.0' ?>
<doc/>
End Module
]]>, <errors>
         <error id="30249"/>
     </errors>)
    End Sub

    <WorkItem(903951, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31177ERR_IllegalXmlWhiteSpace_ParseXmlNameStartsWithNewLine()
        ParseAndVerify(<![CDATA[
Module M1
Dim x1 = <  doc/>
Dim x2 = <
doc/>
dim x3 = < a :b />
dim x4 = <a: b = "1" />
dim x5=<a:b></ a : b >
dim x6=<a b : c="1" />
end module
]]>, Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, "  "),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, Environment.NewLine),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_ExpectedXmlName, "b"),
    Diagnostic(ERRID.ERR_ExpectedXmlName, ""),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_ExpectedXmlName, "b"),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, " "),
    Diagnostic(ERRID.ERR_ExpectedXmlName, "c"))
    End Sub

    <Fact>
    Public Sub BC31177ERR_IllegalXmlWhiteSpace_ParseBracketedXmlQualifiedName()
        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As Object)
        x = x.@< a>
    End Sub
End Module
]]>,
    <errors>
        <error id="31177"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As Object)
        x = x.@< p:a>
    End Sub
End Module
]]>,
    <errors>
        <error id="31177"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As Object)
        x = x.@<a >
    End Sub
End Module
]]>,
    <errors>
        <error id="31177"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As Object)
        x = x.@<p:a >
    End Sub
End Module
]]>,
    <errors>
        <error id="31177"/>
    </errors>)
    End Sub

    <WorkItem(903972, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31150ERR_MismatchedXmlEndTag()
        ParseAndVerify(<![CDATA[
Module M1
Dim d = 
<doc></DOC>
End Module
]]>, <errors>
         <error id="31150"/>
         <error id="31151"/>
     </errors>)
    End Sub

    <WorkItem(903986, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31181ERR_InvalidAttributeValue1_BadVersion()
        ParseAndVerify(<![CDATA[
Module M1
Dim f = 
<?xml version="1.0?"?><e></e>
End Module
]]>, <errors>
         <error id="31181"/>
     </errors>)
    End Sub

    <WorkItem(889870, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlRequiresParensNotReported()
        ParseAndVerify(<![CDATA[
                    Class Class1
                     Sub Goo()
                       Dim f = From e As XProcessingInstruction In <?xpi Val=2?>
                     End Sub
                    End Class
            ]]>)
    End Sub

    <WorkItem(889866, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlEntityReferenceErrorNotExpected()
        ParseAndVerify(<![CDATA[
                    Class Class1
                      Sub Goo()
                          Dim x3 = <goo attr1="&#120; &#60; &#65;"></goo>
                      End Sub
                    End Class

            ]]>)
    End Sub

    <WorkItem(889865, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30026ERR_EndSubExpected_ParseMoreErrorExpectedGreater()
        ParseAndVerify(<![CDATA[
                    Class Class1
                     Sub Goo()
                       dim x1 = <goo attr1='qqq"></>
                     Sub
                    End Class
            ]]>,
            <errors>
                <error id="30026"/>
                <error id="31163"/>
                <error id="30289"/>
                <error id="30026"/>
                <error id="30203"/>
            </errors>)
    End Sub

    <WorkItem(889898, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMoreErrorsExpectedLTAndExpectedXmlEndPI()
        ParseAndVerify(<![CDATA[
                    Class Class1
                        Sub Goo()
                          #If True Then
                             Dim x31 = From i In (<ns:e <%= <ns:e><%= ns & "hello" %></ns:e> %>></ns:e>.<ns:e>) _
                                       Where i.Value <> (<<%= <ns:e><%= ns %></ns:e>.Name %>><%= ns %></>.Value) _
                                       Select <?xml version="1.0">
                                          '</> 'Required for error recovery
                          #Else
                                 'COMPILERWARNING : 42019, "ns & \"hello\""
                          #End If
                        End Sub
                     End Class
            ]]>,
            <errors>
                <error id="31146"/>
                <error id="31160"/>
                <error id="31165"/>
            </errors>)
    End Sub

    <WorkItem(885799, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31170ERR_IllegalXmlNameChar_ParseErrorMismatchSyntaxVSExpectedGreater()
        ParseAndVerify(<![CDATA[
                       Class Class1
                         Sub Scenario1()
                            Dim b = new integer? << <goo/> << <what?/>
                         End Sub
                       End Class
            ]]>,
            <errors>
                <error id="31170"/>
                <error id="30636"/>
            </errors>)
    End Sub

    <WorkItem(885790, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31150ERR_MismatchedXmlEndTag_ParseErrorMismatchExpectedGreaterVSSyntax()
        ParseAndVerify(<![CDATA[
                        Class Class1
                              Sub Main()
                                Dim x = <e a='<%="v"%>'></a>
                             End Sub
                         End Class
            ]]>,
            <errors>
                <error id="31151"/>
                <error id="31155"/>
                <error id="31150"/>
            </errors>)
    End Sub


    <WorkItem(924043, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlEmbeddedExpressionInXmlName()
        ParseAndVerify(<![CDATA[
            Module TestModule
                Sub Main()
                    Dim B = <root>
                                <<%= <abc><root>name</root></abc>...<root>.value %>></>
                            </root>
                End Sub
            End Module      
        ]]>)
    End Sub

    <WorkItem(925953, "DevDiv/Personal")>
    <WorkItem(927711, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31165ERR_ExpectedLT_ParseXmlErrorRecoveryMissingEnd()
        ParseAndVerify(<![CDATA[
            Module TestModule
                dim x=<a><b></a>
            End Module      
        ]]>, <errors>
                 <error id="31151"/>
                 <error id="31165"/>
                 <error id="31146"/>
                 <error id="30636"/>
             </errors>)
    End Sub

    <WorkItem(926593, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlEmbeddedExpressionWithNamespace()
        ParseAndVerify(<![CDATA[
            Namespace ExpandoContext02
Friend Module ExpandoContext02mod
    Sub ExpandoContext02()
        Dim x10 = <?xml version="1.0"?><%= <e <%= New XAttribute("a", "v") %> <%= <e ns:a=<%= "v" %> <%= "b" %>="v"/> %>></e> %>
        Dim a = <e <%= XName.Get("a", GetXmlNamespace(ns).ToString) %>="v"><ns:EE-E>SUCCESS</ns:EE-E></e>
    End Sub
End Module
End Namespace 
        ]]>)
    End Sub

    <WorkItem(926595, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlEmbeddedExpressionWithStatement()
        ParseAndVerify(<![CDATA[
           Module Test
Sub Method()
  Dim b3 = <<%= "e" %> <%= New With {New With {New With {New With {(<e><%= 1 %></e>.Value << 3 >> 1).ToString.ToCharArray}.ToCharArray}.ToCharArray}.ToCharArray}.ToCharArray %>/>
End Sub
End Module
         ]]>)
    End Sub

    <WorkItem(926595, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseErrorXmlEmbeddedExpressionInvalidValue()
        ParseAndVerify(<![CDATA[
           Module Test
Sub Method()
  Dim x7 = <a><%= Class %></a>
  Dim x71 = <a><<%= Function %>/></a>
End Sub
End Module
         ]]>,
            <errors>
                <error id="30201"/>
                <error id="30035"/>
                <error id="30199"/>
                <error id="30198"/>
                <error id="30201"/>
            </errors>)
    End Sub

    <WorkItem(527094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527094")>
    <WorkItem(586871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586871")>
    <Fact>
    Public Sub BC31197ERR_FullWidthAsXmlDelimiter_ParseXmlStart()
        ParseAndVerify(<![CDATA[
            Module Module1

	            Sub Main()
		            Dim x = ＜xml></xml>

                    Dim y =＜STAThread/>

                    Dim z =＜!-- Not a comment --!>

	            End Sub

            End Module      
        ]]>,
    Diagnostic(ERRID.ERR_FullWidthAsXmlDelimiter, "＜"),
    Diagnostic(ERRID.ERR_FullWidthAsXmlDelimiter, "＜"),
    Diagnostic(ERRID.ERR_FullWidthAsXmlDelimiter, "＜"))

        'Note that the first "<" character above
        'is a full-width unicode character (not ascii).
    End Sub

    <WorkItem(927138, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlLargeNumberOfTrailingNewLines()
        ParseAndVerify(<![CDATA[
Module Module1
    Sub Main()
        Try
Dim d = 
<?xml version="1.0"?>

<root/>









        Catch ex as Exception
        End Try
    End Sub
End Module
]]>)
    End Sub

    <WorkItem(927387, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlAttributeValueThatStartsOnNewLine()
        ParseAndVerify(<![CDATA[
Module M1
Dim doc = 
<?xml


version
=
'1.0'
?>
<doc/>

End Module
        ]]>)
    End Sub

    <WorkItem(927823, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31154ERR_IllegalAttributeInXmlDecl_ParseBadXmlWithFullWidthContent()
        ParseAndVerify(<![CDATA[
Module Module2
    Sub Main()
        Dim code = <?xml ｖｅｒｓｉｏｎ＝＂1．0＂？＞
                   ＜ｘｍｌ ｖｅｒ＝＂1．0＂ ｖｅｒ0＝｀1．0＇ ｖｅｒ1＝｀1．0｀ ｖｅｒ2＝＇1．0＇＞
                        ＜＜％＝＂ｘｍｌ＂％＞ ＜％＝＂ｘｍｌ＂％＞＝＜％＝＂ｘｍｌ＂％＞／＞
                        ＜％＝ ＜！－－ｖｅｒｓｉｏｎ＝＂1．0＂－－＞＜？ｖｅｒ？＞ ＜！［ＣＤＡＴＡ［ｖｅｒｓｉｏｎ＝＂1．0＂］］＞ ％＞
                   ＜／＞
    End Sub
End Module]]>,
<errors>
    <error id="31154"/>
    <error id="31169"/>
    <error id="30249"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31169"/>
    <error id="31153"/>
    <error id="31160"/>
    <error id="31165"/>
</errors>)
        'Note that the characters after "<?xml " upto and including "</>" above
        'are full-width unicode characters (not ascii).
    End Sub

    <WorkItem(927834, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31170ERR_IllegalXmlNameChar_ParseXmlWithFullWidthContentInEmbeddedExpression()
        'TODO: This is a change in behavior from Dev10.
        'Please move this test to BreakingChanges.vb if this is a change that we want to keep.
        ParseAndVerify(<![CDATA[
Module Module2
    Sub Main()
        Dim code = <xml ver="hi"><＜％＝＂ｘｍｌ＂％＞/></>
    End Sub
End Module]]>, <errors>
                   <error id="31169"/>
               </errors>)
        'Note that the characters starting from <%= to %> (both inclusive) above
        'are full-width unicode characters (not ascii).
    End Sub

    <WorkItem(928408, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseEmbeddedXMLWithCRLFInTag()
        ParseAndVerify(<![CDATA[Dim x12 = <e><%= <e 
 ]]>, <errors>
          <error id="31151"/>
          <error id="31151"/>
          <error id="30636"/>
          <error id="31165"/>
          <error id="30636"/>
          <error id="31159"/>
          <error id="31165"/>
          <error id="30636"/>
      </errors>)
    End Sub

    <WorkItem(930274, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLegalValueForStandaloneAttributeInPrologue()
        ParseAndVerify(<![CDATA[Module M1
Dim doc = 
<?xml version='1.0' standalone='yes'?><e/>
Dim doc2 = 
<?xml version='1.0' standalone='no'?><e/>
End Module]]>)
    End Sub

    <WorkItem(930256, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31182ERR_InvalidAttributeValue2_ParseEmptyValueForStandaloneAttributeInPrologue()
        ParseAndVerify(<![CDATA[Module M1
Dim x = <?xml version="1.0" standalone=''?><doc/>
End Module
 ]]>,
 <errors>
     <error id="31182"/>
 </errors>)
    End Sub

    <WorkItem(930757, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31182ERR_InvalidAttributeValue2_ParseBadValueForStandaloneAttributeInPrologue()
        ParseAndVerify(<![CDATA[Module M1
Dim doc = 
<?xml version='1.0' standalone='YES'?><e/>
Dim doc2 = 
<?xml version='1.0' standalone='nO'?><e/>
End Module]]>, <errors>
                   <error id="31182"/>
                   <error id="31182"/>
               </errors>)
    End Sub

    <WorkItem(537183, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537183")>
    <WorkItem(930327, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31146ERR_ExpectedXmlName_ParseBadEncodingAttributeInPrologue()
        ParseAndVerify(<![CDATA[module m1
dim x = <?xml version="1.0" "UTF-8"encoding=?>
<!--* wrong ordering in above EncodingDecl *--><root/>
end module

module m2
dim x = <?xml version="1.0"encoding="UTF-8"?>
<!--* missing white space in above EncodingDecl *-->
<root/>
end module]]>, <errors>
                   <error id="31146" message="XML name expected." start="38" end="39"/>
                   <error id="31173" message="Missing required white space." start="161" end="169"/>
               </errors>)
    End Sub

    <WorkItem(930330, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30037ERR_IllegalChar_ParseIllegalXmlCharacters()
        ParseAndVerify("Module M1" & vbCrLf &
"Dim doc = " & vbCrLf &
"<?xml version=""1.0""?><doc>￿</doc>" & vbCrLf &
"End Module" & vbCrLf &
vbCrLf &
"Module M2" & vbCrLf &
"Dim frag = " & vbCrLf &
"<e><doc>￾</doc></>" & vbCrLf &
"End Module", <errors>
                  <error id="30037"/>
                  <error id="30037"/>
              </errors>)
    End Sub

    <WorkItem(538550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538550")>
    <WorkItem(538551, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538551")>
    <Fact>
    Public Sub ParseXmlStringIncludingSmartQuotes()
        ParseAndVerify(
"Module M1" & vbCrLf &
"Dim x1 = <tag attr=""" & ChrW(8216) & """ />" & vbCrLf &
"Dim x2 = <tag attr=""" & ChrW(8217) & """ />" & vbCrLf &
"Dim x3 = <tag attr=""" & ChrW(8220) & """ />" & vbCrLf &
"Dim x4 = <tag attr=""" & ChrW(8221) & """ />" & vbCrLf &
"End Module")
    End Sub

    <Fact>
    Public Sub ParseXmlSmartSingleString()
        ParseAndVerify(
"Module M1" & vbCrLf &
"Dim x1 = <tag attr= " & ChrW(8216) & "text" & ChrW(8216) & "/>" & vbCrLf &
"Dim x2 = <tag attr= " & ChrW(8216) & "text" & ChrW(8217) & "/>" & vbCrLf &
"Dim x3 = <tag attr= " & ChrW(8217) & "text" & ChrW(8216) & "/>" & vbCrLf &
"Dim x4 = <tag attr= " & ChrW(8217) & "text" & ChrW(8217) & "/>" & vbCrLf &
"End Module")
    End Sub

    <Fact>
    Public Sub ParseXmlSmartDoubleString()
        ParseAndVerify(
"Module M1" & vbCrLf &
"Dim x1 = <tag attr= " & ChrW(8220) & "text" & ChrW(8220) & "/>" & vbCrLf &
"Dim x2 = <tag attr= " & ChrW(8220) & "text" & ChrW(8221) & "/>" & vbCrLf &
"Dim x3 = <tag attr= " & ChrW(8221) & "text" & ChrW(8220) & "/>" & vbCrLf &
"Dim x4 = <tag attr= " & ChrW(8221) & "text" & ChrW(8221) & "/>" & vbCrLf &
"End Module")
    End Sub

    <WorkItem(544979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544979")>
    <Fact()>
    Public Sub ParseEmbeddedLambda()
        ParseAndVerify(<![CDATA[
Module Program
    Dim x = <x <%= Sub() Return %>/>
End Module
 ]]>.Value)
    End Sub

    <WorkItem(538241, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538241")>
    <Fact>
    Public Sub ParseXmlMemberFollowedByWSColon()
        Dim tree = ParseAndVerify(<![CDATA[
 Module A
 Sub Main()
 Dim x = <x/>.@x : Console.WriteLine()
 End Sub
 End Module
 ]]>.Value)

        Dim main = tree.GetRoot().ChildNodesAndTokens()(0).ChildNodesAndTokens()(1)

        Dim stmt1 = main.ChildNodesAndTokens()(1)
        Dim stmt2 = main.ChildNodesAndTokens()(2)
        Dim colon = stmt1.ChildNodesAndTokens().LastOrDefault().GetTrailingTrivia().Last
        Assert.Equal(colon.Kind, SyntaxKind.ColonTrivia)
        Assert.Equal(stmt2.Kind(), SyntaxKind.ExpressionStatement)
        Assert.Equal(SyntaxKind.InvocationExpression, DirectCast(stmt2.AsNode, ExpressionStatementSyntax).Expression.Kind)

        Dim exprStmt = TryCast(stmt2.AsNode, ExpressionStatementSyntax)
        Dim invocExp = TryCast(exprStmt.Expression, InvocationExpressionSyntax)
        Dim memAccess = TryCast(invocExp.Expression, MemberAccessExpressionSyntax)

        Assert.Equal(memAccess.Expression.ToString, "Console")
        Assert.Equal(memAccess.Name.ToString, "WriteLine")
    End Sub

    <WorkItem(541291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541291")>
    <Fact()>
    Public Sub Bug7954()
        '                   0123456789ABC
        Dim code = <![CDATA[Dim=<><%=">
<]]>.Value
        Dim tree = VisualBasicSyntaxTree.ParseText(code)
        Assert.Equal(code, tree.GetRoot().ToString())
    End Sub

    <WorkItem(545076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545076")>
    <Fact()>
    Public Sub WhitespaceInClosingTag()
        ParseAndVerify(<![CDATA[
Module M1
    Sub Main
        Dim x = <goo>< /goo>
    End Sub
End Module
]]>,
            Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, "< /"),
            Diagnostic(ERRID.ERR_ExpectedLT, ""))
    End Sub

    <WorkItem(529395, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529395")>
    <Fact()>
    Public Sub Bug12644()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= Sub() If True Then Else %></x>
End Module
]]>)
    End Sub

    <WorkItem(544399, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544399")>
    <Fact()>
    Public Sub BrokenEndElementStartInXmlDoc()
        ParseAndVerify(<![CDATA[
''' </
Module M
End Module


]]>,
            VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
            Diagnostic(ERRID.WRN_XMLDocParseError1, "</").WithArguments("XML end element must be preceded by a matching start element."),
            Diagnostic(ERRID.WRN_XMLDocParseError1, "").WithArguments("'>' expected."))
    End Sub

    <WorkItem(547320, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547320")>
    <WorkItem(548952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/548952")>
    <Fact()>
    Public Sub Bug18598()
        ParseAndVerify(<![CDATA[
Module M
    Private x = <<%= F(End
End Module
]]>,
            <errors>
                <error id="31151"/>
                <error id="30201"/>
                <error id="30198"/>
                <error id="31159"/>
                <error id="30636"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M(+
        Return
    End Sub
End Module
]]>,
            <errors>
                <error id="30203"/>
                <error id="30198"/>
            </errors>)
    End Sub

    <WorkItem(548996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/548996")>
    <Fact()>
    Public Sub Bug548996()
        ParseAndVerify(<![CDATA[
Module M
    Private x = <<%= x +
Return
%>/>
]]>,
            Diagnostic(ERRID.ERR_ExpectedEndModule, "Module M"),
            Diagnostic(ERRID.ERR_MissingXmlEndTag, <![CDATA[<<%= x +
Return
%>]]>),
            Diagnostic(ERRID.ERR_ExpectedExpression, ""),
            Diagnostic(ERRID.ERR_Syntax, "Return"),
            Diagnostic(ERRID.ERR_ExpectedXmlEndEmbedded, ""),
            Diagnostic(ERRID.ERR_IllegalXmlStartNameChar, "%").WithArguments("%", "&H25"),
            Diagnostic(ERRID.ERR_ExpectedEQ, ""),
            Diagnostic(ERRID.ERR_ExpectedLT, ""),
            Diagnostic(ERRID.ERR_ExpectedGreater, ""))
        ParseAndVerify(<![CDATA[
Module M
    Private x = <<%= x +
Return : %>/>
]]>,
            Diagnostic(ERRID.ERR_ExpectedEndModule, "Module M"),
            Diagnostic(ERRID.ERR_MissingXmlEndTag, <![CDATA[<<%= x +
Return : %>]]>),
            Diagnostic(ERRID.ERR_ExpectedExpression, ""),
            Diagnostic(ERRID.ERR_Syntax, "Return"),
            Diagnostic(ERRID.ERR_ExpectedXmlEndEmbedded, ""),
            Diagnostic(ERRID.ERR_ExpectedGreater, ":"),
            Diagnostic(ERRID.ERR_IllegalXmlStartNameChar, "%").WithArguments("%", "&H25"),
            Diagnostic(ERRID.ERR_ExpectedLT, ""),
            Diagnostic(ERRID.ERR_ExpectedGreater, ""))
    End Sub

    <WorkItem(575763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/575763")>
    <Fact()>
    Public Sub Bug575763()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a <%= <b/>
               %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a <%= <b/>

               %>/>
End Module
]]>,
            <errors>
                <error id="30625"/>
                <error id="31151"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31169"/>
                <error id="30249"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a <%= <b/> %>

               c=""/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a <%= <b/>

               c=""/>
End Module
]]>,
            <errors>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
            </errors>)
    End Sub

    <WorkItem(575780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/575780")>
    <Fact()>
    Public Sub Bug575780()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Function() :
    End Sub
End Module
]]>,
            <errors>
                <error id="30201" message="Expression expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() :
    End Sub
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() : Return
    End Sub
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() Return :
    End Sub
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then :
End Module
]]>,
            <errors>
                <error id="30081" message="'If' must end with a matching 'End If'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return :
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return : Return
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else :
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else Return :
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else Return : Return
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then If False Then Else :
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else If False Then Return Else :
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else If False Then Return Else : Return
End Module
]]>)
        ' Dev11: no errors.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then If True Then If True Then :
End Module
]]>,
            <errors>
                <error id="30081" message="'If' must end with a matching 'End If'."/>
            </errors>)
        ' Dev11: no errors.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then If True Then Else If True Then :
End Module
]]>,
            <errors>
                <error id="30081" message="'If' must end with a matching 'End If'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then If True Then If True Then Else :
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then If True Then Else If True Then Else :
End Module
]]>)
    End Sub

    ''' <summary>
    ''' As above but with lambda inside embedded expression.
    ''' </summary>
    <WorkItem(575780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/575780")>
    <Fact()>
    Public Sub Bug575780_EmbeddedExpression()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = <<%= Sub() : Return %>/>
    End Sub
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="30026" message="'End Sub' expected."/>
                <error id="31151"/>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="30636" message="'>' expected."/>
                <error id="31169"/>
                <error id="31165"/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = <<%= Sub() Return : %>/>
    End Sub
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="30026" message="'End Sub' expected."/>
                <error id="31151"/>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="30636" message="'>' expected."/>
                <error id="31169"/>
                <error id="31165"/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then : %>/>
End Module
]]>,
            <errors>
                <error id="31151"/>
                <error id="30081"/>
                <error id="30205"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="30636" message="'>' expected."/>
                <error id="31165"/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then Return : %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then Return : Return %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then Else : %>/>
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="30636" message="'>' expected."/>
                <error id="31169"/>
                <error id="31165"/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then Else Return : %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then Else Return : Return %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then If False Then Else : %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then Else If False Then Return Else : %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then Else If False Then Return Else : Return %>/>
End Module
]]>)
        ' Dev11: no errors.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then If True Then If True Then : %>/>
End Module
]]>,
            <errors>
                <error id="31151"/>
                <error id="30081"/>
                <error id="30205"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="30636" message="'>' expected."/>
                <error id="31165"/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ' Dev11: no errors.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then If True Then Else If True Then : %>/>
End Module
]]>,
            <errors>
                <error id="31151"/>
                <error id="30081"/>
                <error id="30205"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="30636" message="'>' expected."/>
                <error id="31165"/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then If True Then If True Then Else : %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then If True Then If True Then Else : %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <<%= Sub() If True Then If True Then Else If True Then Else : %>/>
End Module
]]>)
    End Sub

    <WorkItem(577617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577617")>
    <Fact()>
    Public Sub Bug577617()
        ParseAndVerify(String.Format(<source>
Module M
    Dim x = {0}x/>
End Module
</source>.Value, FULLWIDTH_LESS_THAN_SIGN),
            Diagnostic(ERRID.ERR_FullWidthAsXmlDelimiter, "＜"))
    End Sub

    <WorkItem(611206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611206")>
    <Fact()>
    Public Sub Bug611206()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <!DOCTYPE
End Module
]]>,
            <errors>
                <error id="31175" message="XML DTDs are not supported."/>
            </errors>)
    End Sub

    <WorkItem(602208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602208")>
    <Fact()>
    Public Sub Bug602208()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a <%= <?xml version="1.0"?>
            <b/>
            <!-- -->
            
        %>/>
End Module
]]>)

        ParseAndVerify(<![CDATA[
Module M
    Dim x = F(<?xml version="1.0"?>
        <b/>

        )
End Module
]]>)

        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a <%= F(<?xml version="1.0"?>
            <b/>



        ) %>/>
End Module
]]>)

        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim q = From c In ""
                Let x = <?xml version="1.0"?>
                        <root/>
 
                Select x
    End Sub
End Module
]]>)

        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim q = From c In ""
                Let x = <?xml version="1.0"?>
                        <root>
                        </root>
 
                Select x
    End Sub
End Module
]]>)

        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim q = From c In ""
                Let x = <?xml version="1.0"?>
                        <root></>
 
                Select x
    End Sub
End Module
]]>)

        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim q = From c In ""
                Let x = <?xml version="1.0"?>
                        <%= <root/> %>
 
                Select x
    End Sub
End Module
]]>)

        ParseAndVerify(<![CDATA[
Module M
    Dim q = From x In ""
        Select <?xml version="1.0"?>
            <x/>
 
        Distinct
End Module
]]>)
    End Sub

    <WorkItem(598156, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598156")>
    <Fact()>
    Public Sub Bug598156()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a <%= <?xml version="1.0"?>
            <b/>
            <!-- -->
        %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a <%= <?xml version="1.0"?>
            <b/>
            
            <?p?>.ToString() %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = F(<?xml version="1.0"?>
        <b/>
        )
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = F(<!-- -->

        )
End Module
]]>,
            <errors>
                <error id="30198" message="')' expected."/>
                <error id="30035" message="Syntax error."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a <%= F(<?xml version="1.0"?>
            <b/>
        ) %>/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = F(<?xml version="1.0"?>
        <b/>

        , Nothing)
End Module
]]>,
            <errors>
                <error id="30198" message="')' expected."/>
                <error id="30035" message="Syntax error."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <a <%= F(<?xml version="1.0"?>
            <b/>

        , Nothing) %>/>
End Module
]]>,
            <errors>
                <error id="30625"/>
                <error id="31151"/>
                <error id="30198" message="')' expected."/>
                <error id="31159"/>
                <error id="30636"/>
                <error id="31169"/>
                <error id="31169"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0"?><x/>
    Dim y = <?xml version="1.0"?><y/>
End Module
]]>)
    End Sub

    <WorkItem(598799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598799")>
    <Fact()>
    Public Sub Bug598799()
        ParseAndVerify(<![CDATA[
Module M
    Dim q = From x In ""
        Select <x/>

        Distinct
End Module
]]>,
            <errors>
                <error id="30188" message="Declaration expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim q = From x In ""
        Select <?xml version="1.0"?>
            <x/> 
        Distinct
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim q = From x In ""
        Select <?xml version="1.0"?>
            <x/>
            ' Comment
        Distinct
End Module
]]>,
            <errors>
                <error id="30188" message="Declaration expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim q = From x In ""
        Select <?xml version="1.0"?>
            <x/> ' Comment
        Distinct
End Module
]]>)
    End Sub

    <WorkItem(601050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601050")>
    <Fact()>
    Public Sub Bug601050()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Sub() If False Then Else
        Dim y = <x/> %>
End Module
]]>,
            <errors>
                <error id="31172" message="An embedded expression cannot be used here."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="30205" message="End of statement expected."/>
            </errors>)
    End Sub

    <WorkItem(601899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601899")>
    <Fact()>
    Public Sub Bug601899()
        ParseAndVerify(<![CDATA[
Module M
    Function F(
        Return <%= Nothing %>
    End Function
End Module
]]>,
            <errors>
                <error id="30183" message="Keyword is not valid as an identifier."/>
                <error id="30198" message="')' expected."/>
                <error id="30037" message="Character is not valid."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F(
        Return <?xml version="1.0"?><%= Nothing %>
    End Function
End Module
]]>,
            <errors>
                <error id="30183" message="Keyword is not valid as an identifier."/>
                <error id="30198" message="')' expected."/>
                <error id="30037" message="Character is not valid."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F(
        Return <!--
    End Function
End Module
]]>,
            <errors>
                <error id="30183" message="Keyword is not valid as an identifier."/>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<source>
Module M
    Function F(
        Return &lt;![CDATA[
    End Function
End Module
</source>.Value,
            <errors>
                <error id="30183" message="Keyword is not valid as an identifier."/>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F(
        Return <? :
    End Function
End Module
]]>,
            <errors>
                <error id="30183" message="Keyword is not valid as an identifier."/>
                <error id="30198" message="')' expected."/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub DocumentWithTrailingMisc()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim q = From c In ""
                Let x = <?xml version="1.0"?>

                        <root/>
                Select x
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim q = From c In ""
                Let x = <?xml version="1.0"?>

                        <root>

                        </root> 
                Select x
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim q = From c In ""
                Let x = <?xml version="1.0"?>

                        <root></> 
                Select x
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim q = From c In ""
                Let x = <?xml version="1.0"?>

                        <%= <root/> %> 
                Select x
    End Sub
End Module
]]>)
    End Sub

    <WorkItem(607253, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607253")>
    <Fact()>
    Public Sub GetXmlNamespaceErrors()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace
End Module
]]>,
            <errors>
                <error id="30199" message="'(' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace :
End Module
]]>,
            <errors>
                <error id="30199" message="'(' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace(
End Module
]]>,
            <errors>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace( :
End Module
]]>,
            <errors>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace(y
End Module
]]>,
            <errors>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace(y:
End Module
]]>,
            <errors>
                <error id="30198" message="')' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace(y :
End Module
]]>,
            <errors>
                <error id="30198" message="')' expected."/>
            </errors>)
        ' Dev11 reports BC30199: "'(' expected."
        ' although that seems unnecessary.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace (y)
End Module
]]>)
    End Sub

    <WorkItem(607352, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607352")>
    <Fact()>
    Public Sub GetXmlNamespaceErrors_2()
        ParseAndVerify(<![CDATA[
Imports <xmlns:y="">
Module M
    Dim x = GetXmlNamespace
        (y)
End Module
]]>,
            <errors>
                <error id="30199" message="'(' expected."/>
                <error id="30035" message="Syntax error."/>
            </errors>)
    End Sub

    <WorkItem(607560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607560")>
    <Fact()>
    Public Sub GetXmlNamespaceErrors_3()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace( y)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace(y )
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace( )
End Module
]]>)
    End Sub

    <WorkItem(610345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/610345")>
    <Fact()>
    Public Sub Bug610345()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = <%= CBool(
        Return
    End Sub
End Module
]]>,
            <errors>
                <error id="31172"/>
                <error id="30201" message="Expression expected."/>
                <error id="30198"/>
                <error id="30035"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%=
End Module
]]>,
            <errors>
                <error id="31172"/>
                <error id="30201" message="Expression expected."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= 'Comment
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="30201" message="Expression expected."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        M(<<%=
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="30026"/>
                <error id="31151"/>
                <error id="30201" message="Expression expected."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="30636"/>
                <error id="31165"/>
                <error id="30636"/>
                <error id="30198"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F( _
#Const c = 0
        ) %></>
End Module
]]>,
            <errors>
                <error id="30201" message="Expression expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F(x </x>
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="32017"/>
                <error id="30198"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F(x $ _
        REM Comment
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="30037"/>
                <error id="32017"/>
                <error id="30198"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (<%= F() )
End Module
]]>,
            <errors>
                <error id="31172"/>
                <error id="30035"/>
                <error id="31159"/>
                <error id="30198"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (<%= F() _
        )
End Module
]]>,
            <errors>
                <error id="31172"/>
                <error id="30035"/>
                <error id="31159"/>
                <error id="30198"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x
        x = <%= F() :::
    End Sub
End Module
]]>,
            <errors>
                <error id="31172"/>
                <error id="31159"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F(x $
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="30037"/>
                <error id="32017"/>
                <error id="30198"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F(x $:
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="30037"/>
                <error id="32017"/>
                <error id="30198"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F(x $ 'Comment
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="30037"/>
                <error id="32017"/>
                <error id="30198"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F(x %>
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="32017"/>
                <error id="30198"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F(x %> :
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="32017"/>
                <error id="30198"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F(x) $ REM
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="30035"/>
                <error id="30037"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F(x) $ _
        REM
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="30035"/>
                <error id="30037"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= {1, 2 3
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="30370"/>
                <error id="30370"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= New Object()(1
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="32014"/>
                <error id="30198"/>
                <error id="30987"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= Function(Of T
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="36674"/>
                <error id="32065"/>
                <error id="30199"/>
                <error id="30198"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= Function(Of T)
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="36674"/>
                <error id="32065"/>
                <error id="30199"/>
                <error id="30198"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= If(Nothing) 'Comment
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="33104"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= If(1, 2 3 REM
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="32017"/>
                <error id="30198"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= F(y z
        ) %>
End Module
]]>,
            <errors>
                <error id="31172" message="An embedded expression cannot be used here."/>
                <error id="32017"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= F(y z
        )
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="32017"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= Sub() a!b c %>
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="30800"/>
                <error id="32017"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= Sub() a!b c %> :
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="31151"/>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
                <error id="30800"/>
                <error id="32017"/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
    End Sub

    <WorkItem(671111, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/671111")>
    <Fact()>
    Public Sub Bug671111()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x
        <y>
        <z></z / </y>
    </x>
End Module
]]>,
            <errors>
                <error id="30636"/>
                <error id="30636"/>
                <error id="31165"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x>
        <y>
        <z></z / </y>
    </x>
End Module
]]>,
            <errors>
                <error id="30636"/>
                <error id="31165"/>
            </errors>)
    End Sub

    <WorkItem(673558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673558")>
    <Fact()>
    Public Sub Bug673558()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><
<]]>,
            <errors>
                <error id="30625"/>
                <error id="31151"/>
                <error id="31151"/>
                <error id="31146"/>
                <error id="30636"/>
                <error id="31151"/>
                <error id="31146"/>
                <error id="30636"/>
                <error id="31165"/>
                <error id="30636"/>
                <error id="31165"/>
                <error id="30636"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><
REM]]>,
            <errors>
                <error id="30625"/>
                <error id="31151"/>
                <error id="31151"/>
                <error id="31177"/>
                <error id="30636"/>
                <error id="31165"/>
                <error id="30636"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
    End Sub

    <WorkItem(673638, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673638")>
    <Fact()>
    Public Sub NotLessThan_Imports()
        ParseAndVerify(<![CDATA[
Imports <%=xmlns="">
]]>,
            <errors>
                <error id="31165"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <%=>
]]>,
            <errors>
                <error id="31165"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <%=%>
]]>,
            <errors>
                <error id="31165"/>
                <error id="31169"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports </xmlns="">
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports </>
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <?xmlns="">
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <?>
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <!--xmlns="">
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <!-->
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <!---->
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <![CDATA[xmlns="">
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <![CDATA[>
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify("Imports <![CDATA[]]>",
            <errors>
                <error id="30203"/>
                <error id="30037"/>
                <error id="30037"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <!DOCTYPExmlns="">
]]>,
            <errors>
                <error id="31165"/>
                <error id="31175"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <!DOCTYPE>
]]>,
            <errors>
                <error id="31165"/>
                <error id="31175"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub NotLessThan_BracketedXmlName()
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.<%= y %>
End Module
]]>,
            <errors>
                <error id="31165"/>
                <error id="31146"/>
                <error id="30201"/>
                <error id="30037"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.@<%= y %>
End Module
]]>,
            <errors>
                <error id="31165"/>
                <error id="31146"/>
                <error id="30201"/>
                <error id="30037"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.</y>
End Module
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.@</y>
End Module
]]>,
            <errors>
                <error id="31146"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.<?y>
End Module
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.@<?y>
End Module
]]>,
            <errors>
                <error id="31146"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.<!--y>
End Module
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.@<!--y>
End Module
]]>,
            <errors>
                <error id="31146"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.<![CDATA[y>
End Module
]]>,
            <errors>
                <error id="30203"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.@<![CDATA[y>
End Module
]]>,
            <errors>
                <error id="31146"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.<!DOCTYPE>
End Module
]]>,
            <errors>
                <error id="31165"/>
                <error id="31146"/>
                <error id="30201"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.@<!DOCTYPE>
End Module
]]>,
            <errors>
                <error id="31165"/>
                <error id="31146"/>
                <error id="30201"/>
            </errors>)
    End Sub

    <WorkItem(674567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674567")>
    <Fact()>
    Public Sub Bug674567()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x><%= <p:
y a=""/>
End Module
]]>,
            <errors>
                <error id="30625"/>
                <error id="31151"/>
                <error id="31177"/>
                <error id="31146"/>
                <error id="31159"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= <p:  
End Module
]]>,
            <errors>
                <error id="31172"/>
                <error id="31151"/>
                <error id="31177"/>
                <error id="31146"/>
                <error id="30636"/>
                <error id="31165"/>
                <error id="30636"/>
                <error id="31159"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub XmlNameTokenPossibleKeywordKind()
        Const sourceTemplate = "
Module M
    Dim x = <{0}:
y a=""/>
End Module
"

        Const squiggleTemplate = "<{0}:
y a=""/>
End Module
"
        Dim commonExpectedErrors =
        {
            Diagnostic(ERRID.ERR_ExpectedEndModule, "Module M"),
            Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, "
"),
            Diagnostic(ERRID.ERR_ExpectedXmlName, "y"),
            Diagnostic(ERRID.ERR_ExpectedQuote, ""),
            Diagnostic(ERRID.ERR_ExpectedLT, ""),
            Diagnostic(ERRID.ERR_ExpectedGreater, "")
        }

        Dim tree1 = Parse(String.Format(sourceTemplate, "e"))
        tree1.GetDiagnostics().Verify(commonExpectedErrors.Concat({Diagnostic(ERRID.ERR_MissingXmlEndTag, String.Format(squiggleTemplate, "e"))}).ToArray())

        Dim tree2 = Parse(String.Format(sourceTemplate, "ee"))
        tree2.GetDiagnostics().Verify(commonExpectedErrors.Concat({Diagnostic(ERRID.ERR_MissingXmlEndTag, String.Format(squiggleTemplate, "ee"))}).ToArray())

        Dim getPossibleKeywordKind = Function(x As XmlNameSyntax) DirectCast(x.Green, InternalSyntax.XmlNameSyntax).LocalName.PossibleKeywordKind

        Dim kinds1 = tree1.GetRoot().DescendantNodes().OfType(Of XmlNameSyntax).Select(getPossibleKeywordKind)
        Assert.NotEmpty(kinds1)
        AssertEx.All(kinds1, Function(k) k = SyntaxKind.XmlNameToken)

        Dim kinds2 = tree2.GetRoot().DescendantNodes().OfType(Of XmlNameSyntax).Select(getPossibleKeywordKind)
        Assert.Equal(kinds1, kinds2)
    End Sub

    <Fact()>
    Public Sub TransitionFromXmlToVB()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace( ' comment
End Module
]]>,
            <errors>
                <error id="30198"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace(p   
End Module
]]>,
            <errors>
                <error id="30198"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <xmlns:p=""   
Module M
End Module
]]>,
            <errors>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.@<  
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31146"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x.@<p ' comment 
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
Dim x = <?xml version="1.0" encoding=<%=
        ]]>,
            <errors>
                <error id="30625"/>
                <error id="31172"/>
                <error id="31160"/>
                <error id="31165"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
Dim x = <?xml version="1.0" encoding=<%=""%>
        ]]>,
            <errors>
                <error id="30625"/>
                <error id="31172"/>
                <error id="31160"/>
                <error id="31165"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
Dim x = F(<?xml version="1.0" encoding=<%=F(
        ]]>,
            <errors>
                <error id="30625"/>
                <error id="31172"/>
                <error id="31160"/>
                <error id="31165"/>
                <error id="30198"/>
            </errors>)
    End Sub

    <WorkItem(682381, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682381")>
    <Fact()>
    Public Sub Bug682381()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= If(1
, 2) %>
End Module
]]>,
            <errors>
                <error id="31172"/>
                <error id="33104"/>
                <error id="30198"/>
                <error id="31159"/>
                <error id="30035"/>
            </errors>)
    End Sub

    <WorkItem(682391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682391")>
    <Fact()>
    Public Sub Bug682391()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version <%= e %>
End Module
]]>,
            <errors>
                <error id="31181"/>
                <error id="31172"/>
                <error id="30249"/>
                <error id="31160"/>
                <error id="31165"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version=1.0 <%= e %>
End Module
]]>,
            <errors>
                <error id="31181"/>
                <error id="31154"/>
                <error id="31169"/>
                <error id="31173"/>
                <error id="31172"/>
                <error id="30249"/>
                <error id="31160"/>
                <error id="31165"/>
            </errors>)
    End Sub

    <WorkItem(682394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682394")>
    <Fact()>
    Public Sub Bug682394()
        ParseAndVerify(<![CDATA[
Imports <%=:p>
]]>,
            <errors>
                <error id="31165"/>
            </errors>)
        ParseAndVerify(<![CDATA[
    Imports <<%=:p>>
]]>,
            <errors>
                <error id="31187"/>
                <error id="30636"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub IncompleteMultilineLambdaInEmbeddedExpression()
        ParseAndVerify(<![CDATA[
Class C
    Dim x = <x><%= Sub()
]]>,
            <errors>
                <error id="30481"/>
                <error id="31151"/>
                <error id="36673"/>
                <error id="31159"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub FullWidthEmbeddedExpressionTokens()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Nothing %>
End Module
]]>.Value.Replace("<", ToFullWidth("<")),
            <errors>
                <error id="31197"/>
                <error id="30037"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Nothing %>
End Module
]]>.Value.Replace("<%", ToFullWidth("<%")),
            <errors>
                <error id="31197"/>
                <error id="30037"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Nothing %>
End Module
]]>.Value.Replace("<%=", ToFullWidth("<%=")),
            <errors>
                <error id="31197"/>
                <error id="30037"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Nothing %>
End Module
]]>.Value.Replace("%>", ToFullWidth("%") & ">"),
            <errors>
                <error id="31172"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Nothing %>
End Module
]]>.Value.Replace("%>", ToFullWidth("%>")),
            <errors>
                <error id="31172"/>
                <error id="30035"/>
                <error id="30037"/>
                <error id="31159"/>
                <error id="30201"/>
            </errors>)
    End Sub

    <WorkItem(684872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/684872")>
    <Fact()>
    Public Sub Bug684872()
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x... 
        <x>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x... 

        <x>
End Module
]]>,
            <errors>
                <error id="31165"/>
                <error id="31146"/>
                <error id="32035"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x... _
        <x>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x... _

        <x>
End Module
]]>,
            <errors>
                <error id="31165"/>
                <error id="31146"/>
                <error id="32035"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x... 
        _
        <x>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x... 

        _
        <x>
End Module
]]>,
            <errors>
                <error id="31165"/>
                <error id="31146"/>
                <error id="32035"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x... _
        _
        <x>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x... 
        </x>
End Module
]]>,
            <errors>
                <error id="31165"/>
                <error id="31146"/>
                <error id="30035"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim y = x... 
        <%=x>
End Module
]]>,
            <errors>
                <error id="31165"/>
                <error id="31146"/>
                <error id="30201"/>
                <error id="30037"/>
            </errors>)
    End Sub

    <WorkItem(693901, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/693901")>
    <Fact()>
    Public Sub Bug693901()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x/
$>
End Module
]]>.Value.Replace("$"c, NEXT_LINE),
            <errors>
                <error id="30625"/>
                <error id="31151"/>
                <error id="30636"/>
                <error id="31169"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x/
$>
End Module
]]>.Value.Replace("$"c, LINE_SEPARATOR),
            <errors>
                <error id="30625"/>
                <error id="31151"/>
                <error id="30636"/>
                <error id="31169"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x/
$>
End Module
]]>.Value.Replace("$"c, PARAGRAPH_SEPARATOR),
            <errors>
                <error id="30625"/>
                <error id="31151"/>
                <error id="30636"/>
                <error id="31169"/>
                <error id="31165"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x
$/>
End Module
]]>.Value.Replace("$"c, NEXT_LINE),
            <errors>
                <error id="31169"/>
                <error id="30249"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x
$=""/>
End Module
]]>.Value.Replace("$"c, NEXT_LINE),
            <errors>
                <error id="31169"/>
            </errors>)
    End Sub

    <WorkItem(716121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716121")>
    <Fact()>
    Public Sub Bug716121()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x
$!/>
End Module
]]>.Value.Replace("$"c, NO_BREAK_SPACE),
            <errors>
                <error id="31169"/>
                <error id="30249"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x
$!/>
End Module
]]>.Value.Replace("$"c, IDEOGRAPHIC_SPACE),
            <errors>
                <error id="31169"/>
                <error id="30249"/>
            </errors>)
        ' Test all Unicode space characters other than &H20.
        For i = &H21 To &HFFFF
            Dim c = ChrW(i)
            ' Note: SyntaxFacts.IsWhitespace(c) considers &H200B as
            ' space even though the UnicodeCategory is Format.
            If (Char.GetUnicodeCategory(c) = Globalization.UnicodeCategory.SpaceSeparator) OrElse
                SyntaxFacts.IsWhitespace(c) Then
                ParseAndVerify(<![CDATA[
Module M
    Dim x = <x
$!/>
End Module
        ]]>.Value.Replace("$"c, c),
                    <errors>
                        <error id="31169"/>
                        <error id="30249"/>
                    </errors>)
            End If
        Next
    End Sub

    <WorkItem(697114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/697114")>
    <Fact()>
    Public Sub Bug697114()
        ' No attributes.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml?><x/>
End Module
]]>,
            <errors>
                <error id="31153"/>
            </errors>)
        ' One attribute.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0"?><x/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a=""?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
                <error id="31153"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8"?><x/>
End Module
]]>,
            <errors>
                <error id="31153"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes"?><x/>
End Module
]]>,
            <errors>
                <error id="31153"/>
            </errors>)
        ' Two attributes, starting with version.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" a=""?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" encoding="utf-8"?><x/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" standalone="yes"?><x/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" version="1.0"?><x/>
End Module
]]>,
            <errors>
                <error id="31149"/>
            </errors>)
        ' Two attributes, starting with unknown.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" a=""?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
                <error id="31154"/>
                <error id="31153"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" encoding="utf-8"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
                <error id="31153"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" standalone="yes"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
                <error id="31153"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" version="1.0"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
            </errors>)
        ' Two attributes, starting with encoding.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8" a=""?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
        <error id="31153"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8" encoding="utf-8"?><x/>
End Module
]]>,
            <errors>
                <error id="31149"/>
                <error id="31153"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8" standalone="yes"?><x/>
End Module
]]>,
            <errors>
                <error id="31153"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8" version="1.0"?><x/>
End Module
]]>,
            <errors>
                <error id="31156"/>
            </errors>)
        ' Two attributes, starting with standalone.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes" a=""?><x/>
End Module
]]>,
<errors>
    <error id="31154"/>
    <error id="31153"/>
</errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes" encoding="utf-8"?><x/>
End Module
]]>,
            <errors>
                <error id="31157"/>
                <error id="31153"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes" standalone="yes"?><x/>
End Module
]]>,
            <errors>
                <error id="31149"/>
                <error id="31153"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes" version="1.0"?><x/>
End Module
]]>,
            <errors>
                <error id="31156"/>
            </errors>)
        ' Three attributes, starting with version.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" a="" encoding="utf-8"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" a="" standalone="yes"?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" encoding="utf-8" a=""?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" encoding="utf-8" standalone="yes"?><x/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" standalone="yes" a=""?><x/>
End Module
]]>,
<errors>
    <error id="31154"/>
</errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" standalone="yes" encoding="utf-8"?><x/>
End Module
]]>,
<errors>
    <error id="31157"/>
</errors>)
        ' Three attributes, starting with unknown.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" version="1.0" encoding="utf-8"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" version="1.0" standalone="yes"?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" encoding="utf-8" version="1.0"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
                <error id="31156"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" encoding="utf-8" standalone="yes"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
                <error id="31153"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" standalone="yes" version="1.0"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
                <error id="31156"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" standalone="yes" encoding="utf-8"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
                <error id="31157"/>
                <error id="31153"/>
            </errors>)
        ' Three attributes, starting with encoding.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8" version="1.0" a=""?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
        <error id="31156"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8" version="1.0" standalone="yes"?><x/>
End Module
]]>,
    <errors>
        <error id="31156"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8" a="" version="1.0"?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
        <error id="31156"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8" a="" standalone="yes"?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
        <error id="31153"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8" standalone="yes" version="1.0"?><x/>
End Module
]]>,
    <errors>
        <error id="31156"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml encoding="utf-8" standalone="yes" a=""?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
        <error id="31153"/>
    </errors>)
        ' Three attributes, starting with standalone.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes" version="1.0" a=""?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
        <error id="31156"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes" version="1.0" encoding="utf-8"?><x/>
End Module
]]>,
    <errors>
        <error id="31156"/>
        <error id="31157"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes" a="" version="1.0"?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
        <error id="31156"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes" a="" encoding="utf-8"?><x/>
End Module
]]>,
    <errors>
        <error id="31154"/>
        <error id="31157"/>
        <error id="31153"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes" encoding="utf-8" version="1.0"?><x/>
End Module
]]>,
    <errors>
        <error id="31157"/>
        <error id="31156"/>
    </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml standalone="yes" encoding="utf-8" a=""?><x/>
End Module
]]>,
    <errors>
        <error id="31153"/>
        <error id="31157"/>
        <error id="31154"/>
    </errors>)
        ' Four attributes.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml a="" version="1.0" encoding="utf-8" standalone="yes"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" a="" encoding="utf-8" standalone="yes"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" encoding="utf-8" a="" standalone="yes"?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0" encoding="utf-8" standalone="yes" a=""?><x/>
End Module
]]>,
            <errors>
                <error id="31154"/>
            </errors>)
    End Sub

    ''' <summary>
    ''' Tests that the REM keyword cannot be neither left nor right part of a qualified XML name.
    ''' But FULLWIDTH COLON (U+FF1A) should never be parsed as a qualified XML name separator, so REM can follow it.
    ''' Also, the second colon should never be parsed as a qualified XML name separator.
    ''' </summary>
    <Fact>
    <WorkItem(529880, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529880")>
    Public Sub NoRemInXmlNames()

        ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        Dim x = <a/>.@rem
    End Sub
End Module]]>,
            Diagnostic(ERRID.ERR_ExpectedXmlName, "@").WithLocation(4, 22))

        ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        Dim x = <a/>.@rem:goo
    End Sub
End Module]]>,
            Diagnostic(ERRID.ERR_ExpectedXmlName, "@").WithLocation(4, 22))

        ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        Dim x = <a/>.@xml:rem
    End Sub
End Module]]>,
            Diagnostic(ERRID.ERR_ExpectedXmlName, "").WithLocation(4, 27))

        ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        Dim x = <a/>.@xml:rem$
    End Sub
End Module]]>,
            Diagnostic(ERRID.ERR_ExpectedXmlName, "").WithLocation(4, 27))

        ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        Dim x = <a/>.@xml :rem
    End Sub
End Module]]>)

        ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        Dim x = <a/>.@xml:goo:rem
    End Sub
End Module]]>)

        ' FULLWIDTH COLON is represented by "~" below
        ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        Dim x = <a/>.@goo~rem
    End Sub
End Module]]>.Value.Replace("~"c, FULLWIDTH_COLON))

        ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        Dim x = <a/>.@goo~rem$
    End Sub
End Module]]>.Value.Replace("~"c, FULLWIDTH_COLON))

    End Sub

    <WorkItem(969980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969980")>
    <WorkItem(123533, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems?_a=edit&id=123533")>
    <Fact>
    Public Sub UnaliasedXmlImport_Local()
        Dim source = "
Imports <xmlns = ""http://xml"">
"
        Dim compilation = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseDll)

        Const bug123533IsFixed = False

        If bug123533IsFixed Then
            compilation.AssertTheseDiagnostics(<expected><![CDATA[
BC50001: Unused import statement.
Imports <xmlns = "http://xml">
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                               ]]></expected>, False)
        Else
            compilation.AssertTheseDiagnostics(<expected><![CDATA[
BC50001: Unused import statement.
Imports <xmlns = "http://xml">
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31187: Namespace declaration must start with 'xmlns'.
Imports <xmlns = "http://xml">
         ~
BC30636: '>' expected.
Imports <xmlns = "http://xml">
         ~~~~~
                                               ]]></expected>, False)
        End If
    End Sub

    <WorkItem(969980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969980")>
    <WorkItem(123533, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems?_a=edit&id=123533")>
    <Fact>
    Public Sub UnaliasedXmlImport_Project()
        Dim import = "<xmlns = ""http://xml"">"
        Const bug123533IsFixed = False

        If bug123533IsFixed Then
            CreateCompilationWithMscorlib40({""}, options:=TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse(import))).VerifyDiagnostics()
        Else
            Assert.Throws(Of ArgumentException)(Sub() GlobalImport.Parse(import))
        End If
    End Sub

    <WorkItem(1042696, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1042696")>
    <Fact>
    Public Sub ParseXmlTrailingNewLinesBeforeDistinct()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From y in "" Select <?xml version="1.0"?>
        <x/>

        <!-- -->


    Dim y = x
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <?xml version="1.0"?>
        <x/>

        <!-- -->


    Distinct
End Module
]]>,
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "Distinct"))
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From y in "" Select <?xml version="1.0"?>
        <x/>

        <!-- -->


    Distinct
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From y in "" Select <?xml version="1.0"?>
        <x/>


    Distinct
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From y in "" Select <x/>


    Distinct
End Module
]]>,
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "Distinct"))
        ParseAndVerify(<![CDATA[
Module M
    Sub F()
        If Nothing Is <?xml version="1.0"?>
            <x/>


        Then
        End If
    End Sub
End Module
]]>,
            Diagnostic(ERRID.ERR_Syntax, "Then"))
        ParseAndVerify(<![CDATA[
Module M
    Sub F()
        If Nothing Is <?xml version="1.0"?>
            <x/>

            <!-- --> Then
        End If
    End Sub
End Module
]]>)
    End Sub

End Class
