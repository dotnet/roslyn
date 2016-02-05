' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Roslyn.Test.Utilities

<CLSCompliant(False)>
Public Class ParseStatements
    Inherits BasicTestBase

    <Fact>
    Public Sub ParseIf()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()

                    if true then f() else if true then g()

                    If True 
                    End If

                    If True Then
                    End If

                    If True Then
                    Else
                    End If

                    If True Then
                    ElseIf False Then
                    End If

                    If True Then
                    ElseIf False Then
                    Else
                    End If

                    if true then else

                    if true then else :
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseDo()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()

                     Do
                     Loop  

                    do while true
                    loop

                    do until true
                    loop

                    do 
                    loop until true

                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseWhile()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        dim x = 1

                        while x < 1
                        end while

                        with x
                        end with

                        synclock x
                        end synclock

                    end sub
                End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseFor()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        for i = 0 to 100
                        next
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseForEach()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        for each c in "hello"
                        next
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseSelect()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        select i
                            case 0
                            case 1
                            case 2
                            case else
                        end select
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseTry()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        try
                        catch 
                        finally    
                        end try
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseUsing()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        using e0
                        end using

                        using e1 as new C, e2 as new C
                        end using

                        using e3 as new with {.foo="bar"}
                        end using
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseUsingMultipleVariablesInAsNew()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        dim a, b as new C
                        using e1, e2 as new C, e3 as new C
                        end using
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseContinue()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        do
                            continue do
                        loop

                        while true
                            continue do
                        end while

                        for i = 0 to 10 
                           continue for
                        next
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseExit()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub s1()
                        do
                            exit do 
                        loop

                        while true
                            exit while 
                        end while

                        for i = 0 to 10 
                           exit for 
                        next

                        select 0
                        case 0
                           exit select
                        end select

                        try
                           exit try
                        catch
                        end try

                        Exit sub
                    end sub

                   function f1() as integer
                      exit function
                   end function

                   readonly property p1 as integer
                   get
                      exit property
                   end get
                   end property
               End Module
            ]]>)
    End Sub

    <WorkItem(538594, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538594")>
    <Fact>
    Public Sub ParseOnErrorGoto()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub s1()
                        on error goto 0
                        on error goto 0UL
                        on error goto -1
                        on error goto -1UL
                        on error goto mylabel
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseResume()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub s1()
                        resume next
                        resume mylabel
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseCallStatement()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub s1()
                        call mysub(of string)(1,2,3,4)
                    end sub
               End Module
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseRedim()
        ParseAndVerify(<![CDATA[
                Class c1
                    Dim a(,) As Integer
                    Sub s()
                        Dim a() As c1
                        ReDim a(10)
                        ReDim a(0 To 10)
                        ReDim Preserve a(10)
                        ReDim Preserve a(10).a(0 To 10, 0 To 20)
                        ' the following is actually wrong according to the grammar
                        ' but VB10 parses it, so we will too for now.
                        ReDim Preserve a(0 To 10).a(0 To 10, 0 To 20)
                    End Sub
                End Class
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseReturn_Bug868414()
        'Bug 868414 - Exception when return is missing expression.
        ParseAndVerify(<![CDATA[
                Class Class1
                    Function Foo() As Integer
                        Return
                    End Function
                End Class
            ]]>)
    End Sub

    <Fact>
    Public Sub ParseAssignmentOrCall()
        ParseAndVerify(<![CDATA[
                class c1
                    sub s
                        i = 1
                        i(10)
                    end sub
                end class
            ]]>)
    End Sub

    <WorkItem(871360, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLineIfThen()
        ParseAndVerify(<![CDATA[
            Class Class1
                Function Foo() As Boolean
                    Return True
                End Function
                Sub Bar()
                    If Foo() = True Then Return True

                    If Foo() <> True Then
                        Return Not True
                    End If
                End Sub
            End Class
        ]]>)
    End Sub

    <WorkItem(539194, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539194")>
    <Fact>
    Public Sub ParseSingleLineIfThenWithColon()
        ' Foo should be a call statement and not a label
        Dim tree = ParseAndVerify(<![CDATA[
Module M
  Sub Main()
    If True Then Foo: Else 
  End Sub
 
  Sub Foo()
  End Sub
End Module
        ]]>)

        Dim compUnit = tree.GetRoot()
        Dim moduleM = TryCast(compUnit.ChildNodesAndTokens()(0).AsNode, TypeBlockSyntax)
        Dim subMain = TryCast(moduleM.ChildNodesAndTokens()(1).AsNode, MethodBlockSyntax)
        Dim ifStmt = TryCast(subMain.ChildNodesAndTokens()(1).AsNode, SingleLineIfStatementSyntax)
        Dim foo = ifStmt.Statements(0)
        Assert.Equal(SyntaxKind.ExpressionStatement, foo.Kind)
        Assert.Equal(SyntaxKind.InvocationExpression, DirectCast(foo, ExpressionStatementSyntax).Expression.Kind)
    End Sub

    <Fact>
    Public Sub ParseSingleLineIfThenWithElseIf()
        Dim tree = ParseAndVerify(<![CDATA[
Module M
  Sub Main()
    If True Then ElseIf True Then x = 2 end if
    If True Then x = 1 elseIf true then x = 2 end if
  End Sub
End Module
        ]]>,
            <errors>
                <error id="30205" message="End of statement expected." start="40" end="56"/>
                <error id="30205" message="End of statement expected." start="57" end="58"/>
                <error id="36005" message="'ElseIf' must be preceded by a matching 'If' or 'ElseIf'."/>
                <error id="30205" message="End of statement expected." start="93" end="99"/>
            </errors>)
    End Sub

    <WorkItem(539204, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539204")>
    <Fact>
    Public Sub ParseColonLineCont()
        ' 2nd Else and ElseIf are dangling in the line ifs
        Dim tree = ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        ' not an error
        Return : _

    End Sub
End Module

Module M2
    Sub Main2()
        ' error
        Return :_

    End Sub
End Module
        ]]>,
        Diagnostic(ERRID.ERR_LineContWithCommentOrNoPrecSpace, "_"))
    End Sub

    <WorkItem(539204, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539204")>
    <Fact>
    Public Sub ParseSingleLineIfThenExtraElse()
        ' 2nd Else and ElseIf are dangling in the line ifs
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module M
    Sub Main()
        If True Then Console.WriteLine Else Else Console.WriteLine

        If True Then Console.WriteLine Else ElseIf Console.WriteLine
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30086" message="'Else' must be preceded by a matching 'If' or 'ElseIf'." start="84" end="88"/>
            <error id="30205" message="End of statement expected."/>
            <error id="36005" message="'ElseIf' must be preceded by a matching 'If' or 'ElseIf'." start="152" end="176"/>
        </errors>)
    End Sub

    <WorkItem(539205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539205")>
    <Fact>
    Public Sub ParseSingleLineIfWithNestedSingleLineIf()
        ' This is a valid nested line if in a line if
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module M
    Sub Main()
        If False Then If True Then Else  Else Console.WriteLine(1)
  End Sub
End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseSingleLineIfWithNestedMultiLineIf1()
        ' This is a single line if that contains an invalid multi line if.  
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module M
    Sub Main()
        If False Then If True Console.WriteLine(1)
  End Sub
End Module
        ]]>,
        <errors>
            <error id="30081" message="'If' must end with a matching 'End If'." start="62" end="69"/>
            <error id="30205" message="End of statement expected." start="70" end="77"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseSingleLineIfWithNestedMultiLineIf2()
        ' This is a single line if that contains an invalid multi line if/then/else. 
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module M
    Sub Main()
        If False Then If True  Else  Else Console.WriteLine(1)
  End Sub
End Module
        ]]>,
        <errors>
            <error id="30081" message="'If' must end with a matching 'End If'." start="62" end="69"/>
            <error id="30205" message="End of statement expected."/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseSingleLineIfWithNestedMultiLineIf3()
        ' This is a single line if that contains an invalid multi line if. 
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module M
    Sub Main()
        If true Then If True  
            Console.WriteLine(1)
        end if
  End Sub
End Module
        ]]>,
        <errors>
            <error id="30081" message="'If' must end with a matching 'End If'." start="61" end="68"/>
            <error id="30087" message="'End If' must be preceded by a matching 'If'." start="112" end="118"/>
        </errors>)
    End Sub

    <WorkItem(539207, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539207")>
    <Fact>
    Public Sub ParseSingleLineIfWithNestedDoLoop1()
        ' This is a single line if that contains an invalid do .. loop 
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module M
    Sub Main()
        If true Then do
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083" message="'Do' must end with a matching 'Loop'." start="61" end="63"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseSingleLineIfWithNestedDoLoop2()
        ' This is a single line if that contains an invalid do .. loop 
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module M
    Sub Main()
        If true Then do
            Console.WriteLine(1)
        loop
  End Sub
End Module
        ]]>,
        <errors>
            <error id="30083" message="'Do' must end with a matching 'Loop'." start="61" end="63"/>
            <error id="30091" message="'Loop' must be preceded by a matching 'Do'." start="105" end="109"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseSingleLineIfWithNestedDoLoop3()
        ' This is a single line if that contains a valid do loop
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module M
    Sub Main()
        If true Then do : Console.WriteLine(1) : loop
  End Sub
