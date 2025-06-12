' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class GotoTests
        Inherits BasicTestBase

        <WorkItem(543106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543106")>
        <Fact()>
        Public Sub BranchOutOfFinallyInLambda()

            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="BranchOutOfFinallyInLambda">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Class C1
    shared Sub MAIN()
        Dim lists = goo()
        lists.Where(Function(ByVal item)
lab1:
                        Try
                        Catch ex As Exception
                            GoTo lab1
                        Finally
                            GoTo lab1
                        End Try
                        Return item.ToString() = String.Empty
                    End Function).ToList()
    End Sub
    Shared Function goo() As List(Of Integer)
        Return Nothing
    End Function
End Class
    </file>
</compilation>, {SystemCoreRef})

            AssertTheseDiagnostics(compilation,
<expected>
BC30101: Branching out of a 'Finally' is not valid.
                            GoTo lab1
                                 ~~~~
            </expected>)
        End Sub

        <WorkItem(543392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543392")>
        <Fact()>
        Public Sub BranchOutOfFinallyInLambda_1()

            CreateCompilationWithMscorlib40AndReferences(
<compilation name="BranchOutOfFinallyInLambda">
    <file name="a.vb">
Imports System.Collections.Generic
Imports System.Linq
Module Module1
    Sub Main()
        Dim lists As New List(Of Integer)
        lists.Where(Function(ByVal item) GoTo lab1)
    End Sub
End Module
    </file>
</compilation>, {MsvbRef, SystemCoreRef}).AssertTheseDiagnostics(<expected>
BC30518: Overload resolution failed because no accessible 'Where' can be called with these arguments:
        lists.Where(Function(ByVal item) GoTo lab1)
              ~~~~~
BC30201: Expression expected.
        lists.Where(Function(ByVal item) GoTo lab1)
                                         ~
                                                                 </expected>)

        End Sub

        <Fact()>
        Public Sub NakedGoto()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NakedGoto">
    <file name="a.vb">
Module M
    Sub Main()
        GoTo 
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
    Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""))

        End Sub

        <Fact()>
        Public Sub GotoInvalidLabel()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoInvalidLabel">
    <file name="a.vb">
Module M
    Sub Main()
        GoTo 1+2
3:      GoTo Return
1:      GoTo If(True,1, 2)
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedEOS, "+"),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
    Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
    Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""),
    Diagnostic(ERRID.ERR_LabelNotDefined1, "").WithArguments(""))

        End Sub

        <Fact()>
        Public Sub GotoOutOfMethod()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoOutOfMethod">
    <file name="a.vb">
Structure struct
    GoTo Labl
    Const x = 1
Lab1:
    Const y = 2
End Structure
    </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "GoTo Labl"),
            Diagnostic(ERRID.ERR_InvOutsideProc, "Lab1:"))
        End Sub

        <Fact()>
        Public Sub GotoOutOfMethod_1()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoOutOfMethod">
    <file name="a.vb">
Namespace ns1
    goto Labl 
    const x = 1
    Lab1:
    const y = 2
End namespace
    </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "goto Labl"),
            Diagnostic(ERRID.ERR_InvOutsideProc, "Lab1:"),
            Diagnostic(ERRID.ERR_InvalidInNamespace, "const x = 1"),
            Diagnostic(ERRID.ERR_InvalidInNamespace, "const y = 2"))
        End Sub

        <Fact()>
        Public Sub GotoOutOfMethod_2()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoOutOfMethod">
    <file name="a.vb">
    goto Labl 
    Lab1:
    </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "goto Labl"),
            Diagnostic(ERRID.ERR_InvOutsideProc, "Lab1:"))
        End Sub

        <Fact()>
        Public Sub GotoMultiLabel()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoMultiLabel">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim i = 0
        GoTo Lab2,Lab1
Lab1:
        i = 1
Lab2:
        i = 2
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedEOS, ","))

        End Sub

        <WorkItem(543364, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543364")>
        <Fact()>
        Public Sub LabelAfterElse()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LabelAfterElse">
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim Flag1 = 1
        GoTo 100
        If Flag1 = 1 Then
            Flag1 = 100
        Else 100: Flag1 = 200
        End If
        Console.Write(Flag1)
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_Syntax, "100"),
                                  Diagnostic(ERRID.ERR_LabelNotDefined1, "100").WithArguments("100"))
        End Sub

        <Fact()>
        Public Sub LabelAfterElse_1()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LabelAfterElse">
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim Flag1 = 1
        GoTo 100
        If Flag1 = 1 Then
            Flag1 = 100
        Else :100: Flag1 = 200
        End If
        Console.Write(Flag1)
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_Syntax, "100"),
                                  Diagnostic(ERRID.ERR_LabelNotDefined1, "100").WithArguments("100"))
        End Sub

        <WorkItem(543364, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543364")>
        <Fact()>
        Public Sub LabelAfterElse_NotNumeric()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LabelAfterElse">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x = 1
        If (x = 1)
lab1:
        Else lab2:
        End If
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_NameNotDeclared1, "lab2").WithArguments("lab2"))

        End Sub

        <Fact()>
        Public Sub LabelBeforeFirstCase()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LabelBeforeFirstCase">
    <file name="a.vb">
