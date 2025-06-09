' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SyncLockTests
        Inherits BasicTestBase
        <Fact()>
        Public Sub SyncLockDecimal()
            CreateCompilationWithMscorlib40AndVBRuntime(
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
            CreateCompilationWithMscorlib40AndVBRuntime(
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
            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyncLockGenericType">
    <file name="a.vb">
Class Program
    Private Shared Sub ABC(Of T)(x As T)
        SyncLock x
        End SyncLock
    End Sub
    Private Shared Sub Goo(Of T As D)(x As T)
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

            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
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
</compilation>, {Net40.References.SystemCore}).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub DeclareVarInSyncLock()
            CreateCompilationWithMscorlib40AndVBRuntime(
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
            CreateCompilationWithMscorlib40AndVBRuntime(
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

            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
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
</compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(Diagnostic(ERRID.ERR_SyncLockRequiresReferenceType1, "syncroot.PrintInt()").WithArguments("Integer"),
                                Diagnostic(ERRID.ERR_VoidValue, "syncroot.PrintVoid"),
                                Diagnostic(ERRID.WRN_DefAsgUseNullRef, "syncroot").WithArguments("syncroot"))
        End Sub

        <Fact()>
        Public Sub SyncLockAnonymous()

            CreateCompilationWithMscorlib40AndVBRuntime(
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

            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyncLockMe_InvalidCase">
    <file name="a.vb">
Structure S1
    Sub GOO()
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

            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyncLockMultiResource">
    <file name="a.vb">
Public Class D
    Public Sub goo()
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

            CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyncLockMalformed">
    <file name="a.vb">
Public Class D
    Public Sub goo()
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

            CreateCompilationWithMscorlib40AndVBRuntime(
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

            CreateCompilationWithMscorlib40AndVBRuntime(
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

            CreateCompilationWithMscorlib40AndVBRuntime(
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

            CreateCompilationWithMscorlib40AndVBRuntime(
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

            CreateCompilationWithMscorlib40AndVBRuntime(
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
            CreateCompilationWithMscorlib40AndVBRuntime(
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
            CreateCompilationWithMscorlib40AndVBRuntime(
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
            CreateCompilationWithMscorlib40AndVBRuntime(
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
            CreateCompilationWithMscorlib40AndVBRuntime(
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

        <Fact>
        Public Sub LockType_InSyncLock()
            Dim source = "
Module Program
    Sub Main()
        Dim l = New System.Threading.Lock()
        SyncLock l
        End SyncLock
    End Sub
End Module

Namespace System.Threading
    Public Class Lock
    End Class
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics(
"BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock l
                 ~
")
        End Sub

        <Fact>
        Public Sub LockType_InSyncLock_Net9()
            Dim source = "
Module Program
    Sub Main()
        Dim l = New System.Threading.Lock()
        SyncLock l
        End SyncLock
    End Sub
End Module
"
            CreateCompilation(source, targetFramework:=TargetFramework.Net90).AssertTheseDiagnostics(
"BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock l
                 ~
")
        End Sub

        ''' <summary>
        ''' Verifies that the suggestion from the error in the test above (to manually call 'Enter' and 'Exit') compiles.
        ''' </summary>
        <Fact>
        Public Sub LockType_InSyncLock_ManualEnterExit()
            Dim source = <![CDATA[
Imports System
Imports System.Threading
Module Program
    Sub Main()
        Dim l = New Lock()
        l.Enter()
        Console.Write("1")
        Try
            Console.Write("2")
        Finally
            Console.Write("3")
            l.Exit()
            Console.Write("4")
        End Try
        Console.Write("5")
    End Sub
End Module
]]>.Value
            Dim comp = CreateCompilation(source, options:=TestOptions.ReleaseExe, targetFramework:=TargetFramework.Net90)
            CompileAndVerify(comp, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "12345", Nothing),
                             verify:=Verification.FailsPEVerify).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub LockType_Generic()
            Dim source = "
Module Program
    Sub Main()
        Dim l = New System.Threading.Lock(Of String)()
        SyncLock l
        End SyncLock
    End Sub
End Module

Namespace System.Threading
    Public Class Lock(Of T)
    End Class
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics()
        End Sub

        <Fact>
        Public Sub LockType_Nested()
            Dim source = "
Module Program
    Sub Main()
        Dim l = New System.Threading.Container.Lock()
        SyncLock l
        End SyncLock
    End Sub
End Module

Namespace System.Threading
    Public Class Container
        Public Class Lock
        End Class
    End Class
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics()
        End Sub

        <Fact>
        Public Sub LockType_WrongNamespace()
            Dim source = "
Module Program
    Sub Main()
        Dim l = New Threading.Lock()
        SyncLock l
        End SyncLock
    End Sub
End Module

Namespace Threading
    Public Class Lock
    End Class
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics()
        End Sub

        <Fact>
        Public Sub LockType_WrongTypeName()
            Dim source = "
Module Program
    Sub Main()
        Dim l = New System.Threading.Lock1()
        SyncLock l
        End SyncLock
    End Sub
End Module

Namespace System.Threading
    Public Class Lock1
    End Class
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics()
        End Sub

        <Theory, CombinatorialData>
        Public Sub LockType_LowercaseTypeName(
            <CombinatorialValues("Lock", "lock")> usage As String,
            <CombinatorialValues("Lock", "lock")> declaration As String)
            Dim source = $"
Module Program
    Sub Main()
        Dim l = New System.Threading.{usage}()
        SyncLock l
        End SyncLock
    End Sub
End Module

Namespace System.Threading
    Public Class {declaration}
    End Class
End Namespace
"
            Dim comp = CreateCompilation(source)
            If declaration = "Lock" Then
                comp.AssertTheseDiagnostics(
"BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock l
                 ~
")
            Else
                Assert.Equal("lock", declaration)
                comp.AssertTheseDiagnostics()
            End If
        End Sub

        <Fact>
        Public Sub LockType_CastToObject()
            Dim source = "
Imports System.Threading

Module Program
    Sub Main()
        Dim l = New Lock()
        Dim o As Object = l

        o = DirectCast(l, Object)
        SyncLock DirectCast(l, Object)
        End SyncLock

        o = CType(l, Object)
        SyncLock CType(l, Object)
        End SyncLock

        o = TryCast(l, Object)
        SyncLock TryCast(l, Object)
        End SyncLock

        o = M1(l)
        SyncLock M1(l)
        End SyncLock

        o = M2(l)
        SyncLock M2(l)
        End SyncLock

        o = M3(l)
        SyncLock M3(l)
        End SyncLock
    End Sub

    Function M1(Of T)(o as T) As Object
        Return o
    End Function

    Function M2(Of T As Class)(o as T) As Object
        Return o
    End Function

    Function M3(Of T As Lock)(o as T) As Object
        Return o
    End Function
End Module

Namespace System.Threading
    Public Class Lock
    End Class
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics(
"BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        Dim o As Object = l
                          ~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        o = DirectCast(l, Object)
                       ~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        SyncLock DirectCast(l, Object)
                            ~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        o = CType(l, Object)
                  ~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        SyncLock CType(l, Object)
                       ~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        o = TryCast(l, Object)
                    ~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        SyncLock TryCast(l, Object)
                         ~
")
        End Sub

        <Fact>
        Public Sub LockType_CastToBase()
            Dim source = "
Imports System.Threading

Module Program
    Sub Main()
        Dim l = New Lock()
        Dim o As LockBase = l
        SyncLock o
        End SyncLock
    End Sub
End Module

Namespace System.Threading
    Public Class LockBase
    End Class

    Public Class Lock
        Inherits LockBase
    End Class
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics(
"BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        Dim o As LockBase = l
                            ~
")
        End Sub

        <Fact>
        Public Sub LockType_CastToInterface()
            Dim source = "
Imports System.Threading

Module Program
    Sub Main()
        Dim l = New Lock()
        Dim o As ILockBase = l
        SyncLock o
        End SyncLock
    End Sub
End Module

Namespace System.Threading
    Public Interface ILockBase
    End Interface

    Public Class Lock
        Implements ILockBase
    End Class
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics(
"BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        Dim o As ILockBase = l
                             ~
")
        End Sub

        <Fact>
        Public Sub LockType_CastToSelf()
            Dim source = "
Imports System.Threading

Module Program
    Sub Main()
        Dim l = New Lock()
        Dim o As Lock = l

        o = DirectCast(l, Lock)
        SyncLock DirectCast(l, Lock)
        End SyncLock

        o = CType(l, Lock)
        SyncLock CType(l, Lock)
        End SyncLock

        o = TryCast(l, Lock)
        SyncLock TryCast(l, Lock)
        End SyncLock

        o = M1(l)
        SyncLock M1(l)
        End SyncLock

        o = M2(l)
        SyncLock M2(l)
        End SyncLock

        o = M3(l)
        SyncLock M3(l)
        End SyncLock
    End Sub

    Function M1(Of T)(o as T) As Lock
        Return CType(CType(o, Object), Lock)
    End Function

    Function M2(Of T As Class)(o as T) As Lock
        Return CType(CType(o, Object), Lock)
    End Function

    Function M3(Of T As Lock)(o as T) As Lock
        Return o
    End Function
End Module

Namespace System.Threading
    Public Class Lock
    End Class
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics(
"BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock DirectCast(l, Lock)
                 ~~~~~~~~~~~~~~~~~~~
BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock CType(l, Lock)
                 ~~~~~~~~~~~~~~
BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock TryCast(l, Lock)
                 ~~~~~~~~~~~~~~~~
BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock M1(l)
                 ~~~~~
BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock M2(l)
                 ~~~~~
BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock M3(l)
                 ~~~~~
")
        End Sub

        <Fact>
        Public Sub LockType_Downcast()
            Dim source = "
Imports System.Threading
Module Program
    Sub Main()
        Dim l = New Lock()
        Dim o As Object = l
        SyncLock CType(o, Lock)
        End SyncLock
    End Sub
End Module

Namespace System.Threading
    Public Class Lock
    End Class
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics(
"BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        Dim o As Object = l
                          ~
BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock CType(o, Lock)
                 ~~~~~~~~~~~~~~
")
        End Sub

        <Fact>
        Public Sub LockType_Derived()
            Dim source = "
Imports System
Imports System.Threading

Module Program
    Private Sub Main()
        Dim l1 As DerivedLock = New DerivedLock()
        SyncLock l1
        End SyncLock

        Dim l2 As Lock = l1
        SyncLock l2 ' 1
        End SyncLock

        Dim l3 As DerivedLock = CType(l2, DerivedLock) ' 2
        l3 = DirectCast(l2, DerivedLock) ' 3
        l3 = TryCast(l2, DerivedLock) ' 4
        SyncLock l3
        End SyncLock

        Dim l4 As IDerivedLock = CType(l2, IDerivedLock) ' 5
        l4 = DirectCast(l2, IDerivedLock) ' 6
        l4 = TryCast(l2, IDerivedLock) ' 7
        SyncLock l4
        End SyncLock
    End Sub
End Module

Namespace System.Threading
    Public Class Lock
    End Class

    Public Class DerivedLock
        Inherits Lock
        Implements IDerivedLock
    End Class

    Interface IDerivedLock
    End Interface
End Namespace
"
            CreateCompilation(source).AssertTheseDiagnostics(
"BC37329: A value of type 'System.Threading.Lock' is not supported in SyncLock. Consider manually calling 'Enter' and 'Exit' methods in a Try/Finally block instead.
        SyncLock l2 ' 1
                 ~~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        Dim l3 As DerivedLock = CType(l2, DerivedLock) ' 2
                                      ~~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        l3 = DirectCast(l2, DerivedLock) ' 3
                        ~~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        l3 = TryCast(l2, DerivedLock) ' 4
                     ~~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        Dim l4 As IDerivedLock = CType(l2, IDerivedLock) ' 5
                                       ~~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        l4 = DirectCast(l2, IDerivedLock) ' 6
                        ~~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        l4 = TryCast(l2, IDerivedLock) ' 7
                     ~~
")
        End Sub

        <Fact>
        Public Sub LockType_Derived_Execution()
            Dim source = <![CDATA[
Imports System
Imports System.Threading

Module Program
    Sub Main()
        Dim l1 As DerivedLock = New DerivedLock()
        Dim l2 As Lock = l1
        Dim l3 As DerivedLock = CType(l2, DerivedLock)
        SyncLock l3
            Console.WriteLine("locked")
        End SyncLock
    End Sub
End Module

Namespace System.Threading
    Public Class Lock
    End Class

    Public Class DerivedLock
        Inherits Lock
        Implements IDerivedLock
    End Class

    Interface IDerivedLock
    End Interface
End Namespace
]]>.Value
            Dim comp = CreateCompilation(source, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(comp, expectedOutput:="locked")
            verifier.Diagnostics.AssertTheseDiagnostics(<![CDATA[
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        Dim l3 As DerivedLock = CType(l2, DerivedLock)
                                      ~~
]]>)
            verifier.VerifyIL("Program.Main", <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (Object V_0,
                Boolean V_1)
  IL_0000:  newobj     "Sub System.Threading.DerivedLock..ctor()"
  IL_0005:  castclass  "System.Threading.DerivedLock"
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.1
  .try
  {
    IL_000d:  ldloc.0
    IL_000e:  ldloca.s   V_1
    IL_0010:  call       "Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)"
    IL_0015:  ldstr      "locked"
    IL_001a:  call       "Sub System.Console.WriteLine(String)"
    IL_001f:  leave.s    IL_002b
  }
  finally
  {
    IL_0021:  ldloc.1
    IL_0022:  brfalse.s  IL_002a
    IL_0024:  ldloc.0
    IL_0025:  call       "Sub System.Threading.Monitor.Exit(Object)"
    IL_002a:  endfinally
  }
  IL_002b:  ret
}]]>)
        End Sub

        <Fact>
        Public Sub LockType_ObjectEquality()
            Dim source = <![CDATA[
Imports System
Imports System.Threading

Module Program
    Sub Main()
        Dim l As Lock = New Lock()

        If l IsNot Nothing Then
            Console.Write("1")
        End If

        If l Is Nothing Then
            Throw New Exception
        End If

        If l IsNot Nothing Then
            Console.Write("2")
        End If

        If l Is Nothing Then
            Throw New Exception
        End If

        If Not (l Is Nothing) Then
            Console.Write("3")
        End If

        If Not (l IsNot Nothing) Then
            Throw New Exception
        End If

        Dim l2 As Lock = New Lock()

        If l Is l2 Then
            Throw New Exception
        End If

        If l IsNot l2 Then
            Console.Write("4")
        End If

        If ReferenceEquals(l, l2) Then
            Throw New Exception
        End If

        If (CObj(l)) Is l2 Then
            Throw New Exception
        End If

        If (CObj(l)) IsNot l2 Then
            Console.Write("5")
        End If

        If l Is New Lock() Then
            Throw New Exception
        End If
    End Sub
End Module

Namespace System.Threading
    Public Class Lock
    End Class
End Namespace
]]>.Value
            Dim comp = CreateCompilation(source, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(comp, expectedOutput:="12345")
            verifier.Diagnostics.AssertTheseDiagnostics(<![CDATA[
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        If ReferenceEquals(l, l2) Then
                           ~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        If ReferenceEquals(l, l2) Then
                              ~~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        If (CObj(l)) Is l2 Then
                 ~
BC42508: A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in SyncLock statement.
        If (CObj(l)) IsNot l2 Then
                 ~
]]>)
        End Sub
    End Class
End Namespace
