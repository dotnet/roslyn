' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CodeGenClosureLambdaTests
        Inherits BasicTestBase

        <WorkItem(546416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546416")>
        <Fact>
        Public Sub TestAnonymousTypeInsideGroupBy_Enumerable()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim db As New DB()
        Dim q0 = db.Products.GroupBy(Function(p)
                                         Return New With {.Conditional =
                                                 If(False,
                                                     New With {p.ProductID, p.ProductName, p.SupplierID},
                                                     New With {p.ProductID, p.ProductName, p.SupplierID})}
                                     End Function).ToList()
    End Sub
End Module

Public Class Product
    Public ProductID As Integer
    Public ProductName As String
    Public SupplierID As Integer
End Class

Public Class DB
    Public Products As IEnumerable(Of Product) = New List(Of Product)()
End Class
    </file>
</compilation>, expectedOutput:="")
        End Sub

        <WorkItem(546538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546538")>
        <WorkItem(546416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546416")>
        <Fact()>
        Public Sub TestAnonymousTypeInsideGroupBy_Queryable_1()
            Dim compilation =
                CompilationUtils.CreateEmptyCompilationWithReferences(
                    <compilation>
                        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim db As New DB()
        Dim q0 = db.Products.GroupBy(Function(p)
                                         Return New With {.Conditional =
                                                 If(False,
                                                     New With {p.ProductID, p.ProductName, p.SupplierID},
                                                     New With {p.ProductID, p.ProductName, p.SupplierID})}
                                     End Function).ToList()
    End Sub
End Module

Public Class Product
    Public ProductID As Integer
    Public ProductName As String
    Public SupplierID As Integer
End Class

Public Class DB
    Public Products As IQueryable(Of Product) ' = New ...(Of Product)()
End Class
                        </file>
                    </compilation>,
                    references:=DefaultVbReferences,
                    options:=TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36675: Statement lambdas cannot be converted to expression trees.
        Dim q0 = db.Products.GroupBy(Function(p)
                                     ~~~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(546538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546538")>
        <Fact()>
        Public Sub TestAnonymousTypeInsideGroupBy_Queryable_2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Try
            Dim db As New DB()
            Dim q0 = db.Products.GroupBy(Function(p) New With {.Conditional =
                                                     If(False,
                                                         New With {p.ProductID, p.ProductName, p.SupplierID},
                                                         New With {p.ProductID, p.ProductName, p.SupplierID})}).ToList()
        Catch e As Exception
            System.Console.WriteLine("Exception")
        End Try
    End Sub
End Module

Public Class Product
    Public ProductID As Integer
    Public ProductName As String
    Public SupplierID As Integer
End Class

Public Class DB
    Public Products As IQueryable(Of Product) ' = New ...(Of Product)()
End Class
    </file>
</compilation>, expectedOutput:="Exception")
        End Sub

        <Fact>
        Public Sub ClosureSimple()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Public Sub Main(args As String())
        Dim d1 As Action = Sub() Console.Write(1)
        d1.Invoke()

        Dim d2 As Action = Sub() Console.Write(2)
        d2.Invoke()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="12")

            c.VerifyIL("M1.Main", <![CDATA[
{
  // Code size       83 (0x53)
  .maxstack  2
  IL_0000:  ldsfld     "M1._Closure$__.$I0-0 As System.Action"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "M1._Closure$__.$I0-0 As System.Action"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "M1._Closure$__.$I As M1._Closure$__"
  IL_0013:  ldftn      "Sub M1._Closure$__._Lambda$__0-0()"
  IL_0019:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "M1._Closure$__.$I0-0 As System.Action"
  IL_0024:  callvirt   "Sub System.Action.Invoke()"
  IL_0029:  ldsfld     "M1._Closure$__.$I0-1 As System.Action"
  IL_002e:  brfalse.s  IL_0037
  IL_0030:  ldsfld     "M1._Closure$__.$I0-1 As System.Action"
  IL_0035:  br.s       IL_004d
  IL_0037:  ldsfld     "M1._Closure$__.$I As M1._Closure$__"
  IL_003c:  ldftn      "Sub M1._Closure$__._Lambda$__0-1()"
  IL_0042:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0047:  dup
  IL_0048:  stsfld     "M1._Closure$__.$I0-1 As System.Action"
  IL_004d:  callvirt   "Sub System.Action.Invoke()"
  IL_0052:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ClosureSimpleLift()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Public Sub Main()
        Dim X as integer = 3

        Dim d1 As Action = Sub() Console.Write(X)
        d1.Invoke()

        X = X + X
        d1.Invoke()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="36")

            c.VerifyIL("M1.Main", <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  4
  .locals init (M1._Closure$__0-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub M1._Closure$__0-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.3
  IL_0008:  stfld      "M1._Closure$__0-0.$VB$Local_X As Integer"
  IL_000d:  ldloc.0
  IL_000e:  ldftn      "Sub M1._Closure$__0-0._Lambda$__0()"
  IL_0014:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0019:  dup
  IL_001a:  callvirt   "Sub System.Action.Invoke()"
  IL_001f:  ldloc.0
  IL_0020:  ldloc.0
  IL_0021:  ldfld      "M1._Closure$__0-0.$VB$Local_X As Integer"
  IL_0026:  ldloc.0
  IL_0027:  ldfld      "M1._Closure$__0-0.$VB$Local_X As Integer"
  IL_002c:  add.ovf
  IL_002d:  stfld      "M1._Closure$__0-0.$VB$Local_X As Integer"
  IL_0032:  callvirt   "Sub System.Action.Invoke()"
  IL_0037:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub InInstanceLift()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo()
            Dim X as integer = 3

            Dim d1 As Action = Sub() 
                                Console.Write(X)
                               End Sub 
            d1.Invoke()

            X = X + X
            d1.Invoke()
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="36")
            c.VerifyIL("M1.C1.Goo", <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  4
  .locals init (M1.C1._Closure$__1-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub M1.C1._Closure$__1-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.3
  IL_0008:  stfld      "M1.C1._Closure$__1-0.$VB$Local_X As Integer"
  IL_000d:  ldloc.0
  IL_000e:  ldftn      "Sub M1.C1._Closure$__1-0._Lambda$__0()"
  IL_0014:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0019:  dup
  IL_001a:  callvirt   "Sub System.Action.Invoke()"
  IL_001f:  ldloc.0
  IL_0020:  ldloc.0
  IL_0021:  ldfld      "M1.C1._Closure$__1-0.$VB$Local_X As Integer"
  IL_0026:  ldloc.0
  IL_0027:  ldfld      "M1.C1._Closure$__1-0.$VB$Local_X As Integer"
  IL_002c:  add.ovf
  IL_002d:  stfld      "M1.C1._Closure$__1-0.$VB$Local_X As Integer"
  IL_0032:  callvirt   "Sub System.Action.Invoke()"
  IL_0037:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub InInstanceLiftMeNested()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo()
            Dim X as integer = 3

            Dim d1 As Action = Sub() 
                                    dim y as integer = x
                                    Dim d2 As Action = Sub() 
                                        Print(y)
                                    End Sub 

                                    d2.Invoke()
                               End Sub 
            d1.Invoke()

            X = X + X
            d1.Invoke()
        End Sub

        Public Sub Print(x as integer)
            Console.Write(X)
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="36").
    VerifyIL("M1.C1.Goo",
            <![CDATA[
{
  // Code size       63 (0x3f)
  .maxstack  4
  .locals init (M1.C1._Closure$__1-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub M1.C1._Closure$__1-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      "M1.C1._Closure$__1-0.$VB$Me As M1.C1"
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.3
  IL_000f:  stfld      "M1.C1._Closure$__1-0.$VB$Local_X As Integer"
  IL_0014:  ldloc.0
  IL_0015:  ldftn      "Sub M1.C1._Closure$__1-0._Lambda$__0()"
  IL_001b:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0020:  dup
  IL_0021:  callvirt   "Sub System.Action.Invoke()"
  IL_0026:  ldloc.0
  IL_0027:  ldloc.0
  IL_0028:  ldfld      "M1.C1._Closure$__1-0.$VB$Local_X As Integer"
  IL_002d:  ldloc.0
  IL_002e:  ldfld      "M1.C1._Closure$__1-0.$VB$Local_X As Integer"
  IL_0033:  add.ovf
  IL_0034:  stfld      "M1.C1._Closure$__1-0.$VB$Local_X As Integer"
  IL_0039:  callvirt   "Sub System.Action.Invoke()"
  IL_003e:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub InInstanceLiftParamNested()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(x as integer)
            Dim d1 As Action(of Integer) = Sub(y) 
                                    Dim d2 As Action(of Integer) = Sub(z) 
                                        Print(x + y + z)
                                    End Sub 

                                    d2.Invoke(y + 1)
                               End Sub 
            d1.Invoke(x + 1)

            X = X + X
            Console.Write(" ")

            d1.Invoke(x + 1)
        End Sub

        Public Sub Print(x as integer)
            Console.Write(X)
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="12 21").
    VerifyIL("M1.C1.Goo",
            <![CDATA[
{
  // Code size       89 (0x59)
  .maxstack  4
  .locals init (M1.C1._Closure$__1-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub M1.C1._Closure$__1-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      "M1.C1._Closure$__1-0.$VB$Me As M1.C1"
  IL_000d:  ldloc.0
  IL_000e:  ldarg.1
  IL_000f:  stfld      "M1.C1._Closure$__1-0.$VB$Local_x As Integer"
  IL_0014:  ldloc.0
  IL_0015:  ldftn      "Sub M1.C1._Closure$__1-0._Lambda$__0(Integer)"
  IL_001b:  newobj     "Sub System.Action(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0020:  dup
  IL_0021:  ldloc.0
  IL_0022:  ldfld      "M1.C1._Closure$__1-0.$VB$Local_x As Integer"
  IL_0027:  ldc.i4.1
  IL_0028:  add.ovf
  IL_0029:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_002e:  ldloc.0
  IL_002f:  ldloc.0
  IL_0030:  ldfld      "M1.C1._Closure$__1-0.$VB$Local_x As Integer"
  IL_0035:  ldloc.0
  IL_0036:  ldfld      "M1.C1._Closure$__1-0.$VB$Local_x As Integer"
  IL_003b:  add.ovf
  IL_003c:  stfld      "M1.C1._Closure$__1-0.$VB$Local_x As Integer"
  IL_0041:  ldstr      " "
  IL_0046:  call       "Sub System.Console.Write(String)"
  IL_004b:  ldloc.0
  IL_004c:  ldfld      "M1.C1._Closure$__1-0.$VB$Local_x As Integer"
  IL_0051:  ldc.i4.1
  IL_0052:  add.ovf
  IL_0053:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_0058:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ClosureNested()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Delegate Function D(i As Integer, j as integer) As D

    Public Sub Main(args As String())
        Dim d1 As D = Function(a, b) Function(c, d) Function(e, f)
                                                  System.Console.Write(a + b + c + d + e + f)
                                                  Return Nothing
                                              End Function

        d1.Invoke(600000, 50000).Invoke(4000, 300).Invoke(20, 1)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="654321")

            c.VerifyIL("M1._Closure$__1-0._Lambda$__1", <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     "Sub M1._Closure$__1-1..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1._Closure$__1-1.$VB$NonLocal_$VB$Closure_2 As M1._Closure$__1-0"
  IL_000c:  dup
  IL_000d:  ldarg.1
  IL_000e:  stfld      "M1._Closure$__1-1.$VB$Local_c As Integer"
  IL_0013:  dup
  IL_0014:  ldarg.2
  IL_0015:  stfld      "M1._Closure$__1-1.$VB$Local_d As Integer"
  IL_001a:  ldftn      "Function M1._Closure$__1-1._Lambda$__2(Integer, Integer) As M1.D"
  IL_0020:  newobj     "Sub M1.D..ctor(Object, System.IntPtr)"
  IL_0025:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ClosureNestedInstance()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Delegate Function D(i As Integer, j as integer) As D

    Public Sub Main()
        dim c as Cls1 = new Cls1
        c.Goo()
    End Sub

    public Class cls1
        public Sub Goo()
            Dim d1 As D = Function(a, b) Function(c, d) Function(e, f)
                                                      System.Console.Write(a + b + c + d + e + f)
                                                      System.Console.Write(Me)
                                                      Return Nothing
                                                  End Function

            d1.Invoke(600000, 50000).Invoke(4000, 300).Invoke(20, 1)
        End Sub
    End Class
End Module
    </file>
</compilation>, expectedOutput:="654321M1+cls1").
    VerifyIL("M1.cls1._Closure$__1-1._Lambda$__2",
            <![CDATA[
{
  // Code size       64 (0x40)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.cls1._Closure$__1-1.$VB$NonLocal_$VB$Closure_2 As M1.cls1._Closure$__1-0"
  IL_0006:  ldfld      "M1.cls1._Closure$__1-0.$VB$Local_a As Integer"
  IL_000b:  ldarg.0
  IL_000c:  ldfld      "M1.cls1._Closure$__1-1.$VB$NonLocal_$VB$Closure_2 As M1.cls1._Closure$__1-0"
  IL_0011:  ldfld      "M1.cls1._Closure$__1-0.$VB$Local_b As Integer"
  IL_0016:  add.ovf
  IL_0017:  ldarg.0
  IL_0018:  ldfld      "M1.cls1._Closure$__1-1.$VB$Local_c As Integer"
  IL_001d:  add.ovf
  IL_001e:  ldarg.0
  IL_001f:  ldfld      "M1.cls1._Closure$__1-1.$VB$Local_d As Integer"
  IL_0024:  add.ovf
  IL_0025:  ldarg.1
  IL_0026:  add.ovf
  IL_0027:  ldarg.2
  IL_0028:  add.ovf
  IL_0029:  call       "Sub System.Console.Write(Integer)"
  IL_002e:  ldarg.0
  IL_002f:  ldfld      "M1.cls1._Closure$__1-1.$VB$NonLocal_$VB$Closure_2 As M1.cls1._Closure$__1-0"
  IL_0034:  ldfld      "M1.cls1._Closure$__1-0.$VB$Me As M1.cls1"
  IL_0039:  call       "Sub System.Console.Write(Object)"
  IL_003e:  ldnull
  IL_003f:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub InvalidGoto()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(x as integer)
            if x > 0
                ' this one is valid
                Dim d2 As Action = Sub() 
                    Console.Write(x)
                End Sub 
                goto label0
            End if

            ' this is an error            
            goto label1

label0:
            if x > 0
                dim y as integer = 1
label1:                
                Dim d1 As Action = Sub() 
                    Console.Write(y)
                End Sub 
            end if
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module
    </file>
</compilation>).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_CannotGotoNonScopeBlocksWithClosure, "goto label1").WithArguments("Goto ", "label1", "label1"))
        End Sub

        <Fact>
        Public Sub InvalidGotoNoImmediateLifting()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(x as integer)
            if x > 0
                ' this one is valid
                goto label0
            End if

            ' this is an error            
            goto label1

label0:
            if x > 0
                dim y as integer = 1
label1:                
                Dim d1 As Action = Sub() 
                    ' we lift "x" which is in a scope outer to both goto and label
                    ' such lifting might work
                    ' this is still considered invalid to not make rules too complicated
                    Console.Write(x)
                End Sub 
            end if
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module
    </file>
</compilation>).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_CannotGotoNonScopeBlocksWithClosure, "goto label1").WithArguments("Goto ", "label1", "label1"))
        End Sub

        <Fact>
        Public Sub InvalidGotoNoLifting()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(x as integer)
            if x > 0
                ' this one is valid
                Dim d2 As Action = Sub() 
                    Console.Write(x)
                End Sub 
                goto label0
            End if

            ' this is not an error since lambda does not lift           
            goto label1

label0:
            if x > 0
                dim y as integer = 1
label1:                
                Dim d1 As Action = Sub() 
                    ' we are not lifting anything for this lambda
                    ' so this is legal
                    Console.Write(1)
                End Sub 
            end if
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="")
        End Sub

        <Fact>
        Public Sub InvalidGotoLiftingAboveGoto()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(x as integer)
            if x &lt; 0
                ' this one is valid
                Dim d2 As Action = Sub() 
                    Console.Write(x)
                End Sub 
                goto label0
            End if

            ' this is an error (Dev10 does not detect because of a bug)
            ' even though we do not have lambda in the goto's block, 
            ' there is a reference to a lifted variable that goes through it
            goto label1

label0:
            Do
                dim y as integer = 1
label1:             
                if x > 0
                    Dim d1 As Action = Sub() 
                        Console.Write(y)
                    End Sub 
                    d1.Invoke()

                    Exit Do
                End If
            Loop
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module

    </file>
</compilation>).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_CannotGotoNonScopeBlocksWithClosure, "goto label1").WithArguments("Goto ", "label1", "label1"))
        End Sub

        <Fact>
        Public Sub InvalidGotoLiftingAboveGoto2()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(x as integer)
            if x &lt; 0
                ' this one is valid
                Dim d2 As Action = Sub() 
                    Console.Write(x)
                End Sub 
                goto label0
            End if

            ' this is an error (Dev10 does not detect because of a bug)
            ' even though we do not have lambda in the goto's block, 
            ' there is a reference to a lifted variable that goes through it
            goto label1

label0:
            if x > 0
                dim y as integer = 1

                While x > 0
label1:             
                    if x > 0
                        Dim d1 As Action = Sub() 
                            Console.Write(y)
                        End Sub 
                        d1.Invoke()
                    End If

                    Exit While
                end While
            end if
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module

    </file>
</compilation>).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_CannotGotoNonScopeBlocksWithClosure, "goto label1").WithArguments("Goto ", "label1", "label1"))
        End Sub

        <Fact>
        Public Sub InvalidGotoLiftingBetweenGotoAndLabel()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(x as integer)
            While x &lt; 0
                ' this one is valid
                Dim d2 As Action = Sub() 
                    Console.Write(x)
                End Sub 
                goto label0
            End While

            ' this is an error (Dev10 does not detect because of a bug)
            ' even though we do not lift from goto's block, 
            ' its parent has a reference to a lifted variable
            goto label1

label0:
            if x > 0
                dim y as integer = 1

                If x > 0
label1:             
                    Dim z As integer = 1
                    Console.WriteLine(z)
                End If

                if x > 0
                    Dim d1 As Action = Sub() 
                        Console.Write(y)
                    End Sub 
                    d1.Invoke()
                End If
            end if
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module

    </file>
</compilation>).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_CannotGotoNonScopeBlocksWithClosure, "goto label1").WithArguments("Goto ", "label1", "label1"))
        End Sub

        <Fact>
        Public Sub InvalidGotoLiftingBetweenGotoAndLabel2()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(x as integer)
            if x &lt; 0
                ' this one is valid
                Dim d2 As Action = Sub() 
                    Console.Write(x)
                End Sub 
                goto label0
            End if

            ' this is an error (Dev10 does not detect because of a bug)
            ' even though we do not lift from goto's block, 
            ' its parent has a reference to a lifted variable
            goto label1

label0:
            if x > 0
                dim y as integer = 1

                While x > 0
label1:             
                    Dim z As integer = 1
                    Console.WriteLine(z)

                    Exit While
                End While

                if x > 0
                    Dim d1 As Action = Sub() 
                        Console.Write(y)
                    End Sub 
                    d1.Invoke()
                End If
            end if
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module

    </file>
</compilation>).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_CannotGotoNonScopeBlocksWithClosure, "goto label1").WithArguments("Goto ", "label1", "label1"))
        End Sub

        <Fact>
        Public Sub InvalidGotoLiftingBetweenGotoAndLabel3()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(x as integer)
            if x &lt; 0
                ' this one is valid
                Dim d2 As Action = Sub() 
                    Console.Write(x)
                End Sub 
                goto label0
            End if

            ' this is an error (Dev10 does not detect because of a bug)
            ' even though we do not have lambda in the goto's block, 
            ' there is a reference to a lifted variable that goes through it

label0:
            if x > 0
                If x > 0
                   ' dummy is needed to make this a true block otherwise it is too easy for analysis
                    dim dummy1 as integer = 1
                    Console.writeline(dummy1)

                    ' this is an error
                    goto label2
                End If

                If x > 0
                    dim dummy2 as integer = 1
                    Console.writeline(dummy2)

                    ' this is an error
                    goto label1 
                End If

                while x > 0           
label1:  
                    dim y as integer = 1

                    if x > 0
                        Dim d1 As Action = Sub() 
                            Console.Write(y)
                        End Sub 
                        d1.Invoke()
                    End If

                    if x > 0
                        dim dummy3 as integer = 1
                        Console.writeline(dummy3)
label2:  
                        ' jumping here could be a problem if y is lifted
                        Console.WriteLine(y)
                    End If
                end While
            end if
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module
    </file>
</compilation>).
            VerifyEmitDiagnostics(
                Diagnostic(ERRID.ERR_CannotGotoNonScopeBlocksWithClosure, "goto label2").WithArguments("Goto ", "label2", "label2"),
                Diagnostic(ERRID.ERR_CannotGotoNonScopeBlocksWithClosure, "goto label1").WithArguments("Goto ", "label1", "label1"))
        End Sub

        <Fact>
        Public Sub InvalidGotoLiftingBetweenGotoAndLabel4()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(x as integer)
            if x &lt; 0
                ' this one is valid
                Dim d2 As Action = Sub() 
                    Console.Write(x)
                End Sub 
                goto label0
            End if