Module M
    Sub Main()
        Dim Fruit As String = "Apple"
        Select Case Fruit
            label1: Case "Banana"
                Exit Select
            Case "Chair"
                Exit Select
        End Select
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedCase, "label1:"))

        End Sub

        <Fact()>
        Public Sub LabelInDifferentMethod()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LabelInDifferentMethod">
    <file name="a.vb">
Module M
    Public Sub Main()
        GoTo label1
    End Sub
    Public Sub goo()
label1:
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_LabelNotDefined1, "label1").WithArguments("label1"))

        End Sub

        <Fact()>
        Public Sub GotoDeeperScope()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GotoDeeperScope">
    <file name="a.vb">
Module M
    Public Sub Main()
        For i = 0 To 10
Label:
        Next
        GoTo Label
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_GotoIntoFor, "Label").WithArguments("Label"))

        End Sub

        <Fact()>
        Public Sub BranchOutFromLambda()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BranchOutFromLambda">
    <file name="a.vb">
Delegate Function del(i As Integer) As Integer
Module Program
    Sub Main(args As String())
        Dim q As del = Function(x)
                           GoTo label2
                           Return x * x
                       End Function
label2:
        Return
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_LabelNotDefined1, "label2").WithArguments("label2"))

        End Sub

        <Fact()>
        Public Sub SameLabelNameInDifferentLambda()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SameLabelNameInDifferentLambda">
    <file name="a.vb">
Imports System
Delegate Function del(i As Integer) As Integer
Module Program
    Sub Main(args As String())
        Dim q As del = Function(x)
                           GoTo label1
label1:
                           Return x * x
                       End Function
        Dim p As del = Function(x)
                           GoTo label1
label1:
                           Return x * x
                       End Function

    End Sub
End Module
    </file>
</compilation>).AssertNoDiagnostics()

        End Sub

        <Fact()>
        Public Sub SameLabelNameInDifferentScop_Lambda()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SameLabelNameInDifferentScop_Lambda">
    <file name="a.vb">
Imports System
Delegate Function del(i As Integer) As Integer
Module Program
    Sub Main(args As String())
        Dim p As del = Function(x)
                           GoTo label1
label1:
                           Return x * x
                       End Function
label1:
        Return
    End Sub
End Module
    </file>
</compilation>).AssertNoDiagnostics()

        End Sub

        <Fact()>
        Public Sub IllegalLabels()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="IllegalLabels">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
'COMPILEERROR: BC30801, "11"
11A:
'COMPILEERROR: BC30801, "12"
12B:
'COMPILEERROR: BC30801, "16", BC30035
16F:
'COMPILEERROR: BC30801, "17"
17G:
'COMPILEERROR: BC30801, "19", BC30035
19I:
'COMPILEERROR: BC30801, "21"
21J:
'COMPILEERROR: BC30801, "23", BC30035
23L:
'COMPILEERROR: BC30801, "24"
24M:
'COMPILEERROR: BC30801, "25"
25N:
'COMPILEERROR: BC30801, "26"
26O:
'COMPILEERROR: BC30801, "27"
27P:
'COMPILEERROR: BC30801, "31", BC30035
31S:
'COMPILEERROR: BC30801, "32"
32T:
'COMPILEERROR: BC30801, "33"
33U:
'COMPILEERROR: BC30801, "34"
34V:
'COMPILEERROR: BC30801, "35"
35W:
'COMPILEERROR: BC30801, "36"
36X:
'COMPILEERROR: BC30801, "37"
37Y:
'COMPILEERROR: BC30801, "38"
38Z:
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "11"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "12"),
                        Diagnostic(ERRID.ERR_Syntax, "16F"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "17"),
                        Diagnostic(ERRID.ERR_Syntax, "19I"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "21"),
                        Diagnostic(ERRID.ERR_Syntax, "23L"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "24"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "25"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "26"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "27"),
                        Diagnostic(ERRID.ERR_Syntax, "31S"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "32"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "33"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "34"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "35"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "36"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "37"),
                        Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "38"))
        End Sub

        <Fact()>
        Public Sub IllegalLabels_1()
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="IllegalLabels">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
                'COMPILEERROR: BC30035, "14D"
        14D:
                'COMPILEERROR: BC30495, "15E"
        15E:
                'COMPILEERROR: BC30035, "16F"
        16F:
                'COMPILEERROR: BC30035, "19I"
19I:
                'COMPILEERROR: BC30035, "23L"
23L:
                'COMPILEERROR: BC30035, "29R"
        29R:
                'COMPILEERROR: BC30035, "31S"
31S:
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_Syntax, "14D"),
                Diagnostic(ERRID.ERR_InvalidLiteralExponent, "15E"),
                Diagnostic(ERRID.ERR_Syntax, "16F"),
                Diagnostic(ERRID.ERR_Syntax, "19I"),
                Diagnostic(ERRID.ERR_Syntax, "23L"),
                Diagnostic(ERRID.ERR_Syntax, "29R"),
                Diagnostic(ERRID.ERR_Syntax, "31S"))
        End Sub

    End Class
End Namespace