End Module
        ]]>)
    End Sub


    <WorkItem(539209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539209")>
    <Fact>
    Public Sub ParseSingleLineSubWithSingleLineIfFollowedByColonComma()
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module Program
    Sub Main()
        Dim a = Sub() If True Then Dim b = 1 :, c = 2
    End Sub
End Module
        ]]>)
    End Sub

    <WorkItem(539210, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539210")>
    <Fact>
    Public Sub ParseSingleLineIfFollowedByColonNewLine()
        ' Per Dev10 the second WriteLine should NOT be part of
        ' the single line if. This regression was caused by scanner change
        ' to scan ":" after trivia. The scanner now scans in new statement state after
        ' the colon and eats up new lines.
        Dim tree = ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        If True Then Console.WriteLine(1) :

        Console.WriteLine(2)
  End Sub
End Module
        ]]>)

        Dim compUnit = tree.GetRoot()
        Dim moduleM = TryCast(compUnit.ChildNodesAndTokens()(0).AsNode, TypeBlockSyntax)
        Dim subMain = TryCast(moduleM.ChildNodesAndTokens()(1).AsNode, MethodBlockSyntax)
        Assert.Equal(4, subMain.ChildNodesAndTokens().Count)
        Assert.Equal(SyntaxKind.SingleLineIfStatement, subMain.ChildNodesAndTokens()(1).Kind())
        Assert.Equal(SyntaxKind.ExpressionStatement, subMain.ChildNodesAndTokens()(2).Kind())
        Assert.Equal(SyntaxKind.InvocationExpression, DirectCast(subMain.ChildNodesAndTokens()(2).AsNode, ExpressionStatementSyntax).Expression.Kind)
    End Sub

    <Fact>
    Public Sub ParseSingleLineIfFollowedByColonNewLine1()
        Dim tree = ParseAndVerify(<![CDATA[
Module M
    Sub Main()
        dim s1 = sub () If True Then Console.WriteLine(1) :
        dim s2 = sub () If True Then Console.WriteLine(1) ::Console.WriteLine(1)::
        dim s3 = sub() If True Then Console.WriteLine(1) :::: Console.WriteLine(1)

        Console.WriteLine(2)
  End Sub
End Module
        ]]>)

    End Sub

    <WorkItem(539211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539211")>
    <Fact>
    Public Sub ParseSingleLineSubWithSingleLineIfFollowedByComma()
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module Program
    Sub Main()
        Dim a = Sub() If True Then Console.WriteLine, b = 2
    End Sub
End Module
        ]]>)
    End Sub

    <WorkItem(539211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539211")>
    <Fact>
    Public Sub ParseSingleLineSubWithSingleLineIfFollowedByParen()
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Module Program
    Sub Main()
        Dim a = (Sub() If True Then Console.WriteLine), b = 2
    End Sub
End Module
        ]]>)
    End Sub

    <WorkItem(530904, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530904")>
    <Fact>
    Public Sub SingleLineLambdaComma()
        Dim tree = ParseAndVerify(<![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim a(0)
        Dim b = Sub() ReDim a(1),
            Return
    End Sub
End Module
        ]]>,
<errors>
    <error id="30201" message="Expression expected." start="153" end="153"/>
</errors>)
    End Sub

    <WorkItem(539212, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539212")>
    <Fact>
    Public Sub ParseSingleLineSubWithSingleLineIfWithAnotherSingleLineSub()
        ' The second single line sub is within a var declaration after the end statement in the then clause!
        Dim tree = ParseAndVerify(<![CDATA[
Module Program
  Sub Main()
    Dim a = Sub() If True Then End _
     : Dim b = Sub() If True Then End Else End Else 
  End Sub
End Module
]]>)
    End Sub

    <WorkItem(539212, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539212")>
    <Fact>
    Public Sub ParseSingleLineSubWithIncompleteSingleLineIf()
        ' Dev10 reports single line if must contain one statement.
        Dim tree = ParseAndVerify(<![CDATA[
Module Program
  Sub Main()
    Dim a = Sub() If True Then 
  End Sub
End Module
]]>, Diagnostic(ERRID.ERR_ExpectedEndIf, "If True Then"))
    End Sub

    <WorkItem(539212, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539212")>
    <Fact>
    Public Sub ParseSubLambdaWithIncompleteSingleLineIf()
        ' The single line if actually gets parsed as a block if in both dev10 and roslyn
        Dim tree = ParseAndVerify(<![CDATA[
Module Program
  Sub Main()
    Dim a = Sub() 
        If True Then 
  End Sub
End Module
]]>,
<errors>
    <error id="30026" message="'End Sub' expected." start="18" end="28"/>
    <error id="30081" message="'If' must end with a matching 'End If'." start="56" end="68"/>
</errors>)
    End Sub

    <WorkItem(871931, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseErase()
        ParseAndVerify(<![CDATA[
            Class Class1
                Public vobja() As Object
                Public Sub Foo()
                    Erase vobja
                End Sub                
            End Class
        ]]>)
    End Sub

    <WorkItem(872003, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseError()
        ParseAndVerify(<![CDATA[
            Class Class1
                Public Sub Foo()
                    Error 5
                End Sub                
            End Class
        ]]>)
    End Sub

    <WorkItem(872005, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLabel()
        ParseAndVerify(<![CDATA[
            Class Class1
                Public Sub Foo()
            10:     Dim x = 42
                End Sub                
            End Class
        ]]>)
    End Sub

    <WorkItem(538606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538606")>
    <Fact>
    Public Sub LabelFollowedByMethodInvocation()
        ParseAndVerify(<![CDATA[
            Module M
              Sub Main()
                Foo : Foo : Foo()
              End Sub
 
              Sub Foo()
              End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(541358, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541358")>
    <Fact>
    Public Sub LabelRelatedToBug8037()
        ParseAndVerify(<![CDATA[
            Module M
              Sub Main()
#if 1 < 2
                label1:
#elseif 2 < 3
                label2:
#elseif false
                label3:
#end if

                label4:

                label5:

              End Sub

::
#region "foo" 
              Sub Foo()
              End Sub
::
#end region
              Sub Foo_()
              End Sub


              ''' <summary>
              ''' </summary>  
              :Sub Foo2()
              :End Sub

              ''' <summary>
              ''' </summary>  
              Sub Foo2_()
              End Sub

              :<summary()>
              Sub Foo3()
              :End Sub

#if false
#end if
              Sub Foo4()
                :              
              :End Sub

              :' nice little innocent comment
              Sub Foo5()
              End Sub

#garbage on
    Sub Foo6()
    End Sub

#Const CustomerNumber = 36
    Sub Foo7()
    End Sub
            End Module
        ]]>, <errors>
                 <error id="32009" message="Method declaration statements must be the first statement on a logical line." start="441" end="451"/>
                 <error id="32009" message="Method declaration statements must be the first statement on a logical line." start="599" end="635"/>
                 <error id="30248" message="'If', 'ElseIf', 'Else', 'End If', 'Const', or 'Region' expected." start="853" end="854"/>
             </errors>)

        ' doesn't work, most probably because of the line break ...
        'Diagnostic(ERRID.ERR_MethodMustBeFirstStatementOnLine, "Sub Foo2()"),
        'Diagnostic(ERRID.ERR_MethodMustBeFirstStatementOnLine, "<summary()>" + vbCrLf + "              Sub Foo3()"),
        'Diagnostic(ERRID.ERR_ExpectedConditionalDirective, "#"))
    End Sub

    <WorkItem(872013, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMid()
        ParseAndVerify(<![CDATA[
            Class Class1
                Public Sub Foo(ByRef r As aType)
                    Mid$(r.S(2, 2), 1, 1) = "-"
                    Mid(r.S(2, 2), 1, 1) = "-"
                End Sub                
            End Class
        ]]>)
    End Sub

    <WorkItem(542623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542623")>
    <Fact>
    Public Sub ParseMidIdentifier1()
        ParseAndVerify(<![CDATA[
            Class Mid
                Public Sub Foo(ByRef r As aType)
                    Mid.Foo(nothing) ' Roslyn will now recognize this correctly as a member access
                    
                    Dim mid(42) as Integer
                    mid(23) = 33 ' false recognition as mid statement assignment -> error for missing ,
                    [mid](23) = 33
                End Sub                

                Public Sub Mid(p as Integer)
                End Sub
            End Class
        ]]>, Diagnostic(ERRID.ERR_ExpectedComma, ""),
             Diagnostic(ERRID.ERR_ExpectedExpression, "")
        )
    End Sub

    <WorkItem(542623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542623")>
    <Fact>
    Public Sub ParseMidIdentifier2()
        ParseAndVerify(<![CDATA[
            Class Mid
                Public Sub Foo(ByRef r As aType)
                End Sub                

                Public Sub Mid(p as Integer)
                    Mid(23) ' false recognition as mid statement assignment -> error for missing expression
                    [Mid](23)
                End Sub
            End Class
        ]]>, Diagnostic(ERRID.ERR_ExpectedComma, ""),
             Diagnostic(ERRID.ERR_ExpectedExpression, ""),
             Diagnostic(ERRID.ERR_ExpectedEQ, ""),
             Diagnostic(ERRID.ERR_ExpectedExpression, "")
        )
    End Sub

    <Fact>
    Public Sub ParseMidIdentifier3()
        ParseAndVerify(<![CDATA[
            Class Mid
                Public Sub Mid(p as Integer)
                    Mid(23, 24,) 
                    [Mid](23)
                End Sub
            End Class
        ]]>, Diagnostic(ERRID.ERR_ExpectedExpression, ""),
             Diagnostic(ERRID.ERR_ExpectedEQ, ""),
             Diagnostic(ERRID.ERR_ExpectedExpression, "")
        )
    End Sub

    <Fact>
    Public Sub ParseMidIdentifier4()
        ParseAndVerify(<![CDATA[
            Class Mid
                Public Sub Mid(p as Integer)
                    Mid(23, 24, 
                End Sub
            End Class
        ]]>, Diagnostic(ERRID.ERR_ExpectedExpression, ""),
             Diagnostic(ERRID.ERR_ExpectedRparen, ""),
             Diagnostic(ERRID.ERR_ExpectedEQ, ""),
             Diagnostic(ERRID.ERR_ExpectedExpression, "")
        )
    End Sub

    <WorkItem(872018, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLineIfThenElse()
        ParseAndVerify(<![CDATA[
            Class Class1
                Sub Method1()
                    Dim IsSA As Short
                    If True Then IsSA = True Else IsSA = False
                End Sub
            End Class
        ]]>)
    End Sub

    <WorkItem(872030, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseRedimClauses()
        ParseAndVerify(<![CDATA[
            Module Module1
                Function FUN1%()
                    Dim C21#(), C22#(,), C23#()
                    ReDim C21#(1), C22#(2, 1), C23#(2)
                End Function
            End Module
        ]]>)
    End Sub

    <WorkItem(872034, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseForNext()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    For i = 0 To 1
                        For j = 0 To 1
                    Next j, i
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(872042, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseWhen()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Try
                    Catch e As InvalidCastException When (Bln = True)
                    End Try
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(873525, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseAndAlsoInOrElseArgumentList()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Foo(ByVal x As Boolean)
                End Sub
                Function Bar() As Boolean
                End Function

                Sub Main()
                    Foo(True AndAlso Bar())
                End Sub
            End Module
        ]]>)

        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Foo(ByVal x As Boolean)
                End Sub
                Function Bar() As Boolean
                End Function

                Sub Main()
                    Foo(False OrElse Bar())
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(873526, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLineIfThenWithFollowingStatement()
        ParseAndVerify(<![CDATA[
        Class c1
            Sub foo()
                If True Then foo()
                bar()
            End Sub
        End Class
        ]]>)
    End Sub

    <WorkItem(874045, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseEnd()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    End
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(874054, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseForEachControlVariableArray()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim files()() As String = {New String() {"Template0.bmp", "Template0.txt"}}

                    For Each fs() As String In files
                    Next fs
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(874067, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseQuerySelectWithInitializerFollowedByFrom()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim q = From i In {} Select p = New Object From j In {} Select j
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(874074, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseExtensionMethodInvokeOnLiteral()
        ParseAndVerify(<![CDATA[
            <System.Runtime.CompilerServices.Extension()>
            Module Module1
                Sub Main()
                    Dim r = 42.IntegerExtension()
                    Dim r = 42.BAD
                End Sub

                <System.Runtime.CompilerServices.Extension()>
                Function IntegerExtension(i As Integer) As Boolean
                    Return True
                End Function
            End Module
        ]]>)
    End Sub

    <WorkItem(874120, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseDoUntilNested()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Do Until True
                        Do Until True
                        Loop
                    Loop
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(874355, "DevDiv/Personal")>
    <Fact>
    Public Sub BC31151ERR_MissingXmlEndTag_ParseLessThan()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim a = ( < 13)
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30198"/>
            <error id="30636"/>
            <error id="31165"/>
            <error id="30035"/>
            <error id="31169"/>
            <error id="31177"/>
            <error id="31151"/>
        </errors>)
    End Sub

    <WorkItem(875188, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseSelectCaseClauses()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main() 
                    Dim bytevar                                        
                    Select Case bytevar                                                                                                                                                                
                        Case Is < 0, Is > 255                                           
                    End Select                                           
                End Sub
            End Module    
        ]]>)
    End Sub

    <WorkItem(875194, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLineContinuationWithTrailingWhitespace()
        ParseAndVerify(
            "Class Class1" & vbCrLf &
            "    <Obsolete(True)> _ " & vbTab & vbCrLf &
            "    Sub AnachronisticMethod()" & vbCrLf &
            "    End Sub" & vbCrLf &
            "End Class")
    End Sub

    <WorkItem(879296, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseRightShiftEquals()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim i = &H1000
                    i >>= 1
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(879373, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaFollowedByColon()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = Function() 3 :
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(879385, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseQueryGroupByIntoWithXmlLiteral()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim col1 As IQueryable = Nothing
                    Dim q3 = From i In col1 Group el1.<moo>.@attr1, el1.<moo> By el1...<moo> Into G=Group
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(879690, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseThrow()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Throw
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(536076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536076")>
    <Fact>
    Public Sub ParseQueryFromNullableRangeVariable()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim col As New Collections.ArrayList()
                    Dim w3 = From i? As Integer In col
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(880312, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseEmptyMultilineFunctionLambda()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim f = Function()
                            End Function
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseEmptyMultilineSubLambda()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    dim s = Sub()
                            End Sub
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881451, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseQueryIntoAllImplicitLineContinuation()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim i10 = Aggregate el In coll1 Into All(
                        el <= 5
                        )
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881528, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMethodInvocationNestedImplicitLineContinuation()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Foo(
                        GetType(
                            Integer
                        )
                    )
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881570, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaInvokeDeclaration()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = Function()
                                Return 42
                            End Function()
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881585, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaBodyWithLineIfForEach()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim m1, list2() As Integer
                    Dim x = Sub() If True Then For Each i In list2 : m1 = i : Exit Sub : Exit For : Next
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881590, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaDeclarationCommaSeparated()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim s1 = Sub()
                             End Sub, s2 = Sub() End
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881597, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseQueryCollectionExpressionContainsLambda()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x2 = From f In {Sub() Console.WriteLine("Hello")}
                             Select f
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(531540, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531540")>
    <Fact>
    Public Sub SelectCaseInLambda()
        ParseAndVerify(<![CDATA[
Module Program
    Sub Main(args As String())
 
        Dim l = Function(m) Function(m3)
                                Dim num = 10
                                Select Case num
 
                                    Case Is = 10
                                        Console.WriteLine("10")
                                End Select
                End Function
    End Sub
End Module
        ]]>)
    End Sub

    <WorkItem(530633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530633")>
    <Fact>
    Public Sub SubImplements()
        ParseAndVerify(<![CDATA[
Interface I
    Property A As Action
End Interface
 
Class C
    Implements I
    Property A As Action = Sub() Implements I.A
End Class
        ]]>,
        <errors>
            <error id="30024" message="Statement is not valid inside a method." start="112" end="126"/>
        </errors>)
    End Sub

    <WorkItem(881603, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseTernaryIfReturningLambda()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x2 = If(True, Nothing, Sub()
                                                   Console.WriteLine("Hi")
                                               End Sub)
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881606, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaMethodArgument()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Foo(a As Action)
                End Sub
                Sub Main()
                    Foo(Sub()
                            Environment.ExitCode = 42
                        End Sub)
                End Sub
            End Module
        ]]>)

        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Foo(a As Action)
                End Sub
                Sub Main()
                    Foo(Sub(c) Console.WriteLine(c))
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881614, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLineIfThenElseIfElse()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = 0
                    If True Then x = 1 Else If False Then x = 2 Else x = 3
                End Sub
            End Module
       ]]>)
    End Sub

    <WorkItem(881640, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseBinaryIfReturningLambdaArray()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim e1 = If({Sub()
                                 End Sub}, {})
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881826, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaDeclareMultipleCommaSeparated()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim m = Sub(ByRef x As String, _
                            y As Integer) Console.WriteLine(x & y), k = Sub(y, _
                                                                                x) m(y, x), _
                                                                                l = Sub(x) _
                                                                                Console.WriteLine(x)     
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(881827, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseGenericNoTypeArgsImplicitLineContinuation()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = GetType(Action(Of
                            )
                            )     
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(882391, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseRightShiftLineContinuation()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = 4 >> _
                            1     
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(882801, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseObjectInitializerParenthesizedLambda()
        ParseAndVerify(<![CDATA[
            Class Class1
                Sub Test()
                    Dim e = New With {.f = New Action(Of Integer)(Sub() If True Then Stop)}               
                    Dim g = 3
                End Sub
            End Class
        ]]>)
    End Sub

    <WorkItem(883286, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaSingleLineWithFollowingStatements()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = Sub() Main()
                    Main()
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(882934, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaSingleLineIfInsideIfBlock()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    If True Then
                        Dim x = 0
                        Dim d = Sub() If True Then x = 1 Else x = 2
                    End If
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseLambdaSingleLineIfWithColon()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() If True Then :
    End Sub
End Module
]]>,
            <errors>
                <error id="30081" message="'If' must end with a matching 'End If'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() If True Then : End If
    End Sub
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
    End Sub

    <WorkItem(546693, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546693")>
    <Fact()>
    Public Sub ParseLambdaSingleLineIfWithColonElseIfInsideIfBlock_1()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then
           Dim x = Sub() If True Then Return : ElseIf
        Else
        End If
    End Sub
End Module
        ]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
    End Sub

    <WorkItem(546693, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546693")>
    <Fact()>
    Public Sub ParseLambdaSingleLineIfWithColonElseIfInsideIfBlock_2()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then
            Dim x = Sub() If True Then Return : ElseIf False Then
        End If
    End Sub
End Module
        ]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub ParseLambdaSingleLineIfWithColonElseInsideIfBlock()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then
            Dim x = Sub() If True Then Return : Else
        End If
    End Sub
End Module
        ]]>)
    End Sub

    <Fact()>
    Public Sub ParseLambdaSingleLineIfWithColonElseSpaceIfInsideIfBlock()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then
            Dim x = Sub() If True Then Return : Else If False Then Return
        End If
    End Sub
End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub ParseLambdaSingleLineIfWithStatementColon()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() If True Then M() :
    End Sub
End Module
]]>)
    End Sub

    <Fact>
    Public Sub ParseLambdaSingleLineIfWithStatementColonElse()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() If True Then M() : Else
    End Sub
End Module
]]>)
    End Sub

    <WorkItem(530940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530940")>
    <Fact()>
    Public Sub ParseSingleLineIfColon()
        ParseAndVerify(<![CDATA[
Module Program
    Sub M()
        If True Then Return : Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module Program
    Sub M()
        If True Then Return : Return Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module Program
    Sub M()
        If True Then M() : M() Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module Program
    Sub M()
        If True Then Return : _
        Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module Program
    Sub M()
        If True Then Return : _
        Return Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module Program
    Sub M()
        If True Then M() : _
        M() Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module Program
    Sub M()
        Return Else
    End Sub
End Module
]]>,
            <errors>
                <error id="30205"/>
            </errors>)
    End Sub

    <WorkItem(601004, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/601004")>
    <Fact()>
    Public Sub ParseSingleLineIfEmptyElse()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return Else
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Return Else
    End Sub
End Module
]]>)
    End Sub

    ''' <summary>
    ''' EmptyStatement following colon.
    ''' </summary>
    <WorkItem(530966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530966")>
    <Fact()>
    Public Sub ParseEmptyStatementFollowingColon()
        Dim tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        M() : 
        If True Then Else M1() : 
        If True Then Else : M2() 
        If True Then Else : 
10:
20::
30: M3()
L4: M4():
L5: : M5()
    End Sub
End Module
]]>)
        Dim root = tree.GetRoot()
        ' If/Else statement lists should not contain EmptyToken.
        Dim tokens = root.DescendantTokens().Select(Function(t) t.Kind).ToArray()
        CheckArray(tokens,
            SyntaxKind.ModuleKeyword,
            SyntaxKind.IdentifierToken,
            SyntaxKind.SubKeyword,
            SyntaxKind.IdentifierToken,
            SyntaxKind.OpenParenToken,
            SyntaxKind.CloseParenToken,
            SyntaxKind.IdentifierToken,
            SyntaxKind.OpenParenToken,
            SyntaxKind.CloseParenToken,
            SyntaxKind.IfKeyword,
            SyntaxKind.TrueKeyword,
            SyntaxKind.ThenKeyword,
            SyntaxKind.ElseKeyword,
            SyntaxKind.IdentifierToken,
            SyntaxKind.OpenParenToken,
            SyntaxKind.CloseParenToken,
            SyntaxKind.EmptyToken,
            SyntaxKind.IfKeyword,
            SyntaxKind.TrueKeyword,
            SyntaxKind.ThenKeyword,
            SyntaxKind.ElseKeyword,
            SyntaxKind.IdentifierToken,
            SyntaxKind.OpenParenToken,
            SyntaxKind.CloseParenToken,
            SyntaxKind.IfKeyword,
            SyntaxKind.TrueKeyword,
            SyntaxKind.ThenKeyword,
            SyntaxKind.ElseKeyword,
            SyntaxKind.IntegerLiteralToken,
            SyntaxKind.ColonToken,
            SyntaxKind.IntegerLiteralToken,
            SyntaxKind.ColonToken,
            SyntaxKind.IntegerLiteralToken,
            SyntaxKind.ColonToken,
            SyntaxKind.IdentifierToken,
            SyntaxKind.OpenParenToken,
            SyntaxKind.CloseParenToken,
            SyntaxKind.IdentifierToken,
            SyntaxKind.ColonToken,
            SyntaxKind.IdentifierToken,
            SyntaxKind.OpenParenToken,
            SyntaxKind.CloseParenToken,
            SyntaxKind.IdentifierToken,
            SyntaxKind.ColonToken,
            SyntaxKind.IdentifierToken,
            SyntaxKind.OpenParenToken,
            SyntaxKind.CloseParenToken,
            SyntaxKind.EndKeyword,
            SyntaxKind.SubKeyword,
            SyntaxKind.EndKeyword,
            SyntaxKind.ModuleKeyword,
            SyntaxKind.EndOfFileToken)
    End Sub

    <WorkItem(531486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531486")>
    <Fact()>
    Public Sub ParseSingleLineIfElse()
        ParseAndVerify(<![CDATA[
Module Program
    Sub Main()
        Dim x = Sub() If True Then ElseIf False Then
    End Sub
End Module
]]>,
<errors>
    <error id="30205" message="End of statement expected." start="66" end="82"/>
</errors>)
    End Sub

    <WorkItem(546910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546910")>
    <Fact()>
    Public Sub ParseMultiLineIfLambdaWithStatementColonElse()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then
            Dim x = Sub() M() : Else
        End If
    End Sub
End Module
]]>,
<errors>
    <error id="36918"/>
</errors>)
    End Sub

    <WorkItem(546910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546910")>
    <Fact()>
    Public Sub ParseSingleLineIfLambdaWithStatementColonElse()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Dim x = Sub() M() : Else
    End Sub
End Module
]]>,
<errors>
    <error id="36918"/>
</errors>)
    End Sub

    <WorkItem(882943, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaExitFunction()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim f = Function()
                                Exit Function
                            End Function
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(883063, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaSingleLineIfInPreprocessorIf()
        ParseAndVerify(<![CDATA[
            Class Class1
            #If True Then
                Dim x = 0
                Dim y = Sub() If True Then x = 1 : x = 2
            #End If
            End Class
        ]]>)
    End Sub

    <WorkItem(883204, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseQueryLetLineContinuation()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x11 = From i In (<e e="v"></e>.<e>) Let j = i.@e _
                              Select j
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(883646, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaCallsEndInArrayInitializer()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()      
                    Dim f = {Sub() End}
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(883726, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLambdaNestedCall()
        ParseAndVerify(<![CDATA[
            Class Class1
                Function Foo()
                    Dim x = Sub() Call Sub()
                                           Console.WriteLine("hi")
                                       End Sub
                    Return True
                End Function
            End Class
        ]]>)
    End Sub

    <WorkItem(895166, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseCommentWithDoubleTicks()
        ParseAndVerify(<![CDATA[
               ''string
            ]]>)
    End Sub

    'Parse a nested line if with an else clause.  Else should associate with nearest if.
    <WorkItem(895059, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseLineIfWithNestedLineIfAndElse()
        ParseAndVerify(<![CDATA[
        Class C
            Sub s
                If o1 Then If o2 Then X = 1 Else X = 2 
            End Sub
        End Class
]]>)
    End Sub

    <Fact>
    Public Sub ParseOneLineStatement()
        Dim str = " Dim x = 3 "

        Dim statement = SyntaxFactory.ParseExecutableStatement(str)
        Assert.Equal(False, statement.ContainsDiagnostics)
        Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind)
    End Sub

    <Fact>
    Public Sub ParseEndSubStatement()
        Dim str = "End Sub "

        Dim statement = SyntaxFactory.ParseExecutableStatement(str)
        Assert.Equal(True, statement.ContainsDiagnostics)
        Assert.Equal(SyntaxKind.EndSubStatement, statement.Kind)
    End Sub

    <Fact>
    Public Sub ParseEndClassStatement()
        Dim str = "End Class "

        Dim statement = SyntaxFactory.ParseExecutableStatement(str)
        Assert.Equal(True, statement.ContainsDiagnostics)
        Assert.Equal(SyntaxKind.EndClassStatement, statement.Kind)
    End Sub

    <Fact>
    Public Sub ParseEmptyStatement()
        Dim str = ""

        Dim statement = SyntaxFactory.ParseExecutableStatement(str)
        Assert.Equal(False, statement.ContainsDiagnostics)
        Assert.Equal(SyntaxKind.EmptyStatement, statement.Kind)

        str = "     "

        statement = SyntaxFactory.ParseExecutableStatement(str)
        Assert.Equal(False, statement.ContainsDiagnostics)
        Assert.Equal(SyntaxKind.EmptyStatement, statement.Kind)


        str = "     " & vbCrLf & vbCrLf

        statement = SyntaxFactory.ParseExecutableStatement(str)
        Assert.Equal(False, statement.ContainsDiagnostics)
        Assert.Equal(SyntaxKind.EmptyStatement, statement.Kind)
    End Sub

    <Fact>
    Public Sub ParseMultiLineStatement()
        Dim str =
            <Q>
                For i as integer = 1 to 10
                    While true
                    End While
                Next
            </Q>.Value

        Dim statement = SyntaxFactory.ParseExecutableStatement(str)
        Assert.Equal(False, statement.ContainsDiagnostics)
        Assert.Equal(SyntaxKind.ForBlock, statement.Kind)
    End Sub

    <Fact>
    Public Sub Parse2MultiLineStatement01()
        Dim str =
            <Q>
                For i as integer = 1 to 10
                    While true
                    End While
                Next
                While true
                End While
            </Q>.Value

        Dim statement = SyntaxFactory.ParseExecutableStatement(str)
        Assert.Equal(True, statement.ContainsDiagnostics)
        Assert.Equal(SyntaxKind.ForBlock, statement.Kind)
    End Sub

    <Fact>
    Public Sub Parse2MultiLineStatement02()
        Dim str =
            <Q>
                For i as integer = 1 to 10
                    While true
                    End While
                Next
                While true
                End While
            </Q>.Value

        Dim statement = SyntaxFactory.ParseExecutableStatement(str, consumeFullText:=False)
        Assert.Equal(False, statement.ContainsDiagnostics)
        Assert.Equal(SyntaxKind.ForBlock, statement.Kind)
    End Sub

    <Fact>
    Public Sub ParseBadMultiLineStatement()
        Dim str =
            <Q>
                For i as integer = 1 to 10
                    While true
                    End Sub
                Next
                While true
                End While
            </Q>.Value

        Dim statement = SyntaxFactory.ParseExecutableStatement(str)
        Assert.Equal(True, statement.ContainsDiagnostics)
        Assert.Equal(SyntaxKind.ForBlock, statement.Kind)
    End Sub

    <WorkItem(537169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537169")>
    <Fact>
    Public Sub ParseGettypeNextString()
        ParseAndVerify(<![CDATA[Next.foo(GetType(Func(Of A))), "")]]>,
            Diagnostic(ERRID.ERR_NextNoMatchingFor, "Next.foo(GetType(Func(Of A))), """""),
            Diagnostic(ERRID.ERR_ExtraNextVariable, ".foo(GetType(Func(Of A)))"))
    End Sub

    <WorkItem(538515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538515")>
    <Fact>
    Public Sub IncParseAGPower17()
        Dim code As String = (<![CDATA[
Namespace AGPower17
    Friend Module AGPower17mod
        Sub AGPower17()
                Scen242358()
                Scen242359()
        End Sub
        Public Sub Scen242200()
            Dim varLeftUInt16242200 As UInt16 = 126US
            Dim varRightByteEnumAlt242200 As ByteEnumAlt = ByteEnumAlt.e1
            Dim varObjLeftUInt16242200 As Object = 126US
            Dim varObjRightByteEnumAlt242200 As Object = ByteEnumAlt.e1
        End Sub

        Public sub Scen242358()
                dim varLeftUInt16242358 as UInt16 = 127US
                dim varRightUlongEnumAlt242358 as UlongEnumAlt = ULongEnumAlt.e2
            Dim varObjLeftUInt16242358 As Object = 127US
                dim varObjRightUlongEnumAlt242358 as object = ULongEnumAlt.e2
        End Sub

        Public sub Scen242359()
                dim varLeftUInt16242359 as UInt16 = 127US
                dim varRightUlongEnumAlt242359 as UlongEnumAlt = ULongEnumAlt.e3
            Dim varObjLeftUInt16242359 As Object = 127US
                dim varObjRightUlongEnumAlt242359 as object = ULongEnumAlt.e3
        End Sub

        Enum ByteEnumAlt As Byte
            e1
            e2
            e3
        End Enum
        

        Enum UlongEnumAlt As ULong
            e1
            e2
            e3
        End Enum
    End Module
End Namespace

]]>).Value

        ParseAndVerify(code)
    End Sub

    <WorkItem(539055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539055")>
    <Fact>
    Public Sub ParseReturnFollowedByComma()
        ParseAndVerify(<![CDATA[
Module Module1    
    Dim x = Sub() Return, r = 42
End Module
]]>)
    End Sub

    <WorkItem(538443, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538443")>
    <Fact>
    Public Sub ParseMultiIfThenElseOnOneLine()
        ParseAndVerify(<![CDATA[
Imports System
Module M
Sub Main()
If True Then : Else Console.WriteLine() : End If

dim x = sub ()
    If True Then : Else Console.WriteLine() : End If
end sub
End Sub
End Module
]]>)
    End Sub

    <WorkItem(538440, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538440")>
    <Fact>
    Public Sub ParseSingleIfElseTerminatedByColon()
        Dim t = ParseAndVerify(<![CDATA[ 
Imports System
Module M
Sub Main()
If True Then Console.WriteLine(1) Else Console.WriteLine(2) : Console.WriteLine(3)
'Colon after the else terminates the line if
If True Then Console.WriteLine(4) Else : Console.WriteLine(5)
End Sub
End Module
]]>)
        Dim moduleBlock = t.GetRoot().ChildNodesAndTokens()(1)
        Dim mainBlock = moduleBlock.ChildNodesAndTokens()(1)
        Dim if1 = mainBlock.ChildNodesAndTokens()(1)
        Dim if2 = mainBlock.ChildNodesAndTokens()(2)
        Dim wl5 = mainBlock.ChildNodesAndTokens()(3)
        Assert.Equal(5, mainBlock.ChildNodesAndTokens().Count)
        Assert.Equal(SyntaxKind.SingleLineIfStatement, if1.Kind())
        Assert.Equal(SyntaxKind.SingleLineIfStatement, if2.Kind())
        Assert.Equal(SyntaxKind.ExpressionStatement, wl5.Kind())
        Assert.Equal(SyntaxKind.InvocationExpression, DirectCast(wl5.AsNode, ExpressionStatementSyntax).Expression.Kind)
    End Sub

    <WorkItem(4784, "DevDiv_Projects/Roslyn")>
    <Fact>
    Public Sub ParseSingleLineSubFollowedByComma()
        Dim t = ParseAndVerify(<![CDATA[
Imports System
Module M
Sub Main()
    session.Raise(Sub(sess) AddHandler sess.SelectedSignatureChanged, Sub(s, e) Return, New SelectedSignatureChangedEventArgs(Nothing, bestMatch))
End Sub
End Module
]]>)
    End Sub

    <WorkItem(538481, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538481")>
    <Fact>
    Public Sub ParseLineContAtEnd()
        Dim t = ParseAndVerify(<![CDATA[
Module M
End Module
 _]]>)
    End Sub

    <Fact>
    Public Sub ParseHandlerStatements()
        Dim t = ParseAndVerify(<![CDATA[
Imports System
Module M

Sub Main()
    dim  button1 = new Button()
    AddHandler Button1.Click, addressof Button1_Click
    RemoveHandler Button1.Click, addressof Button1_Click
End Sub
End Module
]]>)
        Dim moduleBlock = t.GetRoot().ChildNodesAndTokens()(1)
        Dim mainBlock = moduleBlock.ChildNodesAndTokens()(1)
        Dim ah = mainBlock.ChildNodesAndTokens()(2)
        Dim rh = mainBlock.ChildNodesAndTokens()(3)
        Assert.Equal(ah.Kind(), SyntaxKind.AddHandlerStatement)
        Assert.Equal(rh.Kind(), SyntaxKind.RemoveHandlerStatement)
    End Sub

    <Fact>
    Public Sub Regression5150()

        Dim code = <String>Module Program

  Sub Main()

    If True Then : Dim x = Sub() If True Then Dim y :

    ElseIf True Then

      Console.WriteLine()

    End If

  End Sub

End Module
</String>.Value

        Dim compilation = SyntaxFactory.ParseCompilationUnit(code)

        Assert.False(compilation.ContainsDiagnostics)

        Dim ifBlock =
                CType(CType(CType(compilation.Members(0), TypeBlockSyntax).Members(0), MethodBlockSyntax).Statements(0), MultiLineIfBlockSyntax)

        Assert.Equal(1, ifBlock.ElseIfBlocks.Count)
        Dim statements = ifBlock.ElseIfBlocks(0).Statements
        Assert.Equal(1, statements.Count)
        Assert.IsType(Of ExpressionStatementSyntax)(statements(0))
        Assert.IsType(Of InvocationExpressionSyntax)(DirectCast(statements(0), ExpressionStatementSyntax).Expression)

        Assert.Equal(1, ifBlock.Statements.Count)
        Assert.IsType(Of LocalDeclarationStatementSyntax)(ifBlock.Statements(0))

    End Sub

    <WorkItem(15925, "DevDiv_Projects/Roslyn")>
    <Fact()>
    Public Sub Regression5150WithStaticLocal()

        Dim code = <String>Module Program

  Sub Main()

    If True Then : Static x = Sub() If True Then Static y :

    ElseIf True Then

      Console.WriteLine()

    End If

  End Sub

End Module
</String>.Value

        Dim compilation = SyntaxFactory.ParseCompilationUnit(code)

        Assert.False(compilation.ContainsDiagnostics)

        Dim ifBlock =
                CType(CType(CType(compilation.Members(0), TypeBlockSyntax).Members(0), MethodBlockSyntax).Statements(0), MultiLineIfBlockSyntax)

        Assert.Equal(1, ifBlock.ElseIfBlocks.Count)
        Dim statements = ifBlock.ElseIfBlocks(0).Statements
        Assert.Equal(1, statements.Count)
        Assert.IsType(Of ExpressionStatementSyntax)(statements(0))
        Assert.IsType(Of InvocationExpressionSyntax)(DirectCast(statements(0), ExpressionStatementSyntax).Expression)

        Assert.Equal(1, ifBlock.Statements.Count)
        Assert.IsType(Of LocalDeclarationStatementSyntax)(ifBlock.Statements(0))

    End Sub
    <WorkItem(540669, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540669")>
    <Fact>
    Public Sub SingleLineIfNotTerminateByEmptyStatement()

        ' Verify that "::" in the else of a single line if does not terminate the single line if. 
        ' The else contains a call, empty statement and the second single line if.
        Dim code = <String>
    Module Module1
        Sub Main()
             If True Then Foo(1) Else Foo(2) :: If True Then Foo(3) Else Foo(4) : Foo(5)
        End Sub

        Private Sub Foo(i As Integer)
            Console.WriteLine(i)
        End Sub
    End Module
</String>.Value

        Dim compilation = SyntaxFactory.ParseCompilationUnit(code)

        Assert.False(compilation.ContainsDiagnostics)

        Dim singleLineIf =
                CType(CType(CType(compilation.Members(0), TypeBlockSyntax).Members(0), MethodBlockSyntax).Statements(0), SingleLineIfStatementSyntax)

        Dim statements = singleLineIf.ElseClause.Statements
        Assert.Equal(2, statements.Count)
        Assert.IsType(Of ExpressionStatementSyntax)(statements(0))
        Assert.IsType(Of InvocationExpressionSyntax)(DirectCast(statements(0), ExpressionStatementSyntax).Expression)
        Assert.IsType(Of SingleLineIfStatementSyntax)(singleLineIf.ElseClause.Statements(1))

    End Sub

    <WorkItem(540844, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540844")>
    <Fact>
    Public Sub TestForEachAfterOffset()
        Const prefix As String = "GARBAGE"
        Dim forEachText = <![CDATA['A for each statement
                            for each a in b
                            next
                    ]]>.Value
        Dim leading = SyntaxFactory.ParseLeadingTrivia(prefix + forEachText, offset:=prefix.Length)
        Assert.Equal(3, leading.Count)
        Assert.Equal(SyntaxKind.CommentTrivia, leading(0).Kind)
        Assert.Equal(SyntaxKind.EndOfLineTrivia, leading(1).Kind)
        Assert.Equal(SyntaxKind.WhitespaceTrivia, leading(2).Kind)

        Dim trailing = SyntaxFactory.ParseTrailingTrivia(prefix + forEachText, offset:=prefix.Length)
        Assert.Equal(3, trailing.Count)
        Assert.Equal(SyntaxKind.CommentTrivia, trailing(0).Kind)
        Assert.Equal(SyntaxKind.EndOfLineTrivia, trailing(1).Kind)
        Assert.Equal(SyntaxKind.WhitespaceTrivia, trailing(2).Kind)

        Dim t = SyntaxFactory.ParseToken(prefix + forEachText, offset:=prefix.Length, startStatement:=True)
        Assert.Equal(SyntaxKind.ForKeyword, t.Kind)

        Dim tokens = SyntaxFactory.ParseTokens(prefix + forEachText, offset:=prefix.Length)
        Assert.Equal(9, tokens.Count)
        Assert.Equal(SyntaxKind.NextKeyword, tokens(6).Kind)

        Dim statement = SyntaxFactory.ParseExecutableStatement(prefix + forEachText, offset:=prefix.Length)
        Assert.NotNull(statement)
        Assert.Equal(SyntaxKind.ForEachBlock, statement.Kind)
        Assert.Equal(False, statement.ContainsDiagnostics)

    End Sub

    <WorkItem(543248, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543248")>
    <Fact()>
    Public Sub ParseBadCollectionRangeVariableDeclaration1()

        ParseAndVerify(<![CDATA[
            Imports System
             Class Program
                 Shared Sub Main(args As String())
                     Dim x = From y As Char i, In String.Empty    
                 End Sub
             End Class
                     ]]>, Diagnostic(ERRID.ERR_ExpectedIn, ""),
                          Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "In"),
                          Diagnostic(ERRID.ERR_ExpectedIn, ""))
    End Sub

    <WorkItem(543364, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543364")>
    <Fact()>
    Public Sub ParseLabelAfterElse()

        ParseAndVerify(<![CDATA[
    Imports System
    Module M
        Sub Main()
            If False Then
        Else 100:
            End If
        End Sub
    End Module]]>, Diagnostic(ERRID.ERR_Syntax, "100"))
    End Sub

    <WorkItem(544224, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544224")>
    <Fact()>
    Public Sub ParsePartialSingleLineIfStatement()
        Dim stmt = SyntaxFactory.ParseExecutableStatement("If True")
        Assert.Equal(stmt.Kind, SyntaxKind.MultiLineIfBlock)
        Assert.True(stmt.HasErrors)

        stmt = SyntaxFactory.ParseExecutableStatement("If True then foo()")
        Assert.Equal(stmt.Kind, SyntaxKind.SingleLineIfStatement)
        Assert.False(stmt.HasErrors)
    End Sub
#Region "Error Test"

    <Fact()>
    Public Sub BC30003ERR_MissingNext_ParseOnErrorResume()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub s1()
                        on error resume
                        on error resume next
                    end sub
               End Module
            ]]>,
            <errors>
                <error id="30003"/>
            </errors>)
    End Sub

    <WorkItem(536260, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536260")>
    <Fact()>
    Public Sub BC30012ERR_LbExpectedEndIf()
        ParseAndVerify(<![CDATA[
                      Module Module1
                        #If True Then
                             Dim d = <aoeu>
                                       #End If
                                     </aoeu>
                      End Module
            ]]>,
            <errors>
                <error id="30012"/>
            </errors>)
    End Sub

    <WorkItem(527095, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527095")>
    <Fact()>
    Public Sub BC30016ERR_InvOutsideProc_Label()
        ParseAndVerify(<![CDATA[
            Module Module1
                3
            End Module
            Module Module2
                Foo:
            End Module
        ]]>,
        <errors>
            <error id="30801"/>
            <error id="30016"/>
            <error id="30016"/>
        </errors>)
    End Sub

    <WorkItem(874301, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30024ERR_InvInsideProc_Option()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Option Explicit On
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30024"/>
        </errors>)
    End Sub

    <WorkItem(1531, "DevDiv_Projects/Roslyn")>
    <Fact()>
    Public Sub BC30035ERR_Syntax_ParseErrorPrecededByComment()
        ParseAndVerify(<![CDATA[Module M1
Sub Foo
'this is a 
'long
'comment
(1).ToString
End Sub
End Module]]>,
        <errors>
            <error id="30035" message="Syntax error." start="45" end="46"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30058ERR_ExpectedCase()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        select i
                            dim j = false
                            case 0
                            case 1
                            case 2
                            case else
                        end select
                    end sub
               End Module
            ]]>,
            <errors>
                <error id="30058"/>
            </errors>)
        ' ERRID.ERR_ExpectedCase
    End Sub

    <WorkItem(926761, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30071ERR_CaseElseNoSelect()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Case Else
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30071"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30072ERR_CaseNoSelect()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        case 0
                    end sub
               End Module
            ]]>,
            <errors>
                <error id="30072"/>
            </errors>)
        ' ERRID.ERR_CaseNoSelect
    End Sub

    'Parse a nested line if with a block if (if is followed by eol)
    <Fact()>
    Public Sub BC30081ERR_ExpectedEndIf_ParseNestedLineIfWithBlockIf()
        ParseAndVerify(<![CDATA[
        Class C
            Sub s
                If o1 Then If o2 
            End Sub
        End Class
]]>,
<errors>
    <error id="30081"/>
</errors>)
    End Sub

    <WorkItem(878016, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30084ERR_ExpectedNext_For()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main() 
                    For i = 1 To 10
                End Sub
            End Module 
        ]]>,
        <errors>
            <error id="30084"/>
        </errors>)
    End Sub

    <WorkItem(887521, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30084ERR_ExpectedNext_ParseErrorCatchClosesForBlock()
        ParseAndVerify(<![CDATA[
                Module Module1
                   Sub Foo()
                       Try
                            For u = 0 To 2 Step 1
                                For i = 0 To 2 Step 1
                                Next
                       Catch 
                       Finally
                       End Try
                  End Sub
                End Module
            ]]>,
            <errors>
                <error id="30084"/>
            </errors>)
    End Sub

    <WorkItem(874308, "DevDiv/Personal")>
    <WorkItem(879764, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30086ERR_ElseNoMatchingIf()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Else
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30086"/>
        </errors>)
    End Sub

    <WorkItem(875150, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30089ERR_ObsoleteWhileWend()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    While True
                    Wend
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30809"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30091ERR_LoopNoMatchingDo_ParseDoWithErrors()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()

                    loop

                    do while true
                    loop until true

                    do foo
                    loop

                    do

                    end sub
               End Module
            ]]>,
            <errors>
                <error id="30091"/>
                <error id="30238"/>
                <error id="30035"/>
                <error id="30083"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30095ERR_ExpectedEndSelect()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        if true then
                            select i
                                case 0
                                case 1
                                case 2
                        end if
                    end sub
               End Module
            ]]>,
            <errors>
                <error id="30095"/>
            </errors>)
        ' ERRID.ERR_ExpectedEndSelect
    End Sub

    <WorkItem(877929, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30198ERR_ExpectedRparen_If()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    If(1,2)
                    End If
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30198"/>
        </errors>)
    End Sub

    <WorkItem(884863, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30199ERR_ExpectedLparen_ParseWrongNameUsedForOperatorToBeOverloadedErrors()
        ParseAndVerify(<![CDATA[
                      Class c3
                        'COMPILEERROR: BC30199, "\\"
                         shared Operator /\(ByVal x As c3, ByVal y As c3) As Boolean
                         End Operator
                      End Class
            ]]>,
            <errors>
                <error id="30199"/>
            </errors>)
    End Sub

    <WorkItem(885650, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30203ERR_ExpectedIdentifier()
        ParseAndVerify(<![CDATA[
                      Module Module1
                           Sub Main()
                             With New Object
                                     .
                             End With
                           End Sub
                      End Module
            ]]>,
            <errors>
                <error id="30203"/>
            </errors>)
    End Sub

    'Parse a line if followed by dangling elseif
    <Fact()>
    Public Sub BC30205ERR_ExpectedEOS_ParseLineIfDanglingElseIf()
        ParseAndVerify(<![CDATA[
        class c
            sub foo()
                if true then elseif
            end sub
        end class
]]>,
<errors>
    <error id="30205" message="End of statement expected." start="68" end="74"/>
    <error id="30201" message="Expression expected." start="74" end="74"/>
</errors>)
    End Sub

    <WorkItem(885705, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30205ERR_ExpectedEOS_MismatchExpectedEOSVSSyntax()
        ParseAndVerify(<![CDATA[
                     Class Class1
                        Delegate Sub del()
                        Public Custom Event e As del
                         AddHandler(ByVal value As del) as Integer
                        End AddHandler
                        RemoveHandler(ByVal value As del) as Integer
                        End RemoveHandler
                        RaiseEvent() as Integer
                         End RaiseEvent
                        End Event
                     End Class
            ]]>,
            <errors>
                <error id="30205"/>
                <error id="30205"/>
                <error id="30205"/>
            </errors>)
    End Sub

    <WorkItem(879284, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30239ERR_ExpectedRelational_ParseLeftShift()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x
                    x=4 <>>< 2
                    'Comment
                    x = Class1 << 4
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30201"/>
            <error id="30239"/>
        </errors>)

        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x
                    x=4 <>>< 2
                    'COMPILEERROR: BC30108, "Class1"
                    x = Class1 << 4
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30201"/>
            <error id="30239"/>
        </errors>)
    End Sub

    <WorkItem(875155, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30249ERR_ExpectedEQ_ParseFor()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    for x,y as Integer = 1 to 100
                    Next
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30249"/>
            <error id="30035"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30289ERR_InvInsideEndsProc_ParseSingleLineLambdaWithClass()
        ParseAndVerify(<![CDATA[
        Module M1
            Sub Foo()
                Try
                    Dim x1 = Sub(y) Class C
                End Try
            End Sub
        End Module
]]>,
<errors>
    <error id="30289"/>
</errors>)
    End Sub

    <Fact()>
    Public Sub BC30321ERR_CaseAfterCaseElse()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        select i
                            case 0
                            case 1
                            case 2
                            case else
                            case 3
                        end select
                    end sub
               End Module
            ]]>,
            <errors>
                <error id="30321"/>
            </errors>)
        ' ERRID.ERR_CaseAfterCaseElse
    End Sub

    <Fact()>
    Public Sub BC30379ERR_CatchAfterFinally()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        try
                        catch 
                        finally  
                        catch 
                        end try 
                    end sub
               End Module
            ]]>,
            <errors>
                <error id="30379"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30381ERR_FinallyAfterFinally()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        try
                        catch 
                        finally  
                        finally
                        end try 
                    end sub
               End Module
            ]]>,
            <errors>
                <error id="30381"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30384ERR_ExpectedEndTry()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        try
                        catch 
                        finally    
                    end sub
               End Module
            ]]>,
            <errors>
                <error id="30384"/>
            </errors>)
    End Sub

    <WorkItem(904911, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30384ERR_ExpectedEndTry_ParseNestedIncompleteTryBlocks()
        ParseAndVerify(<![CDATA[Namespace n1
    Module m1
        Public Sub bar()
            Try
                Try

                    'Catch ex As Exception

                    'End Try
                Catch ex As Exception

                End Try
        End Sub
    End Module
End Namespace
]]>,
            <errors>
                <error id="30384"/>
            </errors>)
    End Sub

    <WorkItem(899235, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30429ERR_InvalidEndSub_ParseLambdaWithEndSubInsideTryBlock()
        ParseAndVerify(<![CDATA[
        Module M1
            Sub Foo()
                Try
                    Dim x1 = Sub(y) End Sub
                End Try
            End Sub
        End Module
]]>,
<errors>
    <error id="30429"/>
</errors>)
    End Sub

    <WorkItem(904910, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30370ERR_ExpectedRbrace_CollectionInitializer()
        ParseAndVerify(<![CDATA[Module Module1
    Sub Main()
         Dim b1c = {1"", 2, 3}
    End Sub
End Module]]>,
            <errors>
                <error id="30370"/>
            </errors>)
    End Sub

    <WorkItem(880374, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30670ERR_RedimNoSizes()
        ' NOTE: the test has been changed to check for NO ERROR because error 30670
        '       was moved from parser to initial binding phase
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim Obj()
                    ReDim Obj
                    ReDim Obj()
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact()>
    Public Sub BC30670ERR_RedimNoSizes_02()
        ' NOTE: the test has been changed to only check for error 30205 because error 30670
        '       was moved from parser to initial binding phase
        ParseAndVerify(<![CDATA[
                class c1
                    sub s
                        redim a 1 + 2 'Dev 10 reports 30205
                    end sub
                end class
            ]]>,
            <errors>
                <error id="30205"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30781ERR_ExpectedContinueKind()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        do
                            continue 
                        loop

                        while true
                            continue 
                        end while

                        for i = 0 to 10 
                           continue 
                        next

                    end sub
               End Module
            ]]>,
            <errors>
                <error id="30781"/>
                <error id="30781"/>
                <error id="30781"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub BC30800ERR_ObsoleteArgumentsNeedParens()
        ParseAndVerify(<![CDATA[
                class c1
                    sub s
                        i 1,2
                    end sub
                end class
            ]]>,
            <errors>
                <error id="30800"/>
            </errors>)
    End Sub

    <WorkItem(880397, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30801ERR_ObsoleteLineNumbersAreLabels_LambdaLineTerminator()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim s1a = Function ()  : 1 + x
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30201"/>
            <error id="30035"/>
        </errors>
        )
    End Sub

    <Fact()>
    Public Sub BC30807ERR_ObsoleteLetSetNotNeeded()
        ParseAndVerify(<![CDATA[
                class c1
                    sub s
                        let i = 0
                        set j = i
                    end sub
                end class
            ]]>,
            <errors>
                <error id="30807"/>
                <error id="30807"/>
            </errors>)
    End Sub

    <WorkItem(875171, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30814ERR_ObsoleteGosub()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                   GoSub 1000
            1000:
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30814"/>
        </errors>)
    End Sub

    <WorkItem(873634, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30924ERR_NoConstituentArraySizes_ParseArrayInitializer()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim FixedRankArray As Short()
                    FixedRankArray = New Short() ({1, 2})
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30987"/>
            <error id="32014"/>
        </errors>)
    End Sub

    <WorkItem(885229, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC31135ERR_SpecifiersInvOnEventMethod_AddHandler()
        ParseAndVerify(<![CDATA[
                    Class Cls
                       Friend Delegate Sub EH(ByRef x As Integer)
                       Custom Event e1 As EH
                       MustOverride AddHandler(ByVal value As EH)
                       End AddHandler
                       RemoveHandler(ByVal value As EH)
                       End RemoveHandler
                       RaiseEvent(ByRef x As Integer)
                       End RaiseEvent
                       End Event
                    End Class
            ]]>,
            <errors>
                <error id="31135"/>
            </errors>)
    End Sub

    <WorkItem(885655, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC31395ERR_NoTypecharInLabel()
        ParseAndVerify(<![CDATA[
                      Module Module1
                          Sub Main()
                             Label$:
                          End Sub
                      End Module
            ]]>,
            <errors>
                <error id="31395"/>
            </errors>)
    End Sub

    <WorkItem(917, "DevDiv_Projects/Roslyn")>
    <Fact()>
    Public Sub BC31427ERR_BadCCExpression_ConditionalCompilationExpr()
        ParseAndVerify(<![CDATA[
                   Class Class1
                     #If 1 like Nothing Then
                     #End If
                   End Class
            ]]>,
            <errors>
                <error id="31427"/>
            </errors>)
    End Sub

    <WorkItem(527019, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527019")>
    <Fact()>
    Public Sub BC31427ERR_BadCCExpression_ParseErrorMismatchExpectedEOSVSBadCCExpressionExpected()
        ParseAndVerify(<![CDATA[
                     Class Class1
                       'COMPILEERROR: BC31427, "global"
                        #if global.ns1 then
                        #End If
                     End Class
            ]]>,
            <errors>
                <error id="31427"/>
            </errors>)
    End Sub

    <WorkItem(536271, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536271")>
    <Fact()>
    Public Sub BC32020ERR_ExpectedAssignmentOperator()
        ParseAndVerify(<![CDATA[
                     Module Module1
                        Dim ele As XElement = <e/>
                        Sub Main()
                          ele.@h = From i in New String(){"a", "b","c"} let ele.@h = i
                        End Sub
                     End Module
            ]]>,
            <errors>
                <error id="32020"/>
            </errors>)
    End Sub

    <WorkItem(879334, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC32065ERR_GenericParamsOnInvalidMember_Lambda()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x = Function(Of T)(x As T) x
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="32065"/>
        </errors>)
    End Sub

    <WorkItem(881553, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC32093ERR_OfExpected_ParseGenericTypeInstantiation()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim c1 As New Class1(String)()
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="32093"/>
        </errors>)
    End Sub

    <WorkItem(877226, "DevDiv/Personal")>
    <WorkItem(881641, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC33104ERR_IllegalOperandInIIFCount()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x1 = If()
                    Dim x2 = If(True)
                    Dim x3 = If(True, False, True, False)     
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="33104"/>
            <error id="33104"/>
            <error id="33104"/>
        </errors>)
    End Sub

    <WorkItem(883303, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC33105ERR_IllegalOperandInIIFName_TernaryIf()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim s2_a = If(Expression:=True, truepart:=1, falsepart:=2)
                End Sub
            End Module
        ]]>,
            <errors>
                <error id="33105"/>
                <error id="33105"/>
                <error id="33105"/>
            </errors>)
    End Sub

    <WorkItem(875159, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36005ERR_ElseIfNoMatchingIf()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    If True Then
                    Else
                    elseif
                    End If
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="36005"/>
            <error id="30201"/>
        </errors>)
    End Sub

    <WorkItem(875202, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36607ERR_ExpectedIn_ParseForEachControlVariable()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    For each x() as Integer {1,2,3} in New Integer()() {New Integer(){1,2}
                    Next
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30035"/>
            <error id="36607"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC36008ERR_ExpectedEndUsing()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub Foo()
                        using e0

                    end sub
               End Module
            ]]>,
            <errors>
                <error id="36008"/>
            </errors>)
    End Sub

    <WorkItem(880150, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36620ERR_ExpectedAnd_ParseQueryJoinConditionAndAlso()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim q4 = From i In col1 Join j In col1 On i Equals j AndAlso i Equals j
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="36620"/>
        </errors>)
    End Sub

    <WorkItem(881620, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36620ERR_ExpectedAnd_ParseQueryJoinOnOr()
        ParseAndVerify(<![CDATA[
            Module JoinOnInvalid
                Sub JoinOnInvalid()
                    Dim col1 As IQueryable
                    Dim q3 = From i In col1 Join j In col1 On i Equals j OR i Equals j
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="36620"/>
        </errors>)
    End Sub

    <WorkItem(527028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527028")>
    <Fact()>
    Public Sub BC36631ERR_ExpectedJoin()
        ParseAndVerify(<![CDATA[
                      Class Class1
                         Dim l = From pers In {1, 2}
                          Group 
                          Join pet In {3, 4} On pers Equals pet Into PetList = Group
                      End Class
        ]]>,
        <errors>
            <error id="36605"/>
            <error id="36615"/>
        </errors>)
    End Sub

    <WorkItem(904984, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36668ERR_MultilineLambdasCannotContainOnError()
        ' Note, the BC36668 is now reported during binding.
        ParseAndVerify(<![CDATA[
Module M1
Dim a = Sub()
On Error Resume Next
End Sub
Dim b = Sub() On Error GoTo 1
Dim c = Function()
Resume
End Function
End Module
]]>)
    End Sub

    <WorkItem(880300, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36672ERR_StaticInLambda()
        ' Roslyn reports this error during binding.
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim y = Sub()
                              Static a As Integer = 2
                          End Sub
                End Sub
            End Module
        ]]>)
    End Sub

    <WorkItem(884259, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36673ERR_MultilineLambdaMissingSub()
        ParseAndVerify(<![CDATA[
                Module M1
                     Sub Foo()
                        If True Then
                            'COMPILEERROR : BC36673, "Sub()" 
                            Dim x = Sub()
                        End If
                             'COMPILEERROR : BC30429, "End Sub" 
                      End Sub
                  'COMPILEERROR : BC30289, "End Module" 
                 End Module
            ]]>,
            <errors>
                <error id="36673"/>
            </errors>)
    End Sub

    <WorkItem(885258, "DevDiv/Personal")>
    <WorkItem(888613, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36714ERR_InitializedExpandedProperty()

        ParseAndVerify(<![CDATA[
                    Class Class1
                       Public MustOverride Property Foo() As Date = Now
                    End Class
            ]]>,
    <errors>
        <error id="36714"/>
    </errors>)

        ParseAndVerify(<![CDATA[
                       Public Interface IFPropertyAssign
                          Property Scenario2Array() As Integer() = {1,2,3}
                          Property Scenario2List() As List(Of Integer) = NEW List(Of Integer) FROM {1,2,3}
                       End Interface
            ]]>,
            <errors>
                <error id="36714"/>
                <error id="36714"/>
            </errors>)
    End Sub

    <WorkItem(885375, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseExpectedEndSelect()
        ParseAndVerify(<![CDATA[
                           Class Class1
                              Sub Foo()
                                 Dim q = From x In {1, 2, 3} _
                                         Where True _
                                         'Comment
                                             Select 2
                              End Sub
                           End Class
                ]]>,
            <errors>
                <error id="30095" message="'Select Case' must end with a matching 'End Select'."/>
            </errors>)
    End Sub

    <WorkItem(885379, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseObsoleteArgumentsNeedParensAndArgumentSyntax()
        ParseAndVerify(<![CDATA[
                          Class Class1
                                 Sub Foo()
                                      Dim q = From x In {1, 2} _
                                             'Comment
                                               Where true _
                                                Select 2
                                 End Sub
                           End Class
                ]]>,
            <errors>
                <error id="30800" message="Method arguments must be enclosed in parentheses."/>
                <error id="32017" message="Comma, ')', or a valid expression continuation expected."/>
            </errors>)
    End Sub

    <WorkItem(881643, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36720ERR_CantCombineInitializers()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim x0 = New List(Of Integer) FROM {2,3} with {.capacity=2}

                    Dim a As New C2() From {"Hello World!"} with {.a = "foo"}
                    Dim b As New C2() with {.a = "foo"} From {"Hello World!"}
                    Dim c as C2 = New C2() From {"Hello World!"} with {.a = "foo"}
                    Dim d as c2 = New C2() with {.a = "foo"} From {"Hello World!"}
                End Sub
            End Module
        ]]>, Diagnostic(ERRID.ERR_CantCombineInitializers, "FROM {2,3}"),
             Diagnostic(ERRID.ERR_CantCombineInitializers, "with"),
             Diagnostic(ERRID.ERR_CantCombineInitializers, "From"),
             Diagnostic(ERRID.ERR_CantCombineInitializers, "From {""Hello World!""}"),
             Diagnostic(ERRID.ERR_CantCombineInitializers, "with {.a = ""foo""}"))
    End Sub

    <Fact()>
    Public Sub BC30431ERR_InvalidEndProperty_Bug869732()
        'Tree loses text when declaring a property Let/End Let
        ParseAndVerify(<![CDATA[
            Class Class1
                Property Foo() as Single
                    Let
                    End Let
                    Get 
                    End Get
                    Set 
                    End Set
                End Property
            End Class
]]>,
            Diagnostic(ERRID.ERR_ObsoleteLetSetNotNeeded, "Let"),
            Diagnostic(ERRID.ERR_UnrecognizedEnd, "End"),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "Get"),
            Diagnostic(ERRID.ERR_InvalidEndGet, "End Get"),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "Set"),
            Diagnostic(ERRID.ERR_InvalidEndSet, "End Set"),
            Diagnostic(ERRID.ERR_InvalidEndProperty, "End Property"))
    End Sub

    <WorkItem(536268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536268")>
    <Fact()>
    Public Sub ParseExpectedXmlNameAndIllegalChar()
        ParseAndVerify(<![CDATA[
                     Module Module1
                          Sub Main()
                            Dim l = <
                                    <%= "e"%> />
                          End Sub
                     End Module

            ]]>, Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, vbLf),
                 Diagnostic(ERRID.ERR_IllegalXmlWhiteSpace, "                                    "))
    End Sub

    <WorkItem(536270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536270")>
    <Fact()>
    Public Sub ParseExpectedXmlnsAndExpectedEQAndExpectedXmlName()
        ParseAndVerify(<![CDATA[
                     Imports < xmlns:ns3="foo">
                     Imports <xmlns :ns4="foo">
                     Imports <xmlns: ns5="foo">
                     Imports < xmlns="foo">
                     Module Module1
                        Sub Main()
                        End Sub
                     End Module
            ]]>,
            Diagnostic(ERRID.ERR_ExpectedXmlns, ""),
            Diagnostic(ERRID.ERR_ExpectedGreater, "xmlns"),
            Diagnostic(ERRID.ERR_ExpectedXmlns, ""),
            Diagnostic(ERRID.ERR_ExpectedGreater, "xmlns"),
            Diagnostic(ERRID.ERR_ExpectedXmlName, "ns5"),
            Diagnostic(ERRID.ERR_ExpectedXmlns, ""),
            Diagnostic(ERRID.ERR_ExpectedGreater, "xmlns"))
    End Sub

    <WorkItem(880138, "DevDiv/Personal")>
    <Fact()>
    Public Sub ParseLambda_ERR_ExpectedIdentifier()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    Dim s6 = Function ((x) (x + 1))
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30203"/>
            <error id="30638"/>
            <error id="32014"/>
            <error id="36674"/>
        </errors>)
    End Sub

    <WorkItem(880140, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC32017_ParseObjectMemberInitializer()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    ObjTest!New ClsCustomer With {.ID = 106, .Name = "test 106"}
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30800"/>
            <error id="32017"/>
            <error id="32017"/>
        </errors>)
    End Sub

    <WorkItem(880155, "DevDiv/Personal")>
    <Fact()>
    Public Sub ParseObjectMemberInitializer_ERR_ExpectedEOS()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    dim c3 as customer = new customer with {.x=5 : .y=6}
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30370"/>
            <error id="30205"/>
        </errors>)
    End Sub

    <WorkItem(545166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545166")>
    <WorkItem(894062, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC30287ERR_ExpectedDot_ParseVariant()
        ParseAndVerify(<![CDATA[
               If VarType(a1.GetValue(x)) > Variant 'Dev10/11 report expected '.' but Roslyn allows this. Grammar says its OK. 
]]>,
            Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "If VarType(a1.GetValue(x)) > Variant"),
            Diagnostic(ERRID.ERR_ObsoleteObjectNotVariant, "Variant"))
    End Sub

    <WorkItem(896842, "DevDiv/Personal")>
    <Fact()>
    Public Sub ParseIncompleteWithBlocks()
        ParseAndVerify(<![CDATA[
Dim x3 = Function(y As Long)
                                         With]]>,
                                         <errors>
                                             <error id="30085"/>
                                             <error id="30201"/>
                                             <error id="36674"/>
                                         </errors>)
        ParseAndVerify(<![CDATA[
Dim x3 =Sub()
If]]>,
                                         <errors>
                                             <error id="30081"/>
                                             <error id="30201"/>
                                             <error id="36673"/>
                                         </errors>)
    End Sub

    <WorkItem(897824, "DevDiv/Personal")>
    <Fact()>
    Public Sub ParseExplicitLCs()
        ParseAndVerify(<![CDATA[
Namespace RegressDev10660280
Sub Foo1(ByVal x As Integer, _
ByVal y As Integer _
)
apCompare(2, x, "Value of foo1")
End Sub
Sub Foo2( _
 _

]]>,
<errors>
    <error id="30026"/>
    <error id="30198"/>
    <error id="30203"/>
    <error id="30626"/>
</errors>)
    End Sub

    <WorkItem(888562, "DevDiv/Personal")>
    <Fact()>
    Public Sub ParseMoreErrorExpectedRparen()
        ParseAndVerify(<![CDATA[
                          Module M
                            Sub Test()
                              Dim i As Object
                              i = ctype(new Cust1 with {.x=6} , new cust1 with {.x =3})
                            End Sub
                           Class Cust1
                             Public x As Integer
                           End Class
                          End Module
            ]]>,
            <errors>
                <error id="30200"/>
                <error id="30198"/>
            </errors>)
    End Sub

    <WorkItem(887788, "DevDiv/Personal")>
    <Fact()>
    Public Sub ParseMoreErrorExpectedEOS()
        ParseAndVerify(<![CDATA[
                     Module m1
                      Sub Test()
                         Try
                         Catch ex As Exception When New Object With (5) {.x = 9}
                         End Try
                      End Sub
                     End Module
            ]]>,
            <errors>
                <error id="30987"/>
            </errors>)
    End Sub

    <WorkItem(887790, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC31412ERR_HandlesSyntaxInClass_ExpectedIdentifierAndExpectedDot()
        ParseAndVerify(<![CDATA[
                    Class c1
                      sub test handles new customer with {.x = 5, .y = "6"}
                      End Sub
                    End Class
                    Class customer : End Class
            ]]>,
            <errors>
                <error id="30183"/>
                <error id="30287"/>
                <error id="30203"/>
            </errors>)
    End Sub

    <WorkItem(904917, "DevDiv/Personal")>
    <Fact()>
    Public Sub ParseErrorInTryInSub()
        ParseAndVerify(<![CDATA[Namespace n1
    Module m1
        public sub bar()
            try
                dim j =2
                dim k =4
            public sub foo
        End sub
    End Module
End Namespace
]]>,
            <errors>
                <error id="30289"/>
                <error id="30384"/>
                <error id="30026"/>
            </errors>)
    End Sub

    <WorkItem(924035, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36674ERR_MultilineLambdaMissingFunction_ParseLambdaCustomEvent()
        ParseAndVerify(<![CDATA[
            Structure Scen16
                Dim i = Function()
                Custom Event ev
            End Structure       
        ]]>,
        <errors>
            <error id="36674"/>
            <error id="31122"/>
        </errors>)
    End Sub

    <WorkItem(927100, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC32005ERR_BogusWithinLineIf()
        ParseAndVerify(<![CDATA[
            Module Module1

                Sub Main()
                    For i = 1 To 10
                        If True Then Console.WriteLine() : Next
                End Sub

            End Module
        ]]>,
        <errors>
            <error id="30084"/>
            <error id="32005"/>
        </errors>)
    End Sub

    <WorkItem(539208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539208")>
    <Fact()>
    Public Sub BC32005ERR_BogusWithinLineIf_2()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Main()
                    If True : If False Then Console.WriteLine(1) : End If
                    If True : If False Then Else Console.WriteLine(1) : End If
                End Sub
            End Module
        ]]>, Diagnostic(ERRID.ERR_ExpectedEndIf, "If True"),
             Diagnostic(ERRID.ERR_BogusWithinLineIf, "End If"),
             Diagnostic(ERRID.ERR_ExpectedEndIf, "If True"),
             Diagnostic(ERRID.ERR_BogusWithinLineIf, "End If")
)
    End Sub

    <WorkItem(914635, "DevDiv/Personal")>
    <Fact()>
    Public Sub BC36615ERR_ExpectedInto()
        ParseAndVerify(<![CDATA[
            Module P
                Sub M()
                    Dim q = from x in y group x by
                End Sub
            End Module
        ]]>,
        <errors>
            <error id="30201"/>
            <error id="36615"/>
        </errors>)
    End Sub

    <Fact()>
    Public Sub BC30016ERR_InvOutsideProc()
        ParseAndVerify(<![CDATA[
            Module P
            100:

            Interface i1
            200:
            End Interface

            structure s1
            300:
            end structure

            enum e
            300:
            end enum

            End Module
        ]]>,
        Diagnostic(ERRID.ERR_InvOutsideProc, "100:"),
        Diagnostic(ERRID.ERR_InvOutsideProc, "200:"),
        Diagnostic(ERRID.ERR_InvOutsideProc, "300:"),
        Diagnostic(ERRID.ERR_InvInsideEnum, "300:"))
    End Sub

    <WorkItem(539182, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539182")>
    <Fact()>
    Public Sub ParseEmptyStatementWithBadToken()
        ' There should only be one error reported
        ParseAndVerify(<![CDATA[
            Module P

            Sub main()

            $

            End Sub

            End Module
        ]]>, <errors>
                 <error id="30037" message="Character is not valid." start="59" end="60"/>
             </errors>)
    End Sub

    <WorkItem(539515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539515")>
    <Fact()>
    Public Sub ParseNestedSingleLineIfFollowedByEndIf()
        ' Report error for mismatched END IF.  
        ParseAndVerify(<![CDATA[Module M
        Sub Main()
            If False Then  Else If True Then  Else 
        End If
        End Sub
    End Module]]>,
    <errors>
        <error id="30087" message="'End If' must be preceded by a matching 'If'." start="88" end="94"/>
    </errors>)

    End Sub

    <WorkItem(539515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539515")>
    <Fact()>
    Public Sub ParseSingleLineWithNestedMultiLineIf()
        ' Report error for mismatched END IF.
        ParseAndVerify(<![CDATA[
            Module M
                Sub Main()
                    If False Then  Else If True Then
                    End if
                End Sub
            End Module]]>, <errors>
                               <error id="30081" message="'If' must end with a matching 'End If'." start="89" end="101"/>
                               <error id="30087" message="'End If' must be preceded by a matching 'If'." start="122" end="128"/>
                           </errors>)
    End Sub

    <Fact()>
    Public Sub ParseSingleLineIfThenElseFollowedByIfThen()
        ' This is a single line if-then-else with a multi-line-if-then
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub foo()
                    If False Then Else if true then

                    If False Then Else if true then
                                        end if

                    If False Then Else if true then : end if

                End Sub
         End Module]]>,
         <errors>
             <error id="30081" message="'If' must end with a matching 'End If'." start="93" end="105"/>
             <error id="30081" message="'If' must end with a matching 'End If'." start="146" end="158"/>
             <error id="30087" message="'End If' must be preceded by a matching 'If'." start="199" end="205"/>
         </errors>)
    End Sub

    <Fact()>
    Public Sub ParseSingleLineIfThenFollowedByIfThen()
        ' This is a single line if-then-else with a multi-line-if-then
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub foo()
                    If False Then if true then

                    If False Then if true then
                                        end if

                    If False Then if true then : end if

                End Sub
         End Module]]>, <Errors>
                            <error id="30081" message="'If' must end with a matching 'End If'." start="88" end="100"/>
                            <error id="30081" message="'If' must end with a matching 'End If'." start="136" end="148"/>
                            <error id="30087" message="'End If' must be preceded by a matching 'If'." start="189" end="195"/>
                        </Errors>)
    End Sub

    <Fact()>
    Public Sub ParseSingleLineIfThenElseFollowedByDo()
        ' This is a single line if-then-else with a multi-line-if-then
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub foo()
                    If False Then  Else do 

                    If False Then  Else do 
                                        Loop

                    If False Then  Else Do : Loop
                End Sub
         End Module]]>, <errors>
                            <error id="30083" message="'Do' must end with a matching 'Loop'." start="94" end="96"/>
                            <error id="30083" message="'Do' must end with a matching 'Loop'." start="139" end="141"/>
                            <error id="30091" message="'Loop' must be preceded by a matching 'Do'." start="183" end="187"/>
                        </errors>)
    End Sub

    <Fact()>
    Public Sub ParseSingleLineIfThenFollowedByDo()
        ' This is a single line if-then-else with a multi-line-if-then
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub foo()
                    If False Then do 

                    If False Then do 
                                  Loop

                    If False Then Do : Loop
                End Sub
         End Module]]>,
         <errors>
             <error id="30083" message="'Do' must end with a matching 'Loop'." start="88" end="90"/>
             <error id="30083" message="'Do' must end with a matching 'Loop'." start="127" end="129"/>
             <error id="30091" message="'Loop' must be preceded by a matching 'Do'." start="165" end="169"/>
         </errors>)
    End Sub

    <Fact()>
    Public Sub ParseMultiLineIfMissingThen()
        'this is a "multi line if" missing a "then". "f()" after "true" is error as well as missing "end if
        ParseAndVerify(<![CDATA[
            Module Module1
                sub foo()
                    if true f() 
                    if true then else if true f() is an error
                End Sub
         End Module]]>,
         <errors>
             <error id="30081" message="'If' must end with a matching 'End If'." start="74" end="81"/>
             <error id="30205" message="End of statement expected." start="82" end="83"/>
             <error id="30081" message="'If' must end with a matching 'End If'." start="125" end="132"/>
             <error id="30205" message="End of statement expected." start="133" end="134"/>
         </errors>)
    End Sub

    <Fact()>
    Public Sub VarDeclWithKeywordAsIdentifier()
        ParseAndVerify(<![CDATA[
            Class C1
                ' the following usages of Dim/Const do not report parser diagnostics, they will
                ' be reported while binding.
                Dim Sub S1()
                End Sub

                Const Function F1() as Integer
                    return 23
                End Function

                Dim Property P1() as Integer

                Dim Public Shared Operator +(m1 as C1, m2 as C1)
                  return nothing
                End Operator

                ' keyword is not an identifier, outside of method body
                Public Property Namespace As String
                Public Property Class As String
                Const End As String

                Sub Foo()
                    ' keyword is not an identifier, inside of method body
                    Dim Namespace as integer
                    Dim Class
                    Const End
                End Sub

                ' Parser: specifier invalid on this statement
                dim namespace
                end namespace

                ' binding errors, not reported here.
                dim class c2
                end class
            End Class

            ' Parser: specifier invalid on this statement
            dim namespace
            end namespace

            ' binding errors, not reported here.
            dim class c2
            end class
            dim module foo
            end module
        ]]>, Diagnostic(ERRID.ERR_ExpectedEndClass, "Class C1").WithLocation(2, 13),
             Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Namespace").WithLocation(19, 33),
             Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Class").WithLocation(20, 33),
             Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "End").WithLocation(21, 23),
             Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Namespace").WithLocation(25, 25),
             Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Class").WithLocation(26, 25),
             Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "End").WithLocation(27, 27),
             Diagnostic(ERRID.ERR_NamespaceNotAtNamespace, "namespace").WithLocation(31, 21),
             Diagnostic(ERRID.ERR_SpecifiersInvalidOnInheritsImplOpt, "dim").WithLocation(31, 17),
             Diagnostic(ERRID.ERR_ExpectedIdentifier, "").WithLocation(31, 30),
             Diagnostic(ERRID.ERR_EndClassNoClass, "End Class").WithLocation(37, 13),
             Diagnostic(ERRID.ERR_SpecifiersInvalidOnInheritsImplOpt, "dim").WithLocation(40, 13),
             Diagnostic(ERRID.ERR_ExpectedIdentifier, "").WithLocation(40, 26))
    End Sub

    <WorkItem(542066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542066")>
    <Fact()>
    Public Sub Bug9035()
        ParseAndVerify(<![CDATA[
            Module Program
                Sub Main()
                    if true then console.writeline() : ' this goes here.

                    ' test this case in the if part of the single line if statement

                    If True Then try : 
                    finally
                    end try

                    If True Then select case true : 
                    end select

                    If True Then using resource As New Object() : 
                    end using

                    If True Then while true : 
                    end while

                    If True Then do while true : 
                    loop

                    If True Then with nothing : 
                    end with

                    If True Then for each x as object in nothing : 
                    next

                    If True Then for x as object = 1 to 12: 
                    next

                    If True Then if true : 
                    end if

                    If True Then synclock new Object() : 
                    end synclock

                    '
                    ' test this case in the else part of the single line if statement

                    If True Then else try : 
                    finally
                    end try

                    If True Then else select case true : 
                    end select

                    If True Then else using resource As New Object() : 
                    end using

                    If True Then else while true : 
                    end while

                    If True Then else do while true : 
                    loop

                    If True Then else with nothing : 
                    end with

                    If True Then else for each x as object in nothing : 
                    next

                    If True Then else for x as object = 1 to 12: 
                    next

                    If True Then else if true : 
                    end if

                    If True Then else synclock new Object() : 
                    end synclock

                    ' make sure multiline statements in a single line if still work when being written in one line.
                    If True Then select case 1 : case else : end select 

                    ' make sure not to break the single line lambdas
                    Dim s1 = Sub() If True Then Console.WriteLine(1) :
                    Dim s2 = Sub() If True Then Console.WriteLine(1) :: Console.WriteLine(1) ::
                    Dim s3 = Sub() If True Then Console.WriteLine(1) :::: Console.WriteLine(1)
                    Dim s4 = (Sub() If True Then Console.WriteLine(1):)
                End Sub
            End Module
        ]]>,
            Diagnostic(ERRID.ERR_ExpectedEndTry, "try"),
            Diagnostic(ERRID.ERR_FinallyNoMatchingTry, "finally"),
            Diagnostic(ERRID.ERR_EndTryNoTry, "end try"),
            Diagnostic(ERRID.ERR_ExpectedEndSelect, "select case true"),
            Diagnostic(ERRID.ERR_ExpectedCase, ""),
            Diagnostic(ERRID.ERR_EndSelectNoSelect, "end select"),
            Diagnostic(ERRID.ERR_ExpectedEndUsing, "using resource As New Object()"),
            Diagnostic(ERRID.ERR_EndUsingWithoutUsing, "end using"),
            Diagnostic(ERRID.ERR_ExpectedEndWhile, "while true"),
            Diagnostic(ERRID.ERR_EndWhileNoWhile, "end while"),
            Diagnostic(ERRID.ERR_ExpectedLoop, "do while true"),
            Diagnostic(ERRID.ERR_LoopNoMatchingDo, "loop"),
            Diagnostic(ERRID.ERR_ExpectedEndWith, "with nothing"),
            Diagnostic(ERRID.ERR_EndWithWithoutWith, "end with"),
            Diagnostic(ERRID.ERR_ExpectedNext, "for each x as object in nothing"),
            Diagnostic(ERRID.ERR_NextNoMatchingFor, "next"),
            Diagnostic(ERRID.ERR_ExpectedNext, "for x as object = 1 to 12"),
            Diagnostic(ERRID.ERR_NextNoMatchingFor, "next"),
            Diagnostic(ERRID.ERR_ExpectedEndIf, "if true"),
            Diagnostic(ERRID.ERR_EndIfNoMatchingIf, "end if"),
            Diagnostic(ERRID.ERR_ExpectedEndSyncLock, "synclock new Object()"),
            Diagnostic(ERRID.ERR_EndSyncLockNoSyncLock, "end synclock"),
            Diagnostic(ERRID.ERR_ExpectedEndTry, "try"),
            Diagnostic(ERRID.ERR_FinallyNoMatchingTry, "finally"),
            Diagnostic(ERRID.ERR_EndTryNoTry, "end try"),
            Diagnostic(ERRID.ERR_ExpectedEndSelect, "select case true"),
            Diagnostic(ERRID.ERR_ExpectedCase, ""),
            Diagnostic(ERRID.ERR_EndSelectNoSelect, "end select"),
            Diagnostic(ERRID.ERR_ExpectedEndUsing, "using resource As New Object()"),
            Diagnostic(ERRID.ERR_EndUsingWithoutUsing, "end using"),
            Diagnostic(ERRID.ERR_ExpectedEndWhile, "while true"),
            Diagnostic(ERRID.ERR_EndWhileNoWhile, "end while"),
            Diagnostic(ERRID.ERR_ExpectedLoop, "do while true"),
            Diagnostic(ERRID.ERR_LoopNoMatchingDo, "loop"),
            Diagnostic(ERRID.ERR_ExpectedEndWith, "with nothing"),
            Diagnostic(ERRID.ERR_EndWithWithoutWith, "end with"),
            Diagnostic(ERRID.ERR_ExpectedNext, "for each x as object in nothing"),
            Diagnostic(ERRID.ERR_NextNoMatchingFor, "next"),
            Diagnostic(ERRID.ERR_ExpectedNext, "for x as object = 1 to 12"),
            Diagnostic(ERRID.ERR_NextNoMatchingFor, "next"),
            Diagnostic(ERRID.ERR_ExpectedEndIf, "if true"),
            Diagnostic(ERRID.ERR_EndIfNoMatchingIf, "end if"),
            Diagnostic(ERRID.ERR_ExpectedEndSyncLock, "synclock new Object()"),
            Diagnostic(ERRID.ERR_EndSyncLockNoSyncLock, "end synclock"))
    End Sub

    <WorkItem(543724, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543724")>
    <Fact()>
    Public Sub ElseIfStatementOutsideMethodBody()
        ParseAndVerify(<![CDATA[
Class c6    
    else if    
End Class
        ]]>,
        Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "else if    "),
        Diagnostic(ERRID.ERR_ExpectedExpression, ""))
    End Sub

    <WorkItem(544495, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544495")>
    <Fact>
    Public Sub CatchFinallyStatementOutsideMethodBody()
        ParseAndVerify(<![CDATA[
    Catch ex As Exception
    Finally
        ]]>, Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Catch ex As Exception"),
        Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Finally"))
    End Sub

    <WorkItem(544519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544519")>
    <Fact>
    Public Sub TryStatementOutsideMethodBody()
        ParseAndVerify(<![CDATA[
Module Program
    Public _Sub Main()
        Try
        End Try
    End Sub
End Module
        ]]>, Diagnostic(ERRID.ERR_ExpectedEOS, "Main"),
    Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Try"),
    Diagnostic(ERRID.ERR_EndTryNoTry, "End Try"),
    Diagnostic(ERRID.ERR_InvalidEndSub, "End Sub"))
    End Sub

#End Region

    <WorkItem(545543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545543")>
    <Fact>
    Public Sub ParseInvalidUseOfBlockWithinSingleLineLambda()
        Dim compilationDef =
<compilation name="LambdaTests_err">
    <file name="a.vb">
Module Program
    Sub Main()
        For i = 1 To 10
         Dim x = Sub() For j = 1 To 10
      Next j, i
    End Sub
End Module
    </file>
</compilation>

        Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)
        compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedNext, "For j = 1 To 10"),
                                       Diagnostic(ERRID.ERR_ExtraNextVariable, "i"),
                                       Diagnostic(ERRID.ERR_NameNotDeclared1, "j").WithArguments("j"))
    End Sub

    <WorkItem(545543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545543")>
    <ConditionalFact(GetType(WindowsOnly))>
    Public Sub ParseValidUseOfBlockWithinMultiLineLambda()
        Dim compilationDef =
<compilation name="LambdaTests_err">
    <file name="a.vb">
Module Program
    Sub Main()
        For i = 1 To 10
            Dim x = Sub()
                    End Sub
            For j = 1 To 10
        Next j, i
    End Sub
End Module
    </file>
</compilation>

        Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
        CompileAndVerify(compilation)

        CompilationUtils.AssertNoErrors(compilation)
        Dim NodeFound = From lambdaItem In compilation.SyntaxTrees(0).GetRoot.DescendantNodes.OfType(Of MultiLineLambdaExpressionSyntax)()
                        Select lambdaItem

        Assert.Equal(1, NodeFound.Count)
    End Sub

    <WorkItem(545543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545543")>
    <ConditionalFact(GetType(WindowsOnly))>
    Public Sub ParseValidUseOfNonBlockWithinSingleLineLambda()
        Dim compilationDef =
<compilation name="LambdaTests_err">
    <file name="a.vb">
Module Program
    Sub Main()
        Dim Item = 0
        For i = 1 To 10
            Dim x = Sub() Item = 1
            For j = 1 To 10
        Next j, i
    End Sub
End Module
    </file>
</compilation>

        'Should be No errors and a single line lambda is in use
        Dim Compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
        CompilationUtils.AssertTheseDiagnostics(Compilation, <expected>
                                                             </expected>)
        CompileAndVerify(Compilation)

        Dim NodeFound1 = From lambdaItem In Compilation.SyntaxTrees(0).GetRoot.DescendantNodes.OfType(Of SingleLineLambdaExpressionSyntax)()
                         Select lambdaItem

        Assert.Equal(1, NodeFound1.Count)
    End Sub

    <WorkItem(545543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545543")>
    <ConditionalFact(GetType(WindowsOnly))>
    Public Sub ParseValidUseOfBlockWithinSingleLineLambda()
        'Subtle Variation with Single line Statement Lambda and a Block Construct
        Dim compilationDef =
<compilation name="LambdaTests_err">
    <file name="a.vb">
Module Program
    Sub Main()
        Dim Item = 0
        For i = 1 To 10
            Dim x = Sub() if true Then For j = 1 To 2 : Next j            
        Next i
    End Sub
End Module
    </file>
</compilation>

        'Should be No errors and a single line lambda is in use
        Dim Compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
        CompilationUtils.AssertTheseDiagnostics(Compilation, <expected>
                                                             </expected>)
        CompileAndVerify(Compilation)

        Dim NodeFound1 = From lambdaItem In Compilation.SyntaxTrees(0).GetRoot.DescendantNodes.OfType(Of SingleLineLambdaExpressionSyntax)()
                         Select lambdaItem

        Assert.Equal(1, NodeFound1.Count)
    End Sub

    <WorkItem(530516, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530516")>
    <Fact()>
    Public Sub Bug530516()
        Dim tree = ParseAndVerify(<![CDATA[
Class C
    Implements I(
End Class
]]>,
            <errors>
                <error id="32093"/>
                <error id="30182"/>
                <error id="30198"/>
            </errors>)
        CheckTokensForIncompleteImplements(tree, ofMissing:=True)
        tree = ParseAndVerify(<![CDATA[
Class C
    Implements IA(Of A()).IB(
End Class
]]>,
            <errors>
                <error id="32093"/>
                <error id="30182"/>
                <error id="30198"/>
            </errors>)
        CheckTokensForIncompleteImplements(tree, ofMissing:=True)
        tree = ParseAndVerify(<![CDATA[
Class C
    Implements I()
End Class
]]>,
            <errors>
                <error id="32093"/>
                <error id="30182"/>
            </errors>)
        CheckTokensForIncompleteImplements(tree, ofMissing:=True)
        tree = ParseAndVerify(<![CDATA[
Class C
    Implements I(Of
End Class
]]>,
            <errors>
                <error id="30182"/>
                <error id="30198"/>
            </errors>)
        CheckTokensForIncompleteImplements(tree, ofMissing:=False)
        tree = ParseAndVerify(<![CDATA[
Class C
    Implements I(Of)
End Class
]]>,
            <errors>
                <error id="30182"/>
            </errors>)
        CheckTokensForIncompleteImplements(tree, ofMissing:=False)
        ' No parse errors.
        tree = ParseAndVerify(<![CDATA[
Class C
    Implements I(Of A(Of B()).C(Of D()).E())
End Class
]]>)
        tree = ParseAndVerify(<![CDATA[
Class C
    Property P Implements I(
End Class
]]>,
            <errors>
                <error id="30287"/>
                <error id="32093"/>
                <error id="30182"/>
                <error id="30198"/>
            </errors>)
        CheckTokensForIncompleteImplements(tree, ofMissing:=True)
        tree = ParseAndVerify(<![CDATA[
Class C
    Property P Implements IA(Of A()).IB(
End Class
]]>,
            <errors>
                <error id="32093"/>
                <error id="30182"/>
                <error id="30198"/>
            </errors>)
        CheckTokensForIncompleteImplements(tree, ofMissing:=True)
        tree = ParseAndVerify(<![CDATA[
Class C
    Property P Implements I().P
End Class
]]>,
            <errors>
                <error id="32093"/>
                <error id="30182"/>
            </errors>)
        CheckTokensForIncompleteImplements(tree, ofMissing:=True)
        tree = ParseAndVerify(<![CDATA[
Class C
    Property P Implements I(Of
End Class
]]>,
            <errors>
                <error id="30287"/>
                <error id="30182"/>
                <error id="30198"/>
            </errors>)
        CheckTokensForIncompleteImplements(tree, ofMissing:=False)
        tree = ParseAndVerify(<![CDATA[
Class C
    Property P Implements I(Of).P
End Class
]]>,
            <errors>
                <error id="30182"/>
            </errors>)
        CheckTokensForIncompleteImplements(tree, ofMissing:=False)
        ' No parse errors.
        tree = ParseAndVerify(<![CDATA[
Class C
    Property P Implements I(Of A(Of B()).C(Of D()).E()).P
End Class
]]>)
    End Sub

    Private Shared Sub CheckTokensForIncompleteImplements(tree As SyntaxTree, ofMissing As Boolean)
        Dim tokens = tree.GetRoot().DescendantTokens().ToArray()

        ' No tokens should be skipped.
        Assert.False(tokens.Any(Function(t) t.HasStructuredTrivia))

        ' Find last '(' token.
        Dim indexOfOpenParen = -1
        For i = 0 To tokens.Length - 1
            If tokens(i).Kind = SyntaxKind.OpenParenToken Then
                indexOfOpenParen = i
            End If
        Next
        Assert.NotEqual(indexOfOpenParen, -1)

        ' Of token may have been synthesized.
        Dim ofToken = tokens(indexOfOpenParen + 1)
        Assert.Equal(ofToken.Kind, SyntaxKind.OfKeyword)
        Assert.Equal(ofToken.IsMissing, ofMissing)

        ' Type identifier must have been synthesized.
        Dim identifierToken = tokens(indexOfOpenParen + 2)
        Assert.Equal(identifierToken.Kind, SyntaxKind.IdentifierToken)
        Assert.True(identifierToken.IsMissing)
    End Sub

    <WorkItem(546688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546688")>
    <Fact()>
    Public Sub Bug16568_If()
        Dim tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Do : Loop
    End Sub
End Module
        ]]>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Do : : Loop
    End Sub
End Module
        ]]>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Do
                     Loop
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
            <error id="30091"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Do Loop
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
            <error id="30035"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Do :
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Do :
                     Loop
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
            <error id="30091"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Do :: 
                     Loop
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
            <error id="30091"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Do : :
                     Loop
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
            <error id="30091"/>
        </errors>)
    End Sub

    <WorkItem(546688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546688")>
    <Fact()>
    Public Sub Bug16568_Else()
        Dim tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Do : Loop
    End Sub
End Module
        ]]>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Do : : Loop
    End Sub
End Module
        ]]>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Do
                     Loop
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
            <error id="30091"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Do Loop
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
            <error id="30035"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Do :
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Do :
                     Loop
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
            <error id="30091"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Do :: 
                     Loop
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
            <error id="30091"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Do : :
                     Loop
    End Sub
End Module
        ]]>,
        <errors>
            <error id="30083"/>
            <error id="30091"/>
        </errors>)
    End Sub

    <WorkItem(546734, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546734")>
    <Fact()>
    Public Sub Bug16688()
        Dim tree = ParseAndVerify(<![CDATA[
Module M
#Const x = 1 :
End Module
        ]]>,
        <errors>
            <error id="30205" message="End of statement expected." start="70" end="77"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
#Const x = 1 :
End Module
        ]]>,
        <errors>
            <error id="30205" message="End of statement expected." start="70" end="77"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
#Const x = 1 : : 
End Module
        ]]>,
        <errors>
            <error id="30205" message="End of statement expected." start="70" end="77"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
#Const x = 1 _
   :
End Module
        ]]>,
        <errors>
            <error id="30205" message="End of statement expected." start="70" end="77"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
#Const x = 1 End Module
End Module
        ]]>,
        <errors>
            <error id="30205" message="End of statement expected." start="70" end="77"/>
        </errors>)
        tree = ParseAndVerify(<![CDATA[
Module M
#Const x = 1 : End Module
End Module
        ]]>,
        <errors>
            <error id="30205" message="End of statement expected." start="70" end="77"/>
        </errors>)
    End Sub

    ''' <summary>
    ''' Trivia up to and including the last colon on the line
    ''' should be associated with the preceding token.
    ''' </summary>
    <Fact()>
    Public Sub ParseTriviaFollowingColon_1()
        Dim tree = ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Call M0 : ' Comment
        If True Then Call M1 :  :  Call M2
    End Sub
End Module
        ]]>.Value.Trim())
        Dim tokens = tree.GetRoot().DescendantTokens().Select(Function(t) t.Node.ToFullString().NormalizeLineEndings()).ToArray()
        CheckArray(tokens,
                   "Module ",
                   "M" & vbCrLf,
                   "    Sub ",
                   "M",
                   "(",
                   ")" & vbCrLf,
                   "        Call ",
                   "M0 :",
                    " ' Comment" & vbCrLf & "        If ",
                    "True ",
                    "Then ",
                    "Call ",
                    "M1 :  :",
                    "  Call ",
                   "M2" & vbCrLf,
                   "    End ",
                   "Sub" & vbCrLf,
                   "End ",
                   "Module",
                   "")
    End Sub

    <Fact()>
    Public Sub ParseTriviaFollowingColon_2()
        Dim tree = ParseAndVerify(<![CDATA[Interface I : : End Interface : : Class A :: Implements I :: End Class]]>)
        Dim tokens = tree.GetRoot().DescendantTokens().Select(Function(t) t.Node.ToFullString()).ToArray()
        CheckArray(tokens,
                     "Interface ",
                     "I : :",
                     " End ",
                     "Interface : :",
                     " Class ",
                     "A ::",
                     " Implements ",
                     "I ::",
                     " End ",
                     "Class",
                     "")
    End Sub

    <Fact()>
    Public Sub ParseTriviaFollowingColon_3()
        Dim tree = ParseAndVerify(<![CDATA[
<Assembly:  B>
Class B
    Inherits System.Attribute
    Sub M()
L1:    Exit Sub
    End Sub
End Class
        ]]>)
        Dim tokens = tree.GetRoot().DescendantTokens().Select(Function(t) t.Node).ToArray()
        Dim token = tokens.First(Function(t) t.GetValueText() = ":")
        Assert.Equal(token.ToFullString(), ":  ")
        token = tokens.First(Function(t) t.GetValueText() = "B")
        Assert.Equal(token.ToFullString(), "B")
        token = tokens.Last(Function(t) t.GetValueText() = "L1")
        Assert.Equal(token.ToFullString(), "L1")
        token = tokens.First(Function(t) t.GetValueText() = "Exit")
        Assert.Equal(token.ToFullString(), "    Exit ")
    End Sub

    <WorkItem(531059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531059")>
    <Fact()>
    Public Sub ParseSingleLineLambdaInSingleLineIf()
        Dim tree = ParseAndVerify(<![CDATA[
Module Program
    Sub Main1()
        If True Then Dim x = Function() Sub() Console.WriteLine : 
Else Return

    End Sub
End Module
                ]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
                <error id="30086" message="'Else' must be preceded by a matching 'If' or 'ElseIf'."/>
                <error id="30205" message="End of statement expected."/>
            </errors>)

        tree = ParseAndVerify(<![CDATA[
Module Program
    Sub Main()
        If True Then Dim x = Function() Sub() Console.WriteLine :: Else Return
    End Sub
End Module
                ]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)

        tree = ParseAndVerify(<![CDATA[
Module Program
    Sub Main()
        If True Then Dim x = Function() Sub() Console.WriteLine : : Else Return
    End Sub
End Module
                ]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement." start="101" end="118"/>
            </errors>)
    End Sub

    <WorkItem(547060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547060")>
    <Fact()>
    Public Sub ParseSingleLineLambdaInSingleLineIf01()
        Dim tree = ParseAndVerify(<![CDATA[
Module Program
    Sub Main()
        If True Then Dim x = Function() Sub() Console.WriteLine : Else Return
    End Sub
End Module
        ]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement." start="77" end="94"/>
            </errors>)

        tree = ParseAndVerify(<![CDATA[
Module Program
    Sub Main()
        If True Then Dim x = Function() Sub() Console.WriteLine Else Return
    End Sub
End Module
        ]]>)
    End Sub

    <WorkItem(578144, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578144")>
    <Fact()>
    Public Sub ParseStatementLambdaOnSingleLineLambdaWithColon()
        Dim tree = ParseAndVerify(<![CDATA[
Module Module1
    Sub Main()
        Dim x = Sub() : Dim a1 = 1 : End Sub
    End Sub
End Module
        ]]>,
            Diagnostic(ERRID.ERR_SubRequiresSingleStatement, "Sub() "),
            Diagnostic(ERRID.ERR_InvalidEndSub, "End Sub"))
    End Sub

    <WorkItem(531086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531086")>
    <Fact()>
    Public Sub Bug17550()
        ParseAndVerify("<!--" + vbCrLf,
            <errors>
                <error id="30035" message="Syntax error." start="0" end="4"/>
            </errors>)
        ParseAndVerify("%>" + vbCrLf,
            <errors>
                <error id="30035" message="Syntax error." start="0" end="2"/>
            </errors>)
        ParseAndVerify("<!-- :",
            <errors>
                <error id="30035" message="Syntax error." start="0" end="4"/>
            </errors>)
        ParseAndVerify("%> :",
            <errors>
                <error id="30035" message="Syntax error." start="0" end="2"/>
            </errors>)
    End Sub

    <WorkItem(531102, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531102")>
    <Fact()>
    Public Sub Bug17574_XmlAttributeAccess()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@:
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p:
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p: 
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p : 
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p::
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p::y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p:: y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p: :
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p: :y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p:'Comment
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p:y
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p:y:
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p: y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p : y
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.
        @p
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@< ' comment
    End Sub
End Module
]]>,
            <errors>
                <error id="31177" start="62" end="63"/>
                <error id="31146" message="XML name expected." start="63" end="63"/>
                <error id="30636" message="'>' expected." start="63" end="63"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<:
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p
    End Sub
End Module
]]>,
            <errors>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p:
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p:'Comment
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p: 
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p : 
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31177"/>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p::
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p::y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p:: y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p: :
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p: :y
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p:y
    End Sub
End Module
]]>,
            <errors>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p: y
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p : y
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31177"/>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<p:y>
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.
        @<p>
    End Sub
End Module
]]>)
    End Sub

    <WorkItem(531102, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531102")>
    <Fact()>
    Public Sub Bug17574_XmlElementAccess()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<:
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<p
    End Sub
End Module
]]>,
            <errors>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<p:
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<p : 
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31177"/>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<p::
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<p::y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<p:: y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<p: :
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<p: :y
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<p:y
    End Sub
End Module
]]>,
            <errors>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.<p> :
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.
        <p>
    End Sub
End Module
]]>)
    End Sub

    <WorkItem(531102, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531102")>
    <Fact()>
    Public Sub Bug17574_XmlDescendantAccess()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...
    End Sub
End Module
]]>,
            <errors>
                <error id="31165" message="Expected beginning '&lt;' for an XML tag."/>
                <error id="31146" message="XML name expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...:
    End Sub
End Module
]]>,
            <errors>
                <error id="31165" message="Expected beginning '&lt;' for an XML tag."/>
                <error id="31146" message="XML name expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<:
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<p
    End Sub
End Module
]]>,
            <errors>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<p:
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<p : 
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31177"/>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<p::
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<p::y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<p:: y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<p: :
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<p: :y
    End Sub
End Module
]]>,
            <errors>
                <error id="31177"/>
                <error id="31146" message="XML name expected."/>
                <error id="30636" message="'>' expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<p:y
    End Sub
End Module
]]>,
            <errors>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...<p> :
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x...
        <p>
    End Sub
End Module
]]>)
    End Sub

    <WorkItem(531102, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531102")>
    <Fact()>
    Public Sub Bug17574_Comment()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p 'comment
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p:a Rem comment
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@Rem 'comment
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@p:Rem Rem comment
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@Rem:a 'comment
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@<Rem:Rem> Rem comment
    End Sub
End Module
]]>)
    End Sub

    <WorkItem(531102, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531102")>
    <Fact()>
    Public Sub Bug17574_Other()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@Return: Return
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="66" end="66"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = x.@xml:a: Return
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim b = <x/>.@xml:
y
    End Sub
End Module
]]>,
            <errors>
                <error id="31146" message="XML name expected." start="48" end="48"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = <a :Return
    End Sub
End Module
]]>,
            <errors>
                <error id="31151" message="Element is missing an end tag." start="66" end="66"/>
                <error id="31177"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
                <error id="31165"/>
                <error id="30636" message="'>' expected." start="66" end="66"/>
            </errors>)
    End Sub

    <WorkItem(531480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531480")>
    <Fact>
    Public Sub ImplicitLineContinuationAfterQuery()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function() From c In ""
                      Distinct
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Function() From c In ""
                      Distinct
        )
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function() From c In ""
        Distinct
        Is Nothing
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then x = From c In ""
                      Distinct : Return
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then x = From c In ""
                      Distinct : Return
End Module
]]>)
        ' Breaking change: Dev11 allows implicit
        ' line continuation after Distinct.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then x = From c In ""
                      Distinct
                      : Return ' Dev11: no error
End Module
]]>,
            <errors>
                <error id="30689" message="Statement cannot appear outside of a method body."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If Nothing Is From c In ""
                      Distinct Then
        End If
    End Sub
End Module
]]>)
        ' Breaking change: Dev11 allows implicit
        ' line continuation after Distinct.
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If Nothing Is From c In ""
                      Distinct
        Then ' Dev11: no error
        End If
    End Sub
End Module
]]>,
            <errors>
                <error id="30035" message="Syntax error."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If Nothing Is From c In "" Order By c _
            Then
        End If
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If Nothing Is From c In "" Order By c
            Then
        End If
    End Sub
End Module
]]>,
            <errors>
                <error id="30035" message="Syntax error."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then x = From c In "" Order By c Ascending _
                      : Return
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then x = From c In "" Order By c Ascending
                      : Return
End Module
]]>,
            <errors>
                <error id="30689" message="Statement cannot appear outside of a method body."/>
            </errors>)
    End Sub

    <WorkItem(531632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531632")>
    <Fact()>
    Public Sub ColonTerminatorFollowingXmlAttributeAccess()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x As Object
        x = <x/>.@a:b:Return
    End Sub
End Module
]]>)
    End Sub

    <WorkItem(547195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547195")>
    <Fact()>
    Public Sub ColonFollowingImplicitContinuation()
        ParseAndVerify(<![CDATA[
Module M
    Function M(x As Object) As Object
        Return x.
        :
    End Function
End Module
]]>,
            <errors>
                <error id="30203" message="Identifier expected." start="46" end="46"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Function M(x As String) As Object
        Return From c In x
        :
    End Function
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Function M(x As String) As Object
        Return From c In
        :
    End Function
End Module
]]>,
            <errors>
                <error id="30201" message="Expression expected." start="46" end="46"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M(x As Object)
        x = 1 +
        :
    End Sub
End Module
]]>,
            <errors>
                <error id="30201" message="Expression expected." start="46" end="46"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub ColonAtEndOfFile()
        ParseAndVerify(<![CDATA[:]]>)
        ParseAndVerify(<![CDATA[:::]]>)
        ParseAndVerify(<![CDATA[: : :]]>)
        ParseAndVerify(<![CDATA[: : : ]]>)
        ParseAndVerify(<![CDATA[Module M :]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
            </errors>)
        ParseAndVerify(<![CDATA[Module M :::]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
            </errors>)
        ParseAndVerify(<![CDATA[Module M : : :]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
            </errors>)
        ParseAndVerify(<![CDATA[Module M : : : ]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
            </errors>)
    End Sub

    <WorkItem(547305, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547305")>
    <Fact()>
    Public Sub Bug547305()
        ParseAndVerify(<![CDATA[
Module M
    Function F() As C(
    ]]>,
            <errors>
                <error id="30625"/>
                <error id="30027"/>
                <error id="30198"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Private F = Function() As C(
    ]]>,
            <errors>
                <error id="30625"/>
                <error id="36674"/>
                <error id="30198"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Private F = Function() As C(Of
    ]]>,
            <errors>
                <error id="30625"/>
                <error id="36674"/>
                <error id="30182"/>
                <error id="30198"/>
            </errors>)
        ' Unexpected tokens in lambda header.
        ParseAndVerify(<![CDATA[
Module M
    Private F = Function() As A B
    ]]>,
            <errors>
                <error id="30625"/>
                <error id="36674"/>
                <error id="30205"/>
            </errors>)
        ' Unexpected tokens in lambda body.
        ParseAndVerify(<![CDATA[
Module M
    Private F = Function()
    End Func
    ]]>,
            <errors>
                <error id="30625"/>
                <error id="36674"/>
                <error id="30678"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub ImplicitContinuationAfterFrom()
        ParseAndVerify(<![CDATA[
Module M
    Function M(x As String) As Object
        Return From
c in x
    End Function
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Function M(x As String) As Object
        Return From

c in x
    End Function
End Module
]]>,
            <errors>
                <error id="30800" message="Method arguments must be enclosed in parentheses." start="70" end="70"/>
                <error id="30201" message="Expression expected." start="70" end="70"/>
            </errors>)
    End Sub

    <WorkItem(552836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552836")>
    <Fact()>
    Public Sub MoreNextVariablesThanBlockContexts()
        ParseAndVerify(<![CDATA[
    For x = 1 to 5
    Next x, y,
]]>,
            <errors>
                <error id="30689"/>
                <error id="30092"/>
                <error id="30201"/>
                <error id="32037"/>
            </errors>)
        ParseAndVerify(<![CDATA[
    For Each x in Nothing
    Next x, y, z
]]>,
            <errors>
                <error id="30689"/>
                <error id="30092"/>
                <error id="32037"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For x = 1 to 5
            For Each y In Nothing
        Next y, x, w, v, u, t, s
    End Sub
End Module
]]>,
            <errors>
                <error id="32037"/>
            </errors>)
    End Sub

    <WorkItem(553962, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553962")>
    <Fact()>
    Public Sub Bug553962()
        ParseAndVerify(<![CDATA[
Module M
    Private F <!--
End Module
]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(String.Format(<![CDATA[
Module M
    Private F {0}!--
End Module
]]>.Value, FULLWIDTH_LESS_THAN_SIGN),
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Private F <!-- :
End Module
]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Private F <?
End Module
]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(String.Format(<![CDATA[
Module M
    Private F {0}?
End Module
]]>.Value, FULLWIDTH_LESS_THAN_SIGN),
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Private F ?>
End Module
]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(String.Format(<![CDATA[
Module M
    Private F {0}>
End Module
]]>.Value, FULLWIDTH_QUESTION_MARK),
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Private F </
End Module
]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(String.Format(<![CDATA[
Module M
    Private F {0}/
End Module
]]>.Value, FULLWIDTH_LESS_THAN_SIGN),
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Private F %>
End Module
]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(String.Format(<![CDATA[
Module M
    Private F {0}>
End Module
]]>.Value, FULLWIDTH_PERCENT_SIGN),
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Private F <!DOCTYPE
End Module
]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(String.Format(<![CDATA[
Module M
    Private F {0}!DOCTYPE
End Module
]]>.Value, FULLWIDTH_LESS_THAN_SIGN),
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<source>
Module M
    Private F &lt;![CDATA[
    End Module
</source>.Value,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(String.Format(<source>
Module M
    Private F {0}![CDATA[
    End Module
</source>.Value, FULLWIDTH_LESS_THAN_SIGN),
            <errors>
                <error id="30034"/>
                <error id="30203" message="Identifier expected."/>
            </errors>)
        ParseAndVerify(<source>
Module M
    Sub M()
        :&lt;![CDATA[
]]&gt;
    End Sub
End Module
</source>.Value,
            <errors>
                <error id="30035"/>
                <error id="30037"/>
                <error id="30037"/>
            </errors>)
        ParseAndVerify(<source>
Module M
        Sub M() _
            Dim x = &lt;![CDATA[
]]&gt;
        End Sub
End Module
</source>.Value,
            <errors>
                <error id="30205" message="End of statement expected."/>
                <error id="30037"/>
                <error id="30037"/>
            </errors>)
    End Sub

    <WorkItem(553962, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553962")>
    <WorkItem(571807, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/571807")>
    <Fact()>
    Public Sub Bug571807()
        ParseAndVerify(<![CDATA[
Module M
    Private F = <% Function()
                End Function() %>
End Module
]]>,
            <errors>
                <error id="31151"/>
                <error id="31169"/>
                <error id="30249" message="'=' expected."/>
                <error id="30035"/>
                <error id="31165"/>
                <error id="30636"/>
                <error id="30430"/>
                <error id="30205"/>
            </errors>)
        ParseAndVerify(String.Format(<![CDATA[
Module M
    Private F = <% Function()
                End Function() {0}>
End Module
]]>.Value, FULLWIDTH_PERCENT_SIGN),
            <errors>
                <error id="31151"/>
                <error id="31169"/>
                <error id="30249" message="'=' expected."/>
                <error id="30035"/>
                <error id="31165"/>
                <error id="30636"/>
                <error id="30430"/>
                <error id="30205"/>
            </errors>)
    End Sub

    <WorkItem(570756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/570756")>
    <Fact()>
    Public Sub FullWidthKeywords()
        Dim source = <![CDATA[
Ｃｌａｓｓ Ｃ
Ｅｎｄ Ｃｌａｓｓ

]]>.Value.ToFullWidth()
        ParseAndVerify(source)
    End Sub

    <WorkItem(588122, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/588122")>
    <WorkItem(587130, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/587130")>
    <Fact()>
    Public Sub FullWidthKeywords001()
        Dim source = <![CDATA[
Imports System.Security
 
<ＡＳＳＥＭＢＬＹ: CLSCompliant(True)>

#Const x = ＣＤＢＬ(0)
 
Module M
    Dim x = ＣＤＢＬ(0)
End Module
]]>.Value.ToFullWidth()
        ParseAndVerify(source)
    End Sub

    <WorkItem(571529, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/571529")>
    <Fact()>
    Public Sub Bug571529()
        ParseAndVerify(<![CDATA[
Module M
    Private F = Sub()
Async:
            M.F()
        End Sub
End Module
]]>)
    End Sub

    <WorkItem(581662, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581662")>
    <Fact()>
    Public Sub BlankLinesFollowingUnderscore()
        ParseAndVerify(<![CDATA[
Imports System.Linq 
Module M
    Dim x = From c In "" _
        _    
        _    
        Take 1 
End Module
]]>)
        ParseAndVerify(<![CDATA[
Imports System.Linq 
Module M
    Dim x = From c In ""
        _    
        _    
Take 1 
End Module
]]>)
        ParseAndVerify(<![CDATA[
Imports System.Linq 
Module M
    Dim x = From c In "" _
             
        _    
        Take 1 
End Module
]]>,
            <errors>
                <error id="30188" message="Declaration expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports System.Linq 
Module M
    Dim x = From c In "" _
        _    
             
        Take 1 
End Module
]]>,
            <errors>
                <error id="30188" message="Declaration expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports System.Linq 
Module M
    Dim x = From c In "" _
        'Comment
        Take 1 
End Module
]]>,
            <errors>
                <error id="30188" message="Declaration expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports System.Linq 
Module M
    Dim x = From c In "" _
        'Comment _
        Take 1 
End Module
]]>,
            <errors>
                <error id="30188" message="Declaration expected."/>
            </errors>)
    End Sub

    <WorkItem(608214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608214")>
    <Fact()>
    Public Sub Bug608214()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then %
    End Sub
End Module
]]>,
            <errors>
                <error id="30037" message="Character is not valid."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Return : %
    End Sub
End Module
]]>,
            <errors>
                <error id="30037" message="Character is not valid."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Return Else %
    End Sub
End Module
]]>,
            <errors>
                <error id="30037" message="Character is not valid."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Return Else Return : %
    End Sub
End Module
]]>,
            <errors>
                <error id="30037" message="Character is not valid."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then 5 Else 5
    End Sub
End Module
]]>,
            <errors>
                <error id="30035" message="Syntax error."/>
                <error id="30035" message="Syntax error."/>
            </errors>)
    End Sub

    <WorkItem(608214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608214")>
    <WorkItem(610345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/610345")>
    <Fact()>
    Public Sub Bug608214_2()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Sub() If True Then Return :
End Module
]]>,
            <errors>
                <error id="31172" message="An embedded expression cannot be used here."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Sub() If True Then Return : REM
End Module
]]>,
            <errors>
                <error id="31172" message="An embedded expression cannot be used here."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Sub() If True Then Return : _
        : %>
End Module
]]>,
            <errors>
                <error id="31172" message="An embedded expression cannot be used here."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Sub() If True Then Return : _
        :
End Module
]]>,
            <errors>
                <error id="31172" message="An embedded expression cannot be used here."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = <%= Sub() If True Then Return Else % :
End Module
]]>,
            <errors>
                <error id="31172" message="An embedded expression cannot be used here."/>
                <error id="30037" message="Character is not valid."/>
                <error id="31159" message="Expected closing '%>' for embedded expression."/>
            </errors>)
    End Sub

    <WorkItem(608225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608225")>
    <Fact()>
    Public Sub Bug608225()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() 
End Module
]]>,
            <errors>
                <error id="36673" message="Multiline lambda expression is missing 'End Sub'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then _
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="32005"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return : _
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="32005"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else Return : _
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="32005"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="32005"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return : End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="32005"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else Return : End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="32005"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return : _
    Sub M()
    End Sub
End Module
]]>,
            <errors>
                <error id="30289"/>
                <error id="30429" message="'End Sub' must be preceded by a matching 'Sub'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return : _
    <A> Dim y As Object
End Module
]]>,
            <errors>
                <error id="30660" message="Attributes cannot be applied to local variables."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then
            Dim x = Sub() If True Then Else Return : _
        Else
        End If
    End Sub
End Module
]]>,
            <errors>
                <error id="30086" message="'Else' must be preceded by a matching 'If' or 'ElseIf'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Try
            Dim x = Sub() If True Then Return : _
        Catch e As System.Exception
        End Try
    End Sub
End Module
]]>,
            <errors>
                <error id="30380" message="'Catch' cannot appear outside a 'Try' statement."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Try
            Dim x = Sub() If True Then Return : _
        Finally
        End Try
    End Sub
End Module
]]>,
            <errors>
                <error id="30382" message="'Finally' cannot appear outside a 'Try' statement."/>
            </errors>)
    End Sub

    ''' <summary>
    ''' Line continuation trivia should include the underscore only.
    ''' </summary>
    <WorkItem(581662, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/581662")>
    <Fact()>
    Public Sub LineContinuationTrivia()
        Dim source = <![CDATA[
Imports System.Linq 
Module M
    Dim x = From c In "" _
        _    
        _	
        Take 1   _ 

End Module
]]>.Value
        ' Source containing underscores and spaces.
        LineContinuationTriviaCore(source, "_")
        ' Source containing underscores and tabs.
        LineContinuationTriviaCore(source.Replace(" "c, vbTab), "_")
        ' Source containing full-width underscores and spaces.
        LineContinuationTriviaErr(source.Replace("_"c, FULLWIDTH_LOW_LINE), "" + FULLWIDTH_LOW_LINE)
    End Sub

    Private Sub LineContinuationTriviaCore(source As String, charAsString As String)
        Dim tree = ParseAndVerify(source)
        Dim tokens = tree.GetRoot().DescendantTokens().Select(Function(t) t.Node).ToArray()
        Dim allTrivia = tree.GetRoot().DescendantTrivia().ToArray()
        For Each trivia In allTrivia
            If trivia.Kind = SyntaxKind.LineContinuationTrivia Then
                Assert.Equal(trivia.Width, 1)
                Assert.Equal(trivia.ToString(), charAsString)
            End If
        Next
    End Sub

    Private Sub LineContinuationTriviaErr(source As String, charAsString As String)
        Dim tree = ParseAndVerify(source,
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, "＿"))

        Dim tokens = tree.GetRoot().DescendantTokens().Select(Function(t) t.Node).ToArray()
        Dim allTrivia = tree.GetRoot().DescendantTrivia().ToArray()
        For Each trivia In allTrivia
            If trivia.Kind = SyntaxKind.LineContinuationTrivia Then
                Assert.Equal(trivia.Width, 1)
                Assert.Equal(trivia.ToString(), charAsString)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Each colon should be a separate trivia node.
    ''' </summary>
    <WorkItem(612584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/612584")>
    <Fact()>
    Public Sub ConsecutiveColonsTrivia()
        Dim source = <![CDATA[
Module M
    ::
    : :
    Sub M()
10:::
20: : :
Label:::
        :: M() :: : M() : ::
        : : Return : :
    End Sub
End Module
]]>.Value
        ConsecutiveColonsTriviaCore(source, ":")
        ConsecutiveColonsTriviaCore(source.Replace(":"c, FULLWIDTH_COLON), FULLWIDTH_COLON_STRING)
    End Sub

    Private Sub ConsecutiveColonsTriviaCore(source As String, singleColon As String)
        Dim tree = ParseAndVerify(source)
        Dim tokens = tree.GetRoot().DescendantTokens().Select(Function(t) t.Node).ToArray()
        Dim allTrivia = tree.GetRoot().DescendantTrivia().ToArray()
        For Each trivia In allTrivia
            If trivia.Kind = SyntaxKind.ColonTrivia Then
                Assert.Equal(trivia.Width, 1)
                Assert.Equal(trivia.ToString(), singleColon)
            End If
        Next
    End Sub

    <Fact()>
    Public Sub CanFollowExpression()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() Return)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() If True Then Return)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() If True Then Else)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() If True Then Else Return)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() If True Then If False Then Else)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() If True Then If False Then Else : Else)
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() Return, y = Nothing
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return, y = Nothing
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else, y = Nothing
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else Return, y = Nothing
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then If False Then Else, y = Nothing
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then If False Then Else : Else, y = Nothing
End Module
]]>)
    End Sub

    <WorkItem(619627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619627")>
    <Fact()>
    Public Sub OuterEndWithinMultiLineLambda()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function() As Object Else End Module]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="36674"/>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        If True Then
        Else Return
        End If
    End Sub
End Module]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        If True Then
        Else End Module]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="36673" message="Multiline lambda expression is missing 'End Sub'."/>
                <error id="30081" message="'If' must end with a matching 'End If'."/>
                <error id="30622"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        Else End Module]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="36673" message="Multiline lambda expression is missing 'End Sub'."/>
                <error id="30086" message="'Else' must be preceded by a matching 'If' or 'ElseIf'."/>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        If True Then : Else End Module]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="36673" message="Multiline lambda expression is missing 'End Sub'."/>
                <error id="30081" message="'If' must end with a matching 'End If'."/>
                <error id="30622"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For Each c in ""
            Dim x = Sub()
                If True Then : Else Next : End If
            End Sub
    End Sub
End Module
]]>,
            <errors>
                <error id="30084"/>
                <error id="30092"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For Each c in ""
            Dim x = Function()
                If True Then : Else Next : End If
            End Function
    End Sub
End Module
]]>,
            <errors>
                <error id="30084"/>
                <error id="30092"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For Each c in ""
            Dim x = Function()
                If True Then If False Then : Else Next : End If
            End Function
    End Sub
End Module
]]>,
            <errors>
                <error id="30084"/>
                <error id="32005"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For Each c in ""
            Dim x = Sub() If True Then : Else Next : End If
    End Sub
End Module
]]>,
            <errors>
                <error id="30084"/>
                <error id="30092"/>
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
                <error id="36673" message="Multiline lambda expression is missing 'End Sub'."/>
                <error id="30383"/>
                <error id="30429"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Try
            Dim x = Function()
                    Finally
                End Function
        End Try
    End Sub
End Module
]]>,
            <errors>
                <error id="36674" message="Multiline lambda expression is missing 'End Function'."/>
                <error id="30430"/>
            </errors>)
    End Sub

    <WorkItem(620546, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/620546")>
    <Fact()>
    Public Sub NestedMultiLineBlocksInSingleLineIf()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else While True : Using Nothing
                                       End Using : End While
    End Sub
End Module
]]>,
            <errors>
                <error id="30082"/>
                <error id="36008"/>
                <error id="36007"/>
                <error id="30090"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub SingleLineSubMultipleStatements()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() While True : End While
    End Sub
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() While True : End While : M()
    End Sub
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() M() : Using Nothing : End Using
    End Sub
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() Using Nothing : M() : End Using
    End Sub
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() Using Nothing : While True : End While : End Using
    End Sub
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub MoreLambdasAndSingleLineIfs()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Dim y = Sub() If False Then Else Return Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Dim y = Sub() If False Then Else Return Else
End Module
]]>)
        ' Dev11 (incorrectly) reports BC30086.
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Dim y = Sub() If False Then Else Else
    End Sub
End Module
]]>)
        ' Dev11 (incorrectly) reports BC30086.
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Dim y = Sub() If False Then Else Else
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Dim y = Sub() If False Then Else Else
    End Sub
End Module
]]>,
            <errors>
                <error id="30086" message="'Else' must be preceded by a matching 'If' or 'ElseIf'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else Dim y = Sub() If False Then Else Else
End Module
]]>,
            <errors>
                <error id="30205" message="End of statement expected."/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub IncompleteSingleLineIfs()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then E
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then End E
    End Sub
End Module
]]>,
            <errors>
                <error id="30678" message="'End' statement not valid."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else E
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else End E
    End Sub
End Module
]]>,
            <errors>
                <error id="30678" message="'End' statement not valid."/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub SelectOrSelectCase()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = From o In Sub() If True Then Return Select o
    End Sub   
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = From o In Sub() If True Then Else Return Select o
    End Sub   
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = From o In Sub() If True Then Else Select o
    End Sub   
End Module
]]>,
            <errors>
                <error id="30095" message="'Select Case' must end with a matching 'End Select'."/>
            </errors>)
    End Sub

    ''' <summary>
    ''' See reference to Dev10#708061 for ambiguity regarding
    ''' "End Select" in single-line lambda.
    ''' </summary>
    <Fact()>
    Public Sub SelectOrEndSelect()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = From o In Sub() Return Select o
    End Sub   
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = From o In Sub() End Select o
    End Sub   
End Module
]]>,
            <errors>
                <error id="30088" message="'End Select' must be preceded by a matching 'Select Case'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = From o In Sub() If True Then End Select o
    End Sub   
End Module
]]>,
            <errors>
                <error id="30088" message="'End Select' must be preceded by a matching 'Select Case'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = From o In Sub() If True Then Else End Select o
    End Sub   
End Module
]]>,
            <errors>
                <error id="30088" message="'End Select' must be preceded by a matching 'Select Case'."/>
            </errors>)
    End Sub

    <WorkItem(622712, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/622712")>
    <Fact()>
    Public Sub ColonTerminatorWithTrailingTrivia()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return : _
                                      :
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Else Return : _
                                           : 'Comment
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() While True : _
                             : End While
End Module
]]>,
            <errors>
                <error id="36918" message="Single-line statement lambdas must include exactly one statement."/>
            </errors>)
    End Sub

    <WorkItem(623023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623023")>
    <Fact()>
    Public Sub SingleLineIfWithinNestedSingleLineBlocks()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function() Sub() If True Then Return End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function() Sub() If True Then Return _
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function() Sub() If True Then Else Return End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function() Sub() If True Then Else Return _
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Dim y = Sub() If True Then Else Return End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then If False Then Dim y = Sub() If True Then If False Then Else Return End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then If False Then Else Dim y = Sub() If True Then If False Then Return _
End Module
]]>,
            <errors>
                <error id="30625" message="'Module' statement must end with a matching 'End Module'."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Function() (Sub() If True Then Else))
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Function() (Sub() If True Then If False Then Else))
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = (Sub() If True Then Else Dim y = (Sub() If True Then Else))
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Dim x = (Sub() If False Then Else) Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Dim x = (Sub() If False Then Else)
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Dim x = (Sub() If False Then If True Then Else If False Then Else) Else
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function() (Sub() If True Then While True : If False Then Else)
End Module
]]>,
            <errors>
                <error id="30082" message="'While' must end with a matching 'End While'."/>
                <error id="30198" message="')' expected."/>
                <error id="30205" message="End of statement expected."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function() Sub()
            Dim y = (Sub() If True Then Else)
        End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function() Sub()
            Dim y = Sub() If True Then Else Return _
        End Sub
End Module
]]>,
            <errors>
                <error id="36673" message="Multiline lambda expression is missing 'End Sub'."/>
                <error id="30201" message="Expression expected."/>
            </errors>)
    End Sub

    ''' <summary>
    ''' Consecutive colons are handled differently by the
    ''' scanner if the colons are on the same line vs.
    ''' separate lines with line continuations.
    ''' </summary>
    <WorkItem(634703, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/634703")>
    <Fact>
    Public Sub MultipleColons()
        CheckMethodStatementsAndTrivia(<![CDATA[
Module M
    Sub M()
        Return : : :
    End Sub
End Module
]]>,
            SyntaxKind.ModuleBlock,
            SyntaxKind.ModuleStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.SubBlock,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.SubStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ReturnStatement,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ColonTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ColonTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ColonTrivia,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.EndSubStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.EndModuleStatement)
        CheckMethodStatementsAndTrivia(<![CDATA[
Module M
    Sub M()
        Return : _
        : _
        :
    End Sub
End Module
]]>,
            SyntaxKind.ModuleBlock,
            SyntaxKind.ModuleStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.SubBlock,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.SubStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ReturnStatement,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ColonTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.LineContinuationTrivia,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ColonTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.LineContinuationTrivia,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ColonTrivia,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.EndSubStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.EndModuleStatement)
        CheckMethodStatementsAndTrivia(<![CDATA[
Module M
    Sub M()
        If True Then Return : : :
    End Sub
End Module
]]>,
            SyntaxKind.ModuleBlock,
            SyntaxKind.ModuleStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.SubBlock,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.SubStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.SingleLineIfStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.ReturnStatement,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ColonTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ColonTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ColonTrivia,
            SyntaxKind.EmptyStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.EndSubStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.EndModuleStatement)
        CheckMethodStatementsAndTrivia(<![CDATA[
Module M
    Sub M()
        If True Then Return : _
        : _
        :
    End Sub
End Module
]]>,
            SyntaxKind.ModuleBlock,
            SyntaxKind.ModuleStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.SubBlock,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.SubStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.SingleLineIfStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.ReturnStatement,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.ColonTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.LineContinuationTrivia,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.EmptyStatement,
            SyntaxKind.ColonTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.LineContinuationTrivia,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.EmptyStatement,
            SyntaxKind.ColonTrivia,
            SyntaxKind.EmptyStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.WhitespaceTrivia,
            SyntaxKind.EndSubStatement,
            SyntaxKind.EndOfLineTrivia,
            SyntaxKind.EndModuleStatement)
    End Sub

    Private Sub CheckMethodStatementsAndTrivia(source As Xml.Linq.XCData, ParamArray expectedStatementsAndTrivia() As SyntaxKind)
        Dim tree = ParseAndVerify(source.Value.Trim())
        Dim actualStatementsAndTrivia = tree.GetRoot().
            DescendantNodesAndSelf().
            Where(Function(n) TypeOf n Is StatementSyntax).
            SelectMany(Function(s) s.GetLeadingTrivia().Select(Function(trivia) trivia.Kind()).Concat({s.Kind()}).Concat(s.GetTrailingTrivia().Select(Function(trivia) trivia.Kind()))).
            ToArray()
        CheckArray(actualStatementsAndTrivia, expectedStatementsAndTrivia)
    End Sub

    ''' <summary>
    ''' Scanner needs to handle comment trivia at the start of a statement,
    ''' even when the statement is not the first on the line.
    ''' </summary>
    <Fact()>
    Public Sub CommentAtStartOfStatementNotFirstOnLine()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return :'Comment
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return :rem
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return : REM Comment
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return : _
        REM Comment
End Module
]]>)
    End Sub

    <WorkItem(638187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638187")>
    <Fact()>
    Public Sub IsNextStatementInsideLambda()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        Return :
    Sub F()
    End Sub
End Module
]]>,
            <errors>
                <error id="36673" message="Multiline lambda expression is missing 'End Sub'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
10:
    Sub F()
    End Sub
End Module
]]>,
            <errors>
                <error id="36673" message="Multiline lambda expression is missing 'End Sub'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
10: Sub F()
    End Sub
End Module
]]>,
            <errors>
                <error id="36673" message="Multiline lambda expression is missing 'End Sub'."/>
                <error id="32009"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
10
    Sub F()
    End Sub
End Module
]]>,
            <errors>
                <error id="30801"/>
                <error id="36673" message="Multiline lambda expression is missing 'End Sub'."/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Return :
    Sub F()
    End Sub
End Module
]]>)
    End Sub

    <Fact()>
    Public Sub IsNextStatementInsideLambda_2()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For i = 0 To 5
            Dim x = Sub()
10: Call M()
            Next
            End Sub
    End Sub
End Module
]]>,
            <errors>
                <error id="36673"/>
                <error id="30429"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For i = 0 To 5
            Dim x = Sub()
10:
            Next
            End Sub
    End Sub
End Module
]]>,
            <errors>
                <error id="36673"/>
                <error id="30429"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For i = 0 To 5
            Dim x = Sub()
10: Next
            End Sub
    End Sub
End Module
]]>,
            <errors>
                <error id="36673"/>
                <error id="30429"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        For i = 0 To 5
            Dim x = Sub()
