' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

<CLSCompliant(False)>
Public Class ParseErrorTests
    Inherits BasicTestBase

#Region "Targeted Error Tests - please arrange tests in the order of error code"

    <Fact()>
    Public Sub BC30004ERR_IllegalCharConstant()
        ParseAndVerify(<![CDATA[
                class c
                    function foo()
                        System.console.writeline(""c)
                        return 1
                    End function
                End class
            ]]>,
        <errors>
            <error id="30201"/>
            <error id="30004"/>
        </errors>)
    End Sub

    ' old name - ParseHashFollowingElseDirective_ERR_LbExpectedEndIf
    <WorkItem(2908, "DevDiv_Projects/Roslyn")>
    <WorkItem(904916, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30012ERR_LbExpectedEndIf()
        ParseAndVerify(<![CDATA[
#If True
#Else
#
            ]]>,
            Diagnostic(ERRID.ERR_LbExpectedEndIf, "#If True"))
    End Sub

    <WorkItem(527211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527211")>
    <Fact()>
    Public Sub BC30012ERR_LbExpectedEndIf_2()
        ParseAndVerify(<![CDATA[
#If True Then
 
Class C
End Class


            ]]>,
            Diagnostic(ERRID.ERR_LbExpectedEndIf, "#If True Then"))
    End Sub

    <WorkItem(527211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527211")>
    <Fact()>
    Public Sub BC30012ERR_LbExpectedEndIf_3()
        ParseAndVerify(<![CDATA[
#If True ThEn
#If True TheN
#If True Then
 
Class C
End Class


            ]]>,
            Diagnostic(ERRID.ERR_LbExpectedEndIf, "#If True ThEn"),
            Diagnostic(ERRID.ERR_LbExpectedEndIf, "#If True TheN"),
            Diagnostic(ERRID.ERR_LbExpectedEndIf, "#If True Then"))
    End Sub

    <WorkItem(527211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527211")>
    <Fact()>
    Public Sub BC30012ERR_LbExpectedEndIf_4()
        ParseAndVerify(<![CDATA[
#If True ThEn
#End If
#If True TheN
#If True Then
 
Class C
End Class


            ]]>,
            Diagnostic(ERRID.ERR_LbExpectedEndIf, "#If True TheN"),
            Diagnostic(ERRID.ERR_LbExpectedEndIf, "#If True Then"))
    End Sub

    <WorkItem(527211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527211")>
    <Fact()>
    Public Sub BC30012ERR_LbExpectedEndIf_5()
        ParseAndVerify(<![CDATA[
#Else
 
Class C
End Class

            ]]>,
            Diagnostic(ERRID.ERR_LbElseNoMatchingIf, "#Else"),
            Diagnostic(ERRID.ERR_LbExpectedEndIf, ""))
    End Sub

    <WorkItem(2908, "DevDiv_Projects/Roslyn")>
    <Fact()>
    Public Sub BC30014ERR_LbBadElseif()
        Dim code = <![CDATA[
                Module M1
                    Sub main()
                        #ElseIf xxsx Then
                    End Sub
                End Module
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30625"/>
                                 <error id="30026"/>
                                 <error id="30014"/>
                                 <error id="30012"/>
                             </errors>)

        'Diagnostic(ERRID.ERR_ExpectedEndModule, "Module M1")
        'Diagnostic(ERRID.ERR_EndSubExpected, "Sub main()")
        'Diagnostic(ERRID.ERR_LbBadElseif, "#ElseIf xxsx Then")
        'Diagnostic(ERRID.ERR_LbExpectedEndIf, "")
    End Sub

    <Fact()>
    Public Sub BC30018ERR_DelegateCantImplement()
        Dim code = <![CDATA[
                Namespace NS1
                    Module M1
                        Interface i1
                            Sub foo()
                        End Interface
                        Public Class Class1
                            Sub foo()
                            End Sub
                            'COMPILEERROR: BC30018, "i1.foo"
                            Delegate Sub foo1() Implements i1.foo
                        End Class
                        Public Class Class2(Of t)
                            Sub foo()
                            End Sub
                            'COMPILEERROR: BC30018, "i1.foo"
                            Delegate Sub foo1(Of tt)() Implements i1.foo
                        End Class
                        Public Structure s1(Of t)
                            Public Sub foo()
                            End Sub
                            'COMPILEERROR: BC30018, "i1.foo"
                            Delegate Sub foo1(Of tt)() Implements i1.foo
                        End Structure
                    End Module
                End Namespace
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30018"/>
                                 <error id="30018"/>
                                 <error id="30018"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30018ERR_DelegateCantImplement_1()
        Dim code = <![CDATA[
                Public Class D
                    delegate sub delegate1() implements I1.foo
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30018"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30019ERR_DelegateCantHandleEvents()
        Dim code = <![CDATA[
                Namespace NS1
                    Delegate Sub delegate1()
                    Module M1
                        Delegate Sub delegate1() Handles c1.too
                    End Module
                End Namespace
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30019"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30019ERR_DelegateCantHandleEvents_1()
        Dim code = <![CDATA[
                Class class1
                    Event event1()
                    Sub raise()
                        RaiseEvent event1()
                    End Sub
                    Dim WithEvents evnt As class1
                    'COMPILEERROR: BC30019, "evnt.event1"
                    Delegate Sub sub1() Handles evnt.event1
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30019"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30027ERR_EndFunctionExpected()
        Dim code = <![CDATA[
                Module M1
                    Function  B As string
                        Dim x = <!--hello
                    End Sub
                End Module
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30625"/>
                                 <error id="30027"/>
                                 <error id="31161"/>
                             </errors>)
    End Sub

    ' old name - ParsePreProcessorElse_ERR_LbElseNoMatchingIf()
    <WorkItem(897856, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30028ERR_LbElseNoMatchingIf()
        ParseAndVerify(<![CDATA[
                #If False Then
                #Else
                #Else
                #End If
            ]]>,
        <errors>
            <error id="30028"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30032ERR_EventsCantBeFunctions()
        Dim code = <![CDATA[
                Public Class EventSource
                    Public Event LogonCompleted(ByVal UserName As String) as String
                End Class
                Class EventClass
                    Public Event XEvent() as Datetime
                    Public Event YEvent() as file
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30032"/>
                                 <error id="30032"/>
                                 <error id="30032"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30032ERR_EventsCantBeFunctions_1()
        Dim code = <![CDATA[
                Interface I1
                        Event Event4() as boolean
                End Interface
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30032"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30032ERR_EventsCantBeFunctions_2()
        Dim code = <![CDATA[
                        Event Event() as boolean
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30183"/>
                                 <error id="30032"/>
                             </errors>)
    End Sub

    <WorkItem(537989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537989")>
    <Fact()>
    Public Sub BC30033ERR_IdTooLong_1()
        ' Error now happens at emit time.
        ParseAndVerify(<![CDATA[
                Namespace TestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZ
                    Module TestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZ
                        Sub TestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZ()
                        End Sub
                    End Module
                End Namespace
            ]]>)
    End Sub

    <WorkItem(537989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537989")>
    <Fact()>
    Public Sub BC30033ERR_IdTooLong_2()
        ' Error now happens at emit time.
        ParseAndVerify(<![CDATA[
                Module M1
                    Sub FO0(TestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZ As Integer)
                    End Sub
                End Module
            ]]>)
    End Sub

    <Fact, WorkItem(530884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530884")>
    Public Sub BC30033ERR_IdTooLong_3()
        ' Identifiers 1023 characters (no errors)
        ParseAndVerify(<![CDATA[
Imports <xmlns:TestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTes="...">
Module M
    Private F1 As Object = <TestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTes/>
    Private F2 As Object = <TestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTes:x/>
End Module
            ]]>)
        ' Identifiers 1024 characters
        ' Error now happens at emit time.
        ParseAndVerify(<![CDATA[
Imports <xmlns:TestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTest="...">
Module M
    Private F1 As Object = <TestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTest/>
    Private F2 As Object = <TestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTestABCDEFGHIJKLMNOPQRSTUVWXGZTest:x/>
End Module
            ]]>)
    End Sub

    <Fact()>
    Public Sub BC30034ERR_MissingEndBrack()
        Dim code = <![CDATA[
                Class C1
                    Dim DynamicArray_1 = new byte[1,2]
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30034"/>
                                 <error id="30037"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax()
        ParseAndVerify(<![CDATA[
            >Attr()< Class C1
            End Class
    ]]>,
            <errors>
                <error id="30035"/>
                <error id="30460"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_1()
        ParseAndVerify(<![CDATA[
Module Module1
    Sub Main()
        () = 0
    End Sub
End Module
    ]]>,
            <errors>
                <error id="30035"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_2()
        ParseAndVerify(<![CDATA[
Class C1
#ExternalChecksum("D:\Documents and Settings\USERS1\My Documents\Visual Studio\WebSites\WebSite1\Default.aspx","{406ea660-64cf-4c82-b6f0-42d48172a799}","44179F2BE2484F26E2C6AFEBAF0EC3CC")
#End ExternalChecksum
End Class
    ]]>,
            <errors>
                <error id="30035"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_3()
        ParseAndVerify(<![CDATA[
Class C1
    IsNot:
End Class
    ]]>,
            <errors>
                <error id="30035"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_4()
        ParseAndVerify(<![CDATA[
Class C1
    Dim x = Sub(a, b) ' _
                    (System.Console.WriteLine(a))
             End Sub
End Class
    ]]>,
            <errors>
                <error id="30035"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_5()
        ParseAndVerify(<![CDATA[
Class C1
    Dim y = Sub(x) handles 
End Class
    ]]>,
            <errors>
                <error id="30035"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_6()
        ParseAndVerify(<![CDATA[
Structure S1
    Shared Dim i = Function(a as Integer)
                        Partial Class C1
                   End Function
End Structure
    ]]>,
       <errors>
           <error id="36674"/>
           <error id="30481"/>
           <error id="30430"/>
       </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_7()
        ParseAndVerify(<![CDATA[
Structure S1
    .Dim(x131 = Sub())
End Structure
    ]]>,
        Diagnostic(ERRID.ERR_ExecutableAsDeclaration, ".Dim(x131 = Sub())"),
        Diagnostic(ERRID.ERR_SubRequiresSingleStatement, "Sub()"))
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_8()
        ParseAndVerify(<![CDATA[
Structure S1
    Dim r = Sub() AddressOf Foo
End Structure
    ]]>,
            <errors>
                <error id="30035"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_9()
        ParseAndVerify(<![CDATA[
Structure S1
    Dim x As New List(Of String) From 

         {"hello", "world"}
End Structure
    ]]>,
            <errors>
                <error id="30035"/>
                <error id="30987"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_10()
        ParseAndVerify(<![CDATA[
Structure S1
    dim r = GetType (System.Collections.Generic.List(of 

       ))
End Structure
    ]]>,
            <errors>
                <error id="30182"/>
                <error id="30035"/>
                <error id="30198"/>
                <error id="30198"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_11()
        ParseAndVerify(<![CDATA[
Structure S1
    Dim i As Integer = &O
End Structure
    ]]>,
            <errors>
                <error id="30201"/>
                <error id="30035"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_12()
        ParseAndVerify(<![CDATA[
Structure S1
    Sub FOO
7IC:
        GoTo 7IC
    End Sub
End Structure
    ]]>,
            <errors>
                <error id="30801"/>
                <error id="30035"/>
                <error id="30035"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_13()
        ParseAndVerify(<![CDATA[
Class class1
           Global shared c1 as short
End Class
    ]]>,
        Diagnostic(ERRID.ERR_ExpectedDeclaration, "Global"))
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_14()
        ParseAndVerify(<![CDATA[
Imports System
Class C
    Shared Sub Main()
        Dim S As Integer() = New Integer() {1, 2}
            For Each x  AS Integer = 1  In S
        Next
    End Sub
End Class
    ]]>,
            <errors>
                <error id="30035"/>
                <error id="36607"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_15()
        ParseAndVerify(<![CDATA[
Class C
    Shared Sub Main()
        Dim a(10) As Integer
        For Each x,y As Integer In a
        Next
    End Sub
End Class
    ]]>,
            <errors>
                <error id="30035"/>
                <error id="36607"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_16()
        ParseAndVerify(<![CDATA[
Class C
    Shared Sub Main()
        Dim a(10) As Integer
        For Each x as Integer, y As Integer In a
        Next
    End Sub
End Class
    ]]>,
            <errors>
                <error id="30035"/>
                <error id="36607"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_17()
        ParseAndVerify(<![CDATA[
Class C
    Shared Sub Main()
        For Each number New Long() {45, 3, 987}
        Next
    End Sub
End Class
    ]]>,
            <errors>
                <error id="30035"/>
                <error id="36607"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_18()
        ParseAndVerify(<![CDATA[
Class C
    Shared Sub Main()
        For Each 1
        Next
    End Sub
End Class
    ]]>,
            <errors>
                <error id="30035"/>
                <error id="36607"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30035ERR_Syntax_19()
        ParseAndVerify(<![CDATA[
Option Strict On
Option Infer Off
Module Program
    Sub Main(args As String())
        Dim i As Integer
        For i := 1 To 10
        Next i
    End Sub
End Module
    ]]>,
            <errors>
                <error id="30035"/>
                <error id="30249"/>
            </errors>)
    End Sub

    <WorkItem(529861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529861")>
    <Fact>
    Public Sub Bug14632()
        ParseAndVerify(<![CDATA[
    Module M
        Dim x As Decimal = 1E29D
    End Module
            ]]>,
        <errors>
            <error id="30036"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC30036ERR_Overflow()
        ParseAndVerify(<![CDATA[
Module Module1
    Dim a1 As Integer = 45424575757575754547UL
    Dim a2 As double = 18446744073709551617UL

    Sub Main(args As String())
        Dim x = 1.7976931348623157E+308d
    End Sub
End Module
                ]]>,
            <errors>
                <error id="30036"/>
                <error id="30036"/>
                <error id="30036"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30037ERR_IllegalChar()
        ParseAndVerify(<![CDATA[
Structure [$$$]
    Public s As Long
End Structure
                ]]>,
           <errors>
               <error id="30203"/>
               <error id="30203"/>
               <error id="30037"/>
               <error id="30037"/>
               <error id="30037"/>
               <error id="30037"/>
           </errors>)
    End Sub

    <Fact()>
    Public Sub BC30037ERR_IllegalChar_1()
        ParseAndVerify(<![CDATA[
                              class mc(of $)
                                End Class
                ]]>,
           <errors>
               <error id="30037"/>
               <error id="30203"/>
               <error id="32100"/>
           </errors>)
    End Sub

    <Fact()>
    Public Sub BC30037ERR_IllegalChar_2()
        ParseAndVerify(<![CDATA[
                              Class class1
                                    Dim obj As New Object`123
                              End Class
                ]]>,
           <errors>
               <error id="30037"/>
           </errors>)
    End Sub

    <Fact()>
    Public Sub BC30037ERR_IllegalChar_3()
        ParseAndVerify(<![CDATA[
                                Class class1
                                    Dim b = b | c
                                End Class
                ]]>,
           <errors>
               <error id="30037"/>
           </errors>)
    End Sub

    <Fact()>
    Public Sub BC30037ERR_IllegalChar_4()
        ParseAndVerify(<![CDATA[
                                Class class1
                                    Sub foo
                                        System.Console.WriteLine("a";"b")
                                    End Sub
                                End Class
                ]]>,
           <errors>
               <error id="30037"/>
               <error id="32017"/>
           </errors>)
    End Sub

    <Fact()>
    Public Sub BC30037ERR_IllegalChar_5()
        ParseAndVerify(<![CDATA[
                                Class class1
                                    Sub foo
                                        l1( $= l2)
                                    End Sub
                                End Class
                ]]>,
           <errors>
               <error id="30037"/>
               <error id="30201"/>
           </errors>)
    End Sub

    <Fact()>
    Public Sub BC30037ERR_IllegalChar_6()
        ParseAndVerify(<![CDATA[
                                Structure [S1]
                                    dim s = new Short (]
                                End Structure
                ]]>,
           <errors>
               <error id="30037"/>
               <error id="30201"/>
               <error id="30198"/>
           </errors>)
    End Sub

    <Fact()>
    Public Sub BC30037ERR_IllegalChar_7()
        ParseAndVerify(<![CDATA[
                                Structure [S1]
                                    dim s =  new Short() {1,%,.,.}
                                End Structure
                ]]>,
           <errors>
               <error id="30037"/>
               <error id="30201"/>
               <error id="30203"/>
               <error id="30203"/>
           </errors>)
    End Sub

    <Fact()>
    Public Sub BC30037ERR_IllegalChar_8()
        ParseAndVerify(<![CDATA[
                                Structure S1
                                End Structure;
                ]]>,
           <errors>
               <error id="30037"/>
           </errors>)
    End Sub

    <Fact()>
    Public Sub BC30037ERR_IllegalChar_9()
        ParseAndVerify(<![CDATA[
                                Structure S1
                                #const $hoo 
                                End Structure
                ]]>,
           <errors>
               <error id="30035"/>
               <error id="30037"/>
               <error id="30249"/>
               <error id="30201"/>
               <error id="30203"/>
           </errors>)
    End Sub

    ' old name - ParseStatementSeperatorOnSubDeclLine_ERR_MethodBodyNotAtLineStart
    <WorkItem(905020, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30040ERR_MethodBodyNotAtLineStart()
        ParseAndVerify(<![CDATA[
    Public Class C1
        Sub Foo() : Console.Writeline()
        End Sub
    End Class
    ]]>,
            <errors>
                <error id="30040"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30040ERR_MethodBodyNotAtLineStart_EmptyStatement()
        ' Note: Dev11 reports BC30040.
        ParseAndVerify(<![CDATA[
Module M
    Sub M() :
    End Sub
End Module
    ]]>)
    End Sub

    <Fact()>
    Public Sub BC30059ERR_RequiredConstExpr_1()
        CreateCompilationWithMscorlibAndVBRuntime(
        <compilation name="ArrayInitializerForNonConstDim">
            <file name="a.vb">
            Option Infer On
            Module Module1
                Sub Main(args As String())
                End Sub
                Sub foo(Optional ByVal i As Integer(,) = New Integer(1, 1) {{1, 2}, {2, 3}}) 'Invalid
                End Sub
                Sub foo(Optional ByVal i As Integer(,) = Nothing) ' OK
                End Sub
            End Module
                </file>
        </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_RequiredConstExpr, "New Integer(1, 1) {{1, 2}, {2, 3}}"),
            Diagnostic(ERRID.ERR_OverloadWithDefault2, "foo").WithArguments("Public Sub foo([i As Integer(*,*)])", "Public Sub foo([i As Integer(*,*) = Nothing])"))
    End Sub

    <WorkItem(540174, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540174")>
    <Fact()>
    Public Sub BC30060ERR_RequiredConstConversion2_PreprocessorStringToBoolean()
        ParseAndVerify(<![CDATA[
                #Const Var = "-1"
                #If Var Then
                #End If
            ]]>,
        Diagnostic(ERRID.ERR_RequiredConstConversion2, "#If Var Then").WithArguments("String", "Boolean"))
    End Sub

    <Fact()>
    Public Sub BC30065ERR_ExitSubOfFunc()
        Dim code = <![CDATA[
                Public Class Distance
                    Public Property Number() As Double
    
                Private newPropertyValue As String
                Public Property NewProperty() As String
                    Get
                        Exit sub
                        Return newPropertyValue 
                    End Get
                    Set(ByVal value As String)
                        newPropertyValue = value
                        Exit sub
                    End Set
                End Property

                    Public Sub New(ByVal number As Double)
                        Me.Number = number
                        Exit sub
                    End Sub

                    Public Shared Operator +(ByVal op1 As Distance, ByVal op2 As Distance) As Distance
                    Exit sub
                        Return New Distance(op1.Number + op2.Number)
                    End Operator

                    Public Shared Operator -(ByVal op1 As Distance, ByVal op2 As Distance) As Distance
                        Return New Distance(op1.Number - op2.Number)
                    Exit sub
                    End Operator

                    Sub AAA
                        Exit sub
                    End Sub 

                    Function BBB As Integer 
                        Exit sub
                    End Function 
                End Class
            ]]>.Value

        ' No errors now.  The check for exit sub is done in the binder and not by the parser.
        ParseAndVerify(code)
    End Sub

    <Fact()>
    Public Sub BC30066ERR_ExitPropNot()
        Dim code = <![CDATA[
                Public Class Distance
                    Public Property Number() As Double

                    Public Sub New(ByVal number As Double)
                        Me.Number = number
                        Exit property
                    End Sub

                    Public Shared Operator +(ByVal op1 As Distance, ByVal op2 As Distance) As Distance
                    Exit  property
                        Return New Distance(op1.Number + op2.Number)
                    End Operator

                    Public Shared Operator -(ByVal op1 As Distance, ByVal op2 As Distance) As Distance
                        Return New Distance(op1.Number - op2.Number)
                    Exit  property
                    End Operator

                    Sub AAA
                        Exit property
                    End Sub 

                    Function BBB As Integer 
                        Exit property
                    End Function 
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30066"/>
                                 <error id="30066"/>
                                 <error id="30066"/>
                                 <error id="30066"/>
                                 <error id="30066"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30066ERR_ExitPropNot_1()
        Dim code = <![CDATA[
                Public Class C1
                    Function FOO()
                lb1:        Exit Property
                        Return Nothing
                    End Function
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30066"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30067ERR_ExitFuncOfSub()
        Dim code = <![CDATA[
                Class C1
                    Private newPropertyValue As String
                    Public Property NewProperty() As String
                        Get
                            Exit function
                            Return newPropertyValue
                        End Get
                        Set(ByVal value As String)
                            Exit function
                            newPropertyValue = value
                        End Set
                    End Property

                    Shared Sub Main()
                    End Sub
                    Sub abc()
                        Exit function
                    End Sub
                End Class
            ]]>.Value

        ' No errors now.  The check for exit function is done in the binder and not by the parser.
        ParseAndVerify(code)
    End Sub

    <Fact()>
    Public Sub BC30081ERR_ExpectedEndIf()
        Dim code = <![CDATA[
                Public Class C1
                    Sub FOO()
                        Dim i As Short
                        For i = 1 To 10
                            If (i = 1) Then
                     Next i
                    End Sub
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30081"/>
                                 <error id="32037"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30081ERR_ExpectedEndIf_1()
        Dim code = <![CDATA[
                Module Program
                    Sub Main(args As String())
                        Dim X = 1
                        Dim y = 1
                        If (1 > 2,x = x + 1,Y = Y+1) 'invalid
                    End Sub
                End Module
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30081"/>
                                 <error id="30198"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30081ERR_ExpectedEndIf_2()
        Dim code = <![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        Dim s1_a As Object
        Dim s1_b As New Object()
        'COMPILEERROR: BC30081,BC30198
        If(true, s1_a, s1_b).mem = 1
    End Sub
End Module
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30081"/>
                                 <error id="30198"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30082ERR_ExpectedEndWhile()
        Dim code = <![CDATA[
                Module Module1
                    Dim strResp As String
                    Sub Main()
                        Dim counter As Integer = 0
                        While counter < 20
                            counter += 1
                            While True
                                While False
                                    GoTo aaa
                                End While
                            End While
                        aaa:        Exit Sub
                    End Sub
                End Module
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30082"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30083ERR_ExpectedLoop()
        Dim code = <![CDATA[
                Public Class C1
                    Sub FOO()
                        Dim d = Sub() do 
                    End Sub
                End Class
            ]]>.Value

        ParseAndVerify(code,
                       <errors>
                           <error id="30083" message="'Do' must end with a matching 'Loop'."/>
                       </errors>)
    End Sub

    <Fact()>
    Public Sub BC30087ERR_EndIfNoMatchingIf()
        Dim code = <![CDATA[
                Namespace NS1
                    Module M1
                        Sub foo()
                            #If True Then
                                End If
                            #End If
                            End If
                            If True Then
                            ElseIf False Then
                            End If
                            End If
                        End Sub
                    End Module
                End Namespace
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30087"/>
                                 <error id="30087"/>
                                 <error id="30087"/>
                             </errors>)
    End Sub

    <WorkItem(539515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539515")>
    <Fact()>
    Public Sub BC30087ERR_EndIfNoMatchingIf2()
        Dim code = <![CDATA[
                Module M
                    Sub Main()
                        If False Then Else If True Then Else
                        End If
                    End Sub
                End Module
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30087"/>
                             </errors>)
    End Sub
    <Fact()>
    Public Sub BC30092ERR_NextNoMatchingFor()
        Dim code = <![CDATA[
                    Module M1                                           
                        Sub main()
                            For Each item As String In collectionObject
                            End sub
                            Next
                        End Sub
                    End Module                                            
            ]]>.Value

        ParseAndVerify(code,
            Diagnostic(ERRID.ERR_ExpectedNext, "For Each item As String In collectionObject"),
            Diagnostic(ERRID.ERR_NextNoMatchingFor, "Next"),
            Diagnostic(ERRID.ERR_InvalidEndSub, "End Sub"))
    End Sub

    <Fact()>
    Public Sub BC30093ERR_EndWithWithoutWith()
        Dim code = <![CDATA[
                Namespace NS1
                    Module M1
                        Sub main()
                            End With
                        End Sub
                        Sub foo()
                            Dim x As aaa
                            With x
                            End With
                            End With
                        End Sub
                        Structure S1
                            Public i As Short
                        End Structure
                    End Module
                End Namespace
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30093"/>
                                 <error id="30093"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30176ERR_DuplicateAccessCategoryUsed()
        Dim code = <![CDATA[
                Public Class Class1
                    'COMPILEERROR: BC30176, "Protected"
                        Public Protected Function RetStr() as String 
                            Return "Microsoft"
                        End Function
                End Class
            ]]>.Value

        ' Error is now reported by binding
        ParseAndVerify(code)
    End Sub

    <Fact()>
    Public Sub BC30176ERR_DuplicateAccessCategoryUsed_1()
        Dim code = <![CDATA[
                Class C1
                    'COMPILEERROR: BC30176, "friend"
                    public friend foo1 as integer
                End Class
            ]]>.Value

        ' Error is now reported by binding
        ParseAndVerify(code)
    End Sub

    <Fact()>
    Public Sub BC30180ERR_UnrecognizedTypeKeyword()
        Dim code = <![CDATA[
Option strict on
imports system

Class C1
    Public f1 as New With { .Name = "John Smith", .Age = 34 }
    Public Property foo as New With { .Name2 = "John Smith", .Age2 = 34 }
    
    Public shared Sub Main(args() as string)
    End Sub
End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30180"/>
                                 <error id="30180"/>
                             </errors>)

        code = <![CDATA[
            Option strict off
            imports system

            Class C1
                Public f1 as New With { .Name = "John Smith", .Age = 34 }
                Public Property foo as New With { .Name2 = "John Smith", .Age2 = 34 }
                
                Public shared Sub Main(args() as string)
                End Sub
            End Class
                        ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30180"/>
                                 <error id="30180"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30180ERR_UnrecognizedTypeKeyword_2()
        Dim code = <![CDATA[
Class C
    Shared Sub Main()
        For each x as New Integer in New Integer() {1,2,3}
        Next
    End Sub
End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30180"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30180ERR_UnrecognizedTypeKeyword_1()
        Dim code = <![CDATA[
Public Class base
End Class

Public Class child
    'COMPILEERROR: BC30121, "inherits if(true, base, base)", BC30180,"if"
    inherits if(true, base, base)
End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30180"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30182ERR_UnrecognizedType()
        Dim code = <![CDATA[
                Class Outer(Of T)
                    Public Shared Sub Print()
                        System.Console.WriteLine(GetType(Outer(Of T).Inner(Of ))) ' BC30182: Type expected.
                        System.Console.WriteLine(GetType(Outer(Of Integer).Inner(Of ))) ' BC30182: Type expected.
                    End Sub

                    Class Inner(Of U)
                    End Class
                End Class
            ]]>.Value
        ParseAndVerify(code, Diagnostic(ERRID.ERR_UnrecognizedType, ""),
                             Diagnostic(ERRID.ERR_UnrecognizedType, ""))
    End Sub

    <Fact()>
    Public Sub BC30183ERR_InvalidUseOfKeyword()
        Dim code = <![CDATA[
                Class C1
                    Sub foo
                        If (True)
                            Dim continue = 1
                        End If
                    End Sub
                End Class

            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30183"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30183ERR_InvalidUseOfKeyword_1()
        Dim code = <![CDATA[
                imports if_alias=if(true,System,System.IO)
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30183"/>
                             </errors>)
    End Sub

    ' old name - ParsePreProcessorIfTrueAndIfFalse
    <WorkItem(898733, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30188ERR_ExpectedDeclaration()
        ParseAndVerify(<![CDATA[
                #If False Then
                    File: abc
                #End If
            ]]>)

        ParseAndVerify(<![CDATA[
                #If True Then
                    File: abc
                #End If
            ]]>, Diagnostic(ERRID.ERR_InvOutsideProc, "File:"),
             Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "abc"))
    End Sub

    <Fact()>
    Public Sub BC30188ERR_ExpectedDeclaration_1()

        ParseAndVerify(<![CDATA[
                Class C1
                    Unicode Sub sub1()
                    End Sub
                End Class
            ]]>,
            Diagnostic(ERRID.ERR_MethodMustBeFirstStatementOnLine, "Sub sub1()"),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "Unicode"))
    End Sub

    ' No error during parsing.  Param array errors are reported during binding.
    <WorkItem(536245, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536245")>
    <WorkItem(543652, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543652")>
    <Fact()>
    Public Sub BC30192ERR_ParamArrayMustBeLast()
        ParseAndVerify(<![CDATA[
                        Class C1
                          Sub foo(byval Paramarray pArr1() as Integer, byval paramarray pArr2 as integer)
                          End Sub
                        End Class
                ]]>)
    End Sub

    <Fact()>
    Public Sub BC30193ERR_SpecifiersInvalidOnInheritsImplOpt()

        ParseAndVerify(<![CDATA[
                readonly imports System.Threading
            ]]>, <errors>
                     <error id="30193"/>
                 </errors>)
    End Sub

    <Fact()>
    Public Sub BC30195ERR_ExpectedSpecifier()
        ParseAndVerify(<![CDATA[
                        Module Module1
                            Custom
                        End Module
                ]]>,
            <errors>
                <error id="30195"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30196ERR_ExpectedComma()
        ParseAndVerify(<![CDATA[
                       Public Module MyModule
                            Sub RunSnippet()
                                AddHandler Me.Click
                            End Sub
                        End Module
                ]]>,
            <errors>
                <error id="30196"/>
                <error id="30201"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30198ERR_ExpectedRparen()
        ParseAndVerify(<![CDATA[
                       Class C1
                            Dim S = Sub(
                                    End Sub
                        End Class
                ]]>,
            <errors>
                <error id="30203"/>
                <error id="30198"/>
            </errors>)
    End Sub

    <WorkItem(542237, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542237")>
    <Fact()>
    Public Sub BC30198ERR_ExpectedRparen_1()
        ParseAndVerify(<![CDATA[
                        Imports System
                        Module Program
                            Property prop As Integer
                            Sub Main(args As String())
                                Dim replyCounts(,) As Short = New Short(, 2) {}
                            End Sub
                        End Module
                ]]>)
    End Sub

    <Fact()>
    Public Sub BC30200ERR_InvalidNewInType()
        ParseAndVerify(<![CDATA[
                       Class C1
                            Function myfunc(Optional ByVal x As New test()) 
                            End Function
                        End Class
                ]]>,
            <errors>
                <error id="30200"/>
                <error id="30201"/>
                <error id="30812"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30200ERR_InvalidNewInType_1()
        ParseAndVerify(<![CDATA[
                       Structure myStruct1
                        Public Sub m(ByVal s As String)
                            Try
                            catch e as New exception  
                            End Try
                        End Sub
                        End structure
                ]]>,
            <errors>
                <error id="30200"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30201ERR_ExpectedExpression()
        ParseAndVerify(<![CDATA[
                       Class C
                            Dim c1 As New C()
                            Sub foo
                                Do
                                Loop While c1 IsNot
                            End Sub
                        End Class
                ]]>,
            <errors>
                <error id="30201"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30201ERR_ExpectedExpression_1()
        ParseAndVerify(<![CDATA[
                        Class C
                            Shared Sub Main()
                                Dim myarray As Integer() = New Integer(2) {1, 2, 3}
                                For Each  In myarray
                                Next
                            End Sub
                        End Class
                ]]>,
            <errors>
                <error id="30201"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30201ERR_ExpectedExpression_2()
        ParseAndVerify(<![CDATA[
                        Imports System
                            Module Program
                                Sub Main(args As String())
                                    Dim s = If(True, GoTo lab1, GoTo lab2)
                            lab1:
                                    s = 1
                            lab2:
                                    s = 2
                                    Dim s1 = If(True, return, return)
                                End Sub
                            End Module
                ]]>,
            <errors>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
            </errors>)
    End Sub

    <WorkItem(542238, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542238")>
    <Fact()>
    Public Sub BC30201ERR_ExpectedExpression_3()
        Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="ArrayInitializerForNonConstDim">
    <file name="a.vb">
                        Imports System
                        Module Program
                            Property prop As Integer
                            Sub Main(args As String())
                                Dim replyCounts(,) As Short = New Short(2, ) {}
                            End Sub
                        End Module
        </file>
</compilation>)
        Dim expectedErrors1 = <errors>
BC30306: Array subscript expression missing.
                                Dim replyCounts(,) As Short = New Short(2, ) {}
                                                                           ~
                 </errors>
        CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
    End Sub

    <Fact()>
    Public Sub BC30201ERR_ExpectedExpression_4()
        ParseAndVerify(<![CDATA[
                Imports System
                Module Module1
                    Sub Main()
                        Dim myArray1 As Integer(,) = New Integer(2, 1) {{1, 2}, {3, 4}, }
                        Dim myArray2 As Integer(,) = New Integer(2, 1) {{1, 2},, {4, 5}}
                        Dim myArray3 As Integer(,) = New Integer(2, 1) {{, 1}, {2, 3}, {4, 5}}
                        Dim myArray4 As Integer(,) = New Integer(2, 1) {{,}, {,}, {,}}
                    End Sub
                End Module
                ]]>,
            <errors>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30201ERR_ExpectedExpression_5()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Property prop As Integer
                    Sub Main(args As String())
                        Dim arr1(*, *) As Integer
                        Dim arr2(&,!) As Integer
                        Dim arr3(#,@) As Integer
                        Dim arr4($,%) As Integer
                    End Sub
                End Module
                ]]>,
            <errors>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30201"/>
                <error id="30203"/>
                <error id="30037"/>
                <error id="30037"/>
            </errors>)
    End Sub

    ' The parser does not report expected optional error message.  This error is reported during the declared phase when binding the method symbol parameters.
    <Fact(), WorkItem(543658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543658")>
    Public Sub BC30202ERR_ExpectedOptional()
        Dim code = <![CDATA[
                Class C1
                    Sub method(ByVal company As String, Optional ByVal office As String = "QJZ", ByVal company1 As String)
                    End Sub
                    Sub method1(Optional ByVal company As String = "ABC", Optional ByVal office As String = "QJZ", ByVal company1 As String)
                    End Sub
                End Class
            ]]>.Value

        ParseAndVerify(code)
    End Sub

    <Fact()>
    Public Sub BC30204ERR_ExpectedIntLiteral()
        Dim code = <![CDATA[
                Class C1
                     Dim libName As String = "libName"
                    #ExternalSource( "libName" , IntLiteral )
                    #End ExternalSource
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30204"/>
                                 <error id="30198"/>
                                 <error id="30205"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30207ERR_InvalidOptionCompare()
        Dim code = <![CDATA[
                Option Compare On32
                Option Compare off
                Option Compare Text
                Option Compare Binary
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30207"/>
                                 <error id="30207"/>
                             </errors>)
    End Sub

    <WorkItem(537442, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537442")>
    <Fact()>
    Public Sub BC30207ERR_InvalidOptionCompareWithXml()
        Dim code = "Option Compare <![CDATA[qqqqqq]]>" & vbCrLf
        ParseAndVerify(code, <errors>
                                 <error id="30207" message="'Option Compare' must be followed by 'Text' or 'Binary'." start="15" end="24"/>
                                 <error id="30037" message="Character is not valid." start="30" end="31"/>
                                 <error id="30037" message="Character is not valid." start="31" end="32"/>
                             </errors>)
    End Sub

    <WorkItem(527327, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527327")>
    <Fact()>
    Public Sub BC30217ERR_ExpectedStringLiteral()
        Dim code = <![CDATA[
                Class C1
                    Dim libName As String = "libName"
                    Declare Function LDBUser_GetUsers Lib GiveMePath() (lpszUserBuffer() As String, ByVal lpszFilename As String, ByVal nOptions As Long) As Integer
                    Declare Function functionName Lib libName Alias "functionName" (ByVal CompanyName As String, ByVal Options As String, ByVal Key As String) As Integer
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30217"/>
                                 <error id="30217"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30224ERR_MissingIsInTypeOf()
        Dim code = <![CDATA[
                Class C1
                    Sub BB
                        Dim MyControl = New Object ()
                        If (TypeOf MyControl  )
                            END IF
                    End Sub
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30224"/>
                                 <error id="30182"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30238ERR_LoopDoubleCondition()
        Dim code = <![CDATA[
                Structure S1
                    Function foo()
                        do  while (true)
                        Loop unit (false)
                    End Function
                End Structure
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30035"/>
                                 <error id="30238"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30240ERR_ExpectedExitKind()
        Dim code = <![CDATA[
                Namespace NS1
                    Module M1
                        Sub main()
                            With "a"
                                Exit With
                            End With
                        End Sub
                    End Module
                End Namespace
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30240"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30241ERR_ExpectedNamedArgument()
        Dim code = <![CDATA[
                	<Attr1(1, b:=2, 3, e:="Scen1")> Class Class1

	                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30241"/>
                             </errors>)
    End Sub

    ' old name - ParseInvalidDirective_ERR_ExpectedConditionalDirective
    <WorkItem(883737, "DevDiv/Personal")>
    <Fact()>
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

    <Fact()>
    Public Sub BC30252ERR_InvalidEndInterface()
        Dim code = <![CDATA[
                	Public Interface I1
                    End Interface
                    End Interface
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30252"/>
                             </errors>)
    End Sub

    <WorkItem(527673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527673")>
    <Fact()>
    Public Sub BC30311ERR_InvalidArrayInitialize()
        'This is used to verify that only a parse error only is generated for this scenario - as per the bug investigation
        'another test in BindingErrorTest.vb (BC30311ERR_WithArray_ParseAndDeclarationErrors) will verify the diagnostics which will result in multiple errors

        Dim code = <![CDATA[
Imports Microsoft.VisualBasic

Module M
	Dim x As Integer() {1, 2, 3}
    	Dim y = CType({1, 2, 3}, System.Collections.Generic.List(Of Integer))

	Sub main
	End Sub
End Module
            ]]>.Value

        ParseAndVerify(code, Diagnostic(ERRID.ERR_ExpectedEOS, "{"))
    End Sub


    <WorkItem(527673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527673")>
    <WorkItem(99258, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id=99258")>
    <Fact>
    Public Sub BC30357ERR_BadInterfaceOrderOnInherits()
        Dim code = <![CDATA[
                	Interface I1
                        sub foo()
                         Inherits System.Enum
                    End Interface
            ]]>.Value

        Const bug99258IsFixed = False

        If bug99258IsFixed Then
            ParseAndVerify(code, <errors>
                                     <error id="30357"/>
                                 </errors>)
        Else
            ParseAndVerify(code, <errors>
                                     <error id="30603"/>
                                 </errors>)
        End If
    End Sub

    <Fact()>
    Public Sub BC30380ERR_CatchNoMatchingTry()
        ParseAndVerify(<![CDATA[
                Module M
                    Sub M()
                        Try
                        Finally
                        End Try
                        Catch
                    End Sub
                End Module
                ]]>,
            <errors>
                <error id="30380"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30380ERR_CatchNoMatchingTry_CatchInsideLambda()
        ParseAndVerify(<![CDATA[
                Module M
                    Sub M()
                        Try
                            Dim x = Sub() Catch
                        End Try
                    End Sub
                End Module
                ]]>,
            <errors>
                <error id="30380"/>
            </errors>)
        ParseAndVerify(<![CDATA[
                Module M
                    Sub M()
                        Try
                            Dim x = Sub()
                                    Catch
                                End Sub
                        End Try
                    End Sub
                End Module
                ]]>,
            <errors>
                <error id="30384"/>
                <error id="36673"/>
                <error id="30383"/>
                <error id="30429"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30382ERR_FinallyNoMatchingTry()
        ParseAndVerify(<![CDATA[
                Module M
                    Sub M()
                        Try
                        Catch ex As Exception
                        Finally
                        End Try
                    Finally
                    End Sub
                End Module
            ]]>, <errors>
                     <error id="30382"/>
                 </errors>)
    End Sub

    <Fact()>
    Public Sub BC30382ERR_FinallyNoMatchingTry_FinallyInsideLambda()
        ParseAndVerify(<![CDATA[
                Module M
                    Sub M()
                        Try
                            Dim x = Sub() Finally
                        End Try
                    End Sub
                End Module
                ]]>,
            <errors>
                <error id="30382"/>
            </errors>)
        ParseAndVerify(<![CDATA[
                Module M
                    Sub M()
                        Try
                            Dim x = Sub()
                                    Finally
                                End Sub
                        End Try
                    End Sub
                End Module
                ]]>,
            <errors>
                <error id="30384"/>
                <error id="36673"/>
                <error id="30383"/>
                <error id="30429"/>
            </errors>)
    End Sub

    <WorkItem(527315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527315")>
    <Fact()>
    Public Sub BC30383ERR_EndTryNoTry()
        Dim code = <![CDATA[
                Namespace NS1
                    Module M1
                        Sub Main()
                            catch
                            End Try
                        End Sub
                            End Try
                        End Try
                    End Module
                        End Try
                End Namespace
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30380"/>
                                 <error id="30383"/>
                                 <error id="30383"/>
                                 <error id="30383"/>
                                 <error id="30383"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30460ERR_EndClassNoClass()
        Dim code = <![CDATA[
                <scen?()> Public Class C1
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30636"/>
                                 <error id="30460"/>
                             </errors>)
    End Sub

    ' old name - ParseIfDirectiveElseIfDirectiveBothTrue()
    <WorkItem(904912, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30481ERR_ExpectedEndClass()
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

    <Fact()>
    Public Sub BC30495ERR_InvalidLiteralExponent()
        Dim code = <![CDATA[
                Module M1
                    Sub Main()
                            Dim s As  Integer = 15e
                            15E:
                        Exit Sub
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30201"/>
                                 <error id="30495"/>
                                 <error id="30495"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30578ERR_EndExternalSource()
        Dim code = <![CDATA[
                Module M1
                    #End ExternalSource
                End Module                                         

            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30578"/>
                             </errors>)
    End Sub

    <WorkItem(542117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542117")>
    <Fact()>
    Public Sub BC30579ERR_ExpectedEndExternalSource()
        Dim code = <![CDATA[
                #externalsource("",2)
            ]]>.Value
        ParseAndVerify(code, Diagnostic(ERRID.ERR_ExpectedEndExternalSource, "#externalsource("""",2)"))
    End Sub

    <Fact()>
    Public Sub BC30604ERR_InvInsideEndsInterface()
        ParseAndVerify(<![CDATA[
                Interface I
                    Declare Sub Foo Lib "My"
                End Interface
            ]]>,
            Diagnostic(ERRID.ERR_InvInsideInterface, "Declare Sub Foo Lib ""My"""))
    End Sub

    <Fact()>
    Public Sub BC30617ERR_ModuleNotAtNamespace()
        Dim code = <![CDATA[
                Module M1
                    Class c3
                        Class c3_1
                                module s1
                                End Module 
                        End Class
                    End Class
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30617"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30619ERR_InvInsideEndsEnum()
        Dim code = <![CDATA[
                Public Enum e
                    e1
                Class Foo
                End Class
            ]]>.Value
        ParseAndVerify(code,
                       Diagnostic(ERRID.ERR_MissingEndEnum, "Public Enum e"),
                       Diagnostic(ERRID.ERR_InvInsideEndsEnum, "Class Foo"))
    End Sub

    <Fact()>
    Public Sub BC30621ERR_EndStructureNoStructure()
        Dim code = <![CDATA[
                Namespace NS1
                    Module M1
                        End Structure
                        Structure AA : End Structure
                        Structure BB
                        End Structure
                        Structure CC : Dim s As _
                            String : End Structure
                    End Module
                End Namespace
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30621"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30622ERR_EndModuleNoModule()
        Dim code = <![CDATA[
                Structure S1
                    End Module
                End Structure
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30622"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30623ERR_EndNamespaceNoNamespace()
        Dim code = <![CDATA[
                Namespace NS1
                    Namespace NS2
                        Module M1
                            Sub Main()
                            End Sub
                        End Module
                    End Namespace
                End Namespace
                End Namespace
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30623"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30633ERR_MissingEndSet()
        Dim code = <![CDATA[
                Class C1
                    Private newPropertyValue As String
                    Public Property NewProperty() As String
                        Get
                            Return newPropertyValue 
                        End Get

                        Set(ByVal value As String)
                            newPropertyValue = value
                                Dim S As XElement = <A <%Name>
                        End Set
                    End Property
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30481"/>
                                 <error id="30025"/>
                                 <error id="30633"/>
                                 <error id="31151"/>
                                 <error id="30636"/>
                                 <error id="31151"/>
                                 <error id="31169"/>
                                 <error id="31165"/>
                                 <error id="30636"/>
                                 <error id="31165"/>
                                 <error id="30636"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30636ERR_ExpectedGreater()
        Dim code = <![CDATA[
                <scen?()> Public Class C1
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30636"/>
                                 <error id="30460"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30637ERR_AttributeStmtWrongOrder()
        Dim code = <![CDATA[
                Module Module1
                    Sub Main()
                    End Sub
                End Module
                'Set culture as German.
                <Assembly: Reflection.AssemblyCultureAttribute("de")> 
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30637"/>
                             </errors>)
    End Sub

    ' old name - Bug869094
    <WorkItem(869094, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30638ERR_NoExplicitArraySizes()
        'Specifying array bounds on a parameter generates NotImplementedException : Error message needs to be attached somewhere
        ParseAndVerify(<![CDATA[
                Class Class1
                    Event e1(Byval p1 (0 To 10) As Single)
                End Class
            ]]>,
        <errors>
            <error id="30638"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30640ERR_InvalidOptionExplicit()
        ParseAndVerify(<![CDATA[
                Option Explicit On
                Option Explicit Text
                Option Explicit Binary
            ]]>,
        <errors>
            <error id="30640"/>
            <error id="30640"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30641ERR_MultipleParameterSpecifiers()
        ParseAndVerify(<![CDATA[
                Structure S1
                    Function foo(byref byval t as Double) as Double
                    End Function
                End Structure
            ]]>,
        <errors>
            <error id="30641"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30641ERR_MultipleParameterSpecifiers_1()
        ParseAndVerify(<![CDATA[
                interface I1
                    Function foo(byref byval t as Double) as Double
                End interface
            ]]>,
        <errors>
            <error id="30641"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30642ERR_MultipleOptionalParameterSpecifiers()
        ParseAndVerify(<![CDATA[
                Namespace NS1
                    Module Module1
                        Public Function calcSum(ByVal ParamArray optional args() As Double) As Double
                            calcSum = 0
                            If args.Length <= 0 Then Exit Function
                            For i As Integer = 0 To UBound(args, 1)
                                calcSum += args(i)
                            Next i
                        End Function
                    End Module
                End Namespace
            ]]>,
        <errors>
            <error id="30642"/>
            <error id="30812"/>
            <error id="30201"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30648ERR_UnterminatedStringLiteral()
        ParseAndVerify(<![CDATA[
                Option Strict On
                Module  M1
                    Dim A  As String = "HGF
                    End Sub
                 End Module
            ]]>,
        <errors>
            <error id="30625"/>
            <error id="30648"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30648ERR_UnterminatedStringLiteral_02()
        Dim code = <![CDATA[
#If x = "dd
" 
 
#End If
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30012" message="'#If' block must end with a matching '#End If'." start="1" end="12"/>
                                 <error id="30648" message="String constants must end with a double quote." start="9" end="12"/>
                                 <error id="30648" message="String constants must end with a double quote." start="13" end="38"/></errors>)
    End Sub

    <Fact()>
    Public Sub BC30648ERR_UnterminatedStringLiteral_03()
        Dim code = <![CDATA[
#if false
#If x = "dd
" 
 
#End If
#End If
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <Fact()>
    Public Sub BC30648ERR_UnterminatedStringLiteral_04()
        Dim code = <![CDATA[
#region "dd
"

#End Region
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30681" message="'#Region' statement must end with a matching '#End Region'." start="1" end="12"/>
                                 <error id="30648" message="String constants must end with a double quote." start="9" end="12"/>
                                 <error id="30648" message="String constants must end with a double quote." start="13" end="40"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30667ERR_ParamArrayMustBeByVal()
        Dim code = <![CDATA[
                       Module Module1
                            Public Sub foo(ByRef ParamArray x() as string)
                            End Sub
                        End Module
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30667"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30674ERR_EndSyncLockNoSyncLock()
        Dim code = <![CDATA[
                Class C1
                    Public messagesLast As Integer = -1
                    Private messagesLock As New Object
                    Public Sub addAnotherMessage(ByVal newMessage As String)
                        If True Then
                            SyncLock messagesLock
                                messagesLast += 1
                        end if: End SyncLock
                    End Sub
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30674"/>
                                 <error id="30675"/>
                             </errors>)
    End Sub

    ' redefined in Roslyn. returns 30201 more sensitive here
    <Fact()>
    Public Sub BC30675ERR_ExpectedEndSyncLock()
        Dim code = <![CDATA[
                Module M1
                    Public Sub foo(ByVal p1 As Long, ByVal p2 As Decimal)
                    End Sub
                    Sub test()
                            foo(8,
                                synclock)
                    End Sub
                End Module

            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30201"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30675ERR_ExpectedEndSyncLock_1()
        Dim code = <![CDATA[
                Module M1
                    Function foo
                        SyncLock
                    End Function
                End Module
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30675"/>
                                 <error id="30201"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30678ERR_UnrecognizedEnd()
        Dim code = <![CDATA[
                Class C1
                End Shadow Class
            ]]>.Value
        ParseAndVerify(code,
            Diagnostic(ERRID.ERR_ExpectedEndClass, "Class C1"),
            Diagnostic(ERRID.ERR_UnrecognizedEnd, "End"))
    End Sub

    <Fact()>
    Public Sub BC30678ERR_UnrecognizedEnd_1()
        Dim code = <![CDATA[
                Public Property strProperty() as string
                    Get
                        strProperty = XstrProperty
                    End Get
                End
                End Property
            ]]>.Value
        ParseAndVerify(code,
            Diagnostic(ERRID.ERR_EndProp, "Public Property strProperty() as string"),
            Diagnostic(ERRID.ERR_UnrecognizedEnd, "End"),
            Diagnostic(ERRID.ERR_InvalidEndProperty, "End Property"))
    End Sub

    ' old name - ParseRegion_ERR_EndRegionNoRegion()
    <WorkItem(904877, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30680ERR_EndRegionNoRegion()
        ParseAndVerify(<![CDATA[
                #End Region
            ]]>,
        <errors>
            <error id="30680"/>
        </errors>)
    End Sub

    ' old name - ParseRegion_ERR_ExpectedEndRegion
    <WorkItem(2908, "DevDiv_Projects/Roslyn")>
    <WorkItem(904877, "DevDiv/Personal")>
    <WorkItem(527211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527211")>
    <Fact()>
    Public Sub BC30681ERR_ExpectedEndRegion()
        ParseAndVerify(<![CDATA[
                #Region "Start"
            ]]>,
            Diagnostic(ERRID.ERR_ExpectedEndRegion, "#Region ""Start"""))
    End Sub

    <Fact()>
    Public Sub BC30689ERR_ExecutableAsDeclaration()
        ParseAndVerify(<![CDATA[
               On 1 Goto 1000
            ]]>,
            Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "On 1 Goto 1000"),
            Diagnostic(ERRID.ERR_ObsoleteOnGotoGosub, ""))
    End Sub

    <Fact()>
    Public Sub BC30689ERR_ExecutableAsDeclaration_1()
        ParseAndVerify(<![CDATA[
               class C1
                    Continue Do
                End class
            ]]>,
        <errors>
            <error id="30689"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30710ERR_ExpectedEndOfExpression()
        Dim code = <![CDATA[
                Module Module1
                    Dim y = Aggregate x In {1} Into Sum(x) Is Nothing 
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30710"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30785ERR_DuplicateParameterSpecifier()
        Dim code = <![CDATA[
                Class C1
                    Sub foo(ByVal ParamArray paramarray() As Double)
                    end sub
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30785"/>
                                 <error id="30203"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30802ERR_ObsoleteStructureNotType_1()
        Dim code = <![CDATA[
                Structure S1
                    Type type1
                End Structure
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30802"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30802ERR_ObsoleteStructureNotType()
        Dim code = <![CDATA[
                Module M1
                    Public Type typTest2
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30802"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30804ERR_ObsoleteObjectNotVariant()
        Dim code = <![CDATA[
                Module M1
                    function foo() as variant
                        return 1
                    end function
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30804"/>
                             </errors>)
    End Sub

    ' bc30198 is more sensitive here
    <WorkItem(527353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527353")>
    <Fact()>
    Public Sub BC30805ERR_ObsoleteArrayBounds()
        Dim code = <![CDATA[
                Module Module1
                    Public Sub Main()
                        Dim x8(0 To 10 To 100) As Char
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30198"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30811ERR_ObsoleteRedimAs()
        Dim code = <![CDATA[
                Module M1
                    Sub Main()
                        Dim Obj1
		                Redim Obj1(12) as Short                                     
                        Dim boolAry(,) As Boolean
		                Redim boolAry(20, 30) as Boolean
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30811"/>
                                 <error id="30811"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30812ERR_ObsoleteOptionalWithoutValue()
        Dim code = <![CDATA[
                Class C1
                    Function f1(Optional ByVal c1 )
                    End Function
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30812"/>
                                 <error id="30201"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30817ERR_ObsoleteOnGotoGosub()
        Dim code = <![CDATA[
                Module M1
                    Sub main()
                        on 6/3 Goto Label1, Label2
                Label1:
                Label2:
                        On 2 GoSub Label1, 20, Label2
                        Exit Sub
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30817"/>
                                 <error id="30817"/>
                             </errors>)
    End Sub

    ' old name - ParsePreprocessorIfEndIfNoSpace
    <WorkItem(881437, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30826ERR_ObsoleteEndIf()
        ParseAndVerify(<![CDATA[
                #If true
                #Endif
            ]]>,
        <errors>
            <error id="30826"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30827ERR_ObsoleteExponent()
        Dim code = <![CDATA[
                Class C1
                    Public Function NumToText(ByVal dblVal As Double) As String
                         Const Mole = 6.02d+23 ' Same as 6.02D23
                        Const Mole2 = 6.02D23
                    End Function
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30201"/>
                                 <error id="30827"/>
                                 <error id="30201"/>
                                 <error id="30827"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30829ERR_ObsoleteGetStatement()
        Dim code = <![CDATA[
                Public Class ContainerClass
                    Sub Ever()          
                        Get Me.text
                    End Sub
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30829"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30944ERR_SyntaxInCastOp()
        Dim code = <![CDATA[
                Class C1
                    Dim xx = CType(Expression:=55 , Short ) 
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30944"/>
                                 <error id="30182"/>
                                 <error id="30198"/>
                                 <error id="30183"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30985ERR_ExpectedQualifiedNameInInit()
        Dim code = <![CDATA[
                Class C1
                    Dim GetOrderQuerySet As MessageQuerySet = New MessageQuerySet With
                     {  {"OrderID", New XPathMessageQuery("//psns:Order/psns:OrderID", pathContext)}}
                    Dim client As New Customer() With {Name = "Microsoft"}
                End Class

                Class Customer
                    Public Name As String
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30985"/>
                                 <error id="30985"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30996ERR_InitializerExpected()
        Dim code = <![CDATA[
                Class Customer
                    Public Name As String
                    Public Some As Object
                End Class
                Module M1
                    Sub foo()
                        Const b = New Customer With {}
                        Const b1 as Object= New Customer With {}
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30996"/>
                                 <error id="30996"/>
                             </errors>)
    End Sub

    <WorkItem(538001, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538001")>
    <Fact()>
    Public Sub BC30999ERR_LineContWithCommentOrNoPrecSpace()
        Dim code = <![CDATA[
                Public Class C1
	                Dim cn ="Select * From Titles Jion Publishers " _
                    & "ON Publishers.PubId = Titles.PubID "_ 
                "Where Publishers.State = 'CA'"
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30999"/>
                                 <error id="30035"/>
                             </errors>)

        code = <![CDATA[
                Public Class C1
	                Dim cn ="Select * From Titles Jion Publishers " _
                    & "ON Publishers.PubId = Titles.PubID "_ '
                "Where Publishers.State = 'CA'"
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30999"/>
                                 <error id="30035"/>
                             </errors>)

        code = <![CDATA[
                Public Class C1
	                Dim cn ="Select * From Titles Jion Publishers " _
                    & "ON Publishers.PubId = Titles.PubID "_ Rem
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30999"/>
                             </errors>)

        code = <![CDATA[
                Public Class C1
	                Dim cn ="Select * From Titles Jion Publishers " _
                    & "ON Publishers.PubId = Titles.PubID "_]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="30999"/>
                                 <error id="30481"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC30999_Multiple()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From c in "" _
 _
 _

End Module
]]>)
        ' As above, but with tabs instead of spaces.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From c in "" _
 _
 _

End Module
]]>.Value.Replace(" ", vbTab))
        ' As above, but with full-width underscore characters.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From c in "" _
 _
 _

End Module
]]>.Value.Replace("_", SyntaxFacts.FULLWIDTH_LOW_LINE),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"))
    End Sub

    ''' <summary>
    ''' Underscores in XML should not be interpreted
    ''' as continuation characters.
    ''' </summary>
    <Fact()>
    Public Sub BC30999_XML()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <x>_
_
_ _
        </>
    Dim y = <y _=""/>
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = GetXmlNamespace( _ )
    Dim y = GetXmlNamespace(_
        )
End Module
]]>)
    End Sub

    <Fact()>
    Public Sub BC30999_MultipleSameLine()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From c in "" _ _

End Module
]]>,
            <errors>
                <error id="30203" message="Identifier expected."/>
            </errors>)
        ' As above, but with tabs instead of spaces.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From c in "" _ _

End Module
]]>.Value.Replace(" ", vbTab),
            <errors>
                <error id="30203" message="Identifier expected."/>
            </errors>)
        ' As above, but with full-width underscore characters.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From c in "" _ _

End Module
]]>.Value.Replace("_", SyntaxFacts.FULLWIDTH_LOW_LINE),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"))
    End Sub

    <WorkItem(630127, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/630127")>
    <Fact()>
    Public Sub BC30999_AtLineStart()
        ParseAndVerify(<![CDATA[
Module M
_

End Module
]]>,
            <errors>
                <error id="30999"/>
            </errors>)
        ' As above, but with full-width underscore characters.
        ParseAndVerify(<![CDATA[
Module M
_

End Module
]]>.Value.Replace("_", SyntaxFacts.FULLWIDTH_LOW_LINE),
            <errors>
                <error id="30203" message="Identifier expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From c in "" _
_

End Module
]]>,
            <errors>
                <error id="30999"/>
            </errors>)
        ' As above, but with full-width underscore characters.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = From c in "" _
_

End Module
]]>.Value.Replace("_", SyntaxFacts.FULLWIDTH_LOW_LINE),
            <errors>
                <error id="30203" message="Identifier expected."/>
                <error id="30203" message="Identifier expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = 
_

End Module
]]>,
            <errors>
                <error id="30201"/>
                <error id="30999"/>
            </errors>)
        ' As above, but with full-width underscore characters.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = 
_

End Module
]]>.Value.Replace("_", SyntaxFacts.FULLWIDTH_LOW_LINE),
            <errors>
                <error id="30201"/>
                <error id="30203" message="Identifier expected."/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC31001ERR_InvInsideEnum()
        Dim code = <![CDATA[
                Public Enum e
                    e1
                    'COMPILEERROR: BC30619, "if"
                    if(true,3,4)
                'COMPILEERROR: BC30184,"End Enum"
                End Enum
            ]]>.Value
        ParseAndVerify(code,
            Diagnostic(ERRID.ERR_InvInsideEnum, "if(true"),
            Diagnostic(ERRID.ERR_ExpectedRparen, ""))
    End Sub

    <Fact()>
    Public Sub BC31002ERR_InvInsideBlock_If_Class()
        Dim source = <text>
If True
    Class Foo
    
    End Class
End If
</text>.Value

        ParseAndVerify(source, TestOptions.Script,
            Diagnostic(ERRID.ERR_InvInsideBlock, "Class Foo").WithArguments("If"))
    End Sub

    <Fact()>
    Public Sub BC31002ERR_InvInsideBlock_Do_Function()
        Dim source = <text>
Do
    Function Foo
    End Function
Loop
</text>.Value

        ParseAndVerify(source, TestOptions.Script,
            Diagnostic(ERRID.ERR_InvInsideBlock, "Function Foo").WithArguments("Do Loop"))
    End Sub

    <Fact()>
    Public Sub BC31002ERR_InvInsideBlock_While_Sub()
        Dim source = <text>
While True
    Sub Foo
    End Sub
End While
</text>.Value

        ParseAndVerify(source, TestOptions.Script,
            Diagnostic(ERRID.ERR_InvInsideBlock, "Sub Foo").WithArguments("While"))
    End Sub

    <WorkItem(527330, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527330")>
    <Fact()>
    Public Sub BC31085ERR_InvalidDate()
        Dim code = <![CDATA[
                Class C1
                    Dim d2 As Date
                    Dim someDateAndTime As Date = #8/13/2002 29:14:00 PM#
                    Sub foo()
                        d2 = #23/04/2002#
                        Dim da As Date = #02/29/2009#
                    End Sub
                End Class
            ]]>.Value
        ParseAndVerify(code,
            Diagnostic(ERRID.ERR_ExpectedExpression, ""),
            Diagnostic(ERRID.ERR_InvalidDate, "#8/13/2002 29:14:00 PM#"),
            Diagnostic(ERRID.ERR_ExpectedExpression, ""),
            Diagnostic(ERRID.ERR_InvalidDate, "#23/04/2002#"),
            Diagnostic(ERRID.ERR_ExpectedExpression, ""),
            Diagnostic(ERRID.ERR_InvalidDate, "#02/29/2009#"))
    End Sub

    ' Not report 31118 for this case in Roslyn
    <WorkItem(527344, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527344")>
    <Fact()>
    Public Sub BC31118ERR_EndAddHandlerNotAtLineStart()
        Dim code = <![CDATA[
                Class TimerState
                    Public Delegate Sub MyEventHandler(ByVal sender As Object, ByVal e As EventArgs)
                    Private m_MyEvent As MyEventHandler
                    Public Custom Event MyEvent As MyEventHandler
                        AddHandler(ByVal value As MyEventHandler)
                            m_MyEvent = DirectCast ( _
                            [Delegate].Combine(m_MyEvent, value), _
                            MyEventHandler) : End addHandler
                    End Event
                End Class
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    ' Not report 31119 for this case in Roslyn
    <WorkItem(527310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527310")>
    <Fact()>
    Public Sub BC31119ERR_EndRemoveHandlerNotAtLineStart()
        Dim code = <![CDATA[
                Class C1
                    Public Delegate Sub MyEventHandler(ByVal sender As Object, ByVal e As EventArgs)
                    Private m_MyEvent As MyEventHandler
                    Public Custom Event MyEvent As MyEventHandler
                        RemoveHandler(ByVal value As MyEventHandler)
                            m_MyEvent = DirectCast ( _
                            [Delegate].RemoveAll(m_MyEvent, value), _
                            MyEventHandler) : End RemoveHandler
                        RemoveHandler(ByVal value As MyEventHandler)
                            m_MyEvent = DirectCast ( _
                            [Delegate].RemoveAll(m_MyEvent, value),
                            MyEventHandler)
                 :
                        End RemoveHandler
                    End Event
                End Class
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    ' Not report 31120 for this case in Roslyn
    <WorkItem(527309, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527309")>
    <Fact()>
    Public Sub BC31120ERR_EndRaiseEventNotAtLineStart()
        Dim code = <![CDATA[
                Class C1
                    Public Delegate Sub MyEventHandler(ByVal sender As Object, ByVal e As EventArgs)
                    Private m_MyEvent As MyEventHandler
                    Public Custom Event MyEvent As MyEventHandler
                        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
                            m_MyEvent.Invoke(sender, e) : End RaiseEvent
                        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
                            m_MyEvent.Invoke(sender, e)
                 :
                        End RaiseEvent
                    End Event
                End Class
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <WorkItem(887848, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC31122ERR_CustomEventRequiresAs()
        ParseAndVerify(<![CDATA[
    Class c1
       Public Custom Event sc2()
    End Class
    ]]>,
<errors>
    <error id="31122"/>
</errors>)
    End Sub

    <Fact()>
    Public Sub BC31123ERR_InvalidEndEvent()
        Dim code = <![CDATA[
                Class Sender
                    End Event
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31123"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC31124ERR_InvalidEndAddHandler()
        Dim code = <![CDATA[
                Class C1
                    Public Custom Event Fire As EventHandler
                        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                        End RaiseEvent
                                    End AddHandler
                        RemoveHandler(ByVal newValue As EventHandler)
                        End RemoveHandler
                    End Event
                End Class
    ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="31114"/>
                                 <error id="31124"/>
                                 <error id="31112"/>
                                 <error id="30188"/>
                                 <error id="31125"/>
                                 <error id="31123"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC31125ERR_InvalidEndRemoveHandler()
        Dim code = <![CDATA[
                Module module1
                    End removehandler
                    Public Structure S1
                        End removehandler
                    End Structure
                    Sub Main()
                    End Sub
                End Module
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31125"/>
                                 <error id="31125"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC31126ERR_InvalidEndRaiseEvent()
        Dim code = <![CDATA[
                Module module1
                    End raiseevent
                    Public Structure digit
                        End raiseevent
                    End Structure
                    Sub Main()
                    End Sub
                End Module
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31126"/>
                                 <error id="31126"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC31149ERR_DuplicateXmlAttribute()
        Dim tree = Parse(<![CDATA[
Module M
    Dim x = <?xml version="1.0" version="1.0"?><root/>
    Dim y = <?xml version="1.0" encoding="utf-8" encoding="unicode"?><root/>
    Dim z = <?xml version="1.0" standalone="yes" standalone="yes"?><root/>
End Module
            ]]>)
        tree.AssertTheseDiagnostics(<errors><![CDATA[
BC31149: Duplicate XML attribute 'version'.
    Dim x = <?xml version="1.0" version="1.0"?><root/>
                                ~~~~~~~~~~~~~
BC31149: Duplicate XML attribute 'encoding'.
    Dim y = <?xml version="1.0" encoding="utf-8" encoding="unicode"?><root/>
                                                 ~~~~~~~~~~~~~~~~~~
BC31149: Duplicate XML attribute 'standalone'.
    Dim z = <?xml version="1.0" standalone="yes" standalone="yes"?><root/>
                                                 ~~~~~~~~~~~~~~~~
        ]]></errors>)
    End Sub

    <Fact()>
    Public Sub BC31153ERR_MissingVersionInXmlDecl()
        Dim tree = Parse(<![CDATA[
Module M
    Private F = <?xml?><root/>
End Module
                ]]>)
        tree.AssertTheseDiagnostics(<errors><![CDATA[
BC31153: Required attribute 'version' missing from XML declaration.
    Private F = <?xml?><root/>
                     ~
        ]]></errors>)
    End Sub

    <Fact()>
    Public Sub BC31154ERR_IllegalAttributeInXmlDecl()
        Dim tree = Parse(<![CDATA[
Module M
    Private F1 = <?xml version="1.0" a="b"?><x/>
    Private F2 = <?xml version="1.0" xmlns:p="http://roslyn"?><x/>
End Module
            ]]>)
        tree.AssertTheseDiagnostics(<errors><![CDATA[
BC31154: XML declaration does not allow attribute 'a'.
    Private F1 = <?xml version="1.0" a="b"?><x/>
                                     ~~~~~
BC30249: '=' expected.
    Private F2 = <?xml version="1.0" xmlns:p="http://roslyn"?><x/>
                                     ~~~~~
BC31154: XML declaration does not allow attribute 'xmlns'.
    Private F2 = <?xml version="1.0" xmlns:p="http://roslyn"?><x/>
                                     ~~~~~
        ]]></errors>)
    End Sub

    <WorkItem(537222, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537222")>
    <Fact()>
    Public Sub BC31156ERR_VersionMustBeFirstInXmlDecl()
        Dim code = <![CDATA[
    Module M1
        Sub DocumentErrs()
            'COMPILEERROR : BC31156, "version" 
            Dim x = <?xml encoding="utf-8" version="1.0"?><root/>
            'COMPILEERROR : BC31156, "version" 
            Dim x4 = <e><%= <?xml encoding="utf-8" version="1.0"?><root/> %></e>
        End Sub
    End Module
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31156"/>
                                 <error id="31156"/>
                             </errors>).VerifySpanOfChildWithinSpanOfParent()
    End Sub

    <Fact()>
    Public Sub BC31157ERR_AttributeOrder()
        Dim code = <![CDATA[
                Imports System.Xml.Linq
                Imports System.Collections.Generic
                Imports System.Linq
                Module M
                    Dim y = <?xml standalone="yes" encoding="utf-8"?><root/>
                End Module
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31157"/>
                                 <error id="31153"/>
                             </errors>)
    End Sub

    <WorkItem(538964, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538964")>
    <Fact()>
    Public Sub BC31157ERR_AttributeOrder_1()
        Dim code = <![CDATA[
                Imports System.Xml.Linq
                Imports System.Collections.Generic
                Imports System.Linq
                Module M
                    Dim y = <?xml version="1.0" standalone="yes" encoding="utf-8"?><root/>
                End Module
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31157"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC31161ERR_ExpectedXmlEndComment()
        Dim code = <![CDATA[
                Module M1
                    Sub Foo
                        Dim x = <!--hello
                    End Sub
                End Module
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31161"/>
                                 <error id="30026"/>
                                 <error id="30625"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC31162ERR_ExpectedXmlEndCData()
        Dim code = <![CDATA[
                Module M1
                    Sub Foo()
                        'COMPILEERROR : L3, BC31162, "e" 
	                         Dim x = <![CDATA[
                    End Sub
                End Module
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31162"/>
                                 <error id="30026"/>
                                 <error id="30625"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC31163ERR_ExpectedSQuote()
        Dim tree = Parse(<![CDATA[
Module M
    Private F = <x a='b</>
End Module
                    ]]>)
        tree.AssertTheseDiagnostics(<errors><![CDATA[
BC31163: Expected matching closing single quote for XML attribute value.
    Private F = <x a='b</>
                       ~
        ]]></errors>)
    End Sub

    <Fact()>
    Public Sub BC31164ERR_ExpectedQuote()
        Dim tree = Parse(<![CDATA[
Module M
    Private F = <x a="b</>
End Module
                    ]]>)
        tree.AssertTheseDiagnostics(<errors><![CDATA[
BC31164: Expected matching closing double quote for XML attribute value.
    Private F = <x a="b</>
                       ~
        ]]></errors>)
    End Sub

    <WorkItem(537218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537218")>
    <Fact()>
    Public Sub BC31175ERR_DTDNotSupported()
        Dim code = <![CDATA[
    Module DTDErrmod
        Sub DTDErr()

            'COMPILEERROR : BC31175, "DOCTYPE" 
            Dim a = <?xml version="1.0"?><!DOCTYPE Order SYSTEM "dxx_install/samples/db2xml/dtd/getstart.dtd"><e/>

        End Sub
    End Module
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31175"/>
                             </errors>).VerifySpanOfChildWithinSpanOfParent()
    End Sub

    <Fact()>
    Public Sub BC31180ERR_XmlEntityReference()
        Dim code = <![CDATA[
                Class a
                    Dim test = <test>
                                 This is a test. &amp; &copy; &nbsp;
                             </test>
                End Class 
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="31180"/>
                                 <error id="31180"/>
                             </errors>)
    End Sub

    <WorkItem(542975, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542975")>
    <Fact()>
    Public Sub BC31207ERR_XmlEndElementNoMatchingStart()
        Dim tree = Parse(<![CDATA[
Module M
    Private F1 = </x1>
    Private F2 = <?xml version="1.0"?></x2>
    Private F3 = <?xml version="1.0"?><?p?></x3>
End Module
            ]]>)
        tree.AssertTheseDiagnostics(<errors><![CDATA[
BC31207: XML end element must be preceded by a matching start element.
    Private F1 = </x1>
                 ~~~~~
BC31165: Expected beginning '<' for an XML tag.
    Private F2 = <?xml version="1.0"?></x2>
                                      ~
BC31207: XML end element must be preceded by a matching start element.
    Private F3 = <?xml version="1.0"?><?p?></x3>
                                           ~~~~~
        ]]></errors>)
    End Sub

    <Fact()>
    Public Sub BC31426ERR_BadTypeInCCExpression()
        Dim code = <![CDATA[
                Module Module1
                    Sub Main()
                         #Const A = CType(1, System.Int32)
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="31426"/>
                                 <error id="30198"/>
                             </errors>)
    End Sub

    ' old name - ParsePreProcessorIfGetType_ERR_BadCCExpression()
    <WorkItem(888313, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC31427ERR_BadCCExpression()
        ParseAndVerify(<![CDATA[
                #If GetType(x) Then
                #End If
            ]]>,
        <errors>
            <error id="31427"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC32009ERR_MethodMustBeFirstStatementOnLine()
        ParseAndVerify(<![CDATA[
                Namespace NS1
                    Interface IVariance(Of Out T) : Function Foo() As T : End Interface
                    Public Class Variance(Of T As New) : Implements IVariance(Of T) : Public Function Foo() As T Implements IVariance(Of T).Foo
                            Return New T
                        End Function
                    End Class
                    Module M1 : Sub IF001()
                        End Sub
                    End Module
                End Namespace
]]>,
            <errors>
                <error id="32009"/>
                <error id="32009"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Interface I
    Function F():Function G():Sub M()
End Interface
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Function F()
    End Function:Function G()
    End Function
End Module
]]>,
            <errors>
                <error id="32009"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function F()
    End Function:Function G(
)
    End Function
End Module
]]>,
            <errors>
                <error id="32009"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Function F()
    End Function:Sub New()
    End Sub
End Class
]]>,
            <errors>
                <error id="32009"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC32019ERR_ExpectedResumeOrGoto()
        Dim code = <![CDATA[
                Class C1
                    Public Sub OnErrorDemo()
                        On Error    
                        GoTo ErrorHandler
                        On Error 
                        Resume Next
                        On Error 
                ErrorHandler: Exit Sub
                    End Sub
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="32019"/>
                                 <error id="32019"/>
                                 <error id="32019"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC32024ERR_DefaultValueForNonOptionalParam()
        Dim code = <![CDATA[
                Class A
                    Private newPropertyValue As String = Nothing
                    Public Property NewProperty() As String = Nothing
                        Get
                            Return newPropertyValue
                        End Get
                        Set(ByVal value As String = Nothing)
                            newPropertyValue = value
                        End Set
                    End Property
                    Sub Foo(Dt As Date = Nothing) 
                    End Sub
                End Class
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="36714"/>
                                 <error id="32024"/>
                                 <error id="32024"/>
                             </errors>)
    End Sub

    ' changed in roslyn
    <WorkItem(527338, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527338")>
    <Fact()>
    Public Sub BC32025ERR_RegionWithinMethod()
        ParseAndVerify(<![CDATA[
                       Public Module MyModule
                            Sub RunSnippet()
                                Dim a As A = New A(Int32.MaxValue)
                        #region          
                                Console.WriteLine("")
                         #end region 
                            End Sub
                        End Module
                        Class A
                            Public Sub New(ByVal sum As Integer)
                        #region "Foo"
                        #end region
                            End Sub
                        End Class
                ]]>,
            <errors>
                <error id="30217"/>
            </errors>)
    End Sub

    ' bc32026 is removed in roslyn
    <Fact()>
    Public Sub BC32026ERR_SpecifiersInvalidOnNamespace()
        ParseAndVerify(<![CDATA[
                       <C1>
                        Namespace NS1
                        End Namespace
                        Class C1
                            Inherits Attribute
                        End Class
                ]]>,
            <errors>
                <error id="30193"/>
            </errors>)
    End Sub

    <WorkItem(527316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527316")>
    <Fact()>
    Public Sub BC32027ERR_ExpectedDotAfterMyBase()
        Dim code = <![CDATA[
                Namespace NS1
                    Class C1
                        Private Str As String
                        Public Sub S1()
                            MyBase
                            MyBase()
                            If MyBase Is MyBase Then Str = "Yup"
                            If MyBase Is Nothing Then Str = "Yup"
                        End Sub
                        Function F2(ByRef arg1 As Short) As Object
                            F2 = MyBase
                        End Function
                        Sub S3()
                            With MyBase
                            End With
                        End Sub
                    End Class
                End Namespace
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="32027"/>
                                 <error id="32027"/>
                                 <error id="32027"/>
                                 <error id="32027"/>
                                 <error id="32027"/>
                                 <error id="32027"/>
                                 <error id="32027"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC32028ERR_ExpectedDotAfterMyClass()
        Dim code = <![CDATA[
                Namespace NS1
                    Class C1
                        Sub S1()
                            Dim Str As String
                            MyClass = "Test"
                            Str = MyClass
                        End Sub

                        Sub S2()
                            Dim Str As String
                            Str = MyClass()
                            MyClass()
                        End Sub
                    End Class
                End Namespace
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="32028"/>
                                 <error id="32028"/>
                                 <error id="32028"/>
                                 <error id="32028"/>
                             </errors>)
    End Sub

    ' old name - ParsePreProcessorElseIf_ERR_LbElseifAfterElse()
    <WorkItem(897858, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC32030ERR_LbElseifAfterElse()
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

    ' not repro 32031 in this case for Roslyn
    <WorkItem(527312, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527312")>
    <Fact()>
    Public Sub BC32031ERR_EndSubNotAtLineStart()
        Dim code = <![CDATA[
                Namespace NS1
                    Module M1
                        Sub main()
                            Dim  s As string:        End Sub
                        Sub AAA()

                        :End Sub
                    End Module
                End Namespace
            ]]>.Value

        ParseAndVerify(code)
    End Sub

    ' not report 32032 in this case for Roslyn
    <WorkItem(527341, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527341")>
    <Fact()>
    Public Sub BC32032ERR_EndFunctionNotAtLineStart()
        Dim code = <![CDATA[
                Module M1
                    Function  B As string
                        Dim x = <!--hello-->:  End Function
                    Function  C As string
                        Dim x = <!--hello-->
                    :End Function
                End Module
            ]]>.Value

        ParseAndVerify(code)
    End Sub

    ' not report 32033 in this case for Roslyn
    <WorkItem(527342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527342")>
    <Fact()>
    Public Sub BC32033ERR_EndGetNotAtLineStart()
        Dim code = <![CDATA[
                Class C1
    
                    Private _name As String
                    Public READONLY Property Name() As String
                        Get
                            Return _name:    End Get
                    End Property
    
                    Private _age As String
                    Public readonly Property Age() As String
                        Get
                            Return _age
 
                        :End Get
                    End Property
                    End Class
            ]]>.Value

        ParseAndVerify(code)
    End Sub

    ' not report 32034 in this case for Roslyn
    <WorkItem(527311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527311")>
    <Fact()>
    Public Sub BC32034ERR_EndSetNotAtLineStart()
        Dim code = <![CDATA[
                Class C1
                    Private propVal As Integer
                    WriteOnly Property prop1() As Integer
                        Set(ByVal value As Integer)
                            propVal = value:        End Set
                    End Property

                    Private newPropertyValue As String
                    Public Property NewProperty() As String
                        Get
                            Return newPropertyValue
                        End Get
                        Set(ByVal value As String)
                            newPropertyValue = value
                :        End Set
                    End Property
                End Class
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <Fact()>
    Public Sub BC32035ERR_StandaloneAttribute()
        Dim code = <![CDATA[
                Module M1
                    <AttributeUsage(AttributeTargets.All)> Class clsTest
                        Inherits Attribute
                    End Class
                        <clsTest()> ArgI as Integer
                    Sub Main()
                        Exit Sub
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="32035"/>
                             </errors>)
    End Sub

    <WorkItem(527311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527311")>
    <Fact()>
    Public Sub BC32037ERR_ExtraNextVariable()
        Dim code = <![CDATA[
                Class A
                    Sub AAA()
                        For I = 1 To 10
                            For J = 1 To 5
                            Next J, I, K   
                    End Sub
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="32037"/>
                             </errors>)
    End Sub

    <WorkItem(537219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537219")>
    <Fact()>
    Public Sub BC32059ERR_OnlyNullLowerBound()
        Dim code = <![CDATA[
    Module M1
        Sub S1()
            Dim x1() As Single
            ' COMPILEERROR: BC32059, "0!"
    	    ReDim x1(0! To 5)
        End Sub
    End Module
    ]]>.Value
        ParseAndVerify(code).VerifySpanOfChildWithinSpanOfParent()
    End Sub

    <WorkItem(537223, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537223")>
    <Fact()>
    Public Sub BC32065ERR_GenericParamsOnInvalidMember()
        Dim code = <![CDATA[
     Module M1
        Class c2(Of T1)
            'COMPILEERROR: BC32065, "(Of T1)"
            Sub New(Of T1)()
            End Sub
        End Class
    End Module
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="32065"/>
                             </errors>).VerifySpanOfChildWithinSpanOfParent()
    End Sub

    <WorkItem(537988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537988")>
    <Fact()>
    Public Sub BC32066ERR_GenericArgsOnAttributeSpecifier()
        Dim code = <![CDATA[
                Module M1
                    <test(of integer)>
                    Class c2 
                    End Class
                End Module
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="32066"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC32073ERR_ModulesCannotBeGeneric()
        Dim code = <![CDATA[
                Namespace NS1
                    Module Module1(of T)
                    End Module
                End Namespace
            ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="32073"/>
                             </errors>)
    End Sub

    <WorkItem(527337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527337")>
    <Fact()>
    Public Sub BC32092ERR_BadConstraintSyntax()
        Dim code = <![CDATA[
                Public Class itemManager(Of t As )
                    ' Insert code that defines class members.
                End Class
            ]]>.Value

        ParseAndVerify(code, Diagnostic(ERRID.ERR_UnrecognizedType, "").WithLocation(2, 50),
                            Diagnostic(ERRID.ERR_BadConstraintSyntax, "").WithLocation(2, 50))
    End Sub

    <Fact()>
    Public Sub BC32099ERR_TypeParamMissingCommaOrRParen()
        Dim code = <![CDATA[
                Namespace NS1
                    Module M1
                        Class GenCls(of T as new with{})	
                            Function GenFoo(of U as new with{})   
                            End Function
                        End Class

                        Class Outer(Of T)
                            Public Shared Sub Print()
                                System.Console.WriteLine(GetType(Outer(Of ).Inner(Of T))) ' BC32099: Comma or ')' expected.
                                System.Console.WriteLine(GetType(Outer(Of ).Inner(Of Integer))) ' BC32099: Comma or ')' expected.
                            End Sub

                            Class Inner(Of U)
                            End Class
                        End Class
                    End Module
                End Namespace
            ]]>.Value

        ParseAndVerify(code, Diagnostic(ERRID.ERR_TypeParamMissingCommaOrRParen, ""),
                             Diagnostic(ERRID.ERR_TypeParamMissingCommaOrRParen, ""),
                             Diagnostic(ERRID.ERR_TypeParamMissingCommaOrRParen, "T"),
                             Diagnostic(ERRID.ERR_TypeParamMissingCommaOrRParen, "Integer"))
    End Sub

    <Fact()>
    Public Sub BC33000_UnknownOperator()
        ParseAndVerify(<![CDATA[
    Class c1
        Public Shared Operator __ (ByVal x As Integer) As Interaction
        End Operator
    End Class

    Class c1
        Public Shared Operator (ByVal x As Integer) As Interaction
        End Operator
    End Class
            ]]>, Diagnostic(ERRID.ERR_UnknownOperator, "__"),
             Diagnostic(ERRID.ERR_UnknownOperator, ""))
    End Sub

    <WorkItem(3372, "DevDiv_Projects/Roslyn")>
    <Fact()>
    Public Sub BC33001ERR_DuplicateConversionCategoryUsed()
        Dim code = <![CDATA[
                Public Structure digit
                    Private dig As Byte
                    Public Shared Widening Narrowing Operator CType(ByVal d As digit) As Byte
                        Return d.dig
                    End Operator
                End Structure
            ]]>.Value
        ' Error is now reported in binding not the parser
        ParseAndVerify(code)
    End Sub

    <Fact()>
    Public Sub BC33003ERR_InvalidHandles()
        Dim code = <![CDATA[
                Public Structure abc
                    Dim d As Date
                    Public Shared Operator And(ByVal x As abc, ByVal y As abc) Handles global.Obj.Ev_Event
                        Dim r As New abc
                        Return r
                    End Operator
                    Public Shared widening Operator CType(ByVal x As abc) As integer  handles
                        Return 1
                    End Operator
                End Structure
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="33003"/>
                                 <error id="33003"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC33004ERR_InvalidImplements()
        Dim code = <![CDATA[
                Public Structure S1
                    Public Shared Operator +(ByVal v As S1, _
                               ByVal w As S1) Implements ICustomerInfo
                    End Operator
                End Structure
                Public Interface ICustomerInfo
                End Interface
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="33004"/>
                             </errors>)
    End Sub

    <WorkItem(527308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527308")>
    <Fact()>
    Public Sub BC33006ERR_EndOperatorNotAtLineStart()
        Dim code = <![CDATA[
                Public Structure abc
                    Dim d As Date
                    Public Shared Operator And(ByVal x As abc, ByVal y As abc) As abc
                        Dim r As New abc
                        Return r:    End Operator
                    Public Shared Operator Or(ByVal x As abc, ByVal y As abc) As abc
                        Dim r As New abc
                        Return r
                    End _
                        Operator
                    Public Shared Operator IsFalse(ByVal z As abc) As Boolean
                        Dim b As Boolean : Return b:    End Operator
                    Public Shared Operator IsTrue(ByVal z As abc) As Boolean
                        Dim b As Boolean
                        Return b
                    End Operator
                End Structure
            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <Fact()>
    Public Sub BC33007ERR_InvalidEndOperator()
        Dim code = <![CDATA[
                Module module1
                    End Operator
                    Public Structure digit
                        Private dig As Byte
                        End Operator
                    End Structure
                    Sub Main()
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="33007"/>
                                 <error id="33007"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC33008ERR_ExitOperatorNotValid()
        Dim code = <![CDATA[
                Public Class Distance
                    Public Property Number() As Double

                    Public Sub New(ByVal number As Double)
                        Me.Number = number
                    End Sub

                    Public Shared Operator +(ByVal op1 As Distance, ByVal op2 As Distance) As Distance
                    Exit  operator
                        Return New Distance(op1.Number + op2.Number)
                    End Operator

                    Public Shared Operator -(ByVal op1 As Distance, ByVal op2 As Distance) As Distance
                        Return New Distance(op1.Number - op2.Number)
                    Exit  operator
                    End Operator

                    Public Shared Operator >=(ByVal op1 As Distance, ByVal op2 As Distance) As Boolean
                        Exit  operator
                    End Operator

                    Public Shared Operator <=(ByVal op1 As Distance, ByVal op2 As Distance) As Boolean
                        Exit  operator
                    End Operator
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="33008"/>
                                 <error id="33008"/>
                                 <error id="33008"/>
                                 <error id="33008"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC33111ERR_BadNullTypeInCCExpression()
        Dim code = <![CDATA[
                #Const triggerPoint = 0
                ' Not valid.
                #If CType(triggerpoint, Boolean?) = True Then
                        ' Body of the conditional directive.
                #End If
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="33111"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC33201ERR_ExpectedExpression()
        Dim code = <![CDATA[
                Module Program
                    Sub Main(args As String())
                        Dim X = 1
                        Dim Y = 1
                        Dim S = If(True, , Y = Y + 1)
                        S = If(True, X = X + 1, )
                        S = If(, X = X + 1, Y = Y + 1)
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, Diagnostic(ERRID.ERR_ExpectedExpression, ""),
                                Diagnostic(ERRID.ERR_ExpectedExpression, ""),
                                Diagnostic(ERRID.ERR_ExpectedExpression, ""))
    End Sub

    <Fact()>
    Public Sub BC33201ERR_ExpectedExpression_1()
        Dim code = <![CDATA[
                Module Program
                    Sub Main(args As String())
                        Dim X = 1
                        Dim Y = 1
                        Dim S1 = If(Dim B = True, X = X + 1, Y = Y + 1)
                        Dim S2 = If(True,dim x1 = 2,dim y1 =3)
                        Dim S3 = If(True, X = 2,dim y1 = 3)
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, Diagnostic(ERRID.ERR_ExpectedExpression, ""),
                                Diagnostic(ERRID.ERR_ExpectedExpression, ""),
                                Diagnostic(ERRID.ERR_ExpectedExpression, ""),
                                Diagnostic(ERRID.ERR_ExpectedExpression, ""))
    End Sub

    <Fact()>
    Public Sub BC36000ERR_ExpectedDotAfterGlobalNameSpace()
        Dim code = <![CDATA[
                Class AAA
                    Sub BBB
                    Global IDPage  As Long = CLng("123")
                    END Sub 
                End Class 
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36000"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC36001ERR_NoGlobalExpectedIdentifier()
        Dim code = <![CDATA[
                Imports global = Microsoft.CSharp
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36001"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC36002ERR_NoGlobalInHandles()
        Dim code = <![CDATA[
                Public Class C1
                    Sub EventHandler() Handles global.Obj.Ev_Event 
                         MsgBox("EventHandler caught event.")
                    End Sub
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36002"/>
                                 <error id="30287"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC36007ERR_EndUsingWithoutUsing()
        Dim code = <![CDATA[
                Class AAA
                    Using stream As New IO.MemoryStream()
                    End Using
                    Sub bbb()
                        End  USING 
                    End Sub
                End Class
            ]]>.Value
        ParseAndVerify(code,
            Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Using stream As New IO.MemoryStream()"),
            Diagnostic(ERRID.ERR_EndUsingWithoutUsing, "End Using"),
            Diagnostic(ERRID.ERR_EndUsingWithoutUsing, "End  USING"))
    End Sub

    <Fact()>
    Public Sub BC36556ERR_AnonymousTypeFieldNameInference()
        Dim code = <![CDATA[
                Module M 
                    Sub Main()
                        Dim x = New With {Foo(Of String)} 
                        Dim y = New With { new with { .id = 1 } } 
                    End Sub
                    Function Foo(Of T)()
                        Return 1
                    End Function
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36556"/>
                                 <error id="36556"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC36575ERR_AnonymousTypeNameWithoutPeriod()
        Dim code = <![CDATA[
                Module M 
                    Sub Main()
                         Dim instanceName2 = New With {memberName = 10}
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36575"/>
                             </errors>)
    End Sub

    <WorkItem(537985, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537985")>
    <Fact()>
    Public Sub BC36605ERR_ExpectedBy()
        Dim code = <![CDATA[
                Option Explicit On
Namespace NS1
    Module M1
        Sub GBInValidGroup()
                Dim cole = {"a","b","c"}
                Dim q2 = From x In col let y = x Group x Group y By x y Into group
                Dim q3 = From x In col let y = x Group x y By x Into group
                Dim q5 = From x In col let y = x Group i as integer = x, y By x Into group
        End Sub
    End Module
End Namespace
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36605"/>
                                 <error id="36615"/>
                                 <error id="36615"/>
                                 <error id="36605"/>
                                 <error id="36615"/>
                                 <error id="36605"/>
                                 <error id="36615"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC36613ERR_AnonTypeFieldXMLNameInference()
        Dim code = <![CDATA[
                Module M 
                    Sub Main()
                                'COMPILEERROR:BC36613,"Elem-4"
                                Dim y1 = New With {<e/>.@<a-a-a>}
                    End Sub
                End Module
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36613"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC36618ERR_ExpectedOn()
        Dim code = <![CDATA[
                Namespace JoinRhsInvalid
                    Module JoinRhsInvalidmod
                        Sub JoinRhsInvalid()
                                Dim q1 = From i In col1 join j in col2, k in col1 on i Equals j + k
                        End Sub
                    End Module
                End Namespace
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36618"/>
                             </errors>)
    End Sub

    <WorkItem(538492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538492")>
    <Fact()>
    Public Sub BC36618ERR_ExpectedOn_1()
        Dim code = <![CDATA[
                 Module M
                    Sub FOO()
                        Dim col1l = New List(Of Int16) From {1, 2, 3}
                        Dim col1la = New List(Of Int16) From {1, 2, 3}
                        Dim col1r = New List(Of Int16) From {1, 2, 3}
                        Dim col1ra = New List(Of Int16) From {1, 2, 3}
                        Dim q2 = From i In col1l, j In col1la, ii In col1l, jj In col1la Join k In col1r _
                        Join l In col1ra On k Equals l Join kk In col1r On kk Equals k Join ll In col1ra On l Equals ll _
                        On i * j Equals l * k And ll + kk Equals ii + jj Select i
                    End Sub
                End Module  

            ]]>.Value
        ParseAndVerify(code)
    End Sub

    <WorkItem(527317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527317")>
    <Fact()>
    Public Sub BC36619ERR_ExpectedEquals()
        Dim code = <![CDATA[
                Option Explicit On
                Namespace JoinOnInvalid
                    Module JoinOnInvalidmod
                        Sub JoinOnInvalid()
                                Dim col1 = {"a", "b", "c"}
                                Dim q0 = From i In col1 Join j In col1 On i Equals j
                                Dim q1 = From i In col1 Join j In col1 On i = j
                                Dim q2 = From i In col1 Join j In col1 On i And j
                        End Sub
                    End Module
                End Namespace
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36619"/>
                                 <error id="36619"/>
                                 <error id="36619"/>
                             </errors>)
    End Sub

    ' old name - RoundtripForInvalidQueryWithOn()
    <WorkItem(921279, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36620ERR_ExpectedAnd()
        ParseAndVerify(<![CDATA[
            Namespace JoinOnInvalid
                Module JoinOnInvalidmod
                    Sub JoinOnInvalid()
                            Dim col1 = Nothing
                            Dim q4 = From i In col1 Join j In col1 On i Equals j And i Equals j Andalso i Equals j
                    End Sub
                End Module
            End Namespace
                ]]>,
            <errors>
                <error id="36620"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC36629ERR_NullableTypeInferenceNotSupported()
        Dim code = <![CDATA[
                Namespace A
                    Friend Module B
                        Sub C()
                                Dim col2() As Integer = New Integer() {1, 2, 3, 4}
                                Dim q = From a In col2 Select b? = a

                        End Sub
                    End Module
                End Namespace
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36629"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC36637ERR_NullableCharNotSupported()
        Dim code = <![CDATA[
                Class C1
                    Public Function foo() As Short
                        Dim local As Short
                        return local?
                    End Function
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36637"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC36707ERR_ExpectedIdentifierOrGroup()
        Dim code = <![CDATA[
                Namespace IntoInvalid
                    Module IntoInvalidmod
                        Sub IntoInvalid()
                              Dim q1 = from i In col select i Group Join j in col2 On i equals j Into 

                                Dim q9 =From i In col Group Join j in col2 On i equals j Into 
                        End Sub
                    End Module
                End Namespace
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36707"/>
                                 <error id="36707"/>
                             </errors>)
    End Sub

    <Fact()>
    Public Sub BC36708ERR_UnexpectedGroup()
        Dim code = <![CDATA[
                Class C
                    Dim customers As new List(of string) from {"aaa", "bbb"}
                    Dim customerList2 = From cust In customers _
                                    Aggregate order In cust.ToString() _
                                    Into group 
                End Class
            ]]>.Value
        ParseAndVerify(code, <errors>
                                 <error id="36708"/>
                             </errors>)
    End Sub

    ' old name - ParseProperty_ERR_InitializedExpandedProperty
    <WorkItem(880151, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36714ERR_InitializedExpandedProperty()
        ParseAndVerify(<![CDATA[
                class c
                    Public WriteOnly Property P7() As New C1 With {._x = 3} 
                        Set(ByVal value As C1)
                            _P7 = value
                        End Set
                    End Property
                end class
            ]]>,
        <errors>
            <error id="36714"/>
        </errors>)
    End Sub

    ' old name - ParseProperty_ERR_AutoPropertyCantHaveParams
    <Fact()>
    Public Sub BC36759ERR_AutoPropertyCantHaveParams()
        ParseAndVerify(<![CDATA[
                class c
                    Public Property P7(i as integer) As New C1 With {._x = 3} 
                end class
            ]]>,
        <errors>
            <error id="36759"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC36920ERR_SubRequiresParenthesesLParen()
        ParseAndVerify(<![CDATA[
                Public Class MyTest
                    Public Sub Test2(ByVal myArray() As String)
                        Dim yyy = Sub() RaiseEvent cc()()
                    End Sub
                End Class
            ]]>,
        <errors>
            <error id="36920"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC36921ERR_SubRequiresParenthesesDot()
        ParseAndVerify(<![CDATA[
                Public Class MyTest
                    Public Sub Test2(ByVal myArray() As String)
                        Dim yyy = Sub() RaiseEvent cc().Invoke()
                    End Sub
                End Class
            ]]>,
        <errors>
            <error id="36921"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC36922ERR_SubRequiresParenthesesBang()
        ParseAndVerify(<![CDATA[
                Public Class MyTest
                    Public Sub Test2(ByVal myArray() As String)
                        Dim yyy = Sub() RaiseEvent cc()!key
                    End Sub
                End Class
            ]]>,
        <errors>
            <error id="36922"/>
        </errors>)
    End Sub

#End Region

#Region "Targeted Warning Tests - please arrange tests in the order of error code"

    ' old name - ParseXmlDoc_WRNID_XMLDocNotFirstOnLine()
    <WorkItem(527096, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527096")>
    <Fact()>
    Public Sub BC42302WRN_XMLDocNotFirstOnLine()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Main()
                        Dim x = 42 ''' <test />
                    End Sub
                End Module
            ]]>)
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Main()
                        Dim x = 42 ''' <test />
                    End Sub
                End Module
            ]]>,
        VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
        <errors>
            <error id="42302" warning="True"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC42302WRN_XMLDocNotFirstOnLine_NoError()
        ' NOTE: this error is not reported by parser
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Main()
                        Dim x = 42 ''' <test />
                    End Sub
                End Module
            ]]>)
    End Sub

    <WorkItem(530052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530052")>
    <Fact()>
    Public Sub BC42303WRN_XMLDocInsideMethod_NoError()
        ' NOTE: this error is not reported by parser
        ParseAndVerify(<![CDATA[
                 Module M1
                    Sub test()
                        '''
                    End Sub
                End Module
            ]]>)
        ParseAndVerify(<![CDATA[
                 Module M1
                    Sub test()
                        '''
                    End Sub
                End Module
            ]]>, VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose))
    End Sub

    ' old name -ParseNestedCDATA_ERR_ExpectedLT
    <WorkItem(904414, "DevDiv/Personal")>
    <WorkItem(914949, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC42304WRN_XMLDocParseError1()
        Dim code = <!--
    Module M1
    '''<doc>
    '''<![CDATA[
    '''<![CDATA[XML doesn't allow CDATA sections to nest]]>
    ''']]>
    '''</doc>
    Sub Main()
    End Sub
    End Module
    -->.Value
        ParseAndVerify(code)
        ParseAndVerify(code,
                       VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
                       <errors>
                           <error id="42304" warning="True"/>
                       </errors>)
    End Sub

    <Fact()>
    Public Sub BC42306WRN_XMLDocIllegalTagOnElement2()
        Dim code = <!--
    Option Explicit On
    Imports System
    Class C1
        ''' <returns>ret</returns> 
        ''' <remarks>rem</remarks>
        Delegate Sub b(Of T)(ByVal i As Int16, ByVal j As Int16) 
    End Class
    -->.Value
        ParseAndVerify(code) ' NOTE: this error is not reported by parser
    End Sub

#End Region

#Region "Mixed Error Tests - used to be ErrorMessageBugs.vb"

    'Dev10 reports both errors but hides the second.  We report all errors so this is be design.
    <WorkItem(887998, "DevDiv/Personal")>
    <Fact()>
    Public Sub ParseMoreErrorExpectedIdentifier()
        ParseAndVerify(<![CDATA[
                         Class c1
                            Dim x1 = New List(Of Integer) with {.capacity=2} FROM {2,3} 
                         End Class

                ]]>,
            <errors>
                <error id="36720"/>
                <error id="30203"/>
            </errors>)
    End Sub

    'Assert in new parse tree
    <WorkItem(921273, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC31053ERR_ImplementsStmtWrongOrder_NoAssertForInvalidOptionImport()
        ParseAndVerify(<![CDATA[
                         Class c1
        Class cn1
    option Compare Binary 
    option Explicit Off
    option Strict off
    Imports VB6 = Microsoft.VisualBasic 
    Imports Microsoft.VisualBasic 
    deflng x
            Public ss As Long
            Implements I1
            Inherits c2
            Sub foo()
        End Class
    End Class
                ]]>,
            Diagnostic(ERRID.ERR_OptionStmtWrongOrder, "option Compare Binary"),
            Diagnostic(ERRID.ERR_OptionStmtWrongOrder, "option Explicit Off"),
            Diagnostic(ERRID.ERR_OptionStmtWrongOrder, "option Strict off"),
            Diagnostic(ERRID.ERR_ImportsMustBeFirst, "Imports VB6 = Microsoft.VisualBasic"),
            Diagnostic(ERRID.ERR_ImportsMustBeFirst, "Imports Microsoft.VisualBasic"),
            Diagnostic(ERRID.ERR_ExpectedSpecifier, "x"),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "deflng"),
            Diagnostic(ERRID.ERR_ImplementsStmtWrongOrder, "Implements I1"),
            Diagnostic(ERRID.ERR_InheritsStmtWrongOrder, "Inherits c2"),
            Diagnostic(ERRID.ERR_EndSubExpected, "Sub foo()"))
    End Sub

    <WorkItem(930036, "DevDiv/Personal")>
    <Fact()>
    Public Sub TestErrorsOnChildAlsoPresentOnParent()
        Dim code = <![CDATA[
    class c1
                        Dim x = <?xml version='1.0' encoding = <%= "utf-16" %> 
                                    something = ""?><e/>
                        Dim x = <?xml version='1.0' <%= "encoding" %>="utf-16"
                                <%= "something" %>=""?><e/>
    end class
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31172"/>
                                 <error id="31154"/>
                                 <error id="31146"/>
                                 <error id="31172"/>
                                 <error id="31146"/>
                                 <error id="31172"/>
                             </errors>).VerifyErrorsOnChildrenAlsoPresentOnParent()
    End Sub

    <Fact, WorkItem(537131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537131"), WorkItem(527922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527922"), WorkItem(527553, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527553")>
    Public Sub TestNoAdjacentTriviaWithSameKind()
        Dim code = <![CDATA[
    module m1
    sub foo
    Dim x43 = <?xml version="1.0"?>
                              <?xmlspec abcd?>
                              <!-- <%= %= -->
                    <Obsolete("<%=")> Static x as Integer = 5
    end sub
    end module
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30625"/>
                                 <error id="30026"/>
                                 <error id="31151"/>
                                 <error id="30035"/>
                                 <error id="30648"/>
                                 <error id="31159"/>
                                 <error id="31165"/>
                                 <error id="30636"/>
                             </errors>).VerifyNoAdjacentTriviaHaveSameKind()
    End Sub

    <WorkItem(537131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537131")>
    <Fact()>
    Public Sub TestNoAdjacentTriviaWithSameKind2()
        Dim code = <![CDATA[class c1
end c#@1]]>.Value

        ParseAndVerify(code,
            Diagnostic(ERRID.ERR_ExpectedEndClass, "class c1"),
            Diagnostic(ERRID.ERR_UnrecognizedEnd, "end")).VerifyNoAdjacentTriviaHaveSameKind()
    End Sub

    <WorkItem(538861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538861")>
    <WorkItem(539509, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539509")>
    <Fact>
    Public Sub TestIllegalTypeParams()
        Dim code = <![CDATA[Module Program(Of T)
    Sub Main(args As String())
    End Sub
End Module

Enum E(Of T)
End Enum

Structure S
    Sub New(Of T)(x As Integer)
    End Sub
    Event E(Of T)
    Property P(Of T) As Integer
    Shared Operator +(Of T)(x As S, y As S) As S
        Return New S
    End Operator
End Structure]]>

        ParseAndVerify(code, <errors>
                                 <error id="32073"/>
                                 <error id="32065"/>
                                 <error id="32065"/>
                                 <error id="32065"/>
                                 <error id="32065"/>
                                 <error id="32065"/>
                             </errors>).VerifyOccurrenceCount(SyntaxKind.TypeParameterList, 0).
                                        VerifyOccurrenceCount(SyntaxKind.TypeParameter, 0).
                                        VerifyNoAdjacentTriviaHaveSameKind()
    End Sub

    <WorkItem(929948, "DevDiv/Personal")>
    <Fact()>
    Public Sub TestChildSpanWithinParentSpan()
        Dim code = <![CDATA[
    class c1
    Dim x = <?xml version='1.0' encoding = <%= "utf-16" %> 
    something = ""?><e/>
    Dim x = <?xml version='1.0' <%= "encoding" %>="utf-16"
    <%= "something" %>=""?><e/>
    end class
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="31172"/>
                                 <error id="31154"/>
                                 <error id="31146"/>
                                 <error id="31172"/>
                                 <error id="31146"/>
                                 <error id="31172"/>
                             </errors>).VerifySpanOfChildWithinSpanOfParent()
    End Sub

    <Fact()>
    Public Sub BC31042ERR_ImplementsOnNew()
        Dim tree = Parse(<![CDATA[
Interface I
    Sub M()
End Interface
Class C
    Implements I
    Sub New() Implements I.M
    End Sub
End Class
            ]]>)
        tree.AssertTheseDiagnostics(<errors><![CDATA[
BC31042: 'Sub New' cannot implement interface members.
    Sub New() Implements I.M
        ~~~
        ]]></errors>)
    End Sub

    <WorkItem(1905, "DevDiv_Projects/Roslyn")>
    <Fact()>
    Public Sub BC31042ERR_ImplementsOnNew_TestRoundTripHandlesAfterNew()
        Dim code = <![CDATA[
    Module EventError004mod
        Class scenario12
            'COMPILEERROR: BC30497, "New"
            Shared Sub New() Handles var1.event1

            End Sub
        End Class

    Public Interface I1
        Sub foo(ByVal x As Integer)
    End Interface
    Class C
        'COMPILEERROR: BC30149, "I1"   <- not a parser error
        Implements I1
        'COMPILEERROR: BC31042, "new"
        Private Sub New(ByVal x As Integer) Implements I1.foo
        End Sub

    End Class
    End Module
    ]]>.Value

        ParseAndVerify(code, <errors>
                                 <error id="30497"/>
                                 <error id="31042"/>
                             </errors>).VerifySpanOfChildWithinSpanOfParent()
    End Sub

    <WorkItem(541266, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541266")>
    <Fact()>
    Public Sub BC30182_ERR_UnrecognizedType()

        Dim Keypair = New KeyValuePair(Of String, Object)("CompErrorTest", -1)
        Dim opt = VisualBasicParseOptions.Default.WithPreprocessorSymbols(Keypair)

        Dim code = <![CDATA[
    Protected Property p As New
    ]]>.Value
        VisualBasicSyntaxTree.ParseText(code, options:=opt, path:="")
    End Sub

    <WorkItem(541284, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541284")>
    <Fact()>
    Public Sub ParseWithChrw0()

        Dim code = <![CDATA[
    Sub SUB0113 ()
I<
 
    ]]>.Value
        code = code & ChrW(0)
        VisualBasicSyntaxTree.ParseText(code)
    End Sub

    <WorkItem(541286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541286")>
    <Fact()>
    Public Sub BC33002ERR_OperatorNotOverloadable_ParseNotOverloadableOperators1()

        Dim code = <![CDATA[
    Class c1
    'COMPILEERROR: BC33002, "."
    Shared Operator.(ByVal x As c1, ByVal y As c1) As Integer
    End Operator
End Class
    ]]>.Value
        VisualBasicSyntaxTree.ParseText(code)
    End Sub

    <WorkItem(541291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541291")>
    <Fact()>
    Public Sub RoundTrip()
        Dim code = <![CDATA[Dim=<><%=">
<
    ]]>.Value
        Dim tree = VisualBasicSyntaxTree.ParseText(code)
        Assert.Equal(code, tree.GetRoot().ToString())
    End Sub

    <WorkItem(541293, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541293")>
    <Fact()>
    Public Sub RoundTrip_1()
        Dim code = <![CDATA[Property)As new t(Of Integer) FROM {1, 2, 3}]]>.Value
        Dim tree = VisualBasicSyntaxTree.ParseText(code)
        Assert.Equal(code, tree.GetRoot().ToString())
    End Sub

    <WorkItem(541291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541291")>
    <Fact()>
    Public Sub RoundTrip_2()
        Dim code = <![CDATA[Dim=<><%={%>
<
    ]]>.Value
        Dim tree = VisualBasicSyntaxTree.ParseText(code)
        Assert.Equal(code, tree.GetRoot().ToString())
    End Sub

    <WorkItem(716245, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716245")>
    <Fact>
    Public Sub ManySkippedTokens()
        Const numTokens As Integer = 500000 ' Prohibitively slow without fix.
        Dim source As New String("`"c, numTokens)
        Dim tree = VisualBasicSyntaxTree.ParseText(source)
        Dim emptyStatement = tree.GetRoot().DescendantNodes().OfType(Of EmptyStatementSyntax).Single()
        Assert.Equal(numTokens, emptyStatement.FullWidth)
        Assert.Equal(source, tree.ToString())
        Assert.Equal(InternalSyntax.Scanner.BadTokenCountLimit, emptyStatement.GetTrailingTrivia().Single().GetStructure().DescendantTokens().Count) ' Confirm that we built a list.
    End Sub

#End Region

End Class
