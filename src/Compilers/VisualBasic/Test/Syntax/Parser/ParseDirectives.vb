' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

<CLSCompliant(False)>
Public Class ParseDirectives
    Inherits BasicTestBase

    <Fact>
    Public Sub ParseConstDirective()
        ParseAndVerify(<![CDATA[
            #const DEBUG=true
            Module Module1
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseReferenceDirective()
        ParseAndVerify(<![CDATA[
            #r "reference"
        ]]>, TestOptions.Script)

        ParseAndVerify(<![CDATA[
            #r "reference"
        ]]>,
        <errors>
            <error id="36964" message="#R is only allowed in scripts" start="14" end="15"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub FloatsAndUnaryNot()
        ParseAndVerify(<![CDATA[
Imports System
Imports Microsoft.VisualBasic
'Type Types
#If -1S Then
'Short
#End If
#If -1% Then
'Integer
#End If
#If -1@ Then
'Decimal
#End If
#If -1.0! Then
'Single
#End If
#If -1.0# Then
'Double
#End If
'Forced Literal Types
#If -1S Then
'Short
#End If
#If -1I Then
'Integer
#End If
#If -1L Then
'Long
#End If
#If -1.0F Then
'Single
#End If
#If -1D Then
'Double
#End If
#If -1UI Then
'unsigned Integer
#End If
#If -1UL Then
'unsigned Long
#End If
'Type Types
#If +1S Then
'Short
#End If
#If +1% Then
'Integer
#End If
#If +1@ Then
'Decimal
#End If
#If +1.0! Then
'Single
#End If
#If +1.0# Then
'Double
#End If
'Forced Literal Types
#If +1S Then
'Short
#End If
#If +1I Then
'Integer
#End If
#If +1L Then
'Long
#End If
#If +1.0F Then
'Single
#End If
#If +1D Then
'Double
#End If
#If +1UI Then
'unsigned Integer
#End If
#If +1UL Then
'unsigned Long
#End If
'Type Types
#If Not 1S Then
'Short
#End If
#If Not 1% Then
'Integer
#End If
#If Not 1@ Then
'Decimal
#End If
#If Not 1.0! Then
'Single
#End If
#If Not 1.0# Then
'Double
#End If
'Forced Literal Types
#If Not 1S Then
'Short
#End If
#If Not 1I Then
'Integer
#End If
#If Not 1L Then
'Long
#End If
#If Not 1.0F Then
'Single
#End If
#If Not 1D Then
'Double
#End If
#If Not 1UI Then
'unsigned Integer
#End If
#If Not 1UL Then
'unsigned Long
#End If
Module Module1
    Sub main()
    End Sub
End Module

        ]]>)
    End Sub

    <WorkItem(545871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545871")>
    <Fact>
    Public Sub FW_Hash()
        ParseAndVerify(<![CDATA[
＃If True Then
＃Else
＃End If

        ]]>)
    End Sub


    <WorkItem(679758, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/679758")>
    <Fact>
    Public Sub TypeCharMismatch()
        ParseAndVerify(<![CDATA[
#Const C2 = "."c
#If C2% = 1 Then
#End If
        ]]>,
        <errors>
            <error id="31427" message="Syntax error in conditional compilation expression." start="18" end="34"/>
        </errors>
        )
    End Sub

    <WorkItem(530922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530922")>
    <WorkItem(658448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658448")>
    <Fact>
    Public Sub FullWidthDirective()
        ParseAndVerify(<![CDATA[

#Const x = 1

＃If x = 1 Then

＃Else
    Blah Blah
＃End If

#If False Then
＃End Ｒｅｇｉｏｎ
#End If

#If False Then
#Ｒｅｇｉｏｎ
#End Ｒｅｇｉｏｎ
#End If

        ]]>)
    End Sub

    <Fact>
    Public Sub PreprocessorSkipped001()
        ParseAndVerify(<![CDATA[
#If False Then
' _
#End If

#If False Then
' " _
#End If

#If False Then
#If False Then
#Const X = 1
#End If
#End If

#If False Then

#if _
#End If

#End If

#If False Then

#if <!-- %% ~ "garbage ' REM # _
#End If

#End If

#If False Then
#if True
  disabled code
#Else
  also disabled
#End If
#End If
        ]]>)
    End Sub

    <Fact>
    Public Sub PreprocessorSkipped001Err()
        ParseAndVerify(<![CDATA[
#If False Then
#Const X = <!--
#Else
-->
#End If
        ]]>,
        <errors>
            <error id="30035" message="Syntax error." start="38" end="39"/>
        </errors>)
    End Sub

    <WorkItem(531493, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531493")>
    <Fact>
    Public Sub Repro18189()
        ParseAndVerify(<![CDATA[
#If False Then
REM _
#End If

        ]]>)
    End Sub

    <WorkItem(697520, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/697520")>
    <Fact>
    Public Sub BigShift()
        ParseAndVerify(<![CDATA[

Module Module1

    Sub Main()

    End Sub

End Module


#If 2 << 311 Then
                BlahBlah
#End If

#Const c1 = 1
#Const c2 = 2147483647

#If c1 + CLng(c2) <> -CLng(-21474836 >> 48) Then
                BlahBlah
#End If

        ]]>,
    Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "BlahBlah"),
    Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "BlahBlah")
)
    End Sub

    <WorkItem(530921, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530921")>
    <Fact>
    Public Sub Repro17195()
        ParseAndVerify(<![CDATA[
#If False Then
#If _
_
#End If
#End If

        ]]>)
    End Sub

    <WorkItem(530679, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530679")>
    <Fact>
    Public Sub Repro16694()
        ParseAndVerify(<![CDATA[
#If False Then
#If False Then
#End If ' _ _
#Else
Module M
End Module
#End If ' _ _

#If False Then
#If False Then
#End If ' a_ _
#Else
Module M1
End Module
#End If ' _ _

        ]]>)
    End Sub

    <WorkItem(545871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545871")>
    <Fact>
    Public Sub ParseIfDirectiveWithCChar()
        ParseAndVerify(<![CDATA[
            #If CChar("")
        ]]>,
        <errors>
            <error id="30311" message="Value of type 'Char' cannot be converted to 'Boolean'." start="13" end="26"/>
            <error id="30012" message="'#If' block must end with a matching '#End If'." start="35" end="35"/>
        </errors>)

    End Sub

    <Fact>
    Public Sub ParseEnabledIfDirective()
        ParseAndVerify(<![CDATA[
            #const DEBUG=true
            #if DEBUG
            ' This is for debug and will not be skipped
            class c1
                Sub s
                end sub
            end class
            #end if

            Module Module1
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseDisabledIfDirective()
        ParseAndVerify(<![CDATA[
            #const DEBUG=false
            #if DEBUG
            ' This should be disabled
            class c1
                Sub s
                end sub
            end class
            #end if

            Module Module1
            End Module
        ]]>)
    End Sub

    <WorkItem(538581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538581")>
    <Fact>
    Public Sub ParseDisabledIfDirectiveWithBad()
        ParseAndVerify(<![CDATA[
#If False Then
##EndIf
#End If
            Module Module1
            End Module
        ]]>)
    End Sub

    <WorkItem(528617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528617")>
    <Fact>
    Public Sub LineContinuationInDisabledText()
        ParseAndVerify(<![CDATA[
#If False
#Const x = <!--"--> _
#End If
#End If

        ]]>,
        <errors>
            <error id="30013"/>
        </errors>)
    End Sub

    <WorkItem(545211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545211")>
    <Fact>
    Public Sub FunctionKeywordInDisabledText()
        ParseAndVerify(<![CDATA[
#If False Then
#Const = Function  
#End If
        ]]>)
    End Sub

    <WorkItem(586984, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586984")>
    <Fact>
    Public Sub DW_Underscore()
        ParseAndVerify(<![CDATA[
Module Module1

    Sub Main()
        If _
            True Then

        End _
                If

        Dim x = 2 +
2
    End Sub

End Module


#If False Then
#If
#End _
Region
#End If
#End If
        ]]>)
    End Sub

    <WorkItem(586984, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586984")>
    <Fact>
    Public Sub DW_Underscore_001()
        ParseAndVerify(<![CDATA[
Module Module1

    Sub Main()
        If _
            True Then

        End _
                If

        Dim x = 2 +
2
    End Sub

End Module


#If False Then
#If
#End _
Region
#End If
#End If
        ]]>.Value.Replace("_"c, SyntaxFacts.FULLWIDTH_LOW_LINE),
    Diagnostic(ERRID.ERR_ExpectedEndIf, "If "),
    Diagnostic(ERRID.ERR_ExpectedExpression, ""),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"),
    Diagnostic(ERRID.ERR_Syntax, "True"),
    Diagnostic(ERRID.ERR_UnrecognizedEnd, "End"),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"),
    Diagnostic(ERRID.ERR_ExpectedEndIf, "If"),
    Diagnostic(ERRID.ERR_ExpectedExpression, ""),
    Diagnostic(ERRID.ERR_LbNoMatchingIf, "#End If"))
    End Sub

    <WorkItem(538578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538578")>
    <Fact>
    Public Sub ParseDisabledIfDirectiveWithUnderscore()
        ParseAndVerify(<![CDATA[
#if true
Module M
#If False
_ 
#End If

#If False
 _ 
#End If

End Module

#If False Then
_ _
#End If

#End If

        ]]>)
    End Sub

    <Fact>
    Public Sub ParseIfElseIfDirective()
        ParseAndVerify(<![CDATA[
            #const DEBUG=false
            #if DEBUG
            ' This should be disabled
            class c1
                Sub s
                end sub
            end class
            #Elseif IDE
            ' This should also be disabled
            class c2
                Sub s2
                end sub
            end class
            #end if
            Module Module1
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub BC30028ERR_LbElseNoMatchingIf_ParseElseBeforeIfDirective()
        ParseAndVerify(<![CDATA[
            #const DEBUG=false
            class c1
                Sub s
                end sub
            end class
            ' Else without a preceding #if
            #Else
            class c2
                Sub s2
                end sub
            end class
            #end if
            Module Module1
            End Module
        ]]>,
        <Errors>
            <error id="30028"/>
        </Errors>)
    End Sub

    <WorkItem(880778, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30012_ParsePreprocessorIf()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
            #If CONFIG Then
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30026"/>
            <error id="30625"/>
            <error id="30012"/>
        </errors>)
    End Sub

    <WorkItem(542109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542109")>
    <Fact>
    Public Sub BC30277_ParseConstTypeChar()
        ParseAndVerify(<![CDATA[
#Const X% = 1
#Const Y = X$
        ]]>,
        <errors>
            <error id="30277"/>
        </errors>)
    End Sub

    <WorkItem(541882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541882")>
    <Fact>
    Public Sub ParseConstWithLineContinuation()
        ParseAndVerify(<![CDATA[
#If False
#Const x = 1 ' _
#End If 

        ]]>)
    End Sub

    <Fact>
    Public Sub ParseWithLineContinuation()
        ParseAndVerify(<![CDATA[
#If False
blah _
#End If 

        ]]>)
    End Sub

    <WorkItem(528617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528617")>
    <Fact()>
    Public Sub ParseConstWithLineContinuation1()
        ParseAndVerify(<![CDATA[
#If False
#Const x = <!--"--> _
#End If
#End If

        ]]>,
        <errors>
            <error id="30013" message="'#ElseIf', '#Else', or '#End If' must be preceded by a matching '#If'." start="41" end="48"/>
        </errors>)
    End Sub

    <WorkItem(537851, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537851")>
    <WorkItem(538488, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538488")>
    <Fact>
    Public Sub ParseLiteralIfDirective()
        ParseAndVerify(<![CDATA[
            #if 1D < 0
                blah blah
            #end if

            #if 1D > 0
                Class cls1
            #end if

            End Class

            #if 1.1 < 0
                blah blah
            #end if

            #Const Scen2 = 1.1D

            #If Scen2 <> 1.1D Then
                lala
            #End IF

            Module Module1
            End Module
        ]]>)
    End Sub

    <WorkItem(538486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538486")>
    <Fact>
    Public Sub ParseNothingStringCompare()
        ParseAndVerify(<![CDATA[
#Const A = Nothing
#Const B = ""
#If A = B Then
Class X
#End If
End Class

#If A >= B Then
Class Y
#End If
End Class

Class Z
#If A < B Then
Class Z
#End If
End Class
        ]]>)
    End Sub

    <WorkItem(536090, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536090")>
    <Fact>
    Public Sub BC30035ERR_Syntax_ParsePreprocessorIfAfterLineTerminator()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x : #If CONFIG Then
            #End If
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30035"/>
            <error id="30013"/>
        </errors>)
    End Sub

    <WorkItem(538589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538589")>
    <Fact>
    Public Sub ParsePreprocessorSeparatedWithColon()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
#If False Then : #Else
#End If
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30205"/>
        </errors>)
    End Sub

    <WorkItem(881425, "DevDiv/Personal")>
    <Fact>
    Public Sub ParsePreprocessorIfNested()
        ParseAndVerify(<![CDATA[
            #If FIRST Then
            #If SECOND
            #End If
            #End If
        ]]>)
    End Sub

    <WorkItem(881437, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30826ERR_ObsoleteEndIf_ParsePreprocessorIfEndIfNoSpace()
        ParseAndVerify(<![CDATA[
            #If true
            #Endif
        ]]>,
        <errors>
            <error id="30826"/>
        </errors>)
    End Sub

    <WorkItem(881560, "DevDiv/Personal")>
    <Fact>
    Public Sub ParsePreprocessorIfParenthesizedExpression()
        ParseAndVerify(<![CDATA[
            #If Not (VALUE Or EULAV) Then
            #End If
        ]]>)
    End Sub

    <WorkItem(881586, "DevDiv/Personal")>
    <Fact>
    Public Sub ParsePreprocessorIfInLambdaBody()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = Sub()
            #If TEST Then
                                Console.WriteLine("Hi")
            #End If
                            End Sub
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(882906, "DevDiv/Personal")>
    <Fact>
    Public Sub ParsePreprocessorIfNestedDate()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
            #If False Then
                    Dim y2k =
                    #1/1/2000#
            #End If
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(883737, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30248ERR_ExpectedConditionalDirective()
        ParseAndVerify(<![CDATA[
            #
            #X
        ]]>,
        <errors>
            <error id="30248"/>
            <error id="30248"/>
        </errors>)
    End Sub

    <WorkItem(883744, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30013ERR_LbNoMatchingIf_ParsePreprocessorIfLineContinuation()
        ParseAndVerify(<![CDATA[
            #If (
            True) Then
            #End If

                #If(true
                ) then
                #End If
#End If
        ]]>,
        <errors>
            <error id="30013"/>
            <error id="30198"/>
            <error id="30201"/>
            <error id="30198"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParsePreprocessorExternalSource()
        ParseAndVerify(<![CDATA[
            module module1
                sub main()
                    #externalsource("c:\wwwwroot\inetpub\test.aspc", 30)
                    console.writeline("In test.aspx")
                    #end Externalsource
                end sub
            end module
        ]]>)
    End Sub

    <Fact>
    Public Sub ParsePreprocessorExternalChecksumBad()
        ParseAndVerify(<![CDATA[#externalchecksum("c:\wwwwroot\inetpub\test.aspc", _
"{12345678-1234-1234-1234-12345678901bc}", _
"1a2b3c4e65f617239a49b9a9c0391849d34950f923fab9484")
            module module1
                sub main()
                    #externalsource("c:\wwwwroot\inetpub\test.aspc", 30)
                    console.writeline("In test.aspx")
                    #end Externalsource
                end sub
            end module
        ]]>,
            Diagnostic(ERRID.WRN_BadGUIDFormatExtChecksum, """{12345678-1234-1234-1234-12345678901bc}"""),
            Diagnostic(ERRID.WRN_BadChecksumValExtChecksum, """1a2b3c4e65f617239a49b9a9c0391849d34950f923fab9484""")
        )
    End Sub

    <Fact>
    Public Sub ParsePreprocessorExternalChecksumBad001()
        ParseAndVerify(<![CDATA[#externalchecksum("c:\wwwwroot\inetpub\test.aspc", _
"{406EA660-64CF-4C82-B6F0-42D48172A79A}", _
"1a2v")
            module module1
                sub main()
                    #externalsource("c:\wwwwroot\inetpub\test.aspc", 30)
                    console.writeline("In test.aspx")
                    #end Externalsource
                end sub
            end module
        ]]>,
            Diagnostic(ERRID.WRN_BadChecksumValExtChecksum, """1a2v""")
        )
    End Sub

    <Fact>
    Public Sub ParsePreprocessorExternalChecksum()
        ParseAndVerify(<![CDATA[#externalchecksum("c:\wwwwroot\inetpub\test.aspc", _
"{406EA660-64CF-4C82-B6F0-42D48172A79A}", _
"1a2b3c4e")
            module module1
                sub main()
                    #externalsource("c:\wwwwroot\inetpub\test.aspc", 30)
                    console.writeline("In test.aspx")
                    #end Externalsource
                end sub
            end module
        ]]>)
    End Sub

    <WorkItem(888306, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30035_ParseEndExternalChecksum()
        ParseAndVerify(<![CDATA[
            #End ExternalChecksum
        ]]>,
        <errors>
            <error id="30035"/>
        </errors>)
    End Sub

    <WorkItem(888313, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31427ERR_BadCCExpression_ParsePreProcessorIfGetType()
        ParseAndVerify(<![CDATA[
            #If GetType(x) Then
            #End If
        ]]>,
        <errors>
            <error id="31427"/>
        </errors>)
    End Sub

    <WorkItem(893255, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30012_ParsePreProcessorIfWithEnd()
        ParseAndVerify(<![CDATA[
            Module m1
                Public Sub goo()
#If True Then
#End
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30012"/>
            <error id="30035"/>
        </errors>)
    End Sub

    <WorkItem(620, "https://github.com/dotnet/roslyn/issues/620")>
    <Fact>
    Public Sub TestRecentUnicodeVersion()
        ' Ensure that the characters Ǉ and ǈ are considered matching under case insensitivity
        ParseAndVerify(<![CDATA[
#Const Ǉ = True
#if ǈ
Class MissingEnd
#end if
        ]]>,
        Diagnostic(ERRID.ERR_ExpectedEndClass, "Class MissingEnd").WithLocation(4, 1)
        )
    End Sub

    <WorkItem(893259, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30012_ParsePreProcessorIfIncompleteExpression()
        ParseAndVerify(<![CDATA[
            Module m1
            Public Sub goo()
#If Not
            End Sub
            End Module

        ]]>,
        <errors>
            <error id="30012"/>
            <error id="30201"/>
        </errors>)
    End Sub

    <WorkItem(893956, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30201_ParseDirectiveUnaryOp()
        ParseAndVerify(<![CDATA[
           #Const Defined = -
        ]]>,
        <errors>
            <error id="30201"/>
        </errors>)

        ParseAndVerify(<![CDATA[
           #If Not as
        ]]>,
        <errors>
            <error id="30012"/>
            <error id="31427"/>
        </errors>)
    End Sub

    <WorkItem(893962, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30625ERR_ExpectedEndModule_ParseIncompleteDirective()
        ParseAndVerify(<![CDATA[
           Namespace DynLateSetLHS010
Friend Module DynLateSetLHS010mod
Sub DynLateSetLHS010()
#If TestIDOLateBinding Then
#Else If Not TestOrcasLateBinding
#End If
        ]]>,
        <errors>
            <error id="30026"/>
            <error id="30625"/>
            <error id="30626"/>
        </errors>)
    End Sub

    <WorkItem(895828, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseRegionDirective()
        ParseAndVerify(<![CDATA[
           #Region "Scen7"
#End Region
        ]]>)
    End Sub

    <WorkItem(897107, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30201_ParseComparisonOpInIF()
        ParseAndVerify(<![CDATA[
          #If Not VS7_BETA2 =
        ]]>,
        <errors>
            <error id="30012"/>
            <error id="30201"/>
        </errors>)
    End Sub

    <WorkItem(897858, "DevDiv/Personal")>
    <Fact>
    Public Sub BC32030ERR_LbElseifAfterElse_ParsePreProcessorElseIf()
        ParseAndVerify(<![CDATA[
            #If False Then
            #Else
            #ElseIf True Then
            #End If
        ]]>,
        <errors>
            <error id="32030"/>
        </errors>)
    End Sub

    <WorkItem(898733, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30188_ParsePreProcessorIfTrueAndIfFalse()
        ParseAndVerify(<![CDATA[
            #If False Then
                $
            #End If
        ]]>)

        ParseAndVerify(<![CDATA[
            #If True Then
                $
            #End If
        ]]>,
            Diagnostic(ERRID.ERR_IllegalChar, "$"))
    End Sub

    <WorkItem(899059, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30012ERR_LbExpectedEndIf_ParsePreProcessorMissingEndIf()
        'With Then
        ParseAndVerify(<![CDATA[
            #If False Then
        ]]>, <errors>
                 <error id="30012"/>
             </errors>)
        'Without Then
        ParseAndVerify(<![CDATA[
            #If A
        ]]>, <errors>
                 <error id="30012"/>
             </errors>)
    End Sub

    <WorkItem(899913, "DevDiv/Personal")>
    <Fact>
    Public Sub ParsePreProcWithComment()
        ParseAndVerify(<![CDATA[
             #If CBool(Win32) Or CBool(Win16) Then 'Just return the input string
        ]]>, <errors>
                 <error id="30012"/>
             </errors>)
    End Sub

    <WorkItem(899941, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30199ERR_ExpectedLparen_ParseConstDirWithCtype()
        ParseAndVerify(<![CDATA[
    #Const Scen4Cons = CByte
        ]]>, <errors>
                 <error id="30198"/>
                 <error id="30201"/>
                 <error id="30199"/>
             </errors>)
    End Sub

    <WorkItem(900194, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30188_ParseIfNumber()
        '#if 0 should be equivalent to #if false
        ParseAndVerify(<![CDATA[
                #if 0 Then
                    $
                #end if
        ]]>)

        ParseAndVerify(<![CDATA[
                #if 0 + 1 -1 Then
                    $
                #end if
        ]]>)

        '#if 1 should be equivalent to #if true
        ParseAndVerify(<![CDATA[
            #If 1 Then
                $
            #End If
        ]]>, Diagnostic(ERRID.ERR_IllegalChar, "$"))

        '#if -1 should be equivalent to #if true
        ParseAndVerify(<![CDATA[
            #If -1 Then
                $
            #End If
        ]]>, Diagnostic(ERRID.ERR_IllegalChar, "$"))
    End Sub

    <WorkItem(899913, "DevDiv/Personal")>
    <Fact>
    Public Sub ParsePreProcessorBuiltinCast()
        ParseAndVerify(<![CDATA[
            #If CBool(Win32) Or CBool(Win16) Then 'Just return the input string
                garbage
            #ElseIf True Then

            #else 
                garbage
            #End If
        ]]>)
    End Sub

    <WorkItem(898448, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30059ERR_RequiredConstExpr_ParsePreProcessorBuiltinCastCObj()
        ParseAndVerify(<![CDATA[
            #Const x = CObj(1)
            #Const x = CObj(Nothing)
        ]]>, <errors>
                 <error id="30060"/>
                 <error id="30059"/>
             </errors>)
    End Sub

    <WorkItem(527211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527211")>
    <WorkItem(904877, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30681ERR_ExpectedEndRegion()
        ParseAndVerify(<![CDATA[
            #Region "Start"
        ]]>,
        Diagnostic(ERRID.ERR_ExpectedEndRegion, "#Region ""Start"""))
    End Sub

    <WorkItem(527211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527211")>
    <WorkItem(904877, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30681ERR_ExpectedEndRegion2()
        ParseAndVerify(<![CDATA[
            #Region "Start"
            Class C
                #Region "Continue"
                #End Region
            End Class
            #Region "Tailing"
        ]]>,
        Diagnostic(ERRID.ERR_ExpectedEndRegion, "#Region ""Start"""),
        Diagnostic(ERRID.ERR_ExpectedEndRegion, "#Region ""Tailing"""))
    End Sub

    <WorkItem(904877, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30680ERR_EndRegionNoRegion()
        ParseAndVerify(<![CDATA[
            #End Region
        ]]>,
        <errors>
            <error id="30680"/>
        </errors>)
    End Sub

    <WorkItem(904899, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30201ERR_ExpectedExpression_ParseMultilineConditionalCompile()
        ParseAndVerify(<![CDATA[#If CompErrorTest =
true Then
#End IF]]>,
        <errors>
            <error id="30201"/>
        </errors>)
    End Sub

    <WorkItem(904912, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30481ERR_ExpectedEndClass_ParseIfDirectiveElseIfDirectiveBothTrue()
        ParseAndVerify(<![CDATA[
            #If True Then
            Class Class1
            #ElseIf True Then
            End Class
            #End If
        ]]>,
        <errors>
            <error id="30481"/>
        </errors>)
    End Sub

    <WorkItem(905021, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30580ERR_NestedExternalSource()
        ParseAndVerify(<![CDATA[
class c1
    Sub goo()
#externalsource("",2)
#externalsource("",2)
#end externalsource
#end externalsource
    End Sub
End Class
        ]]>,
        <errors>
            <error id="30580"/>
            <error id="30578"/>
        </errors>)
    End Sub

    <WorkItem(527211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527211")>
    <WorkItem(927710, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30205ERR_ExpectedEOS()
        ParseAndVerify(<![CDATA[Module M1
#region ""#end region
End Module]]>,
        Diagnostic(ERRID.ERR_ExpectedEndRegion, "#region """""),
        Diagnostic(ERRID.ERR_ExpectedEOS, "#"))
    End Sub

    <Fact>
    Public Sub ParseIfElseIfDirectiveViaOptions()
        Dim text = <![CDATA[
            #const DEBUG=false
            #if DEBUG
            ' This should be disabled
            class c1
                Sub s
                end sub
            end class
            #Elseif IDE
            ' This should be enabled
            class c2
                Sub s2
                end sub
            end class
            #end if
            Module Module1
            End Module
        ]]>.Value

        ' define DEBUG, IDE, and then undefine DEBUG
        Dim options = VisualBasicParseOptions.Default.WithPreprocessorSymbols({
                          New KeyValuePair(Of String, Object)("DEBUG", True),
                          New KeyValuePair(Of String, Object)("Ide", True),
                          New KeyValuePair(Of String, Object)("DeBuG", Nothing)})

        Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.From(text), options, "")

        Dim tk = tree.GetRoot().FindToken(text.IndexOf("class c2", StringComparison.Ordinal))
        Assert.Equal(SyntaxKind.ClassKeyword, tk.Kind)

        tk = tree.GetRoot().FindToken(text.IndexOf("class c1", StringComparison.Ordinal))
        Assert.Equal(260, tk.FullWidth)

    End Sub

    <WorkItem(537144, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537144")>
    <WorkItem(929947, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseNestedDirectives()
        ParseAndVerify(<![CDATA[
#If LANG_OE_JP Then
#If Not ULTRAVIOLET Then
#End If 'UV
#end if
            ]]>).VerifyNoZeroWidthNodes().VerifyOccurrenceCount(SyntaxKind.DisabledTextTrivia, 1)
    End Sub


    <WorkItem(538483, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538483")>
    <Fact>
    Public Sub ParseDirectiveWithStatementOnLine()
        ParseAndVerify(<![CDATA[
#If True : Module M : End Module
#End If
            ]]>,
            <error>
                <error id="30205" message="End of statement expected." start="10" end="11"/>
            </error>).VerifyNoZeroWidthNodes().VerifyOccurrenceCount(SyntaxKind.DisabledTextTrivia, 0)
    End Sub

    <WorkItem(538750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538750")>
    <Fact>
    Public Sub ParseDirectiveWithStrings()
        ParseAndVerify(<![CDATA[
#If False Then
#Const "c _
#Else
Class X
#End If
End Class
            ]]>).VerifyNoZeroWidthNodes().VerifyOccurrenceCount(SyntaxKind.DisabledTextTrivia, 1)

        ParseAndVerify(<![CDATA[
#If False Then
#Const """c _
#Else
Class X
#End If
End Class
            ]]>).VerifyNoZeroWidthNodes().VerifyOccurrenceCount(SyntaxKind.DisabledTextTrivia, 1)

        ParseAndVerify(<![CDATA[
#If False Then
#Const ""c_
#Else
Class X
#End If
End Class
            ]]>).VerifyNoZeroWidthNodes().VerifyOccurrenceCount(SyntaxKind.DisabledTextTrivia, 1)

        ParseAndVerify(<![CDATA[
Class c1
#If False Then
#Const c _
#Else
Class X
#End If
End Class
End Class
            ]]>).VerifyNoZeroWidthNodes().VerifyOccurrenceCount(SyntaxKind.DisabledTextTrivia, 1)

        ParseAndVerify(<![CDATA[
Class c1
#If False Then
#Const ""c _
#Else
Class X
#End If
End Class
End Class
            ]]>).VerifyNoZeroWidthNodes().VerifyOccurrenceCount(SyntaxKind.DisabledTextTrivia, 1)
    End Sub

    <WorkItem(528675, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528675")>
    <Fact()>
    Public Sub ParseDirectiveAfterLabel()
        ParseAndVerify(<![CDATA[
Module M
    Sub Main()
Label: #Const a = 1
    End Sub
End Module
            ]]>,
            <errors>
                <error id="30035"/>
            </errors>)
    End Sub

    <WorkItem(552845, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552845")>
    <Fact()>
    Public Sub Repro552845()
        ParseAndVerify(<![CDATA[
#If comperrortest then
 #: BC30452]]>, <errors>
                    <error id="30012"/>
                </errors>)
    End Sub

    <WorkItem(552845, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552845")>
    <Fact()>
    Public Sub Repro552845_1()
        ParseAndVerify(<![CDATA[
#If comperrortest then
 #10/10/1956#: BC30452]]>, <errors>
                               <error id="30012"/>
                           </errors>)
    End Sub

    <WorkItem(552845, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552845")>
    <Fact()>
    Public Sub Repro552845_2()
        ParseAndVerify(<![CDATA[
#If comperrortest then
 #
 BC30452]]>, <errors>
                 <error id="30012"/>
             </errors>)
    End Sub

    <WorkItem(552845, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552845")>
    <Fact()>
    Public Sub Repro552845_3()
        ParseAndVerify(<![CDATA[
#If comperrortest then
 #10/10/1956#
 BC30452]]>, <errors>
                 <error id="30012"/>
             </errors>)
    End Sub


    <WorkItem(9710, "DevDiv_Projects/Roslyn")>
    <WorkItem(542447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542447")>
    <Fact>
    Public Sub ParseConditionalIfElseIfElse()
        ParseAndVerify(<![CDATA[
#Const Win32 = -1

Module Program

#If Win32 Then
    Dim MaxLimit As Integer = 67000
#ElseIf Mac Then
    Dim MaxLimit as Integer = 5000                                     
#Else
    Dim MaxLimit As Integer = 33000
#End If

End Module]]>).VerifyOccurrenceCount(SyntaxKind.FieldDeclaration, 1)
    End Sub

    <WorkItem(2914, "DevDiv_Projects/Roslyn")>
    <Fact>
    Public Sub Bug2914()
        ParseAndVerify(<![CDATA[

Namespace CHDIR48
{
    Class CHDIR48mod
    {
        public static void CHDIR48()
        {
           #if Mac//ORIGINAL: #If Mac Then

#else//ORIGINAL: #Else

#End If

        }
    }
}
]]>, <errors>
         <error id="30626" message="'Namespace' statement must end with a matching 'End Namespace'." start="2" end="19"/>
         <error id="30035" message="Syntax error." start="20" end="21"/>
         <error id="30481" message="'Class' statement must end with a matching 'End Class'." start="26" end="42"/>
         <error id="30035" message="Syntax error." start="47" end="48"/>
         <error id="30205" message="End of statement expected." start="76" end="83"/>
         <error id="30035" message="Syntax error." start="94" end="95"/>
         <error id="31427" message="Syntax error in conditional compilation expression." start="115" end="116"/>
         <error id="30205" message="End of statement expected." start="124" end="125"/>
         <error id="30205" message="End of statement expected." start="145" end="146"/>
         <error id="30035" message="Syntax error." start="203" end="204"/>
         <error id="30035" message="Syntax error." start="205" end="206"/>
     </errors>)
    End Sub

    <WorkItem(675842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/675842")>
    <Fact()>
    Public Sub BadDateInConditionalCompilation()
        ' Failed to parse.
        ParseAndVerify(<![CDATA[
#If True Then
#05/01/#
#End If
]]>,
            <errors>
                <error id="31085"/>
            </errors>)
        ParseAndVerify(<![CDATA[
#If False Then
#05/01/#
#End If
]]>)
        ' Parsed but invalid.
        ParseAndVerify(<![CDATA[
#If True Then
#05/01/13#
#End If
]]>,
            <errors>
                <error id="31085"/>
            </errors>)
        ParseAndVerify(<![CDATA[
#If False Then
#05/01/13#
#End If
]]>)
        ' Full-width versions of above.
        ParseAndVerify(<![CDATA[
#If True Then
#05/01/13#
#End If
]]>.Value.Replace("#"c, SyntaxFacts.FULLWIDTH_NUMBER_SIGN),
            <errors>
                <error id="31085"/>
            </errors>)
        ParseAndVerify(<![CDATA[
#If False Then
#05/01/13#
#End If
]]>.Value.Replace("#"c, SyntaxFacts.FULLWIDTH_NUMBER_SIGN))
    End Sub

    <Fact()>
    Public Sub EOFInConditionalCompilation()
        ParseAndVerify(<![CDATA[#If True Then
#]]>,
            <errors>
                <error id="30248"/>
                <error id="30012"/>
            </errors>)
        ParseAndVerify(<![CDATA[#If False Then
#]]>,
            <errors>
                <error id="30012"/>
            </errors>)
        ParseAndVerify(<![CDATA[#If True Then
]]>,
            <errors>
                <error id="30012"/>
            </errors>)
        ParseAndVerify(<![CDATA[#If False Then
]]>,
            <errors>
                <error id="30012"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub Bug586811()
        Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
        <compilation>
            <file name="a.vb">
#Const X = If(True, 1, "2")
#Const Y = 1
#Const Z = If(True, 1, 2)
#Const U = If(False, "2", 1)
#Const V = If(1, "2")
#Const W = If("2",1)

#Const X1 = If(True, 1UL, 2L)
#Const X2 = If(True, 1, Nothing)
#Const X3 = If(True, 1, CObj(Nothing))
#Const X4 = 2.1D  
#Const X5 = If(True, Nothing, Nothing)
#Const X6 = If(True, Nothing, 1)
                </file>
        </compilation>, TestOptions.ReleaseDll)

        AssertTheseParseDiagnostics(compilation,
<expected>
BC30060: Conversion from 'Integer' to 'Object' cannot occur in a constant expression.
#Const X = If(True, 1, "2")
~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'String' to 'Object' cannot occur in a constant expression.
#Const U = If(False, "2", 1)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
#Const V = If(1, "2")
~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'ULong' to 'Object' cannot occur in a constant expression.
#Const X1 = If(True, 1UL, 2L)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
#Const X3 = If(True, 1, CObj(Nothing))
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
    End Sub

    <WorkItem(780817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/780817")>
    <Fact>
    Public Sub ParseProjConstsCaseInsensitivity()

        Dim psymbols = ImmutableArray.Create({Roslyn.Utilities.KeyValuePairUtil.Create("Blah", CObj(False)), Roslyn.Utilities.KeyValuePairUtil.Create("blah", CObj(True))})

        Dim options As VisualBasicParseOptions = VisualBasicParseOptions.Default.WithPreprocessorSymbols(psymbols)

        ParseAndVerify(
        <![CDATA[
            Module module1
        Sub main()

            If True Then
#If Blah Then
                    end if    
#End If

                If True
        #If blah Then
                    end if    
#End If

        End Sub
    End Module
        ]]>, options)
    End Sub

#Region "#Disable Warning, #Enable Warning"

#Region "Parser Tests"
    <Fact>
    Public Sub ParseWarningDirective_NoErrorCodes()
        Dim tree = ParseAndVerify(<![CDATA[
# disable warning rem comment
# _
enable _
warning 'comment]]>)
        tree.VerifyNoMissingChildren()
        tree.VerifyNoZeroWidthNodes()
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.Equal(0, disableNode.ErrorCodes.Count)

        Dim enableNode = DirectCast(root.GetLastDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.Equal(0, enableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_WithErrorCodes()
        Dim tree = ParseAndVerify(<![CDATA[
Module Module1
    Sub Main
    # _
enable warning[BC42024], _789 'comment
#  disable   warning _
disable , BC41008   rem comment
    End Sub
End Module]]>)
        tree.VerifyNoMissingChildren()
        tree.VerifyNoZeroWidthNodes()
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.Equal(2, enableNode.ErrorCodes.Count)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.Equal("[BC42024]", enableNode.ErrorCodes(0).ToString)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
        Assert.Equal("_789", enableNode.ErrorCodes(1).Identifier.ValueText)

        Dim disableNode = DirectCast(root.GetLastDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.Equal(2, disableNode.ErrorCodes.Count)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(0).Kind)
        Assert.Equal("disable", disableNode.ErrorCodes(0).ToString)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(1).Kind)
        Assert.Equal("BC41008", disableNode.ErrorCodes(1).Identifier.ValueText)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_FullWidth()
        Dim tree = ParseAndVerify(<![CDATA[
Module Module1
    Sub Main
＃ＤＩＳＡＢＬＥ ＷＡＲＮＩＮＧ ［ＷＡＲＮＩＮＧ］ _

＃ _ 
 ｅｎａｂｌｅ     ｗａｒｎｉｎｇ _
ｅｎａｂｌｅ
    End Sub
End Module]]>)
        tree.VerifyNoMissingChildren()
        tree.VerifyNoZeroWidthNodes()
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes.Single.Kind)
        Assert.Equal("［ＷＡＲＮＩＮＧ］", disableNode.ErrorCodes.Single.Identifier.ToString)

        Dim enableNode = DirectCast(root.GetLastDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes.Single.Kind)
        Assert.Equal("ｅｎａｂｌｅ", enableNode.ErrorCodes.Single.Identifier.ValueText)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadDisableKeyword1()
        Dim tree = ParseAndVerify(<![CDATA[#[disable] warning BC42024]]>,
            <errors>
                <error id="30248" message="'If', 'ElseIf', 'Else', 'Const', 'Region', 'ExternalSource', 'ExternalChecksum', 'Enable', 'Disable', or 'End' expected." start="0" end="1"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.BadDirectiveTrivia, 2)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadDisableKeyword2()
        Dim tree = ParseAndVerify(<![CDATA[#disable$ warning BC42025]]>,
            <errors>
                <error id="30248" message="'If', 'ElseIf', 'Else', 'Const', 'Region', 'ExternalSource', 'ExternalChecksum', 'Enable', 'Disable', or 'End' expected." start="0" end="1"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.BadDirectiveTrivia, 2)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_MissingWarningKeyword1()
        Dim tree = ParseAndVerify(<![CDATA[
#enable 'warning
Class Class1
End Class]]>,
            <errors>
                <error id="31218" message="'Warning' expected." start="20" end="20"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.True(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, enableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_MissingWarningKeyword2()
        Dim tree = ParseAndVerify(<![CDATA[#disable BC42024]]>,
            <errors>
                <error id="31218" message="'Warning' expected." start="9" end="9"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Single.Kind)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.True(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, disableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_MissingWarningKeyword3()
        Dim tree = ParseAndVerify(<![CDATA[#disable , BC42024]]>,
            <errors>
                <error id="31218" message="'Warning' expected." start="9" end="9"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(2, skippedTokens.DescendantTokens.Count)
        Assert.Equal(SyntaxKind.CommaToken, skippedTokens.DescendantTokens.First.Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Last.Kind)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.True(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, disableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadWarningKeyword1()
        Dim tree = ParseAndVerify(<![CDATA[
Enum E
    A
#disable disable
End Enum]]>,
            <errors>
                <error id="31218" message="'Warning' expected." start="26" end="26"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Single.Kind)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.True(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, disableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadWarningKeyword2()
        Dim tree = ParseAndVerify(<![CDATA[#enable Const BC42025]]>,
            <errors>
                <error id="31218" message="'Warning' expected." start="8" end="8"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(2, skippedTokens.DescendantTokens.Count)
        Assert.Equal(SyntaxKind.ConstKeyword, skippedTokens.DescendantTokens.First.Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Last.Kind)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.True(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, enableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadWarningKeyword3()
        Dim tree = ParseAndVerify(<![CDATA[
Enum E
    A
#Disable enable blah, BC42024 BC41005
End Enum]]>,
            <errors>
                <error id="31218" message="'Warning' expected." start="26" end="26"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(5, skippedTokens.DescendantTokens.Count)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.First.Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Skip(1).First.Kind)
        Assert.Equal(SyntaxKind.CommaToken, skippedTokens.DescendantTokens.Skip(2).First.Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Skip(3).First.Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Last.Kind)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.True(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, disableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadWarningKeyword4()
        Dim tree = ParseAndVerify(<![CDATA[#Disable Warning$]]>,
            <errors>
                <error id="31218" message="'Warning' expected." start="9" end="9"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Single.Kind)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.True(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, disableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadWarningKeyword5()
        Dim tree = ParseAndVerify(<![CDATA[#disable [warning] BC42024]]>,
            <errors>
                <error id="31218" message="'Warning' expected." start="9" end="9"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(2, skippedTokens.DescendantTokens.Count)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.First.Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Last.Kind)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.True(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, disableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_MissingErrorCode1()
        Dim tree = ParseAndVerify(<![CDATA[#enable warning BC42024,,bc123]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="26" end="26"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(3, enableNode.ErrorCodes.Count)
        Assert.False(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.True(enableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
        Assert.False(enableNode.ErrorCodes(2).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(2).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_MissingErrorCode2()
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning bc42025, _]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="24" end="24"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(2, enableNode.ErrorCodes.Count)
        Assert.False(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.True(enableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadErrorCode1()
        Dim tree = ParseAndVerify(<![CDATA[#enable warning @42024, bc42025]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="16" End="16"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(2, skippedTokens.DescendantTokens.Count)
        Assert.Equal(SyntaxKind.AtToken, skippedTokens.DescendantTokens.First.Kind)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, skippedTokens.DescendantTokens.Last.Kind)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(2, enableNode.ErrorCodes.Count)
        Assert.True(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.False(enableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadErrorCode2()
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning Dim]]>,
            <errors>
                <error id="30183" message="Keyword is not valid as an identifier." start="16" end="19"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.False(enableNode.ErrorCodes.Single.IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes.Single.Kind)
        Assert.Equal("Dim", enableNode.ErrorCodes.Single.ToString)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_MissingComma1()
        Dim tree = ParseAndVerify(<![CDATA[#enable warning BC42024 bc42025]]>,
            <errors>
                <error id="30196" message="Comma expected." start="24" end="24"/>
                <error id="30203" message="Identifier expected." start="31" end="31"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Single.Kind)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(2, enableNode.ErrorCodes.Count)
        Assert.False(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.True(enableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_MissingComma2()
        Dim tree = ParseAndVerify(<![CDATA[#enable warning bc42105, bc42024 _
            SomeId, SomeOtherId]]>,
            <errors>
                <error id="30196" message="Comma expected." start="47" end="47"/>
                <error id="30203" message="Identifier expected." start="66" end="66"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(3, skippedTokens.DescendantTokens.Count)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.First.Kind)
        Assert.Equal(SyntaxKind.CommaToken, skippedTokens.DescendantTokens.Skip(1).First.Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Last.Kind)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(3, enableNode.ErrorCodes.Count)
        Assert.False(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.False(enableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
        Assert.True(enableNode.ErrorCodes(2).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadComma1()
        Dim tree = ParseAndVerify(<![CDATA[#enable warning, bc41008, someid]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="15" end="15"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(3, enableNode.ErrorCodes.Count)
        Assert.True(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.False(enableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
        Assert.False(enableNode.ErrorCodes(2).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(2).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_BadComma2()
        Dim tree = ParseAndVerify(<![CDATA[#enable warning bc42025; someid]]>,
            <errors>
                <error id="30196" message="Comma expected." start="23" end="23"/>
                <error id="30037" message="Character is not valid." start="23" end="24"/>
                <error id="30203" message="Identifier expected." start="31" end="31"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(2, skippedTokens.DescendantTokens.Count)
        Assert.Equal(SyntaxKind.BadToken, skippedTokens.DescendantTokens.First.Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Last.Kind)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(2, enableNode.ErrorCodes.Count)
        Assert.False(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.True(enableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_EscapedKeywords()
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning [Dim], [Rem]]]>)
        tree.VerifyNoMissingChildren()
        tree.VerifyNoZeroWidthNodes()
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(2, enableNode.ErrorCodes.Count)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.Equal("Dim", enableNode.ErrorCodes(0).Identifier.ValueText)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
        Assert.Equal("[Rem]", enableNode.ErrorCodes(1).ToString)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_NoTypeChars()
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning Dim$, BC&]]>,
            <errors>
                <error id="30468" message="Type declaration characters are not valid in this context." start="16" end="20"/>
                <error id="30468" message="Type declaration characters are not valid in this context." start="22" end="25"/>
            </errors>)
        tree.VerifyNoMissingChildren()
        tree.VerifyNoZeroWidthNodes()
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(2, enableNode.ErrorCodes.Count)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.Equal("Dim", enableNode.ErrorCodes(0).Identifier.ValueText)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
        Assert.Equal("BC&", enableNode.ErrorCodes(1).ToString)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_VeryLongIdentifier()
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning __123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890, bc42025]]>)
        tree.VerifyNoMissingChildren()
        tree.VerifyNoZeroWidthNodes()
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(2, enableNode.ErrorCodes.Count)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_NoIntegerLiterals()
        Dim tree = ParseAndVerify(<![CDATA[
Module Module1
    Sub Main
#disable warning 42024, 42024L, 2025UI, &HA428, BC&O122050
    End Sub
End Module]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="46" end="46"/>
                <error id="30203" message="Identifier expected." start="53" end="53"/>
                <error id="30203" message="Identifier expected." start="61" end="61"/>
                <error id="30203" message="Identifier expected." start="69" end="69"/>
                <error id="30468" message="Type declaration characters are not valid in this context." start="77" end="80"/>
                <error id="30196" message="Comma expected." start="80" end="80"/>
                <error id="30203" message="Identifier expected." start="88" end="88"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax)
        Assert.Equal(5, skippedTokens.Count)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, skippedTokens.First.DescendantTokens.Single.Kind)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, skippedTokens.Skip(1).First.DescendantTokens.Single.Kind)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, skippedTokens.Skip(2).First.DescendantTokens.Single.Kind)
        Assert.Equal(SyntaxKind.IntegerLiteralToken, skippedTokens.Skip(3).First.DescendantTokens.Single.Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.Last.DescendantTokens.Single.Kind)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.False(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(6, disableNode.ErrorCodes.Count)
        Assert.True(disableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(0).Kind)
        Assert.True(disableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(1).Kind)
        Assert.True(disableNode.ErrorCodes(2).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(2).Kind)
        Assert.True(disableNode.ErrorCodes(3).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(3).Kind)
        Assert.False(disableNode.ErrorCodes(4).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(4).Kind)
        Assert.Equal("BC&", disableNode.ErrorCodes(4).ToString)
        Assert.True(disableNode.ErrorCodes(5).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(5).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_NoStringLiterals()
        Dim tree = ParseAndVerify(<![CDATA[#disable warning "a"c, "b"]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="17" end="17"/>
                <error id="30203" message="Identifier expected." start="23" end="23"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax)
        Assert.Equal(2, skippedTokens.Count)
        Assert.Equal(SyntaxKind.CharacterLiteralToken, skippedTokens.First.DescendantTokens.Single.Kind)
        Assert.Equal(SyntaxKind.StringLiteralToken, skippedTokens.Skip(1).First.DescendantTokens.Single.Kind)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.True(disableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(0).Kind)
        Assert.True(disableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(1).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_NoOtherLiterals()
        Dim tree = ParseAndVerify(<![CDATA[#disable warning True, False, #1/2/2014#, Nothing]]>,
            <errors>
                <error id="30183" message="Keyword is not valid as an identifier." start="17" end="21"/>
                <error id="30183" message="Keyword is not valid as an identifier." start="23" end="28"/>
                <error id="30203" message="Identifier expected." start="30" end="30"/>
                <error id="30183" message="Keyword is not valid as an identifier." start="42" end="49"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(SyntaxKind.DateLiteralToken, skippedTokens.DescendantTokens.Single.Kind)

        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.Equal(4, disableNode.ErrorCodes.Count)
        Assert.False(disableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(0).Kind)
        Assert.Equal("True", disableNode.ErrorCodes(0).ToString)
        Assert.False(disableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(1).Kind)
        Assert.Equal("False", disableNode.ErrorCodes(1).ToString)
        Assert.True(disableNode.ErrorCodes(2).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(2).Kind)
        Assert.Equal(String.Empty, disableNode.ErrorCodes(2).ToString)
        Assert.False(disableNode.ErrorCodes(3).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(3).Kind)
        Assert.Equal("Nothing", disableNode.ErrorCodes(3).ToString)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_NoExpressions1()
        Dim tree = ParseAndVerify(<![CDATA[#disable warning -bc42024, (bc42025), Chr(42024)]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="17" end="17"/>
                <error id="30203" message="Identifier expected." start="27" end="27"/>
                <error id="30196" message="Comma expected." start="41" end="41"/>
                <error id="30203" message="Identifier expected." start="48" end="48"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.Equal(4, disableNode.ErrorCodes.Count)
        Assert.True(disableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(0).Kind)
        Assert.True(disableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(1).Kind)
        Assert.False(disableNode.ErrorCodes(2).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(2).Kind)
        Assert.Equal("Chr", disableNode.ErrorCodes(2).ToString)
        Assert.True(disableNode.ErrorCodes(3).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(3).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_NoExpressions2()
        Dim tree = ParseAndVerify(<![CDATA[#Disable Warning string.Empty, bc42024 & bc42025 + bc42015]]>,
            <errors>
                <error id="30183" message="Keyword is not valid as an identifier." start="17" end="23"/>
                <error id="30196" message="Comma expected." start="39" end="39"/>
                <error id="30203" message="Identifier expected." start="58" end="58"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.Equal(3, disableNode.ErrorCodes.Count)
        Assert.False(disableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(0).Kind)
        Assert.Equal("string", disableNode.ErrorCodes(0).ToString)
        Assert.False(disableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(1).Kind)
        Assert.Equal("bc42024", disableNode.ErrorCodes(1).ToString)
        Assert.True(disableNode.ErrorCodes(2).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(2).Kind)
        Assert.Equal(String.Empty, disableNode.ErrorCodes(2).ToString)
    End Sub

    <Fact()>
    Public Sub ParseWarningDirective_LineContinuation1()
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning _]]>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)
        tree.VerifyNoMissingChildren()
        tree.VerifyNoZeroWidthNodes()

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, enableNode.ErrorCodes.Count)
    End Sub

    <Fact()>
    Public Sub ParseWarningDirective_LineContinuation2()
        Assert.Equal(37306, ERRID.ERR_CommentsAfterLineContinuationNotAvailable1)
        Dim tree = ParseAndVerify(code:=<![CDATA[#Enable Warning _ 'Comment]]>,
                                  options:=New VisualBasicParseOptions(LanguageVersion.VisualBasic15),
                                    <errors>
                                        <error id="37306" message="Please use language version 16 or greater to use comments after line continuation character." start="24" end="25"/>
                                    </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax)
        Assert.Empty(skippedTokens)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Empty(enableNode.ErrorCodes)
    End Sub

    <Fact()>
    Public Sub ParseWarningDirective_LineContinuation2V15_5()
        Assert.Equal(37306, ERRID.ERR_CommentsAfterLineContinuationNotAvailable1)
        Dim tree = ParseAndVerify(code:=<![CDATA[#Enable Warning _ 'Comment]]>,
                                  options:=New VisualBasicParseOptions(LanguageVersion.VisualBasic15_5),
                                    <errors>
                                        <error id="37306" message="Please use language version 16 or greater to use comments after line continuation character." start="24" end="25"/>
                                    </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax)
        Assert.Empty(skippedTokens)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Empty(enableNode.ErrorCodes)
    End Sub

    <Fact()>
    Public Sub ParseWarningDirective_LineContinuation2V16()
        Dim tree = ParseAndVerify((<![CDATA[#Enable Warning _  'Comment]]>), New VisualBasicParseOptions(LanguageVersion.VisualBasic16))
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens As IEnumerable(Of SkippedTokensTriviaSyntax) = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax)
        Assert.Empty(skippedTokens)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Dim tt As SyntaxTriviaList = enableNode.GetTrailingTrivia
        Assert.True(tt.Count = 4)
        Assert.True(tt(0).Kind = SyntaxKind.WhitespaceTrivia)
        Assert.True(tt(1).Kind = SyntaxKind.LineContinuationTrivia)
        Assert.True(tt(2).Kind = SyntaxKind.WhitespaceTrivia)
        Assert.True(tt(3).Kind = SyntaxKind.CommentTrivia)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Empty(enableNode.ErrorCodes)
    End Sub

    <Fact()>
    Public Sub ParseWarningDirective_LineContinuation3()
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning bc42025 _]]>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)
        tree.VerifyNoMissingChildren()
        tree.VerifyNoZeroWidthNodes()

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.False(enableNode.ErrorCodes.Single.IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes.Single.Kind)
    End Sub

    <Fact()>
    Public Sub ParseWarningDirective_LineContinuation4()
        Assert.Equal(37306, ERRID.ERR_CommentsAfterLineContinuationNotAvailable1)
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning bc41007 _ 'Comment]]>,
                                  New VisualBasicParseOptions(LanguageVersion.VisualBasic15),
            <errors>
                <error id="37306" message="Please use language version 16 or greater to use comments after line continuation character." start="24" end="25"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax)
        Assert.Empty(skippedTokens)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(1, enableNode.ErrorCodes.Count)
        Assert.False(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
    End Sub

    <Fact()>
    Public Sub ParseWarningDirective_LineContinuation4V15_5()
        Assert.Equal(37306, ERRID.ERR_CommentsAfterLineContinuationNotAvailable1)
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning bc41007 _ 'Comment]]>,
                                  New VisualBasicParseOptions(LanguageVersion.VisualBasic15_5),
            <errors>
                <error id="37306" message="Please use language version 16 or greater to use comments after line continuation character." start="24" end="25"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax)
        Assert.Empty(skippedTokens)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(1, enableNode.ErrorCodes.Count)
        Assert.False(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
    End Sub

    <Fact()>
    Public Sub ParseWarningDirective_LineContinuation4V16()
        Dim tree = ParseAndVerify((<![CDATA[#Enable Warning bc41007 _ 'Comment]]>),
                                  New VisualBasicParseOptions(LanguageVersion.VisualBasic16))
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax)
        Assert.Empty(skippedTokens)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Dim tt As SyntaxTriviaList = enableNode.GetTrailingTrivia
        Assert.True(tt.Count = 4)
        Assert.True(tt(0).Kind = SyntaxKind.WhitespaceTrivia)
        Assert.True(tt(1).Kind = SyntaxKind.LineContinuationTrivia)
        Assert.True(tt(2).Kind = SyntaxKind.WhitespaceTrivia)
        Assert.True(tt(3).Kind = SyntaxKind.CommentTrivia)
        Assert.Equal(1, enableNode.ErrorCodes.Count)
        Assert.False(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
    End Sub

    <Fact()>
    Public Sub ParseWarningDirective_LineContinuation5()
        Dim tree = ParseAndVerify(<![CDATA[#Enable _
]]>,
            <errors>
                <error id="31218" message="'Warning' expected." start="11" end="11"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.True(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, enableNode.ErrorCodes.Count)
    End Sub


    <Fact()>
    Public Sub ParseWarningDirective_LineContinuation6()
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning [ _
bc42025]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="16" end="16"/>
                <error id="30203" message="Identifier expected." start="16" end="17"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim skippedTokens = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Single
        Assert.Equal(2, skippedTokens.DescendantTokens.Count)
        Assert.Equal(SyntaxKind.BadToken, skippedTokens.DescendantTokens.First.Kind)
        Assert.Equal(SyntaxKind.IdentifierToken, skippedTokens.DescendantTokens.Last.Kind)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.True(enableNode.ErrorCodes.Single.IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_NoImplicitLineContinuation()
        Dim tree = ParseAndVerify(<![CDATA[
Module Module1
    Sub Main
#enable warning BC42025, someid, 
SomeOtherId
    End Sub
End Module]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="63" end="63"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.Equal(3, enableNode.ErrorCodes.Count)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
        Assert.True(enableNode.ErrorCodes(2).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(2).Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_StatementSeparator1()
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning :]]>,
            <errors>
                <error id="30205" message="End of statement expected." start="17" end="17"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, enableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_StatementSeparator2()
        Dim tree = ParseAndVerify(<![CDATA[#Enable Warning bc42024 :'comment]]>,
            <errors>
                <error id="30205" message="End of statement expected." start="23" end="23"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim enableNode = DirectCast(root.GetFirstDirective(), EnableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.False(enableNode.EnableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.False(enableNode.WarningKeyword.IsMissing)
        Assert.False(enableNode.ErrorCodes.Single.IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes.Single.Kind)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_StatementSeparator3()
        Dim tree = ParseAndVerify(<![CDATA[#Disable :
]]>,
            <errors>
                <error id="31218" message="'Warning' expected." start="10" end="10"/>
                <error id="30205" message="End of statement expected." start="10" end="10"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Dim disableNode = DirectCast(root.GetFirstDirective(), DisableWarningDirectiveTriviaSyntax)
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.False(disableNode.DisableKeyword.IsMissing)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.True(disableNode.WarningKeyword.IsMissing)
        Assert.Equal(0, disableNode.ErrorCodes.Count)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_InsideNestedIfDirectives()
        Dim tree = ParseAndVerify(<![CDATA[
Module Program
    Sub Main()
#If True Then
#If False Then
        #if true then
        #disable warning bc42024, bc42025
        #end if
#ElseIf True Then
#Disable Warning bc42024, [BC42025]
    End Sub
#Else
        #enable warning bc41008, bc41008
#End If
End Module
#Enable Warning someOtherId, someId
#Else
#Enable Warning bc42024, bc42025
End Module
#End If]]>)
        tree.VerifyNoMissingChildren()
        tree.VerifyNoZeroWidthNodes()
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)

        Dim root = tree.GetRoot()
        Assert.False(root.DescendantNodes(descendIntoTrivia:=True).OfType(Of SkippedTokensTriviaSyntax).Any)

        Dim disableNode = root.DescendantNodes(descendIntoTrivia:=True).
            OfType(Of DisableWarningDirectiveTriviaSyntax).Single
        Assert.Equal(SyntaxKind.DisableKeyword, disableNode.DisableKeyword.Kind)
        Assert.Equal(SyntaxKind.WarningKeyword, disableNode.WarningKeyword.Kind)
        Assert.Equal(2, disableNode.ErrorCodes.Count)
        Assert.False(disableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(0).Kind)
        Assert.Equal("bc42024", disableNode.ErrorCodes(0).ToString)
        Assert.False(disableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, disableNode.ErrorCodes(1).Kind)
        Assert.Equal("BC42025", disableNode.ErrorCodes(1).Identifier.ValueText)

        Dim enableNode = root.DescendantNodes(descendIntoTrivia:=True).
            OfType(Of EnableWarningDirectiveTriviaSyntax).Single
        Assert.Equal(SyntaxKind.EnableKeyword, enableNode.EnableKeyword.Kind)
        Assert.Equal(SyntaxKind.WarningKeyword, enableNode.WarningKeyword.Kind)
        Assert.Equal(2, enableNode.ErrorCodes.Count)
        Assert.False(enableNode.ErrorCodes(0).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(0).Kind)
        Assert.Equal("someOtherId", enableNode.ErrorCodes(0).Identifier.Value)
        Assert.False(enableNode.ErrorCodes(1).IsMissing)
        Assert.Equal(SyntaxKind.IdentifierName, enableNode.ErrorCodes(1).Kind)
        Assert.Equal("someId", enableNode.ErrorCodes(1).ToString)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_NoCSharpSyntax()
        Dim tree = ParseAndVerify(<![CDATA[
#restore warning
#pragma warning disable
#pragma warning restore
#pragma restore
#warning]]>,
            <errors>
                <error id="30248" message="'If', 'ElseIf', 'Else', 'Const', 'Region', 'ExternalSource', 'ExternalChecksum', 'Enable', 'Disable', or 'End' expected." start="1" end="2"/>
                <error id="30248" message="'If', 'ElseIf', 'Else', 'Const', 'Region', 'ExternalSource', 'ExternalChecksum', 'Enable', 'Disable', or 'End' expected." start="18" end="19"/>
                <error id="30248" message="'If', 'ElseIf', 'Else', 'Const', 'Region', 'ExternalSource', 'ExternalChecksum', 'Enable', 'Disable', or 'End' expected." start="42" end="43"/>
                <error id="30248" message="'If', 'ElseIf', 'Else', 'Const', 'Region', 'ExternalSource', 'ExternalChecksum', 'Enable', 'Disable', or 'End' expected." start="66" end="67"/>
                <error id="30248" message="'If', 'ElseIf', 'Else', 'Const', 'Region', 'ExternalSource', 'ExternalChecksum', 'Enable', 'Disable', or 'End' expected." start="82" end="83"/>
            </errors>)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_DisallowInMultilineExpressionContext1()
        Dim tree = ParseAndVerify(<![CDATA[
Class C
    Sub Method(j As Short)
        Dim x = From i As Integer In {}
#Disable Warning BC42025
                Where i < j.MaxValue
#Enable Warning
                Select i
    End Sub
End Class]]>,
            <errors>
                <error id="30800" message="Method arguments must be enclosed in parentheses." start="123" end="137"/>
                <error id="30095" message="'Select Case' must end with a matching 'End Select'." start="170" end="178"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 2)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 2)
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_DisallowInMultilineExpressionContext2()
        Dim tree = ParseAndVerify(<![CDATA[
Class C
    Sub Method(k As Integer, j As Short)
        Dim x = <root>
                    <%=
#disable warning bc42025
                    i < j.MaxValue
                        %>
                </root>
    End Sub
End Class]]>,
            <errors>
                <error id="30201" message="Expression expected." start="97" end="97"/>
                <error id="30035" message="Syntax error." start="97" end="98"/>
                <error id="31159" message="Expected closing '%>' for embedded expression." start="121" end="121"/>
                <error id="31151" message="Element is missing an end tag." start="144" end="183"/>
                <error id="31177" message="White space cannot appear here." start="145" end="146"/>
                <error id="31169" message="Character '%' (&amp;H25) is not allowed at the beginning of an XML name." start="181" end="182"/>
                <error id="30249" message="'=' expected." start="182" end="182"/>
                <error id="31165" message="Expected beginning '&lt;' for an XML tag." start="183" end="183"/>
                <error id="31146" message="XML name expected." start="183" end="183"/>
                <error id="30636" message="'>' expected." start="183" end="183"/>
            </errors>)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 0)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 0)
    End Sub
#End Region

#Region "E2E Tests"
    <Fact>
    Public Sub TestWarningDirective_NoErrorCodes()
        Dim compXml =
<compilation>
    <file name="a.vb">
Module Program
#Disable Warning rem comment
    Sub Main()
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
#enable warning
        Dim d 'BC42024: Unused local variable: 'd'.
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13).WithWarningAsError(True))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub TestWarningDirective_WithErrorCodes1()
        Dim compXml =
<compilation>
    <file name="a.vb">
Module Program
#Disable Warning BC42104, [BC42024]
    Sub Main()
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
#enable warning BC42024, _
    BC42104 rem Comment
        Dim d 'BC42024: Unused local variable: 'd'.
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13).WithWarningAsError(True))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub TestWarningDirective_WithErrorCodes2()
        Dim compXml =
<compilation>
    <file name="a.vb">
#Disable Warning _
    BC42104, BC42024
Module Program
    Sub Main()
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
#enable warning
        Dim d 'BC42024: Unused local variable: 'd'.
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13).WithWarningAsError(True))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub TestWarningDirective_WithErrorCodes3()
        Dim compXml =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
#Disable Warning
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
#enable warning [bc42024] _
    , bc42104
        Dim d 'BC42024: Unused local variable: 'd'.
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13).WithWarningAsError(True))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub TestWarningDirective_NoOpEnable()
        Dim compXml =
<compilation>
    <file name="a.vb">#Enable Warning
Module Program
    Sub Main()
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
#enable warning bc42024, bc42104
        Dim d 'BC42024: Unused local variable: 'd'.
    End Sub
End Module
#enable warning bc42105</file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(4, 13),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(4, 13).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13).WithWarningAsError(True))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(4, 13).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub TestWarningDirective_NoOpDisable()
        Dim compXml =
<compilation>
    <file name="a.vb">#Disable Warning BC42015
Module Program
    Sub Main()
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.

        Dim d 'BC42024: Unused local variable: 'd'.
    End Sub
End Module
#Disable Warning</file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(4, 13),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(4, 13).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13).WithWarningAsError(True))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(4, 13).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub TestWarningDirective_FullWidth()
        Dim compXml =
<compilation>
    <file name="a.vb">
＃ _ 
 ｅｎａｂｌｅ     ｗａｒｎｉｎｇ
Module Program
    Sub Main()
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
＃ＤＩＳＡＢＬＥ ＷＡＲＮＩＮＧ
        Dim d 'BC42024: Unused local variable: 'd'.
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(7, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(5, 13))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(5, 13).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(7, 17))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(7, 17))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(5, 13).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(7, 17).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub TestWarningDirective_IdsAreCaseInsensitive1()
        Dim compXml =
<compilation>
    <file name="a.vb">
Module Program
#Disable Warning BC42024
    Sub Main()
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
#enable warning [bc42024]
        Dim d 'BC42024: Unused local variable: 'd'.
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add("Bc42024", ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13).WithWarningAsError(True))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add("bc42024", ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()
    End Sub

    <Fact>
    Public Sub TestWarningDirective_IdsAreCaseInsensitive2()
        Dim compXml =
<compilation>
    <file name="a.vb">
Module Program
#Disable Warning BC42024, [bc42104]
    Sub Main()
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
#enable warning bc42024
        Dim d
        Dim e = d 'BC42014: BC42104: Variable 'd' is used before it has been assigned a value.
        Dim f  'BC42024: Unused local variable: 'f'.
#enable warning Bc42104
        Dim g
        Dim h = g 'BC42014: BC42104: Variable 'g' is used before it has been assigned a value.
        Dim i = d
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "f").WithArguments("f").WithLocation(10, 13),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "g").WithArguments("g").WithLocation(13, 17))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add("BC42024", ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "f").WithArguments("f").WithLocation(10, 13).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "g").WithArguments("g").WithLocation(13, 17))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add("bC42024", ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "g").WithArguments("g").WithLocation(13, 17))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "f").WithArguments("f").WithLocation(10, 13).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "g").WithArguments("g").WithLocation(13, 17).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()
    End Sub

    Private Class CustomDiagnosticAnalyzer
        Inherits DiagnosticAnalyzer

        Private ReadOnly _descriptor As DiagnosticDescriptor
        Private ReadOnly _kind As SyntaxKind
        Private ReadOnly _reporter As Func(Of SyntaxNode, DiagnosticDescriptor, Diagnostic)

        Public Sub New(descriptor As DiagnosticDescriptor, kind As SyntaxKind, reporter As Func(Of SyntaxNode, DiagnosticDescriptor, Diagnostic))
            Me._descriptor = descriptor
            Me._kind = kind
            Me._reporter = reporter
        End Sub

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, _kind)
        End Sub

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(_descriptor)
            End Get
        End Property

        Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            context.ReportDiagnostic(_reporter(context.Node, _descriptor))
        End Sub
    End Class

    Private Class CustomDiagnosticAnalyzerWithFullWidthId
        Inherits CustomDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(
                New DiagnosticDescriptor("ｓＯＭＥＩＤ", "something1", "something2", "something3", DiagnosticSeverity.Warning, isEnabledByDefault:=True),
                SyntaxKind.VariableDeclarator,
                Function(n, d)
                    Dim varDecl = DirectCast(n, VariableDeclaratorSyntax)
                    Return CodeAnalysis.Diagnostic.Create(d, varDecl.AsClause.GetLocation)
                End Function)
        End Sub
    End Class

    <Fact>
    Public Sub TestWarningDirective_FullWidthIdsAreCaseInsensitive()
        Dim compXml =
<compilation>
    <file name="a.vb">
＃ＤＩＳＡＢＬＥ ＷＡＲＮＩＮＧ ［ＳＯＭＥＩＤ］
Module Program
    Sub Main()
        Dim x As Integer 'Warning with id "ＳＯＭＥＩＤ" is reported on "As Integer" clause
    End Sub
End Module
＃ｅｎａｂｌｅ     ｗａｒｎｉｎｇ ｓｏＭｅｉｄ
Module Other
    Sub Other()
        Dim y As Long 'Warning with id "ＳＯＭＥＩＤ" is reported on "As Long" clause
    End Sub
End Module
    </file>
</compilation>
        Dim analyzer = New CustomDiagnosticAnalyzerWithFullWidthId
        Dim analyzers = {analyzer}
        Dim expectedId = analyzer.SupportedDiagnostics.Single.Id
        Dim expectedMsg = analyzer.SupportedDiagnostics.Single.MessageFormat
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyAnalyzerDiagnostics(analyzers, Nothing, Nothing, False,
            New Test.Utilities.DiagnosticDescription(expectedId, "As Long", Nothing, Nothing, Nothing, False, GetType(String)).WithLocation(10, 15))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add("ｓｏＭｅＩｄ", ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyAnalyzerDiagnostics(analyzers, Nothing, Nothing, False,
            New Test.Utilities.DiagnosticDescription(expectedId, "As Long", Nothing, Nothing, Nothing, False, GetType(String)).WithLocation(10, 15).WithWarningAsError(True))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add("ＳｏＭｅｉＤ", ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyAnalyzerDiagnostics(analyzers)

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyAnalyzerDiagnostics(analyzers, Nothing, Nothing, False,
            New Test.Utilities.DiagnosticDescription(expectedId, "As Long", Nothing, Nothing, Nothing, False, GetType(String)).WithLocation(10, 15).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyAnalyzerDiagnostics(analyzers)
    End Sub

    <Fact>
    Public Sub TestWarningDirective_FullWidthIdsAndNonFullWidthIdsAreSeparate()
        Dim compXml =
<compilation>
    <file name="a.vb">
＃ _ 
 ｅｎａｂｌｅ     ｗａｒｎｉｎｇ bc42024
Module Program
    Sub Main()
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
＃ＤＩＳＡＢＬＥ ＷＡＲＮＩＮＧ ｂｃ４２０２４
        Dim d 'BC42024: Unused local variable: 'd'.
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(5, 13),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(7, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(5, 13).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(7, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13).WithWarningAsError(True))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(7, 17))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(5, 13).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(7, 17).WithWarningAsError(True),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(9, 13).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyDiagnostics()
    End Sub

    Private Class CustomDiagnosticAnalyzerWithVeryLongId
        Inherits CustomDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(
                New DiagnosticDescriptor("__Something_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890",
                                         "something1", "something2", "something3", DiagnosticSeverity.Warning, isEnabledByDefault:=True),
                SyntaxKind.VariableDeclarator,
                Function(n, d)
                    Dim varDecl = DirectCast(n, VariableDeclaratorSyntax)
                    Return CodeAnalysis.Diagnostic.Create(d, varDecl.AsClause.GetLocation)
                End Function)
        End Sub
    End Class

    <Fact>
    Public Sub TestWarningDirective_VeryLongIdentifier()
        Dim compXml =
<compilation>
    <file name="a.vb">
＃ＤＩＳＡＢＬＥ ＷＡＲＮＩＮＧ __something_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
Module Program
    Sub Main()
        Dim x As Integer 'Warning with above very long id is reported on "As Integer" clause
    End Sub
End Module
＃ｅｎａｂｌｅ     ｗａｒｎｉｎｇ [__SomeThing_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890]
Module Other
    Sub Other()
        Dim y As Long 'Warning with above very long id is reported on "As Long" clause
    End Sub
End Module
    </file>
</compilation>
        Dim analyzer = New CustomDiagnosticAnalyzerWithVeryLongId
        Dim analyzers = {analyzer}
        Dim expectedId = analyzer.SupportedDiagnostics.Single.Id
        Dim expectedMsg = analyzer.SupportedDiagnostics.Single.MessageFormat
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyAnalyzerDiagnostics(analyzers, Nothing, Nothing, False,
            New Test.Utilities.DiagnosticDescription(expectedId, "As Long", Nothing, Nothing, Nothing, False, GetType(String)).WithLocation(10, 15))

        Dim diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add("__someThing_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890", ReportDiagnostic.Error)
        Dim compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyAnalyzerDiagnostics(analyzers, Nothing, Nothing, False,
            New Test.Utilities.DiagnosticDescription(expectedId, "As Long", Nothing, Nothing, Nothing, False, GetType(String)).WithLocation(10, 15).WithWarningAsError(True))

        diagOptions = New Dictionary(Of String, ReportDiagnostic)
        diagOptions.Add("__somethIng_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890", ReportDiagnostic.Suppress)
        compOptions = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(diagOptions)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyAnalyzerDiagnostics(analyzers)

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyAnalyzerDiagnostics(analyzers, Nothing, Nothing, False,
            New Test.Utilities.DiagnosticDescription(expectedId, "As Long", Nothing, Nothing, Nothing, False, GetType(String)).WithLocation(10, 15).WithWarningAsError(True))

        compOptions = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
        CreateCompilationWithMscorlib40AndVBRuntime(compXml, compOptions).VerifyAnalyzerDiagnostics(analyzers)
    End Sub

    <Fact>
    Public Sub TestWarningDirective_CompilerWarningIdFormat()
        Dim compXml =
<compilation>
    <file name="a.vb">
Module Program
#Disable Warning BC042024 'Incorrect and hence warnings for 'a' and 'b' below won't be disabled.
    Sub Main()
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
#Disable Warning
#enable warning bc42024L 'Incorrect and hence warning for 'd' below won't be enabled.
        Dim d 'BC42024: Unused local variable: 'd'.
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "b").WithArguments("b").WithLocation(6, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(4, 13))
    End Sub

    <Fact>
    Public Sub TestWarningDirective_CantSuppressCompilerErrors()
        Dim compXml =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
        Dim x = 1
#Disable Warning BC30311
        Dim y As System.Exception = x
#Enable Warning BC30311
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_TypeMismatch2, "x").WithArguments("Integer", "System.Exception").WithLocation(5, 37))
    End Sub

    Private Function GetStartPosition(tree As SyntaxTree, text As String) As Integer
        Dim index = tree.GetText.ToString.IndexOf(text, 0, StringComparison.Ordinal)
        Assert.True(index >= 0, String.Format("'{0}' not found", text))
        Return index
    End Function

    Private Function GetEndPosition(tree As SyntaxTree, text As String) As Integer
        Return GetStartPosition(tree, text) + text.Length
    End Function

    <Fact>
    Public Sub TestWarningDirective_StateMap1()
        Dim tree = ParseAndVerify(<![CDATA[#Disable Warning
Module Program
#enable Warning
    Sub Main()
#Disable Warning BC42024
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
#enable warning [bc42024]
        Dim d 'BC42024: Unused local variable: 'd'.
#disable warning BC42024, [bc42104]
    End Sub
End Module
#enable Warning]]>)

        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", 0))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42104", 0))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42104", 1))

        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "#Disable Warning") - 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42104", GetEndPosition(tree, "#Disable Warning") - 1))

        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetEndPosition(tree, "#Disable Warning")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetStartPosition(tree, "Module Program")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetEndPosition(tree, "Module Program")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetStartPosition(tree, "#enable Warning")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("bC42104", GetEndPosition(tree, "#Disable Warning")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("bC42104", GetStartPosition(tree, "Module Program")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("bC42104", GetEndPosition(tree, "Module Program")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("bC42104", GetStartPosition(tree, "#enable Warning")))

        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "#enable Warning")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "Sub Main()")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("bc42104", GetEndPosition(tree, "#enable Warning")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("bc42104", GetStartPosition(tree, "Sub Main()")))

        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetEndPosition(tree, "#Disable Warning BC42024")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetEndPosition(tree, "Dim b")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("Bc42104", GetEndPosition(tree, "#Disable Warning BC42024")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("Bc42104", GetEndPosition(tree, "Dim b")))

        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetEndPosition(tree, "#enable warning [bc42024]") - 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "#enable warning [bc42024]")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "Dim d")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("Bc42104", GetEndPosition(tree, "#enable warning [bc42024]") - 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("Bc42104", GetEndPosition(tree, "#enable warning [bc42024]")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("Bc42104", GetEndPosition(tree, "Dim d")))

        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "#disable warning BC42024") - 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "#disable warning BC42024")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "#disable warning BC42024, [bc42104]") - 1))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetEndPosition(tree, "#disable warning BC42024, [bc42104]")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetEndPosition(tree, "End Module")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("bc42104", GetEndPosition(tree, "#disable warning BC42024")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("bc42104", GetEndPosition(tree, "#disable warning BC42024, [bc42104]") - 1))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("bc42104", GetEndPosition(tree, "#disable warning BC42024, [bc42104]")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("bc42104", GetEndPosition(tree, "End Module")))

        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetEndPosition(tree, "#enable Warning") - 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "#enable Warning")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("bc42104", GetEndPosition(tree, "#enable Warning") - 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("bc42104", GetEndPosition(tree, "#enable Warning")))

        Dim endPos = tree.GetText.ToString.Length
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", endPos - 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", endPos))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("bc42104", endPos - 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("bc42104", endPos))
    End Sub

    <Fact>
    Public Sub TestWarningDirective_StateMap2()
        Dim tree = ParseAndVerify(<![CDATA[
Module Program
    Sub Main()
#If True Then
    #If False Then
        #if true then
            #disable warning bc42024, bc42025
        #end if
    #ElseIf True Then
            #Disable Warning bc42024, [BC42025]
    End Sub
    #Else
        #enable warning bc42024, bc42025
    #End If
End Module
        #Enable Warning [bc42025], [bC42024]
#Else
    #Disable Warning bc42024, bc42025
End Module
#End If]]>)

        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", 0))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42025", 0))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42025", 1))

        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetStartPosition(tree, "#disable warning bc42024, bc42025")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "#disable warning bc42024, bc42025") + 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42025", GetStartPosition(tree, "#disable warning bc42024, bc42025")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42025", GetEndPosition(tree, "#disable warning bc42024, bc42025") + 1))

        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetStartPosition(tree, "#Disable Warning bc42024, [BC42025]")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetEndPosition(tree, "#Disable Warning bc42024, [BC42025]")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42025", GetStartPosition(tree, "#Disable Warning bc42024, [BC42025]")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42025", GetEndPosition(tree, "#Disable Warning bc42024, [BC42025]")))

        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetStartPosition(tree, "#enable warning bc42024, bc42025")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetEndPosition(tree, "#enable warning bc42024, bc42025")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42025", GetStartPosition(tree, "#enable warning bc42024, bc42025")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42025", GetEndPosition(tree, "#enable warning bc42024, bc42025")))

        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42024", GetStartPosition(tree, "#Enable Warning [bc42025], [bC42024]") + 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "#Enable Warning [bc42025], [bC42024]")))
        Assert.Equal(ReportDiagnostic.Suppress, tree.GetWarningState("BC42025", GetStartPosition(tree, "#Enable Warning [bc42025], [bC42024]") + 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42025", GetEndPosition(tree, "#Enable Warning [bc42025], [bC42024]")))

        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetStartPosition(tree, "#Disable Warning bc42024, bc42025")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", GetEndPosition(tree, "#Disable Warning bc42024, bc42025")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42025", GetStartPosition(tree, "#Disable Warning bc42024, bc42025")))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42025", GetEndPosition(tree, "#Disable Warning bc42024, bc42025")))

        Dim endPos = tree.GetText.ToString.Length
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", endPos - 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42024", endPos))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42025", endPos - 1))
        Assert.Equal(ReportDiagnostic.Default, tree.GetWarningState("BC42025", endPos))
    End Sub

    <Fact>
    Public Sub TestWarningDirective_ErrorCases1()
        Dim compXml =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
#Disable BC42024
        Dim a 'BC42024: Unused local variable: 'a'.
#Enable , BC42024
    End Sub
End Module
    </file>
</compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_ExpectedWarningKeyword, "").WithLocation(3, 10),
            Diagnostic(ERRID.ERR_ExpectedWarningKeyword, "").WithLocation(5, 9),
            Diagnostic(ERRID.WRN_UnusedLocal, "a").WithArguments("a").WithLocation(4, 13))
    End Sub

    <Fact>
    Public Sub TestWarningDirective_ErrorCases2()
        Dim compXml =
        <compilation>
            <file name="a.vb">
Module Program
    Sub Main()
#Disable Warning BC42104,, BC42024
        Dim a 'BC42024: Unused local variable: 'a'.
        Dim b
        Dim c = b 'BC42014: BC42104: Variable 'b' is used before it has been assigned a value.
#Enable WarNING ,BC42104,BC42024, 
        Dim d 'BC42024: Unused local variable: 'd'.
        Dim e
        Dim f = e 'BC42014: BC42104: Variable 'e' is used before it has been assigned a value.
    End Sub
End Module
    </file>
        </compilation>
        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_ExpectedIdentifier, "").WithLocation(3, 26),
            Diagnostic(ERRID.ERR_ExpectedIdentifier, "").WithLocation(7, 17),
            Diagnostic(ERRID.ERR_ExpectedIdentifier, "").WithLocation(8, 1),
            Diagnostic(ERRID.WRN_DefAsgUseNullRef, "e").WithArguments("e").WithLocation(10, 17),
            Diagnostic(ERRID.WRN_UnusedLocal, "d").WithArguments("d").WithLocation(8, 13))
    End Sub

    <Fact>
    Public Sub ParseWarningDirective_UnrecognizedInXmlLiteralContext()
        Dim code = <![CDATA[
Module Module1
    Sub Main()
    End Sub
    Sub Other(i As Integer, j As Short)
        Dim x = <root>
#disable warning bc42025
                    <%=
                    i < j.MaxValue
                        %>
#enable warning
#disable warning
                </root>
        Dim y = i < j.MaxValue
    End Sub
End Module]]>
        Dim tree = ParseAndVerify(code)
        tree.VerifyOccurrenceCount(SyntaxKind.DisableWarningDirectiveTrivia, 0)
        tree.VerifyOccurrenceCount(SyntaxKind.EnableWarningDirectiveTrivia, 0)

        Dim comp = CreateCompilationWithMscorlib40({tree}).
            AddReferences({MsvbRef}).
            AddReferences(XmlReferences).
            VerifyDiagnostics(
                Diagnostic(ERRID.WRN_SharedMemberThroughInstance, "j.MaxValue").WithLocation(9, 25),
                Diagnostic(ERRID.WRN_SharedMemberThroughInstance, "j.MaxValue").WithLocation(14, 21))
    End Sub

    <Fact>
    Public Sub TestWarningDirective_Precedence1()
        Dim compXml =
<compilation>
    <file name="a.vb">
Module Program
    &lt;System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "TestId", Justification:="&lt;Pending&gt;")&gt;
    Sub Main()
    #enable warning TestId
        Dim x As Integer 'Warning with above very long id is reported on "As Integer" clause
    End Sub
End Module
    </file>
</compilation>
        Dim analyzer = New CustomDiagnosticAnalyzer(
            New DiagnosticDescriptor("TestId", "something1", "something2", "something3", DiagnosticSeverity.Warning, isEnabledByDefault:=True),
                SyntaxKind.VariableDeclarator,
                Function(n, d)
                    Dim varDecl = DirectCast(n, VariableDeclaratorSyntax)
                    Return CodeAnalysis.Diagnostic.Create(d, varDecl.AsClause.GetLocation)
                End Function)

        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyAnalyzerOccurrenceCount({analyzer}, 0)
    End Sub

    <Fact>
    Public Sub TestWarningDirective_Precedence2()
        Dim compXml =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
    #enable warning TestId
        Dim x As Integer 'Warning with above very long id is reported on "As Integer" clause
    End Sub
End Module
    </file>
    <file name="suppression.vb">
        &lt;Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "TestId", Justification:="&lt;Pending&gt;")&gt;
    </file>
</compilation>
        Dim analyzer = New CustomDiagnosticAnalyzer(
            New DiagnosticDescriptor("TestId", "something1", "something2", "something3", DiagnosticSeverity.Warning, isEnabledByDefault:=True),
                SyntaxKind.VariableDeclarator,
                Function(n, d)
                    Dim varDecl = DirectCast(n, VariableDeclaratorSyntax)
                    Return CodeAnalysis.Diagnostic.Create(d, varDecl.AsClause.GetLocation)
                End Function)

        CreateCompilationWithMscorlib40AndVBRuntime(compXml).VerifyAnalyzerOccurrenceCount({analyzer}, 0)
    End Sub
#End Region

#End Region
End Class
