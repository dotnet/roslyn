' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports System.Collections.Immutable

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

    <WorkItem(545871, "DevDiv")>
    <Fact>
    Public Sub FW_Hash()
        ParseAndVerify(<![CDATA[
＃If True Then
＃Else
＃End If

        ]]>)
    End Sub


    <WorkItem(679758, "DevDiv")>
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

    <WorkItem(530922, "DevDiv")>
    <WorkItem(658448, "DevDiv")>
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

    <WorkItem(531493, "DevDiv")>
    <Fact>
    Public Sub Repro18189()
        ParseAndVerify(<![CDATA[
#If False Then
REM _
#End If

        ]]>)
    End Sub

    <WorkItem(697520, "DevDiv")>
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

    <WorkItem(530921, "DevDiv")>
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

    <WorkItem(530679, "DevDiv")>
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

    <WorkItem(545871, "DevDiv")>
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

    <WorkItem(538581, "DevDiv")>
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

    <WorkItem(528617, "DevDiv")>
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

    <WorkItem(545211, "DevDiv")>
    <Fact>
    Public Sub FunctionKeywordInDisabledText()
        ParseAndVerify(<![CDATA[
#If False Then
#Const = Function  
#End If
        ]]>)
    End Sub

    <WorkItem(586984, "DevDiv")>
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

    <WorkItem(586984, "DevDiv")>
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
        ]]>.Value.Replace("_"c, SyntaxFacts.FULLWIDTH_LC),
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

    <WorkItem(538578, "DevDiv")>
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

    <WorkItem(542109, "DevDiv")>
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

    <WorkItem(541882, "DevDiv")>
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

    <WorkItem(528617, "DevDiv")>
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

    <WorkItem(537851, "DevDiv")>
    <WorkItem(538488, "DevDiv")>
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

    <WorkItem(538486, "DevDiv")>
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

    <WorkItem(536090, "DevDiv")>
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

    <WorkItem(538589, "DevDiv")>
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
                Public Sub foo()
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

    <WorkItem(893259, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30012_ParsePreProcessorIfIncompleteExpression()
        ParseAndVerify(<![CDATA[
            Module m1
            Public Sub foo()
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

    <WorkItem(527211, "DevDiv")>
    <WorkItem(904877, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30681ERR_ExpectedEndRegion()
        ParseAndVerify(<![CDATA[
            #Region "Start"
        ]]>,
        Diagnostic(ERRID.ERR_ExpectedEndRegion, "#Region ""Start"""))
    End Sub

    <WorkItem(527211, "DevDiv")>
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
    Sub foo()
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

    <WorkItem(527211, "DevDiv")>
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

        Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.From(text), "", options)

        Dim tk = tree.GetRoot().FindToken(text.IndexOf("class c2"))
        Assert.Equal(SyntaxKind.ClassKeyword, tk.VisualBasicKind)

        tk = tree.GetRoot().FindToken(text.IndexOf("class c1"))
        Assert.Equal(260, tk.FullWidth)

    End Sub

    <WorkItem(537144, "DevDiv")>
    <WorkItem(929947, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseNestedDirectives()
        ParseAndVerify(<![CDATA[
#If LANG_OE_JP Then
#If Not ULTRAVIOLET Then
#End If 'UV
#end if
            ]]>).VerifyNoZeroWidthNodes().VerifyOccuranceCount(SyntaxKind.DisabledTextTrivia, 1)
    End Sub


    <WorkItem(538483, "DevDiv")>
    <Fact>
    Public Sub ParseDirectiveWithStatementOnLine()
        ParseAndVerify(<![CDATA[
#If True : Module M : End Module
#End If
            ]]>,
            <error>
                <error id="30205" message="End of statement expected." start="10" end="11"/>
            </error>).VerifyNoZeroWidthNodes().VerifyOccuranceCount(SyntaxKind.DisabledTextTrivia, 0)
    End Sub

    <WorkItem(538750, "DevDiv")>
    <Fact>
    Public Sub ParseDirectiveWithStrings()
        ParseAndVerify(<![CDATA[
#If False Then
#Const "c _
#Else
Class X
#End If
End Class
            ]]>).VerifyNoZeroWidthNodes().VerifyOccuranceCount(SyntaxKind.DisabledTextTrivia, 1)

        ParseAndVerify(<![CDATA[
#If False Then
#Const """c _
#Else
Class X
#End If
End Class
            ]]>).VerifyNoZeroWidthNodes().VerifyOccuranceCount(SyntaxKind.DisabledTextTrivia, 1)

        ParseAndVerify(<![CDATA[
#If False Then
#Const ""c_
#Else
Class X
#End If
End Class
            ]]>).VerifyNoZeroWidthNodes().VerifyOccuranceCount(SyntaxKind.DisabledTextTrivia, 1)

        ParseAndVerify(<![CDATA[
Class c1
#If False Then
#Const c _
#Else
Class X
#End If
End Class
End Class
            ]]>).VerifyNoZeroWidthNodes().VerifyOccuranceCount(SyntaxKind.DisabledTextTrivia, 1)

        ParseAndVerify(<![CDATA[
Class c1
#If False Then
#Const ""c _
#Else
Class X
#End If
End Class
End Class
            ]]>).VerifyNoZeroWidthNodes().VerifyOccuranceCount(SyntaxKind.DisabledTextTrivia, 1)
    End Sub

    <WorkItem(528675, "DevDiv")>
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

    <WorkItem(552845, "DevDiv")>
    <Fact()>
    Public Sub Repro552845()
        ParseAndVerify(<![CDATA[
#If comperrortest then
 #: BC30452]]>, <errors>
                    <error id="30012"/>
                </errors>)
    End Sub

    <WorkItem(552845, "DevDiv")>
    <Fact()>
    Public Sub Repro552845_1()
        ParseAndVerify(<![CDATA[
#If comperrortest then
 #10/10/1956#: BC30452]]>, <errors>
                               <error id="30012"/>
                           </errors>)
    End Sub

    <WorkItem(552845, "DevDiv")>
    <Fact()>
    Public Sub Repro552845_2()
        ParseAndVerify(<![CDATA[
#If comperrortest then
 #
 BC30452]]>, <errors>
                 <error id="30012"/>
             </errors>)
    End Sub

    <WorkItem(552845, "DevDiv")>
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
    <WorkItem(542447, "DevDiv")>
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

End Module]]>).VerifyOccuranceCount(SyntaxKind.FieldDeclaration, 1)
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

    <WorkItem(675842, "DevDiv")>
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
]]>.Value.Replace("#"c, SyntaxFacts.FULLWIDTH_HASH),
            <errors>
                <error id="31085"/>
            </errors>)
        ParseAndVerify(<![CDATA[
#If False Then
#05/01/13#
#End If
]]>.Value.Replace("#"c, SyntaxFacts.FULLWIDTH_HASH))
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
        Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
        </compilation>, OptionsDll)

        AssertTheseParseDiagnostics(compilation,
<expected>
BC30060: Conversion from 'Int32' to 'Object' cannot occur in a constant expression.
#Const X = If(True, 1, "2")
~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'String' to 'Object' cannot occur in a constant expression.
#Const U = If(False, "2", 1)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
#Const V = If(1, "2")
~~~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'UInt64' to 'Object' cannot occur in a constant expression.
#Const X1 = If(True, 1UL, 2L)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
#Const X3 = If(True, 1, CObj(Nothing))
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
    End Sub

    <WorkItem(780817, "DevDiv")>
    <Fact>
    Public Sub ParseProjConstsCaseInsensitivity()

        Dim psymbols = ImmutableArray.Create({KeyValuePair.Create("Blah", CObj(False)), KeyValuePair.Create("blah", CObj(True))})

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

End Class