10 Next
            End Sub
    End Sub
End Module
]]>,
            <errors>
                <error id="30084"/>
                <error id="30801"/>
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
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Try
            Dim x = Sub()
10:
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

    ''' <summary>
    ''' Should parse (and report errors in) a statement
    ''' following a label even if the label is invalid.
    ''' Currently, any statement on the same line as
    ''' the invalid label is ignored.
    ''' </summary>
    <WorkItem(642558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/642558")>
    <Fact()>
    Public Sub ErrorInStatementFollowingInvalidLabel()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
10
        Call
    End Sub
End Module
]]>,
            <errors>
                <error id="30801"/>
                <error id="30201"/>
            </errors>)
        ' Dev11 reports 30801 and 30201.
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
10 Call
    End Sub
End Module
]]>,
            <errors>
                <error id="30801"/>
            </errors>)
    End Sub

    <Fact()>
    Public Sub LabelsFollowedByStatementsWithTrivia()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
10: 'Comment
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
10: _
    _
    :
    End Sub
End Module
]]>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
10'Comment
    End Sub
End Module
]]>,
            <errors>
                <error id="30801"/>
            </errors>)
    End Sub

    <WorkItem(640520, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/640520")>
    <Fact()>
    Public Sub Bug640520()
        ParseAndVerify(<![CDATA[
Class C
    Sub M()
    End : Sub
    Public Custom Event E
    End Event
End Class
]]>,
            <errors>
                <error id="30026"/>
                <error id="30289"/>
                <error id="32009"/>
                <error id="30026"/>
                <error id="30203"/>
                <error id="30289"/>
                <error id="31122"/>
                <error id="31123"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Sub M()
    End : Sub
    <A> Custom Event E
    End Event
End Class
]]>,
            <errors>
                <error id="30026"/>
                <error id="30289"/>
                <error id="32009"/>
                <error id="30026"/>
                <error id="30203"/>
                <error id="30289"/>
                <error id="31122"/>
                <error id="31123"/>
            </errors>)
    End Sub

    <WorkItem(648998, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/648998")>
    <Fact()>
    Public Sub Bug648998()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = F(a:=False,
    Dim y, z = Nothing
End Module
]]>,
            <errors>
                <error id="32017"/>
                <error id="30241"/>
                <error id="30201"/>
                <error id="30241"/>
                <error id="30198"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = F(a:=False,
    Dim y()
End Module
]]>,
            <errors>
                <error id="32017"/>
                <error id="30241"/>
                <error id="30201"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = F(a:=False,
    Dim y
End Module
]]>,
            <errors>
                <error id="32017"/>
                <error id="30241"/>
                <error id="30201"/>
                <error id="30198"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = F(a:=False,
        b True,
        c:=Nothing)