label0:
            If x > 0
                ' dummy is needed to make this a true block otherwise it is too easy for analysis
                dim dummy1 as integer = 1
                Console.writeline(dummy1)

                ' this is NOT an error
                goto label2
            End If

            While x > 0
                dim dummy2 as integer = 1
                Console.writeline(dummy2)

                ' this is NOT an error
                goto label1 
            End While

            While x > 0           
label1:  
                dim y as integer = 1

                if x > 0
                    Dim d1 As Action = Sub() 
                        ' NO LIFTING HERE
                        Console.Write(1)
                    End Sub 
                    d1.Invoke()
                End If

                While x > 0
                    dim dummy3 as integer = 1
                    Console.writeline(dummy3)
                    Exit Sub
label2:  
                    ' x is lifted, but not through any lambdas below goto
                    ' so this location is as safe as goto itself
                    Console.WriteLine(x)
                End While
            end While
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        Try
            x.Goo(3)
        Catch ex As Exception
            Console.WriteLine(ex)
            Console.WriteLine(ex.Message)
            Console.WriteLine(ex.StackTrace)
        End Try
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
3
1
]]>)
        End Sub

        <Fact>
        Public Sub SimpleGenericLambda()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        Shared X as integer

        public Sub Goo(of T)()
            Dim d1 As Action = Sub() 
                                Console.Write(X)
                               End Sub 
            d1.Invoke()        
            X = X + 5
            d1.Invoke()
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(of Integer)()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="05").
    VerifyIL("M1.C1.Goo",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  3
  IL_0000:  ldsfld     "M1.C1._Closure$__2(Of T).$I2-0 As System.Action"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "M1.C1._Closure$__2(Of T).$I2-0 As System.Action"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "M1.C1._Closure$__2(Of T).$I As M1.C1._Closure$__2(Of T)"
  IL_0013:  ldftn      "Sub M1.C1._Closure$__2(Of T)._Lambda$__2-0()"
  IL_0019:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "M1.C1._Closure$__2(Of T).$I2-0 As System.Action"
  IL_0024:  dup
  IL_0025:  callvirt   "Sub System.Action.Invoke()"
  IL_002a:  ldsfld     "M1.C1.X As Integer"
  IL_002f:  ldc.i4.5
  IL_0030:  add.ovf
  IL_0031:  stsfld     "M1.C1.X As Integer"
  IL_0036:  callvirt   "Sub System.Action.Invoke()"
  IL_003b:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub StaticClosureSerializable()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Public Sub Main()
        Dim x as Func(of Integer) = Function() 42
        System.Console.Write(x.Target.GetType().IsSerializable())

        Dim y as Func(of Integer) = Function() x()
        System.Console.WriteLine(y.Target.GetType().IsSerializable())
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="TrueFalse", options:=TestOptions.ReleaseExe)

        End Sub

        <Fact>
        Public Sub StaticClosureSerializableD()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Public Sub Main()
        Dim x as Func(of Integer) = Function() 42
        System.Console.Write(x.Target.GetType().IsSerializable())

        Dim y as Func(of Integer) = Function() x()
        System.Console.WriteLine(y.Target.GetType().IsSerializable())
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="TrueFalse", options:=TestOptions.DebugExe)

        End Sub

        <Fact>
        Public Sub SimpleGenericClosure()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public shared Sub Goo(of T)()
            Dim X as integer
            Dim d1 As Action = Sub() 
                                Console.Write(X)
                               End Sub 
            d1.Invoke()        
            X = X + 5
            d1.Invoke()
        End Sub
    End Class

    Public Sub Main()
        C1.Goo(of Integer)()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="05").
    VerifyIL("M1.C1.Goo",
            <![CDATA[
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (System.Action V_0) //d1
  IL_0000:  newobj     "Sub M1.C1._Closure$__1-0(Of T)..ctor()"
  IL_0005:  dup
  IL_0006:  ldftn      "Sub M1.C1._Closure$__1-0(Of T)._Lambda$__0()"
  IL_000c:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  callvirt   "Sub System.Action.Invoke()"
  IL_0018:  dup
  IL_0019:  ldfld      "M1.C1._Closure$__1-0(Of T).$VB$Local_X As Integer"
  IL_001e:  ldc.i4.5
  IL_001f:  add.ovf
  IL_0020:  stfld      "M1.C1._Closure$__1-0(Of T).$VB$Local_X As Integer"
  IL_0025:  ldloc.0
  IL_0026:  callvirt   "Sub System.Action.Invoke()"
  IL_002b:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub SimpleGenericClosureWithLocals()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public shared Sub Goo(of T)(p as T)
            Dim X as T = nothing
            Dim d1 As Action = Sub() 
                                Console.Write(X.ToString())
                               End Sub 
            d1.Invoke()       
            X = p
            d1.Invoke()       
        End Sub
    End Class

    Public Sub Main()
        C1.Goo(of Integer)(42)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="042").
    VerifyIL("M1.C1.Goo",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (System.Action V_0) //d1
  IL_0000:  newobj     "Sub M1.C1._Closure$__1-0(Of T)..ctor()"
  IL_0005:  dup
  IL_0006:  ldflda     "M1.C1._Closure$__1-0(Of T).$VB$Local_X As T"
  IL_000b:  initobj    "T"
  IL_0011:  dup
  IL_0012:  ldftn      "Sub M1.C1._Closure$__1-0(Of T)._Lambda$__0()"
  IL_0018:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001d:  stloc.0
  IL_001e:  ldloc.0
  IL_001f:  callvirt   "Sub System.Action.Invoke()"
  IL_0024:  ldarg.0
  IL_0025:  stfld      "M1.C1._Closure$__1-0(Of T).$VB$Local_X As T"
  IL_002a:  ldloc.0
  IL_002b:  callvirt   "Sub System.Action.Invoke()"
  IL_0030:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub SimpleGenericClosureWithParams()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public shared Sub Goo(of T)(p as T)
            Dim d1 As Action = Sub() 
                                Console.Write(p.ToString())
                               End Sub 
            d1.Invoke()       
            p = nothing
            d1.Invoke()       
        End Sub
    End Class

    Public Sub Main()
        C1.Goo(of Integer)(42)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="420").
    VerifyIL("M1.C1.Goo",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  3
  .locals init (System.Action V_0) //d1
  IL_0000:  newobj     "Sub M1.C1._Closure$__1-0(Of T)..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.C1._Closure$__1-0(Of T).$VB$Local_p As T"
  IL_000c:  dup
  IL_000d:  ldftn      "Sub M1.C1._Closure$__1-0(Of T)._Lambda$__0()"
  IL_0013:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0018:  stloc.0
  IL_0019:  ldloc.0
  IL_001a:  callvirt   "Sub System.Action.Invoke()"
  IL_001f:  ldflda     "M1.C1._Closure$__1-0(Of T).$VB$Local_p As T"
  IL_0024:  initobj    "T"
  IL_002a:  ldloc.0
  IL_002b:  callvirt   "Sub System.Action.Invoke()"
  IL_0030:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub SimpleGenericClosureWithParamsAndTemp()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        Class C2(Of U)
            Public Shared Property M(i As U()) As U()
                Get
                    Return i
                End Get
                Set(value As U())
                End Set
            End Property
        End Class

        Private Shared Sub Test(Of M)(ByRef x As M())
            Console.Write(x(0).ToString)
        End Sub

        Public Shared Sub Goo(Of T)(p As T())
            Dim d1 As Action = Sub()
                                   Test(Of T)(C2(Of T).M(p))
                               End Sub
            d1.Invoke()
        End Sub
    End Class

    Public Sub Main()
        C1.Goo(Of Integer)(New Integer() {42})
    End Sub
End Module
    </file>
</compilation>

            Dim c = CompileAndVerify(source, expectedOutput:="42")

            c.VerifyIL("M1.C1._Closure$__3-0(Of $CLS0)._Lambda$__0()", <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init ($CLS0() V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.C1._Closure$__3-0(Of $CLS0).$VB$Local_p As $CLS0()"
  IL_0006:  dup
  IL_0007:  call       "Function M1.C1.C2(Of $CLS0).get_M($CLS0()) As $CLS0()"
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Sub M1.C1.Test(Of $CLS0)(ByRef $CLS0())"
  IL_0014:  ldloc.0
  IL_0015:  call       "Sub M1.C1.C2(Of $CLS0).set_M($CLS0(), $CLS0())"
  IL_001a:  ret
}
]]>)
        End Sub

        'NOTE: it is important that we do not capture outer frame in the inner one here
        <Fact>
        Public Sub SimpleGenericClosureWithLocalsAndParams()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public shared Sub Goo(of T)(p as T)
            Dim d1 As Action = Sub() 
                    Dim X as T = p

                    Dim d2 As Action = Sub() 
                           Console.Write(X.ToString())
                         End Sub

                    d2.Invoke()       
                    X = nothing
                    d2.Invoke()       
                End Sub 

            d1.Invoke()       
            d1.Invoke()
            p = nothing
            d1.Invoke()      
        End Sub
    End Class

    Public Sub Main()
        C1.Goo(of Integer)(42)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="42042000").
    VerifyIL("M1.C1._Closure$__1-0(Of $CLS0)._Lambda$__0()",
            <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (System.Action V_0) //d2
  IL_0000:  newobj     "Sub M1.C1._Closure$__1-1(Of $CLS0)..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  ldfld      "M1.C1._Closure$__1-0(Of $CLS0).$VB$Local_p As $CLS0"
  IL_000c:  stfld      "M1.C1._Closure$__1-1(Of $CLS0).$VB$Local_X As $CLS0"
  IL_0011:  dup
  IL_0012:  ldftn      "Sub M1.C1._Closure$__1-1(Of $CLS0)._Lambda$__1()"
  IL_0018:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001d:  stloc.0
  IL_001e:  ldloc.0
  IL_001f:  callvirt   "Sub System.Action.Invoke()"
  IL_0024:  ldflda     "M1.C1._Closure$__1-1(Of $CLS0).$VB$Local_X As $CLS0"
  IL_0029:  initobj    "$CLS0"
  IL_002f:  ldloc.0
  IL_0030:  callvirt   "Sub System.Action.Invoke()"
  IL_0035:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub SimpleGenericClosureWithLocalsParamsAndParent()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public shared Sub Goo(of T, U)(p as T, p1 as U)
            Dim d1 As Action = Sub() 
                    Dim X as T = p
                    Dim d2 As Action = Sub() 
                           Console.Write(X.ToString())

                           ' this will require lifting parent frame pointer
                           ' since "p1" lives above parent lambda
                           Console.Write(p1.ToString())
                         End Sub

                    d2.Invoke()       
                    X = nothing
                    d2.Invoke()       
                End Sub 

            d1.Invoke()       
            d1.Invoke()
            p = nothing
            d1.Invoke()      
        End Sub
    End Class

    Public Sub Main()
        C1.Goo(of Integer, String)(42, "#"c)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="42#0#42#0#0#0#").
    VerifyIL("M1.C1.Goo",
            <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init (System.Action V_0) //d1
  IL_0000:  newobj     "Sub M1.C1._Closure$__1-0(Of T, U)..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "M1.C1._Closure$__1-0(Of T, U).$VB$Local_p As T"
  IL_000c:  dup
  IL_000d:  ldarg.1
  IL_000e:  stfld      "M1.C1._Closure$__1-0(Of T, U).$VB$Local_p1 As U"
  IL_0013:  dup
  IL_0014:  ldftn      "Sub M1.C1._Closure$__1-0(Of T, U)._Lambda$__0()"
  IL_001a:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001f:  stloc.0
  IL_0020:  ldloc.0
  IL_0021:  callvirt   "Sub System.Action.Invoke()"
  IL_0026:  ldloc.0
  IL_0027:  callvirt   "Sub System.Action.Invoke()"
  IL_002c:  ldflda     "M1.C1._Closure$__1-0(Of T, U).$VB$Local_p As T"
  IL_0031:  initobj    "T"
  IL_0037:  ldloc.0
  IL_0038:  callvirt   "Sub System.Action.Invoke()"
  IL_003d:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub SimpleGenericClosureWithLocalsParamsAndParent2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        Public Shared Sub Goo(Of T, U)(p As T, p1 As U)
            Dim d1 As Action = Sub()
                                   Dim d2 As Action(Of T) = Sub(X As T)
                                                                Console.Write(X.ToString())
                                                                Console.Write(p1.ToString())
                                                            End Sub

                                   d2.Invoke(p)
                                   p1 = Nothing
                                   d2.Invoke(p)
                               End Sub

            d1.Invoke()
            d1.Invoke()
            p = Nothing
            d1.Invoke()
        End Sub
    End Class

    Public Sub Main()
        C1.Goo(Of Integer, Integer)(42, 333)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="423334204204200000").
    VerifyIL("M1.C1._Closure$__1-0(Of $CLS0, $CLS1)._Lambda$__0",
            <![CDATA[
{
  // Code size       73 (0x49)
  .maxstack  3
  .locals init (System.Action(Of $CLS0) V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.C1._Closure$__1-0(Of $CLS0, $CLS1).$I1 As System.Action(Of $CLS0)"
  IL_0006:  brfalse.s  IL_0010
  IL_0008:  ldarg.0
  IL_0009:  ldfld      "M1.C1._Closure$__1-0(Of $CLS0, $CLS1).$I1 As System.Action(Of $CLS0)"
  IL_000e:  br.s       IL_0025
  IL_0010:  ldarg.0
  IL_0011:  ldarg.0
  IL_0012:  ldftn      "Sub M1.C1._Closure$__1-0(Of $CLS0, $CLS1)._Lambda$__1($CLS0)"
  IL_0018:  newobj     "Sub System.Action(Of $CLS0)..ctor(Object, System.IntPtr)"
  IL_001d:  dup
  IL_001e:  stloc.0
  IL_001f:  stfld      "M1.C1._Closure$__1-0(Of $CLS0, $CLS1).$I1 As System.Action(Of $CLS0)"
  IL_0024:  ldloc.0
  IL_0025:  dup
  IL_0026:  ldarg.0
  IL_0027:  ldfld      "M1.C1._Closure$__1-0(Of $CLS0, $CLS1).$VB$Local_p As $CLS0"
  IL_002c:  callvirt   "Sub System.Action(Of $CLS0).Invoke($CLS0)"
  IL_0031:  ldarg.0
  IL_0032:  ldflda     "M1.C1._Closure$__1-0(Of $CLS0, $CLS1).$VB$Local_p1 As $CLS1"
  IL_0037:  initobj    "$CLS1"
  IL_003d:  ldarg.0
  IL_003e:  ldfld      "M1.C1._Closure$__1-0(Of $CLS0, $CLS1).$VB$Local_p As $CLS0"
  IL_0043:  callvirt   "Sub System.Action(Of $CLS0).Invoke($CLS0)"
  IL_0048:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub SimpleGenericClosureWithLocalsParamsAndParent3()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1(Of G)
        Public Sub Print(Of TPrint)(x As TPrint)
            Console.Write(x.ToString())
        End Sub

        Public Shared Sub PrintShared(Of TPrint)(x As TPrint, y As G)
            Console.Write(x.ToString())
            Console.Write(y.ToString())
        End Sub

        Public Sub Goo(Of TFun1, TFun2)(p As TFun1, p1 As TFun2)
            Dim d1 As Action = Sub()
                                   Dim d2 As Action(Of TFun1) = Sub(X As TFun1)
                                                                    Print(Of TFun1)(X)

                                                                    ' this will require lifting parent frame pointer
                                                                    ' since "p1" lives above parent lambda
                                                                    C1(Of TFun2).PrintShared(Of TFun1)(X, p1)
                                                                End Sub

                                   d2.Invoke(p)
                                   p1 = Nothing
                                   d2.Invoke(p)
                               End Sub

            d1.Invoke()
            d1.Invoke()
            p = Nothing
            d1.Invoke()
        End Sub
    End Class

    Public Sub Main()
        Dim inst As New C1(Of Integer)
        inst.Goo(Of Integer, Integer)(42, 333)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="4242333424204242042420000000").
    VerifyIL("M1.C1(Of G)._Closure$__3-0(Of $CLS0, $CLS1)._Lambda$__1($CLS0)",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "M1.C1(Of G)._Closure$__3-0(Of $CLS0, $CLS1).$VB$Me As M1.C1(Of G)"
  IL_0006:  ldarg.1
  IL_0007:  call       "Sub M1.C1(Of G).Print(Of $CLS0)($CLS0)"
  IL_000c:  ldarg.1
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "M1.C1(Of G)._Closure$__3-0(Of $CLS0, $CLS1).$VB$Local_p1 As $CLS1"
  IL_0013:  call       "Sub M1.C1(Of $CLS1).PrintShared(Of $CLS0)($CLS0, $CLS1)"
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PropagatingValueSimple()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
        
Module Module1
    Sub Main()
        Dim a(5) As Action
        If True Then
            Dim i As Integer
            i = 0
            GoTo Test
loopBody:
            If True Then
                ' every iteration gets its own instance of lifted x, 
                ' but the value of x is copied over from the previous iteration
                Dim x As Integer
 
                a(i) = Sub()
                           Console.Write(x)
                       End Sub
                x = x + 1
            End If
Increment:
            i = i + 1
Test:
            If i > 5 Then
                GoTo break
            End If
            GoTo loopBody
        End If
break:
        a(0).Invoke()
        a(1).Invoke()
        a(2).Invoke()
        a(3).Invoke()
        a(4).Invoke()
        a(5).Invoke()
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="123456").
    VerifyIL("Module1._Closure$__0-0..ctor",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.1
  IL_0007:  brfalse.s  IL_0015
  IL_0009:  ldarg.0
  IL_000a:  ldarg.1
  IL_000b:  ldfld      "Module1._Closure$__0-0.$VB$Local_x As Integer"
  IL_0010:  stfld      "Module1._Closure$__0-0.$VB$Local_x As Integer"
  IL_0015:  ret
}
]]>)
        End Sub

        'No loops here, should not copy construct
        <Fact>
        Public Sub PropagatingValueNegative()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
        
Module Module1
    Sub Main()
        Dim a(5) As Action
        If True Then
            Dim i As Integer
            i = 0
            ' COMMENTED OUT!!!!!! GoTo Test
loopBody:
            If True Then
                ' every iteration gets its own instance of lifted x, 
                ' but the value of x is copied over from the previous iteration
                Dim x As Integer
 
                a(i) = Sub()
                           Console.Write(x)
                       End Sub
                x = x + 1
            End If
Increment:
            i = i + 1
Test:
            If i > 5 Then
                GoTo break
            End If
            ' COMMENTED OUT!!!!!! GoTo loopBody
        End If
break:
        a(0).Invoke()
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="1").
    VerifyIL("Module1._Closure$__0-0..ctor",
            <![CDATA[
{
// Code size        7 (0x7)
.maxstack  1
IL_0000:  ldarg.0
IL_0001:  call       "Sub Object..ctor()"
IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PropagatingValueWhile()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
        
Module Module1
    Sub Main()
        Dim a(5) As Action
        Dim i As Integer
        i = 0

        While i &lt; 6
            ' every iteration gets its own instance of lifted x, 
            ' but the value of x is copied over from the previous iteration
            Dim x As Integer
 
            a(i) = Sub()
                        Console.Write(x)
                    End Sub
            x = x + 1

            i = i + 1
        End While

        a(0).Invoke()
        a(1).Invoke()
        a(2).Invoke()
        a(3).Invoke()
        a(4).Invoke()
        a(5).Invoke()
    End Sub
End Module

    </file>
</compilation>, expectedOutput:="123456").
    VerifyIL("Module1._Closure$__0-0..ctor",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.1
  IL_0007:  brfalse.s  IL_0015
  IL_0009:  ldarg.0
  IL_000a:  ldarg.1
  IL_000b:  ldfld      "Module1._Closure$__0-0.$VB$Local_x As Integer"
  IL_0010:  stfld      "Module1._Closure$__0-0.$VB$Local_x As Integer"
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PropagatingValueWhileReenter()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        Dim a(5) As Action
        Dim i As Integer = 0

tryAgain:
        While i &lt; 2
            Dim j As Integer
            While j &lt; 3
                ' every iteration gets its own instance of lifted x, 
                ' but the value of x is copied over from the previous iteration
                Dim x As Integer = x + 1

                If x Mod 2 = 0 Then
                    GoTo tryAgain
                End If

                a(i * 3 + j) = Sub()
                                   Console.Write(x)
                               End Sub

                j = j + 1
            End While

            i = i + 1
            j = 0
        End While

            a(0).Invoke()
            a(1).Invoke()
            a(2).Invoke()
            a(3).Invoke()
            a(4).Invoke()
            a(5).Invoke()
        End Sub
End Module
    </file>
</compilation>, expectedOutput:="1357911").
    VerifyIL("Module1._Closure$__0-0..ctor",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.1
  IL_0007:  brfalse.s  IL_0015
  IL_0009:  ldarg.0
  IL_000a:  ldarg.1
  IL_000b:  ldfld      "Module1._Closure$__0-0.$VB$Local_x As Integer"
  IL_0010:  stfld      "Module1._Closure$__0-0.$VB$Local_x As Integer"
  IL_0015:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub InvalidLiftByRef()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        public Sub Goo(ByRef x as integer)
            if x > 0
                ' this one is valid
                Dim d2 As Action = Sub() 
                    Console.Write(x)
                End Sub 
            End if
        End Sub
    End Class

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module
    </file>
</compilation>).
    VerifyDiagnostics(Diagnostic(ERRID.ERR_CannotLiftByRefParamLambda1, "x").WithArguments("x"))

        End Sub

        <Fact>
        Public Sub InvalidLiftMeInStruct()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Structure C1
        public Sub Goo(ByVal x as integer)
            if x > 0
                ' this one is valid
                Dim d2 As Action = Sub() 
                    Console.Write(Me.ToString())
                End Sub 
            End if
        End Sub
    End Structure

    Public Sub Main()
        Dim x as C1 = New C1
        x.Goo(3)
    End Sub
End Module
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics(<![CDATA[
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                    Console.Write(Me.ToString())
                                  ~~
]]>)

        End Sub

        <Fact>
        Public Sub InvalidLiftByRestrictedType()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System

Module M1
    Class C1
        Public Sub Goo(ByVal p As Integer)
            Dim lifted As ArgIterator = Nothing
            If p > 0 Then
                ' this one is valid
                Dim d2 As Action = Sub()
                                       lifted = Nothing
                                   End Sub
            End If
        End Sub
    End Class

    Public Sub Main()
        Dim x As C1 = New C1
        x.Goo(3)
    End Sub
End Module
    </file>
</compilation>).
            VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_CannotLiftRestrictedTypeLambda, "lifted").WithArguments("System.ArgIterator"))

        End Sub

        <Fact>
        Public Sub PropagatingValueSimpleFor()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
        
