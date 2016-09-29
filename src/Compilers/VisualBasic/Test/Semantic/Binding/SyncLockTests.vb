' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SyncLockTests
        Inherits BasicTestBase
        <Fact()>
        Public Sub SyncLockDecimal()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockDecimal">
    <file name="a.vb">
Class C1
    Shared Sub Main()
        SyncLock (1.0)
        End SyncLock
        Dim x As Decimal
        SyncLock (x)
        End SyncLock
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_SyncLockRequiresReferenceType1, "(1.0)").WithArguments("Double"),
                            Diagnostic(ERRID.ERR_SyncLockRequiresReferenceType1, "(x)").WithArguments("Decimal"))
        End Sub

        <Fact()>
        Public Sub SyncLockMultiDimensionalArray()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockMultiDimensionalArray">
    <file name="a.vb">
Class Test
    Public Shared Sub Main()
        Dim y(,) = New Integer(,) {{1}, {2}}
        SyncLock y
        End SyncLock
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub SyncLockGenericType()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockGenericType">
    <file name="a.vb">
Class Program
    Private Shared Sub ABC(Of T)(x As T)
        SyncLock x
        End SyncLock
    End Sub
    Private Shared Sub Foo(Of T As D)(x As T)
        SyncLock x
        End SyncLock
    End Sub
    Private Shared Sub Bar(Of T As Structure)(x As T)
        SyncLock x
        End SyncLock
    End Sub
End Class
Class D
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_SyncLockRequiresReferenceType1, "x").WithArguments("T"),
                            Diagnostic(ERRID.ERR_SyncLockRequiresReferenceType1, "x").WithArguments("T"))
        End Sub

        <Fact()>
        Public Sub SyncLockQuery()

            CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="SyncLockQuery">
    <file name="a.vb">
Option Strict On
Imports System.Linq
Class Program
    Shared Sub Main()
        SyncLock From w In From x In New Integer() {1, 2, 3}
                           From y In New Char() {"a"c, "b"c}
                           Let bOdd = (x And 1) = 1
                           Where
                               bOdd Where y > "a"c Let z = x.ToString() &amp; y.ToString()
        End SyncLock
    End Sub
End Class
    </file>
</compilation>, {SystemCoreRef}).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub DeclareVarInSyncLock()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Class Program
    Public Shared Sub Main(args As String())
        SyncLock d As Object = New Object()
        End SyncLock
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedEOS, "As"),
                        Diagnostic(ERRID.ERR_NameNotDeclared1, "d").WithArguments("d"))
        End Sub

        <Fact()>
        Public Sub SyncLockLambda()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockLambda">
    <file name="a.vb">
Class Program
    Public Shared Sub Main(args As String())
        SyncLock Function(x) x
        End SyncLock

        SyncLock Function()
                 End Function
        End SyncLock

        SyncLock Function(ByRef int As Integer)
                     Return int
                 End Function
        End SyncLock

        SyncLock Function(ByVal x As Object) x
        End SyncLock

        SyncLock Sub(x As Object)
                 End Sub
        End SyncLock

        SyncLock Sub()
                     Exit While
                 End Sub
        End SyncLock

    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExitWhileNotWithinWhile, "Exit While"),
                        Diagnostic(ERRID.WRN_DefAsgNoRetValFuncRef1, "End Function").WithArguments("<anonymous method>"))
        End Sub

        <Fact()>
        Public Sub SyncLockExtend()

            CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="SyncLockExtend">
    <file name="a.vb">
Option Strict On
Imports System.Runtime.CompilerServices
Module StringExtensions
    &lt;Extension()&gt;
    Public Function PrintString(ByVal aString As String) As String
        Return aString.ToString()
    End Function

    &lt;Extension()&gt;
    Public Function PrintInt(ByVal aString As String) As Integer
        Return 1
    End Function

    &lt;Extension()&gt;
    Public Sub PrintVoid(ByVal aString As String)
        Return
    End Sub

    Sub Main()
        Dim syncroot As String
        SyncLock syncroot.PrintString
            SyncLock syncroot.PrintInt()
                SyncLock syncroot.PrintVoid
                End SyncLock
            End SyncLock
        End SyncLock
    End Sub
End Module
    </file>
</compilation>, {SystemCoreRef}).VerifyDiagnostics(Diagnostic(ERRID.ERR_SyncLockRequiresReferenceType1, "syncroot.PrintInt()").WithArguments("Integer"),
                                Diagnostic(ERRID.ERR_VoidValue, "syncroot.PrintVoid"),
                                Diagnostic(ERRID.WRN_DefAsgUseNullRef, "syncroot").WithArguments("syncroot"))
        End Sub

        <Fact()>
        Public Sub SyncLockAnonymous()

            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockAnonymous">
    <file name="a.vb">
Module M1
    Sub Main()
        Dim p1 = New With {Key .p1 = 1.0D, .p2 = ""}
        SyncLock p1
        End SyncLock        
        SyncLock p1.p1
        End SyncLock
        SyncLock p1.p2
        End SyncLock
        SyncLock New With {Key .p1 = 10.0}
        End SyncLock
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_SyncLockRequiresReferenceType1, "p1.p1").WithArguments("Decimal"))
        End Sub

        <Fact()>
        Public Sub SyncLockMe_InvalidCase()

            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockMe_InvalidCase">
    <file name="a.vb">
Structure S1
    Sub FOO()
        SyncLock Me
        End SyncLock
    End Sub