End Module
]]>,
            <errors>
                <error id="32017"/>
                <error id="30241"/>
            </errors>)
    End Sub

    <WorkItem(649162, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649162")>
    <Fact()>
    Public Sub Bug649162()
        ParseAndVerify(<![CDATA[
Imports <xmlns:=''>, Imports <xmlns::=''>, Imports <xmlns==''>
]]>,
            <errors>
                <error id="31146"/>
                <error id="30183"/>
                <error id="30035"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Imports <xmlns:=''>, Imports <xmlns::=''>, Imports <xmlns==''>
]]>.Value.Replace(":"c, FULLWIDTH_COLON).Replace("="c, FULLWIDTH_EQUALS_SIGN),
            <errors>
                <error id="31187"/>
                <error id="30636"/>
                <error id="31170"/>
                <error id="30183"/>
                <error id="30035"/>
            </errors>)
    End Sub

    <WorkItem(650318, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/650318")>
    <Fact()>
    Public Sub Bug650318()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        x ::= Nothing
    End Sub
End Module
]]>,
            <errors>
                <error id="30035"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        x : : = Nothing
    End Sub
End Module
]]>,
            <errors>
                <error id="30035"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        x : : = Nothing
    End Sub
End Module
]]>.Value.Replace(":"c, FULLWIDTH_COLON).Replace("="c, FULLWIDTH_EQUALS_SIGN),
            <errors>
                <error id="30035"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        ::= Nothing
    End Sub