Module Module1
    Sub Main()
        Dim a(5) As Action
        For i As Integer = 0 To 5
            ' every iteration gets its own instance of lifted x, 
            ' but the value of x is copied over from the previous iteration
            Dim x As Integer

            a(i) = Sub()
                       Console.Write(x)
                   End Sub
            x = x + 1
        Next
        Dump(a)
    End Sub

    Sub Dump(a As Action())
        Call a(0)()
        Call a(1)()
        Call a(2)()
        Call a(3)()
        Call a(4)()
        Call a(5)()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="123456")

            c.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  4
  .locals init (System.Action() V_0, //a
                Integer V_1, //i
                Module1._Closure$__0-0 V_2) //$VB$Closure_0
  IL_0000:  ldc.i4.6
  IL_0001:  newarr     "System.Action"
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  IL_0009:  ldloc.2
  IL_000a:  newobj     "Sub Module1._Closure$__0-0..ctor(Module1._Closure$__0-0)"
  IL_000f:  stloc.2
  IL_0010:  ldloc.0
  IL_0011:  ldloc.1
  IL_0012:  ldloc.2
  IL_0013:  ldftn      "Sub Module1._Closure$__0-0._Lambda$__0()"
  IL_0019:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001e:  stelem.ref
  IL_001f:  ldloc.2
  IL_0020:  ldloc.2
  IL_0021:  ldfld      "Module1._Closure$__0-0.$VB$Local_x As Integer"
  IL_0026:  ldc.i4.1
  IL_0027:  add.ovf
  IL_0028:  stfld      "Module1._Closure$__0-0.$VB$Local_x As Integer"
  IL_002d:  ldloc.1
  IL_002e:  ldc.i4.1
  IL_002f:  add.ovf
  IL_0030:  stloc.1
  IL_0031:  ldloc.1
  IL_0032:  ldc.i4.5
  IL_0033:  ble.s      IL_0009
  IL_0035:  ldloc.0
  IL_0036:  call       "Sub Module1.Dump(System.Action())"
  IL_003b:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub PropagatingValueSimpleForNonConstStep()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
        