End Structure
Class Program
    Public Shared Property MyProperty() As Integer
        Get
        End Get
        Set
            SyncLock Me
            End SyncLock
            SyncLock Value
            End SyncLock
        End Set
    End Property
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_SyncLockRequiresReferenceType1, "Me").WithArguments("S1"),
                            Diagnostic(ERRID.WRN_DefAsgNoRetValPropVal1, "End Get").WithArguments("MyProperty"),
                            Diagnostic(ERRID.ERR_UseOfKeywordNotInInstanceMethod1, "Me").WithArguments("Me"),
                            Diagnostic(ERRID.ERR_SyncLockRequiresReferenceType1, "Value").WithArguments("Integer"))
        End Sub

        <Fact()>
        Public Sub SyncLockMultiResource()

            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockMultiResource">
    <file name="a.vb">
Public Class D
    Public Sub foo()
        Dim x1, x2 As New Object
        SyncLock x1,x2
        End SyncLock
        SyncLock x,z As New Object, y,t As New Object
        End SyncLock
        SyncLock i,j As String
        End SyncLock
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedEOS, ","),
                            Diagnostic(ERRID.ERR_ExpectedEOS, ","),
                            Diagnostic(ERRID.ERR_ExpectedEOS, ","),
                            Diagnostic(ERRID.ERR_NameNotDeclared1, "x").WithArguments("x"),
                            Diagnostic(ERRID.ERR_NameNotDeclared1, "i").WithArguments("i"))
        End Sub

        <Fact()>
        Public Sub SyncLockMalformed()

            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockMalformed">
    <file name="a.vb">
Public Class D
    Public Sub foo()
        SyncLock
        End SyncLock
        SyncLock SyncLock
        End SyncLock
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedExpression, ""),
                                Diagnostic(ERRID.ERR_ExpectedExpression, ""))
        End Sub

        <Fact, WorkItem(529545, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529545"), WorkItem(782216, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782216")>
        Public Sub ExitPropertyInSyncLock()

            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="ExitPropertyInSyncLock">
    <file name="a.vb">
Module M1
    Property Age() As Integer
        Get
            Return 1
        End Get
        Set(ByVal Value As Integer)
            Dim a, b, c As Object
            SyncLock New Object
                c = 342108
                Exit Property
                System.Console.WriteLine(a)
            End SyncLock
            System.Console.WriteLine(b)
            System.Console.WriteLine(c)
        End Set
    End Property
    Sub Main()
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics()
        End Sub

        <WorkItem(543319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543319")>
        <Fact()>
        Public Sub SyncLockInSelect()

            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockInSelect">
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Select ""
            Case "a"
                SyncLock New Object()
                    Case Else
                    GoTo lab1
                End SyncLock
        End Select
lab1:
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_CaseElseNoSelect, "Case Else"),
    Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports System"))
        End Sub

        'Unassigned variable declared before the block gives no warnings if used before label to where we exit using unconditional goto
        <Fact()>
        Public Sub UnreachableCode()

            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="UnreachableCode">
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Dim x1 As Object
        SyncLock New Object
            GoTo label1
        End SyncLock
        Console.WriteLine(x1)
label1:
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub AssignmentInSyncLock()

            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="AssignmentInSyncLock">
    <file name="a.vb">
Option Infer On
Class Program
    Shared Sub Main()
        Dim myLock As Object
        SyncLock myLock = New Object()
            System.Console.WriteLine(myLock)
        End SyncLock
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.WRN_DefAsgUseNullRef, "myLock").WithArguments("myLock"))
        End Sub

        <Fact()>
        Public Sub SyncLockEndSyncLockMismatch()

            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockEndSyncLockMismatch">
    <file name="a.vb">
Imports System
Class Test
    Public Shared Sub Main()
        SyncLock Nothing
            Try
            Catch ex As Exception
                End SyncLock
        End Try
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedEndTry, "Try"),
                        Diagnostic(ERRID.ERR_EndTryNoTry, "End Try"))
        End Sub

        <WorkItem(11022, "DevDiv_Projects/Roslyn")>
        <Fact>
        Public Sub SyncLockOutOfMethod()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockOutOfMethod">
    <file name="a.vb">
        SyncLock Nothing
        End SyncLock
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "SyncLock Nothing"),
                        Diagnostic(ERRID.ERR_EndSyncLockNoSyncLock, "End SyncLock"))
        End Sub

        <WorkItem(11022, "DevDiv_Projects/Roslyn")>
        <Fact>
        Public Sub SyncLockOutOfMethod_1()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SyncLockOutOfMethod">
    <file name="a.vb">
Class m1
    SyncLock New Object()
    End SyncLock
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "SyncLock New Object()"),
                        Diagnostic(ERRID.ERR_EndSyncLockNoSyncLock, "End SyncLock"))
        End Sub

        <Fact(), WorkItem(529059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529059")>
        Public Sub SingleElseInSyncLock()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SingleElseInSyncLock">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        SyncLock Nothing
            Else
        End SyncLock
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ElseNoMatchingIf, "Else"))
        End Sub

        <WorkItem(529066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529066")>
        <Fact()>
        Public Sub SingleCaseElseInSyncLock()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="SingleCaseElseInSyncLock">
    <file name="a.vb">
Module M
    Sub Main()
        Dim doublevar As Double
        Select Case doublevar
            Case 1
                GoTo L410
L100:       Case 2.25
                SyncLock Nothing
L200:               Case Else
                End SyncLock
        End Select
        GoTo L510
L410:
        GoTo L510
L510:
        GoTo L100
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_CaseElseNoSelect, "Case Else"))
        End Sub
    End Class
End Namespace