End Module
]]>,
            <errors>
                <error id="30035"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        ::= Nothing
    End Sub
End Module
]]>.Value.Replace(":"c, FULLWIDTH_COLON).Replace("="c, FULLWIDTH_EQUALS_SIGN),
            <errors>
                <error id="30035"/>
            </errors>)
    End Sub

    <WorkItem(671115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/671115")>
    <Fact()>
    Public Sub IsNewLine()
        Dim sourceFormat = "Module M{0}    Dim x = 1 'Comment{0}End Module{0}"
        ParseAndVerify(String.Format(sourceFormat, CARRIAGE_RETURN))
        ParseAndVerify(String.Format(sourceFormat, LINE_FEED))
        ParseAndVerify(String.Format(sourceFormat, NEXT_LINE))
        ParseAndVerify(String.Format(sourceFormat, LINE_SEPARATOR))
        ParseAndVerify(String.Format(sourceFormat, PARAGRAPH_SEPARATOR))
    End Sub

    <WorkItem(674590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674590")>
    <Fact()>
    Public Sub Bug674590()
        ParseAndVerify(<![CDATA[
Class C
    Shared Operator</
    End Operator
End Class
]]>,
            <errors>
                <error id="30199"/>
                <error id="30198"/>
                <error id="33000"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Shared Operator %>
    End Operator
End Class
]]>,
            <errors>
                <error id="30199"/>
                <error id="30198"/>
                <error id="33000"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Shared Operator <!--
    End Operator
End Class
]]>,
            <errors>
                <error id="30199"/>
                <error id="30198"/>
                <error id="33000"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Shared Operator <? 'Comment
    End Operator