Module Module1
    Sub Main()
        Dim a(5) As Action
        Dim S as Integer = 1
        For i As Integer = 0 To 5 Step S
            ' every iteration gets its own instance of lifted x, 
            ' but the value of x is copied over from the previous iteration
            Dim x As Integer

            a(i) = Sub()
                       Console.Write(i)
                       Console.Write(x)
                   End Sub
            x = x + 1
        Next
        Dump(a)
    End Sub

    Sub Dump(a As Action())
        Call a(0)()
        Call a(1)()
        Call a(2)()
        Call a(3)()
        Call a(4)()
        Call a(5)()
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="616263646566")

            c.VerifyIL("Module1._Closure$__0-0..ctor", <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.1
  IL_0007:  brfalse.s  IL_0015
  IL_0009:  ldarg.0
  IL_000a:  ldarg.1
  IL_000b:  ldfld      "Module1._Closure$__0-0.$VB$Local_i As Integer"
  IL_0010:  stfld      "Module1._Closure$__0-0.$VB$Local_i As Integer"
  IL_0015:  ret
}
]]>)
            c.VerifyIL("Module1.Main", <![CDATA[
{
  // Code size      127 (0x7f)
  .maxstack  4
  .locals init (System.Action() V_0, //a
                Integer V_1, //S
                Module1._Closure$__0-0 V_2, //$VB$Closure_0
                Integer V_3,
                Module1._Closure$__0-1 V_4) //$VB$Closure_1
  IL_0000:  ldc.i4.6
  IL_0001:  newarr     "System.Action"
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  stloc.1
  IL_0009:  ldloc.2
  IL_000a:  newobj     "Sub Module1._Closure$__0-0..ctor(Module1._Closure$__0-0)"
  IL_000f:  stloc.2
  IL_0010:  ldloc.2
  IL_0011:  ldloc.1
  IL_0012:  stloc.3
  IL_0013:  ldc.i4.0
  IL_0014:  stfld      "Module1._Closure$__0-0.$VB$Local_i As Integer"
  IL_0019:  br.s       IL_0065
  IL_001b:  ldloc.s    V_4
  IL_001d:  newobj     "Sub Module1._Closure$__0-1..ctor(Module1._Closure$__0-1)"
  IL_0022:  stloc.s    V_4
  IL_0024:  ldloc.s    V_4
  IL_0026:  ldloc.2
  IL_0027:  stfld      "Module1._Closure$__0-1.$VB$NonLocal_$VB$Closure_2 As Module1._Closure$__0-0"
  IL_002c:  ldloc.0
  IL_002d:  ldloc.s    V_4
  IL_002f:  ldfld      "Module1._Closure$__0-1.$VB$NonLocal_$VB$Closure_2 As Module1._Closure$__0-0"
  IL_0034:  ldfld      "Module1._Closure$__0-0.$VB$Local_i As Integer"
  IL_0039:  ldloc.s    V_4
  IL_003b:  ldftn      "Sub Module1._Closure$__0-1._Lambda$__0()"
  IL_0041:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0046:  stelem.ref
  IL_0047:  ldloc.s    V_4
  IL_0049:  ldloc.s    V_4
  IL_004b:  ldfld      "Module1._Closure$__0-1.$VB$Local_x As Integer"
  IL_0050:  ldc.i4.1
  IL_0051:  add.ovf
  IL_0052:  stfld      "Module1._Closure$__0-1.$VB$Local_x As Integer"
  IL_0057:  ldloc.2
  IL_0058:  ldloc.2
  IL_0059:  ldfld      "Module1._Closure$__0-0.$VB$Local_i As Integer"
  IL_005e:  ldloc.3
  IL_005f:  add.ovf
  IL_0060:  stfld      "Module1._Closure$__0-0.$VB$Local_i As Integer"
  IL_0065:  ldloc.3
  IL_0066:  ldc.i4.s   31
  IL_0068:  shr
  IL_0069:  ldloc.2
  IL_006a:  ldfld      "Module1._Closure$__0-0.$VB$Local_i As Integer"
  IL_006f:  xor
  IL_0070:  ldloc.3
  IL_0071:  ldc.i4.s   31
  IL_0073:  shr
  IL_0074:  ldc.i4.5
  IL_0075:  xor
  IL_0076:  ble.s      IL_001b
  IL_0078:  ldloc.0
  IL_0079:  call       "Sub Module1.Dump(System.Action())"
  IL_007e:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub LiftingMeInInitializer()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class C
    Dim A As Action = Sub() Console.Write(ToString)
    Shared Sub Main()
        Dim c As New C()
        c.A()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="C").
    VerifyIL("C..ctor",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.0
  IL_0008:  ldftn      "Sub C._Lambda$__0-0()"
  IL_000e:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0013:  stfld      "C.A As System.Action"
  IL_0018:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub LiftingMeInInitializer1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class C
    Dim A As Action = Sub() Console.Write(ToString)
    Dim B as Action

    Shared Sub Main()
        Dim c As New C(42)
        c.A()
        c.B()
    End Sub

    Sub New(x as integer)
        B = Sub() Console.WriteLine(x)
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="C42").
    VerifyIL("C..ctor",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  3
  .locals init (C._Closure$__3-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub C._Closure$__3-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      "C._Closure$__3-0.$VB$Local_x As Integer"
  IL_000d:  ldarg.0
  IL_000e:  call       "Sub Object..ctor()"
  IL_0013:  ldarg.0
  IL_0014:  ldarg.0
  IL_0015:  ldftn      "Sub C._Lambda$__3-0()"
  IL_001b:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0020:  stfld      "C.A As System.Action"
  IL_0025:  ldarg.0
  IL_0026:  ldloc.0
  IL_0027:  ldftn      "Sub C._Closure$__3-0._Lambda$__1()"
  IL_002d:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0032:  stfld      "C.B As System.Action"
  IL_0037:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub LiftingMeOrParams()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class C
    Shared Sub Main()
    End Sub

    Sub goo(x As Integer)

        Dim A As action = Sub() Console.WriteLine(x)
        A()

        Dim B As action = Sub() Console.WriteLine(Me)
        B()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="").
    VerifyIL("C.goo",
            <![CDATA[
{
  // Code size       46 (0x2e)
  .maxstack  3
  IL_0000:  newobj     "Sub C._Closure$__2-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.1
  IL_0007:  stfld      "C._Closure$__2-0.$VB$Local_x As Integer"
  IL_000c:  ldftn      "Sub C._Closure$__2-0._Lambda$__0()"
  IL_0012:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0017:  callvirt   "Sub System.Action.Invoke()"
  IL_001c:  ldarg.0
  IL_001d:  ldftn      "Sub C._Lambda$__2-1()"
  IL_0023:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0028:  callvirt   "Sub System.Action.Invoke()"
  IL_002d:  ret
}
]]>)
        End Sub

        ' This sample crashes whole VS in Dev10.
        <Fact>
        Public Sub CatchIntoLiftedError()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Class Program
    Shared Function Goo(x As Action) As Boolean
        x()
        Return True
    End Function

    Shared Sub Main()
        Dim ex As Exception = Nothing
        Boo(ex)
    End Sub

    Shared Sub Boo(ByRef ex As Exception)
        Try
            Throw New Exception("blah")

        Catch ex When Goo(Sub()
                              Try
                                  Throw New Exception("pass")
                              Catch ex

                              End Try
                          End Sub)

            Console.Write(ex.Message)
        End Try

    End Sub
End Class
    </file>
</compilation>).
    VerifyDiagnostics(Diagnostic(ERRID.ERR_CannotLiftByRefParamLambda1, "ex").WithArguments("ex"))
        End Sub

        <Fact>
        Public Sub CatchIntoLifted()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class Program
    Private Shared Function AppendMessage(ByRef ex As Exception, ByVal msg As String) As Boolean
        ex = New Exception(ex.Message &amp; msg)
        Return True
    End Function

    Shared Sub Main()
        Dim ex As Exception = Nothing
        Dim a As Action = Sub() Console.WriteLine(ex.Message)

        Try
           Throw new Exception("_try")

        Catch ex When AppendMessage(ex, "_filter")

            ex = New Exception(ex.Message &amp; "_catch")
        End Try

        a()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="_try_filter_catch").
    VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      131 (0x83)
  .maxstack  3
  .locals init (Program._Closure$__2-0 V_0, //$VB$Closure_0
                System.Action V_1, //a
                System.Exception V_2)
  IL_0000:  newobj     "Sub Program._Closure$__2-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldnull
  IL_0008:  stfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
  IL_000d:  ldloc.0
  IL_000e:  ldftn      "Sub Program._Closure$__2-0._Lambda$__0()"
  IL_0014:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0019:  stloc.1
  .try
  {
    IL_001a:  ldstr      "_try"
    IL_001f:  newobj     "Sub System.Exception..ctor(String)"
    IL_0024:  throw
  }
  filter
  {
    IL_0025:  isinst     "System.Exception"
    IL_002a:  dup
    IL_002b:  brtrue.s   IL_0031
    IL_002d:  pop
    IL_002e:  ldc.i4.0
    IL_002f:  br.s       IL_0052
    IL_0031:  dup
    IL_0032:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0037:  stloc.2
    IL_0038:  ldloc.0
    IL_0039:  ldloc.2
    IL_003a:  stfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
    IL_003f:  ldloc.0
    IL_0040:  ldflda     "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
    IL_0045:  ldstr      "_filter"
    IL_004a:  call       "Function Program.AppendMessage(ByRef System.Exception, String) As Boolean"
    IL_004f:  ldc.i4.0
    IL_0050:  cgt.un
    IL_0052:  endfilter
  }  // end filter
  {  // handler
    IL_0054:  pop
    IL_0055:  ldloc.0
    IL_0056:  ldloc.0
    IL_0057:  ldfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
    IL_005c:  callvirt   "Function System.Exception.get_Message() As String"
    IL_0061:  ldstr      "_catch"
    IL_0066:  call       "Function String.Concat(String, String) As String"
    IL_006b:  newobj     "Sub System.Exception..ctor(String)"
    IL_0070:  stfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
    IL_0075:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_007a:  leave.s    IL_007c
  }
  IL_007c:  ldloc.1
  IL_007d:  callvirt   "Sub System.Action.Invoke()"
  IL_0082:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CatchIntoLifted1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class Program
    Private Shared Function Filter(a As Action) As Boolean
        a()
        Return True
    End Function

    Shared Sub Main()
        Dim ex As Exception = Nothing
        Dim a As Action = Sub() Console.WriteLine(ex.Message)

        Try
            Throw new Exception("_try")

        Catch ex When Filter(Sub()
                                 ex = New Exception(ex.Message &amp; "_filter")
                             End Sub)

            ex = New Exception(ex.Message &amp; "_catch")
        End Try

        a()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="_try_filter_catch").
    VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      132 (0x84)
  .maxstack  3
  .locals init (Program._Closure$__2-0 V_0, //$VB$Closure_0
                System.Action V_1, //a
                System.Exception V_2)
  IL_0000:  newobj     "Sub Program._Closure$__2-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldnull
  IL_0008:  stfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
  IL_000d:  ldloc.0
  IL_000e:  ldftn      "Sub Program._Closure$__2-0._Lambda$__0()"
  IL_0014:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0019:  stloc.1
  .try
  {
    IL_001a:  ldstr      "_try"
    IL_001f:  newobj     "Sub System.Exception..ctor(String)"
    IL_0024:  throw
  }
  filter
  {
    IL_0025:  isinst     "System.Exception"
    IL_002a:  dup
    IL_002b:  brtrue.s   IL_0031
    IL_002d:  pop
    IL_002e:  ldc.i4.0
    IL_002f:  br.s       IL_0053
    IL_0031:  dup
    IL_0032:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0037:  stloc.2
    IL_0038:  ldloc.0
    IL_0039:  ldloc.2
    IL_003a:  stfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
    IL_003f:  ldloc.0
    IL_0040:  ldftn      "Sub Program._Closure$__2-0._Lambda$__1()"
    IL_0046:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
    IL_004b:  call       "Function Program.Filter(System.Action) As Boolean"
    IL_0050:  ldc.i4.0
    IL_0051:  cgt.un
    IL_0053:  endfilter
  }  // end filter
  {  // handler
    IL_0055:  pop
    IL_0056:  ldloc.0
    IL_0057:  ldloc.0
    IL_0058:  ldfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
    IL_005d:  callvirt   "Function System.Exception.get_Message() As String"
    IL_0062:  ldstr      "_catch"
    IL_0067:  call       "Function String.Concat(String, String) As String"
    IL_006c:  newobj     "Sub System.Exception..ctor(String)"
    IL_0071:  stfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
    IL_0076:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_007b:  leave.s    IL_007d
  }
  IL_007d:  ldloc.1
  IL_007e:  callvirt   "Sub System.Action.Invoke()"
  IL_0083:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CatchVarLifted1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class Program
    Shared Sub Main()
        Dim a As Action

        Try
            Throw New Exception("pass")
        Catch ex As Exception
            a = Sub() Console.WriteLine(ex.Message)
        End Try

        a()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="pass").
    VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (System.Action V_0, //a
                Program._Closure$__1-0 V_1, //$VB$Closure_0
                System.Exception V_2)
  .try
  {
    IL_0000:  ldstr      "pass"
    IL_0005:  newobj     "Sub System.Exception..ctor(String)"
    IL_000a:  throw
  }
  catch System.Exception
  {
    IL_000b:  dup
    IL_000c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0011:  newobj     "Sub Program._Closure$__1-0..ctor()"
    IL_0016:  stloc.1
    IL_0017:  stloc.2
    IL_0018:  ldloc.1
    IL_0019:  ldloc.2
    IL_001a:  stfld      "Program._Closure$__1-0.$VB$Local_ex As System.Exception"
    IL_001f:  ldloc.1
    IL_0020:  ldftn      "Sub Program._Closure$__1-0._Lambda$__0()"
    IL_0026:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
    IL_002b:  stloc.0
    IL_002c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0031:  leave.s    IL_0033
  }
  IL_0033:  ldloc.0
  IL_0034:  callvirt   "Sub System.Action.Invoke()"
  IL_0039:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CatchVarLifted2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class Program
    Shared Function Goo(x As Action) As Boolean
        x()
        Return True
    End Function

    Shared Sub Main()
        Dim a As Action

        Try
            Throw New Exception("_try")

        Catch ex As Exception When Goo(Sub()
                                           ex = New Exception(ex.Message &amp; "_filter")
                                       End Sub)

            a = Sub() Console.WriteLine(ex.Message)
        End Try

        a()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="_try_filter").
    VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       93 (0x5d)
  .maxstack  2
  .locals init (System.Action V_0, //a
                Program._Closure$__2-0 V_1, //$VB$Closure_0
                System.Exception V_2)
  .try
  {
    IL_0000:  ldstr      "_try"
    IL_0005:  newobj     "Sub System.Exception..ctor(String)"
    IL_000a:  throw
  }
  filter
  {
    IL_000b:  isinst     "System.Exception"
    IL_0010:  dup
    IL_0011:  brtrue.s   IL_0017
    IL_0013:  pop
    IL_0014:  ldc.i4.0
    IL_0015:  br.s       IL_003f
    IL_0017:  dup
    IL_0018:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_001d:  newobj     "Sub Program._Closure$__2-0..ctor()"
    IL_0022:  stloc.1
    IL_0023:  stloc.2
    IL_0024:  ldloc.1
    IL_0025:  ldloc.2
    IL_0026:  stfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
    IL_002b:  ldloc.1
    IL_002c:  ldftn      "Sub Program._Closure$__2-0._Lambda$__0()"
    IL_0032:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
    IL_0037:  call       "Function Program.Goo(System.Action) As Boolean"
    IL_003c:  ldc.i4.0
    IL_003d:  cgt.un
    IL_003f:  endfilter
  }  // end filter
  {  // handler
    IL_0041:  pop
    IL_0042:  ldloc.1
    IL_0043:  ldftn      "Sub Program._Closure$__2-0._Lambda$__1()"
    IL_0049:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
    IL_004e:  stloc.0
    IL_004f:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0054:  leave.s    IL_0056
  }
  IL_0056:  ldloc.0
  IL_0057:  callvirt   "Sub System.Action.Invoke()"
  IL_005c:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CatchVerLifted3()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class Program
    Shared Sub Main()
        Try
            Throw New Exception("xxx")
        Catch e As Exception When (New Func(Of Boolean)(Function() e.Message = "xxx"))()
            Console.Write("pass")
        End Try
    End Sub
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerify(source, expectedOutput:="pass")
            verifier.VerifyIL("Program.Main", <![CDATA[
{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (Program._Closure$__1-0 V_0, //$VB$Closure_0
                System.Exception V_1)
  .try
  {
    IL_0000:  ldstr      "xxx"
    IL_0005:  newobj     "Sub System.Exception..ctor(String)"
    IL_000a:  throw
  }
  filter
  {
    IL_000b:  isinst     "System.Exception"
    IL_0010:  dup
    IL_0011:  brtrue.s   IL_0017
    IL_0013:  pop
    IL_0014:  ldc.i4.0
    IL_0015:  br.s       IL_003f
    IL_0017:  dup
    IL_0018:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_001d:  newobj     "Sub Program._Closure$__1-0..ctor()"
    IL_0022:  stloc.0
    IL_0023:  stloc.1
    IL_0024:  ldloc.0
    IL_0025:  ldloc.1
    IL_0026:  stfld      "Program._Closure$__1-0.$VB$Local_e As System.Exception"
    IL_002b:  ldloc.0
    IL_002c:  ldftn      "Function Program._Closure$__1-0._Lambda$__0() As Boolean"
    IL_0032:  newobj     "Sub System.Func(Of Boolean)..ctor(Object, System.IntPtr)"
    IL_0037:  callvirt   "Function System.Func(Of Boolean).Invoke() As Boolean"
    IL_003c:  ldc.i4.0
    IL_003d:  cgt.un
    IL_003f:  endfilter
  }  // end filter
  {  // handler
    IL_0041:  pop
    IL_0042:  ldstr      "pass"
    IL_0047:  call       "Sub System.Console.Write(String)"
    IL_004c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0051:  leave.s    IL_0053
  }
  IL_0053:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CatchVarLifted4()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class Program
    Shared Function Goo(x As Action) As Boolean
        x()
        Return True
    End Function

    Shared Sub Main()
        Try
            Throw New Exception("blah")

        Catch ex As Exception When Goo(Sub()
                                           Try
                                               Throw New Exception("pass")
                                           Catch ex

                                           End Try
                                       End Sub)

            Console.Write(ex.Message)
        End Try
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="pass").
    VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       90 (0x5a)
  .maxstack  2
  .locals init (Program._Closure$__2-0 V_0, //$VB$Closure_0
                System.Exception V_1)
  .try
  {
    IL_0000:  ldstr      "blah"
    IL_0005:  newobj     "Sub System.Exception..ctor(String)"
    IL_000a:  throw
  }
  filter
  {
    IL_000b:  isinst     "System.Exception"
    IL_0010:  dup
    IL_0011:  brtrue.s   IL_0017
    IL_0013:  pop
    IL_0014:  ldc.i4.0
    IL_0015:  br.s       IL_003f
    IL_0017:  dup
    IL_0018:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_001d:  newobj     "Sub Program._Closure$__2-0..ctor()"
    IL_0022:  stloc.0
    IL_0023:  stloc.1
    IL_0024:  ldloc.0
    IL_0025:  ldloc.1
    IL_0026:  stfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
    IL_002b:  ldloc.0
    IL_002c:  ldftn      "Sub Program._Closure$__2-0._Lambda$__0()"
    IL_0032:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
    IL_0037:  call       "Function Program.Goo(System.Action) As Boolean"
    IL_003c:  ldc.i4.0
    IL_003d:  cgt.un
    IL_003f:  endfilter
  }  // end filter
  {  // handler
    IL_0041:  pop
    IL_0042:  ldloc.0
    IL_0043:  ldfld      "Program._Closure$__2-0.$VB$Local_ex As System.Exception"
    IL_0048:  callvirt   "Function System.Exception.get_Message() As String"
    IL_004d:  call       "Sub System.Console.Write(String)"
    IL_0052:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0057:  leave.s    IL_0059
  }
  IL_0059:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CatchVarLifted_Generic()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.IO

Class Program

    Shared Sub Main()
        F(Of IOException)()
    End Sub

    Shared Sub F(Of T As Exception)() 
        Dim a = New Action(
            Sub()
                Try
                    Throw New IOException("xxx")
                Catch e As T When e.Message = "xxx"
                    Console.Write("pass")
                End Try
            End Sub)
        a()
    End Sub
End Class

    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="pass")
            verifier.VerifyIL("Program._Closure$__2(Of $CLS0)._Lambda$__2-0", <![CDATA[
{
  // Code size       86 (0x56)
  .maxstack  3
  .locals init ($CLS0 V_0) //e
  .try
  {
    IL_0000:  ldstr      "xxx"
    IL_0005:  newobj     "Sub System.IO.IOException..ctor(String)"
    IL_000a:  throw
  }
  filter
  {
    IL_000b:  isinst     "$CLS0"
    IL_0010:  dup
    IL_0011:  brtrue.s   IL_0017
    IL_0013:  pop
    IL_0014:  ldc.i4.0
    IL_0015:  br.s       IL_0041
    IL_0017:  dup
    IL_0018:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_001d:  unbox.any  "$CLS0"
    IL_0022:  stloc.0
    IL_0023:  ldloca.s   V_0
    IL_0025:  constrained. "$CLS0"
    IL_002b:  callvirt   "Function System.Exception.get_Message() As String"
    IL_0030:  ldstr      "xxx"
    IL_0035:  ldc.i4.0
    IL_0036:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareString(String, String, Boolean) As Integer"
    IL_003b:  ldc.i4.0
    IL_003c:  ceq
    IL_003e:  ldc.i4.0
    IL_003f:  cgt.un
    IL_0041:  endfilter
  }  // end filter
  {  // handler
    IL_0043:  pop
    IL_0044:  ldstr      "pass"
    IL_0049:  call       "Sub System.Console.Write(String)"
    IL_004e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0053:  leave.s    IL_0055
  }
  IL_0055:  ret
}
]]>)
        End Sub

        <WorkItem(542070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542070")>
        <Fact>
        Public Sub DeeplyNestedLambda()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Test
    Sub Main
        Dim x As New c1(Of Long, Long)
        x.Test()
    End Sub
End Module

Public Class c1(Of T, U)
    Private Sub bar(Of TT, UU, VV)()
        Dim ttt As TT = Nothing, uuu As UU = Nothing, vvv As VV = Nothing
        Dim t As T = Nothing, u As U = Nothing, ltt As List(Of TT) = New List(Of TT)()

        ' 5 Levels Deep Nested Lambda, Closures
        Dim func As Func(Of TT, UU, Func(Of UU, VV, Func(Of VV, TT, Func(Of T, U, Func(Of U, T))))) =
            Function(a, b)
                Console.WriteLine("Level1")
                Dim v1 As Boolean = ttt.Equals(a)
                Return Function(aa, bb)
                           Console.WriteLine("Level2")
                           Dim v2 As Boolean = v1
                           If ltt.Count >= 0 Then
                               Dim dtu As Dictionary(Of T, List(Of U)) = New Dictionary(Of T, List(Of U))()
                               v2 = aa.Equals(b) : aa.Equals(uuu)
                               Return Function(aaa, bbb)
                                          Console.WriteLine("Level3")
                                          Dim v3 As Boolean = v1
                                          If dtu.Count = 0 Then
                                              v3 = v2
                                              Dim duuvv As Dictionary(Of List(Of UU), List(Of VV)) = New Dictionary(Of List(Of UU), List(Of VV))()
                                              If ltt.Count >= 0 Then
                                                  v3 = aaa.Equals(bb)
                                                  v2 = aa.Equals(b)
                                                  aaa.Equals(vvv)
                                                  Return Function(aaaa, bbbb)
                                                             Console.WriteLine("Level4")
                                                             Dim lu As List(Of U) = New List(Of U)()
                                                             Dim v4 As Boolean = v3 : v4 = v2 : v4 = v1
                                                             If duuvv.Count > 0 Then
                                                                 Console.WriteLine("Error - Should not have reached here")
                                                                 Return Nothing
                                                             Else
                                                                 v4 = aaaa.Equals(t)
                                                                 v3 = aaa.Equals(bb)
                                                                 v2 = aa.Equals(b)
                                                                 Return Function(aaaaa)
                                                                            Console.WriteLine("Level5")
                                                                            If lu.Count &lt; 0 Then
                                                                                Console.WriteLine("Error - Should not have reached here")
                                                                                Return t
                                                                            Else
                                                                                v2 = v1 : v3 = v2 : v4 = v3
                                                                                u.Equals(bbbb)
                                                                                aa.Equals(b)
                                                                                aaa.Equals(bb)
                                                                                aaaa.Equals(t)
                                                                                Return aaaa
                                                                            End If
                                                                        End Function
                                                             End If
                                                         End Function
                                              Else
                                                  Console.WriteLine("Error - Should not have reached here")
                                                  Return Nothing
                                              End If
                                          Else
                                              Console.WriteLine("Error - Should not have reached here")
                                              Return Nothing
                                          End If
                                      End Function
                           Else
                               Console.WriteLine("Error - Should not have reached here")
                               Return Nothing
                           End If
                       End Function
            End Function
        func(ttt, uuu)(uuu, vvv)(vvv, ttt)(t, u)(u)
    End Sub

    Public Sub Test()
        bar(Of Integer, Long, Double)()
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=
"Level1" & Environment.NewLine &
"Level2" & Environment.NewLine &
"Level3" & Environment.NewLine &
"Level4" & Environment.NewLine &
"Level5" & Environment.NewLine)
        End Sub

        <WorkItem(542121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542121")>
        <Fact>
        Public Sub TestLambdaNoClosureClass()

            Dim source = <compilation>
                             <file name="a.vb">
Imports System

Delegate Function D() As Integer

Class Test

    Shared field As D = Function() field2
    Shared field2 As Short = -1

    Public Shared Sub Main()
        Dim myd As D = Function()
                           Return 1
                       End Function
        Console.WriteLine("({0},{1})", myd(), field())
    End Sub
End Class
    </file>
                         </compilation>

            CompileAndVerify(source).VerifyIL("Test..cctor",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     "Test._Closure$__.$I As Test._Closure$__"
  IL_0005:  ldftn      "Function Test._Closure$__._Lambda$__0-0() As Integer"
  IL_000b:  newobj     "Sub D..ctor(Object, System.IntPtr)"
  IL_0010:  stsfld     "Test.field As D"
  IL_0015:  ldc.i4.m1
  IL_0016:  stsfld     "Test.field2 As Short"
  IL_001b:  ret
}
]]>)
        End Sub

        <WorkItem(545390, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545390")>
        <Fact>
        Public Sub Regress13769()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System  
Imports System.Linq     

Module Generics
    Sub Main()
        System.Console.WriteLine("======== Generic-26 ===========")
        Test26_Helper(Of Exception)()
    End Sub

    Sub Test26_Helper(Of T)()
        Dim col1 = {"hi", "bye"}
        Dim x As New CTest26(Of T)
        Dim q = From i In col1 Select a = x.Value Is Nothing
        Dim q1 = col1.Select(Function(s) x.Value Is Nothing)

        Dim val = x.Value Is Nothing
    End Sub

    Class CTest26(Of T)
        Public Value As T
    End Class
End Module

    </file>
</compilation>, expectedOutput:="======== Generic-26 ===========")
        End Sub

        <WorkItem(545391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545391")>
        <Fact>
        Public Sub Regress13770()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
        
Imports System  
Imports System.Linq

Module Generics
    Sub Main()
        System.Console.WriteLine("======== Generic-12 ===========")

        Test12_Helper("goo")
    End Sub

    Sub Test12_Helper(Of T)(ByVal value As T)
        Dim c As New ParentGeneric(Of T).ChildGeneric(value)

        Dim aa = {1, 2, 3, 4, 5, 6}
        'Dim q As QueryableCollection(Of String)
        Dim q = From a In aa
                Select a.ToString() &amp; c.Value.ToString()
    End Sub

    Class ParentGeneric(Of T)
        Public Class ChildGeneric
            Public Value As T

            Sub New(ByVal v As T)
                Value = v
            End Sub
        End Class

        Public Class OtherChildGeneric(Of V)
            Public FirstValue As T
            Public SecondValue As V

            Sub New(ByVal first As T, ByVal second As V)
                FirstValue = first
                SecondValue = second
            End Sub

        End Class
    End Class
End Module


    </file>
</compilation>, expectedOutput:="======== Generic-12 ===========")
        End Sub

        <WorkItem(545392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545392")>
        <Fact>
        Public Sub Regress13771()
            CompileAndVerify(
<compilation>
    <file name="a.vb">

Imports System  
Imports System.Linq

Module Generics
    Sub Main()
        System.Console.WriteLine("======== Generic-14 ===========")

        Test14_Helper(Of String)("goo")
    End Sub


    Sub Test14_Helper(Of T)(ByVal arg As T)
        Dim c As New ParentGeneric(Of T).OtherChildGeneric(Of Integer)(arg, 4)

        Dim aa = {1, 2, 3, 4, 5, 6}

        Dim q = From a In aa
                Select a.ToString() &amp; c.FirstValue.ToString() &amp; c.SecondValue.ToString()
    End Sub

    Class ParentGeneric(Of T)
        Public Class ChildGeneric
            Public Value As T

            Sub New(ByVal v As T)
                Value = v
            End Sub
        End Class

        Public Class OtherChildGeneric(Of V)
            Public FirstValue As T
            Public SecondValue As V

            Sub New(ByVal first As T, ByVal second As V)
                FirstValue = first
                SecondValue = second
            End Sub

        End Class
    End Class
End Module

    </file>
</compilation>, expectedOutput:="======== Generic-14 ===========")
        End Sub

        <WorkItem(545393, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545393")>
        <Fact>
        Public Sub Regress13772()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System  
Imports System.Linq

Module Generics
    Sub Main()
        System.Console.WriteLine("======== Generic-4 ===========")

        Test4_Helper(Of Integer, Integer)(42, 52)
    End Sub

    Sub Test4_Helper(Of K, V)(ByVal key As K, ByVal value As V)
        Dim p As New GenPair(Of K, V)(key, value)
        Dim aa = {1, 2, 3, 4, 5, 6}

        Dim q = From a In aa
                Select a.ToString() &amp; p.Key.ToString()

    End Sub

    Class GenPair(Of K, V)
        Public Key As K
        Public Value As V

        Sub New()

        End Sub

        Sub New(ByVal kArg As K, ByVal vArg As V)
            Key = kArg
            Value = vArg
        End Sub

        Public Function GetKey() As K
            Return Key
        End Function

        Public Function GetValue() As V
            Return Value
        End Function
    End Class
End Module

    </file>
</compilation>, expectedOutput:="======== Generic-4 ===========")
        End Sub

        <WorkItem(545394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545394")>
        <Fact>
        Public Sub Regress13773()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System  
Imports System.Linq

Module Generics
    Sub Main()
        System.Console.WriteLine("======== Generic-5 ===========")

        Test5_Helper(Of Integer, Integer)(42, 52)
    End Sub

    Sub Test5_Helper(Of K, V)(ByVal key As K, ByVal value As V)
        Dim p As New GenPair(Of K, V)(key, value)
        Dim aa = {1, 2, 3, 4, 5, 6}

        Dim q = From a In aa
                Select a.ToString() &amp; p.GetKey().ToString()

    End Sub
    Sub Test4_Helper(Of K, V)(ByVal key As K, ByVal value As V)
        Dim p As New GenPair(Of K, V)(key, value)
        Dim aa = {1, 2, 3, 4, 5, 6}

        Dim q = From a In aa
                Select a.ToString() &amp; p.Key.ToString()

    End Sub

    Class GenPair(Of K, V)
        Public Key As K
        Public Value As V

        Sub New()

        End Sub

        Sub New(ByVal kArg As K, ByVal vArg As V)
            Key = kArg
            Value = vArg
        End Sub

        Public Function GetKey() As K
            Return Key
        End Function

        Public Function GetValue() As V
            Return Value
        End Function
    End Class
End Module


    </file>
</compilation>, expectedOutput:="======== Generic-5 ===========")
        End Sub

        <WorkItem(545395, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545395")>
        <Fact>
        Public Sub Regress13774()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System  
Imports System.Linq

Module Generics
    Sub Main()
        System.Console.WriteLine("======== Generic-5 ===========")

        Test9_Helper(Of Integer, Integer)(42, 52)
    End Sub

    Sub Test9_Helper(Of K, V)(ByVal key As K, ByVal value As V)
        Dim p As GenPair(Of GenPair(Of K, V), V)
        Dim inner As New GenPair(Of K, V)(key, value)
        p = New GenPair(Of GenPair(Of K, V), V)(inner, value)

        Dim aa = {1, 2, 3, 4, 5, 6}

        Dim q = From a In aa
                Select a.ToString() &amp; p.Key.Key.ToString()
    End Sub

    Class GenPair(Of K, V)
        Public Key As K
        Public Value As V

        Sub New()

        End Sub

        Sub New(ByVal kArg As K, ByVal vArg As V)
            Key = kArg
            Value = vArg
        End Sub

        Public Function GetKey() As K
            Return Key
        End Function

        Public Function GetValue() As V
            Return Value
        End Function
    End Class
End Module


    </file>
</compilation>, expectedOutput:="======== Generic-5 ===========")
        End Sub

        <WorkItem(545389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545389")>
        <Fact>
        Public Sub Regress13768()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System  
Imports System.Linq

Imports System.Collections
Imports System.Collections.Generic

Module Simple

    ' Use a lifted variable in a Return conditional
    Sub main()
        Dim aa = {1, 2, 3, 4, 5, 6}
        Dim max As Integer = 4
        Dim q = From a In aa
                Where a &lt; max
                Select a

        Select Case (max)
                    Case 4
                        Console.WriteLine("correct")
                    Case Else
                        Console.WriteLine("incorrect")
                End Select
        End Sub
End Module


    </file>
</compilation>, expectedOutput:="correct")
        End Sub

        <WorkItem(531533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531533")>
        <Fact>
        Public Sub Regress531533()

            ' IMPORTANT: we should not be initializing anything that looks like $VB$NonLocal_$VB$Closure_XXX
            ' This code is not supposed to require parent frame pointers in any of its closures.

            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Explicit Off
Friend Module SLamContext01mod
    Delegate Sub MyDelSub(Of T)(ByVal x As T)
    Delegate Function MyDelFun(Of T)(ByVal x As T) As T
    Sub Main()
        Dim x32 As MyDelFun(Of Integer) = AddressOf Function(a As Short)
                                                        Dim x33 As MyDelSub(Of Long) = AddressOf (Sub() implicit = Sub()
                                                                                                                   End Sub).Invoke
                                                    End Function.Invoke
        System.Console.WriteLine("success")
    End Sub
End Module


    </file>
</compilation>, expectedOutput:="success").
    VerifyIL("SLamContext01mod.Main",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  4
  .locals init (SLamContext01mod._Closure$__2-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     "Sub SLamContext01mod._Closure$__2-0..ctor()"
  IL_0005:  stloc.0
  IL_0006:  newobj     "Sub SLamContext01mod._Closure$__R2-1..ctor()"
  IL_000b:  dup
  IL_000c:  ldloc.0
  IL_000d:  ldftn      "Function SLamContext01mod._Closure$__2-0._Lambda$__0(Short) As Object"
  IL_0013:  newobj     "Sub VB$AnonymousDelegate_0(Of Short, Object)..ctor(Object, System.IntPtr)"
  IL_0018:  stfld      "SLamContext01mod._Closure$__R2-1.$VB$NonLocal_3 As <generated method>"
  IL_001d:  pop
  IL_001e:  ldstr      "success"
  IL_0023:  call       "Sub System.Console.WriteLine(String)"
  IL_0028:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub LiftMeByProxy()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        Dim c As New cls1
        System.Console.WriteLine(c.bar)
    End Sub

    Class cls1
        Public goo As Integer = 42

        Public Function bar()
            If T
                Dim a As Func(Of Integer, Boolean) = Function(s)
                                                         Return s = goo andalso (Function() s = goo).Invoke
                                                     End Function

                Return a.Invoke(42)
            end if

            Dim aaa As Integer = 42

            if T
                Dim a As Func(Of Integer, Boolean) = Function(s)
                                                         Return aaa = goo 
                                                     End Function

                Return a.Invoke(42)
            end if

            Return nothing
        End Function

        Private Function T as boolean
            return true
        end function
    End Class
End Module



    </file>
</compilation>, expectedOutput:="True")
        End Sub

        <WorkItem(836488, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/836488")>
        <Fact>
        Public Sub RelaxedInitializer()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Function f() as integer
        Console.Writeline("hello")
        return 1
    End Function

    Public y As Action = AddressOf f
End Class

Module Module1
    Sub Main
        dim v as new C1
        v.y()
    end sub
end Module
    </file>
</compilation>, expectedOutput:="hello")
        End Sub

        <Fact>
        Public Sub GenericStaticFrames()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Public Class C
	Shared Sub F(Of TF)()
	    Dim f = New Func(Of TF)(Function() Nothing)
	End Sub
	
	Shared Sub G(Of TG)()
		Dim f = New Func(Of TG)(Function() Nothing)
	End Sub
	
	Shared Sub F(Of TF1, TF2)()
		Dim f = New Func(Of TF1, TF2)(Function(a) Nothing)
	End Sub
	
	Shared Sub G(Of TG1, TG2)()
		Dim f = New Func(Of TG1, TG2)(Function(a) Nothing)
	End Sub
End Class
    </file>
</compilation>
            CompileAndVerify(source, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator:=
            Sub(m)
                Dim c = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                AssertEx.Equal({
                    "C._Closure$__1(Of $CLS0)",
                    "C._Closure$__2(Of $CLS0)",
                    "C._Closure$__3(Of $CLS0, $CLS1)",
                    "C._Closure$__4(Of $CLS0, $CLS1)"
                }, c.GetMembers().Where(Function(member) member.Kind = SymbolKind.NamedType).Select(Function(member) member.ToString()))

                Dim c0 = c.GetMember(Of NamedTypeSymbol)("_Closure$__1")
                AssertEx.SetEqual({
                    "Public Shared ReadOnly $I As C._Closure$__1(Of $CLS0)",
                    "Public Shared $I1-0 As System.Func(Of $CLS0)",
                    "Public Sub New()",
                    "Private Shared Sub New()",
                    "Friend Function _Lambda$__1-0() As $CLS0"
                }, c0.GetMembers().Select(Function(member) member.ToString()))

                Dim c1 = c.GetMember(Of NamedTypeSymbol)("_Closure$__2")
                AssertEx.SetEqual({
                    "Public Shared ReadOnly $I As C._Closure$__2(Of $CLS0)",
                    "Public Shared $I2-0 As System.Func(Of $CLS0)",
                    "Public Sub New()",
                    "Private Shared Sub New()",
                    "Friend Function _Lambda$__2-0() As $CLS0"
                }, c1.GetMembers().Select(Function(member) member.ToString()))

                Dim c2 = c.GetMember(Of NamedTypeSymbol)("_Closure$__3")
                AssertEx.SetEqual({
                    "Public Shared ReadOnly $I As C._Closure$__3(Of $CLS0, $CLS1)",
                    "Public Shared $I3-0 As System.Func(Of $CLS0, $CLS1)",
                    "Public Sub New()",
                    "Private Shared Sub New()",
                    "Friend Function _Lambda$__3-0(a As $CLS0) As $CLS1"
                }, c2.GetMembers().Select(Function(member) member.ToString()))

                Dim c3 = c.GetMember(Of NamedTypeSymbol)("_Closure$__4")
                AssertEx.SetEqual({
                    "Public Shared ReadOnly $I As C._Closure$__4(Of $CLS0, $CLS1)",
                    "Public Shared $I4-0 As System.Func(Of $CLS0, $CLS1)",
                    "Public Sub New()",
                    "Private Shared Sub New()",
                    "Friend Function _Lambda$__4-0(a As $CLS0) As $CLS1"
                }, c3.GetMembers().Select(Function(member) member.ToString()))
            End Sub)
        End Sub

        <Fact>
        Public Sub GenericInstance()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Public Class C
	Sub F(Of TF)()
	    Dim f = New Func(Of TF)(Function() 
                                    Me.F()
                                    Return Nothing
                                End Function)
	End Sub
	
	Sub G(Of TG)()
		Dim f = New Func(Of TG)(Function() 
                                    Me.F()
                                    Return Nothing
                                End Function)
	End Sub
	
	Sub F(Of TF1, TF2)()
		Dim f = New Func(Of TF1, TF2)(Function(a) 
                                          Me.F()
                                          Return Nothing
                                      End Function)
	End Sub
	
	Sub G(Of TG1, TG2)()
		Dim f = New Func(Of TG1, TG2)(Function(a) 
                                          Me.F()
                                          Return Nothing
                                      End Function)
	End Sub

    Sub F()
    End Sub
End Class
    </file>
</compilation>
            CompileAndVerify(source, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator:=
            Sub(m)
                Dim c = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                AssertEx.SetEqual({
                    "Public Sub New()",
                    "Public Sub F(Of TF)()",
                    "Public Sub G(Of TG)()",
                    "Public Sub F(Of TF1, TF2)()",
                    "Public Sub G(Of TG1, TG2)()",
                    "Public Sub F()",
                    "Private Function _Lambda$__1-0(Of $CLS0)() As $CLS0",
                    "Private Function _Lambda$__2-0(Of $CLS0)() As $CLS0",
                    "Private Function _Lambda$__3-0(Of $CLS0, $CLS1)(a As $CLS0) As $CLS1",
                    "Private Function _Lambda$__4-0(Of $CLS0, $CLS1)(a As $CLS0) As $CLS1"
                }, c.GetMembers().Select(Function(member) member.ToString()))
            End Sub)
        End Sub

        <Fact>
        Public Sub DeclarationBlockClosures()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C
    Sub New(a As Integer)
        Dim f = Function() a
    End Sub

    Sub F1(a As Integer)
        Dim f = Function() a
    End Sub

    Function F2(a As Integer) As Integer
        Dim f = Function() a
        Return 1
    End Function

    Property F3(a As Integer) As Integer
        Get
            Dim f = Function() a
            Return 1
        End Get

        Set(value As Integer)
            Dim f1 = Function() a
            Dim f2 = Function() value
        End Set
    End Property

    Custom Event F4 As Action
        AddHandler(value As Action)
            Dim f1 = Function() value
        End AddHandler

        RemoveHandler(value As Action)
            Dim f1 = Function() value
        End RemoveHandler

        RaiseEvent()
            Dim x = 1
            Dim f1 = Function() x
        End RaiseEvent
    End Event

    Shared Operator *(a As C, b As C) As C
        Dim f1 = Function() a
        Return a
    End Operator

    Shared Widening Operator CType(a As C) As Integer
        Dim f1 = Function() a
        Return 1
    End Operator
End Class    </file>
</compilation>

            CompileAndVerify(source)
        End Sub

        <Fact>
        Public Sub StatementBlockClosures()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class D
    Public Q As Integer

    Shared Function Z(Of T)(f As Func(Of T)) As T
        Return f()
    End Function

    Sub F()
        While True
            Dim a = 0
            Dim f1 = Function() a
        End While

        For x As Integer = 0 To 1
            Dim a = 0
            Dim f1 = Function() a
        Next

        For Each x In {1}
            Dim a = 0
            Dim f1 = Function() a
        Next

        Do
            Dim a = 0
            Dim f1 = Function() a
        Loop

        Do
            Dim a = 0
            Dim f1 = Function() a
        Loop While True

        Do
            Dim a = 0
            Dim f1 = Function() a
        Loop Until True

        Do While True
            Dim a = 0
            Dim f1 = Function() a
        Loop

        Do Until True
            Dim a = 0
            Dim f1 = Function() a
        Loop

        Dim u As IDisposable = Nothing
        Using u
            Dim a = 0
            Dim f1 = Function() a
        End Using

        SyncLock u
            Dim a = 0
            Dim f1 = Function() a
        End SyncLock

        With u
            Dim a = 0
            Dim f1 = Function() a
        End With

        Select Case Q
            Case 1
                Dim a = 0
                Dim f1 = Function() a

            Case 2
                Dim a = 0
                Dim f1 = Function() a

            Case Else
                Dim a = 0
                Dim f1 = Function() a
        End Select

        If True Then _
            Dim a As Integer = Z(Function() a) _
            Else Dim a As Integer = Z(Function() a)

        If True Then
            Dim a = 0
            Dim f1 = Function() a
        ElseIf False
            Dim a = 0
            Dim f1 = Function() a
        Else
            Dim a = 0
            Dim f1 = Function() a
        End If

        Try
            Dim a = 0
            Dim f1 = Function() a
        Catch ex As InvalidOperationException When Z(Function() ex) IsNot Nothing
            Dim a = 0
            Dim f1 = Function() a
        Catch
            Dim a = 0
            Dim f1 = Function() a
        Finally
            Dim a = 0
            Dim f1 = Function() a
        End Try
    End Sub
End Class   
</file>
</compilation>

            CompileAndVerify(source)
        End Sub

        <Fact>
        Public Sub ObjectMemberInitializerClosure()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C
    Public Q As Integer

    Shared Function Z(Of T)(f As Func(Of T)) As T
        Return f()
    End Function

    Sub F()
        Dim obj = New C With {.Q = Z(Function() .Q)}
    End Sub
End Class   
</file>
</compilation>

            CompileAndVerify(source)
        End Sub

        <Fact>
        Public Sub QueryRangeVariableClosures()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq

Class C
    Function G(Of T)(f As Func(Of T)) As T
        Return f()
    End Function

    Sub F()
        Dim result = From c1 In {1}, c2 In {2}
                     Join c3 In {3} On G(Function() c3) Equals G(Function() c1) And G(Function() c3) Equals G(Function() c2)
                     Join c4 In {4} On G(Function() c4) Equals G(Function() c1) And G(Function() c4) Equals G(Function() c2)
                     Group Join c5 In {5} On G(Function() c5) Equals G(Function() c4) Into a1 = Count(G(Function() c1)), Group
                     Let e3 = G(Function() a1), e4 = G(Function() Group.First())
                     Group e4 = G(Function() e3), e5 = G(Function() e4 + 1) By e6 = G(Function() e3), e7 = G(Function() e4 + 2) Into a2 = Count(G(Function() e4 + 3)), a3 = LongCount(G(Function() e4 + 4)), Group
                     Aggregate c6 In {6}, c7 In {7} From c8 In {8} Select G(Function() c6 + c7 + c8) Into a4 = Sum(G(Function() e6 + 9))
                     Where G(Function() e6) > 0
                     Take While G(Function() e6) > 0
                     Skip While G(Function() e6) > 0
                     Order By G(Function() e6), G(Function() e7)
                     Select e8 = G(Function() a2), e9 = G(Function() a3), e10 = G(Function() a4)
    End Sub
End Class   
    </file>
</compilation>

            CompileAndVerify(source)
        End Sub

        <Fact>
        Public Sub QueryRangeVariableClosures_Aggregate()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq

Class C
    Sub F()
        Dim result = From x In {1} Aggregate y In {2} Into Sum(x + y), z2 = Sum(x - y)
    End Sub
End Class   
    </file>
</compilation>

            CompileAndVerify(source)
        End Sub

        <Fact>
        Public Sub QueryRangeVariableClosures_JoinAbsorbedClauses()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq

Class C
    Shared Function G(Of T)(f As Func(Of T)) As T
        Return f()
    End Function

    Sub FSelect()
        Dim result = From x In {1} Join y In {2} On x Equals y Select G(Function() x + y)
    End Sub

    Sub FLet()
        Dim result = From x In {1} Join y In {2} On x Equals y Let c = G(Function() x + y)
    End Sub

    Sub FAggregate()
        Dim result = From x In {1} Join y In {2} On x Equals y Aggregate z In {3} Skip G(Function() x) Into Sum(G(Function() y))
    End Sub
End Class   
    </file>
</compilation>
            CompileAndVerify(source)
        End Sub

        <Fact>
        Public Sub ClosureInSwitchStatementWithNullableExpression()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        Dim i As Integer? = Nothing
        Select Case i
        Case 0
        Case Else
            Dim o As Object = Nothing
            Dim f = Function() o
            Console.Write("{0}", f() Is Nothing)
        End Select
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="True")
            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size      104 (0x68)
  .maxstack  3
  .locals init (Integer? V_0,
                Boolean? V_1,
                VB$AnonymousDelegate_0(Of Object) V_2) //f
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Integer?"
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloca.s   V_1
  IL_0015:  initobj    "Boolean?"
  IL_001b:  ldloc.1
  IL_001c:  br.s       IL_002d
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0025:  ldc.i4.0
  IL_0026:  ceq
  IL_0028:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_002d:  stloc.1
  IL_002e:  ldloca.s   V_1
  IL_0030:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0035:  brtrue.s   IL_0067
  IL_0037:  newobj     "Sub C._Closure$__1-0..ctor()"
  IL_003c:  dup
  IL_003d:  ldnull
  IL_003e:  stfld      "C._Closure$__1-0.$VB$Local_o As Object"
  IL_0043:  ldftn      "Function C._Closure$__1-0._Lambda$__0() As Object"
  IL_0049:  newobj     "Sub VB$AnonymousDelegate_0(Of Object)..ctor(Object, System.IntPtr)"
  IL_004e:  stloc.2
  IL_004f:  ldstr      "{0}"
  IL_0054:  ldloc.2
  IL_0055:  callvirt   "Function VB$AnonymousDelegate_0(Of Object).Invoke() As Object"
  IL_005a:  ldnull
  IL_005b:  ceq
  IL_005d:  box        "Boolean"
  IL_0062:  call       "Sub System.Console.Write(String, Object)"
  IL_0067:  ret
}
]]>)
        End Sub
    End Class
End Namespace