End Class
]]>,
            <errors>
                <error id="30199"/>
                <error id="30198"/>
                <error id="33000"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Shared Operator <![CDATA[  _

    End Operator
End Class
]]>,
            <errors>
                <error id="30199"/>
                <error id="30198"/>
                <error id="33000"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Shared Operator <!DOCTYPE
    End Operator
End Class
]]>,
            <errors>
                <error id="30199"/>
                <error id="30198"/>
            </errors>)
        ParseAndVerify(<source>
Class C
    Shared Operator ]]&gt;
    End Operator
End Class
                       </source>.Value,
            <errors>
                <error id="30199"/>
                <error id="30198"/>
                <error id="33000"/>
                <error id="30037"/>
            </errors>)
    End Sub

    <WorkItem(684860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/684860")>
    <Fact()>
    Public Sub Bug684860_SkippedTokens()
        Const n = 100000
        ' 100000 instances of "0+" in:
        ' Class C
        '     Dim x = M(0 0+0+0+...)
        ' End Class
        Dim builder = New System.Text.StringBuilder()
        builder.AppendLine("Class C")
        builder.Append("    Dim x = M(0 ")
        For i = 0 To n
            builder.Append("0+")
        Next
        builder.AppendLine(")")
        builder.AppendLine("End Class")
        Dim text = builder.ToString()
        Dim tree = VisualBasicSyntaxTree.ParseText(text)
        Dim root = tree.GetRoot()
        Dim walker = New TokenAndTriviaWalker()
        walker.Visit(root)
        Assert.True(walker.Tokens > n)
        Dim tokens1 = root.DescendantTokens(descendIntoTrivia:=False).ToArray()
        Dim tokens2 = root.DescendantTokens(descendIntoTrivia:=True).ToArray()
        Assert.True((tokens2.Length - tokens1.Length) > n)
    End Sub

    <WorkItem(684860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/684860")>
    <Fact()>
    Public Sub Bug684860_XmlText()
        Const n = 100000
        ' 100000 instances of "&lt;" in:
        ' ''' <x a="&lt;&lt;&lt;..."/>
        ' Class C
        ' End Class
        Dim builder = New System.Text.StringBuilder()
        builder.Append("''' <x a=""")
        For i = 0 To n
            builder.Append("&lt;")
        Next
        builder.AppendLine("""/>")
        builder.AppendLine("Class C")
        builder.AppendLine("End Class")
        Dim text = builder.ToString()
        Dim tree = VisualBasicSyntaxTree.ParseText(text, options:=New VisualBasicParseOptions(documentationMode:=DocumentationMode.Parse))
        Dim root = tree.GetRoot()
        Dim walker = New TokenAndTriviaWalker()
        walker.Visit(root)
        Assert.True(walker.Tokens > n)
        Dim tokens = root.DescendantTokens(descendIntoTrivia:=True).ToArray()
        Assert.True(tokens.Length > n)
    End Sub

    Private NotInheritable Class TokenAndTriviaWalker
        Inherits VisualBasicSyntaxWalker
        Public Tokens As Integer
        Public Sub New()
            MyBase.New(SyntaxWalkerDepth.StructuredTrivia)
        End Sub
        Public Overrides Sub VisitToken(token As SyntaxToken)
            Tokens += 1
            MyBase.VisitToken(token)
        End Sub
    End Class

    <WorkItem(685268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/685268")>
    <Fact()>
    Public Sub Bug685268()
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        If True Then Sub M()
        Return
    End Sub
End Module
]]>,
            <errors>
                <error id="36673"/>
                <error id="30289"/>
                <error id="30429"/>
            </errors>)
    End Sub

    <WorkItem(685474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/685474")>
    <Fact()>
    Public Sub Bug685474()
        ParseAndVerify(<![CDATA[
<A(Sub()
End Su
b

]]>,
            <errors>
                <error id="36673"/>
                <error id="30678"/>
                <error id="30198"/>
                <error id="30636"/>
            </errors>)
        ParseAndVerify(<![CDATA[
<A(Sub()
End Su
b
'One
'Two
]]>,
            <errors>
                <error id="36673"/>
                <error id="30678"/>
                <error id="30198"/>
                <error id="30636"/>
            </errors>)
    End Sub

    <WorkItem(697117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/697117")>
    <Fact()>
    Public Sub Bug697117()
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Return Function()
Declare Function F()
]]>,
            <errors>
                <error id="30625"/>
                <error id="30026"/>
                <error id="36674"/>
                <error id="30289"/>
                <error id="30218"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Return Function()
Delegate Function F()
]]>,
            <errors>
                <error id="30625"/>
                <error id="30026"/>
                <error id="36674"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Return Function()
    Delegate Sub F()
End Module
]]>,
            <errors>
                <error id="30026"/>
                <error id="36674"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function()
Declare Function F()
]]>,
            <errors>
                <error id="30625"/>
                <error id="36674"/>
                <error id="30218"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Function()
Delegate Function F()
]]>,
            <errors>
                <error id="30625"/>
                <error id="36674"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() Declare Sub F()
End Module
]]>,
            <errors>
                <error id="30289"/>
                <error id="30218"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() Delegate Sub F()
End Module
]]>,
            <errors>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() Imports I
End Module
]]>,
            <errors>
                <error id="30024"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub()
Imports I
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30024"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub()
        If True Then Imports I
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30024"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Delegate Function F()
End Module
]]>,
            <errors>
                <error id="30026"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub()
            If True Then Delegate Function F()
End Module
]]>,
            <errors>
                <error id="30026"/>
                <error id="36673"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        If True Then Else Delegate Function F()
End Module
]]>,
            <errors>
                <error id="30026"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub() If True Then Delegate Sub F()
End Module
]]>,
            <errors>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        If True Then Else Delegate Sub F()
End Module
]]>,
            <errors>
                <error id="36673"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Sub M()
        Dim x = Sub() If True Then Else Delegate Function F()
End Module
]]>,
            <errors>
                <error id="30026"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        If True Then Return : Event E
End Module
]]>,
            <errors>
                <error id="36673"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub()
        If True Then Return : Inherits I
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30024"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        If True Then Else Property P
End Module
]]>,
            <errors>
                <error id="36673"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub() If True Then Else Implements I
End Class
]]>,
            <errors>
                <error id="30205"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub()
        If True Then Else Imports I
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30024"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        If True Then If False Then Else Return : Private Class C
    End Class
End Module
]]>,
            <errors>
                <error id="36673"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Module M
    Dim x = Sub()
        If True Then Else Return : _
    Friend Protected Enum E
    End Enum
End Module
]]>,
            <errors>
                <error id="36673"/>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub()
        If True Then Else Return : _
Option Strict On
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30024"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Function F()
        Return Sub()
            Return : Implements I
    End Function
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30024"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub()
        Return : _
Imports I
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30024"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Function F()
        Using Nothing
            Return Sub()
                Return : End Using
        End Sub
    End Function
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30429"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Sub M()
        Using Nothing
            Dim x = Sub() If True Then End Using
    End Sub
End Class
]]>,
            <errors>
                <error id="36008"/>
                <error id="32005"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Sub M()
        Try
            Dim x = Sub() If True Then Catch e
        End Try
    End Sub
End Class
]]>,
            <errors>
                <error id="30380"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Sub M()
        If True Then
            Dim x = Sub() Else
        End If
    End Sub
End Class
]]>,
            <errors>
                <error id="30086"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub() <A>Delegate Sub D()
End Class
]]>,
            <errors>
                <error id="32035"/>
                <error id="30660"/>
                <error id="30183"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub() If True Then Return : <A> Property P
End Class
]]>,
            <errors>
                <error id="30289"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub() If True Then Else <A> Property P
End Class
]]>,
            <errors>
                <error id="30201"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub()
        If True Then Return : _
    <A>
    Enum E
    End Enum
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30289"/>
            </errors>)
    End Sub

    <WorkItem(716242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716242")>
    <Fact()>
    Public Sub Bug716242()
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub()
        Select Case x
    Delegate Function F()
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30095"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub() Select Case x : _
    Delegate Function F()
End Class
]]>,
            <errors>
                <error id="30095"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub()
        Select Case x
Option Strict On
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30095"/>
                <error id="30058"/>
                <error id="30024"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Function F()
        Return Sub() If True Then Select Case x : _
Implements I
    End Function
End Class
]]>,
            <errors>
                <error id="30095"/>
                <error id="30058"/>
                <error id="30024"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Sub M()
        Using Nothing
            Dim x = Sub()
                Select Case x
        End Using
    End Sub
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30095"/>
            </errors>)
        ParseAndVerify(<![CDATA[
Class C
    Dim x = Sub()
        Select Case x
    <A>
    Enum E
    End Enum
End Class
]]>,
            <errors>
                <error id="36673"/>
                <error id="30095"/>
            </errors>)
    End Sub

    Private Shared Sub CheckArray(Of T)(actual As T(), ParamArray expected As T())
        Assert.Equal(expected.Length, actual.Length)
        For i = 0 To actual.Length - 1
            Assert.Equal(expected(i), actual(i))
        Next
    End Sub

    <WorkItem(539515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539515")>
    <Fact()>
    Public Sub ParseIllegaLineCont()
        ParseAndVerify(
        <![CDATA[
Module M
    Sub Main()
_ 
    End Sub
End Module
]]>,
        Diagnostic(ERRID.ERR_LineContWithCommentOrNoPrecSpace, "_").WithLocation(4, 1)
)
    End Sub

    <WorkItem(539515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539515")>
    <Fact()>
    Public Sub ParseIllegaLineCont_1()
        ParseAndVerify(
        <![CDATA[
Module M
    Sub Main() _
 _
    _
_ 
    End Sub
End Module
]]>,
    Diagnostic(ERRID.ERR_LineContWithCommentOrNoPrecSpace, "_").WithLocation(6, 1)
)
    End Sub
End Class
